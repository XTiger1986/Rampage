using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Rampage.Idempotency;
using Rampage.Model;
using System;

namespace Rampage.Web.Mvc
{
    /// <summary>
    /// 写接口可选幂等：请求头 Idempotency-Key。
    /// 首次成功：缓存完整 R 的 JSON；重复：原样回放（兼容旧缓存标记 "1"）。
    /// 类职责：框架级 HTTP 幂等拦截，存储与键规则由 DI 注入。
    /// </summary>
    public class IdempotencyActionFilter : IAsyncActionFilter
    {
        public const string DefaultHeaderName = "Idempotency-Key";

        private readonly IIdempotencyStore _store;
        private readonly IIdempotencyKeyBuilder _keyBuilder;
        private readonly IdempotencyOptions _options;

        public IdempotencyActionFilter(
            IIdempotencyStore store,
            IIdempotencyKeyBuilder keyBuilder,
            IOptions<IdempotencyOptions>? options = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _keyBuilder = keyBuilder ?? throw new ArgumentNullException(nameof(keyBuilder));
            _options = options?.Value ?? new IdempotencyOptions();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string headerName = string.IsNullOrWhiteSpace(_options.HeaderName)
                ? DefaultHeaderName
                : _options.HeaderName;

            string? key = context.HttpContext.Request.Headers[headerName].ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                await next();
                return;
            }

            key = key.Trim();
            int maxLen = _options.MaxKeyLength > 0 ? _options.MaxKeyLength : 64;
            if (key.Length > maxLen)
            {
                key = key[..maxLen];
            }

            string path = context.HttpContext.Request.Path.Value ?? "api";

            // 幂等键必须绑定调用方身份：否则用户 B 用相同路径 + 相同 Idempotency-Key 可回放用户 A 的成功响应体
            // Bind the key to the caller identity, otherwise another user can replay someone else's cached response.
            if (_options.BindToIdentity)
            {
                string identity = ResolveIdentity(context.HttpContext);
                key = string.Concat(identity, ":", key);
            }

            string storeKey = _keyBuilder.Build(path, key);

            TimeSpan ttl = _options.Ttl <= TimeSpan.Zero ? TimeSpan.FromHours(24) : _options.Ttl;

            string? cached = null;
            try
            {
                cached = _store.Get(storeKey);
            }
            catch (Exception ex)
            {
                if (!HandleStoreFailure(context, ex, "Get"))
                {
                    return;
                }
            }

            if (IdempotencyResponseHelper.IsProcessingMarker(cached))
            {
                // 同 Key 请求处理中（前一个进程崩溃遗留的占位也在此列）
                context.Result = CreateConflictResult();
                return;
            }
            if (IdempotencyResponseHelper.IsReplayable(cached))
            {
                context.Result = CreateReplayResult(cached!);
                return;
            }

            // SET NX 原子占位：同 Key 并发请求只有一个能执行业务，其余直接拒绝
            bool occupied;
            try
            {
                occupied = _store.TrySet(storeKey, IdempotencyResponseHelper.ProcessingMarker, ttl);
            }
            catch (Exception ex)
            {
                if (!HandleStoreFailure(context, ex, "TrySet"))
                {
                    return;
                }
                // fail-open：存储不可用时不做占位，直接执行业务，由业务层幂等兜底
                occupied = true;
            }

            if (!occupied)
            {
                context.Result = CreateConflictResult();
                return;
            }

            ActionExecutedContext executed = await next();
            if (executed.Exception != null || executed.Canceled)
            {
                try { _store.Remove(storeKey); } catch { /* ignore */ }
                return;
            }

            R? r = ExtractR(executed.Result);
            string? json = IdempotencyResponseHelper.SerializeSuccess(r);
            if (json == null)
            {
                // 非成功响应：释放占位，允许客户端重试
                try { _store.Remove(storeKey); } catch { /* ignore */ }
                return;
            }

            try { _store.Set(storeKey, json, ttl); }
            catch (Exception ex)
            {
                Log.RampageLogger.Error($"Idempotency store Set failed for '{storeKey}'.", ex);
            }
        }

        /// <summary>
        /// 存储故障处理：返回 true 表示按 fail-open 放行继续执行；返回 false 表示已写入拒绝响应，调用方应直接 return。
        /// </summary>
        private bool HandleStoreFailure(ActionExecutingContext context, Exception ex, string operation)
        {
            Log.RampageLogger.Error($"Idempotency store {operation} failed (failOpen={_options.FailOpenOnStoreError}).", ex);
            if (_options.FailOpenOnStoreError)
            {
                return true;
            }
            context.Result = CreateStoreUnavailableResult();
            return false;
        }

        /// <summary>
        /// 解析调用方身份：优先已认证用户（NameIdentifier / nameid / sub / Identity.Name），未登录回退到客户端 IP。
        /// </summary>
        private static string ResolveIdentity(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            System.Security.Claims.ClaimsPrincipal? user = httpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                string? id = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("nameid")?.Value
                    ?? user.FindFirst("sub")?.Value
                    ?? user.Identity.Name;
                if (!string.IsNullOrEmpty(id))
                {
                    return string.Concat("u:", id);
                }
            }
            return string.Concat("ip:", Utilities.WebTool.GetClientIP(httpContext.Request));
        }

        /// <summary>同 Key 并发/处理中冲突响应。</summary>
        public static IActionResult CreateConflictResult()
        {
            return new JsonResult(R.Create(ResultCode.OperationTooFrequent, "相同幂等键的请求正在处理中，请稍后重试"));
        }

        /// <summary>幂等存储不可用（fail-closed）响应。</summary>
        public static IActionResult CreateStoreUnavailableResult()
        {
            return new JsonResult(R.Create(ResultCode.OperationTooFrequent, "幂等校验服务暂不可用，请稍后重试"))
            {
                StatusCode = 503
            };
        }

        /// <summary>
        /// 回放缓存正文；旧标记 "1" 回退为带 Idempotent 标记的成功响应。
        /// </summary>
        public static IActionResult CreateReplayResult(string cached)
        {
            if (IdempotencyResponseHelper.IsLegacyMarker(cached))
            {
                // 使用 Create 避免依赖本地化文案（单测/无配置环境亦可回放）
                return new JsonResult(R.Create(0, string.Empty, new { Idempotent = true }));
            }

            return new ContentResult
            {
                Content = cached,
                ContentType = "application/json; charset=utf-8",
                StatusCode = 200
            };
        }

        private static R? ExtractR(IActionResult? result)
        {
            if (result is ObjectResult obj && obj.Value is R r1)
            {
                return r1;
            }
            if (result is JsonResult jr && jr.Value is R r2)
            {
                return r2;
            }
            return null;
        }
    }

    /// <summary>HTTP 幂等 Filter 配置。</summary>
    public class IdempotencyOptions
    {
        /// <summary>请求头名称，默认 Idempotency-Key。</summary>
        public string HeaderName { get; set; } = IdempotencyActionFilter.DefaultHeaderName;

        /// <summary>成功响应缓存 TTL，默认 24 小时。</summary>
        public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(24);

        /// <summary>客户端键最大长度（超出截断），默认 64。</summary>
        public int MaxKeyLength { get; set; } = 64;

        /// <summary>
        /// 是否把调用方身份（用户标识，未登录则为客户端 IP）拼入幂等键，默认 true。
        /// 关闭后同一路径下不同用户的相同 Idempotency-Key 会互相回放响应。
        /// </summary>
        public bool BindToIdentity { get; set; } = true;

        /// <summary>
        /// 幂等存储不可用时是否放行（fail-open），默认 false：返回 503 让客户端重试，避免重复执行写操作。
        /// </summary>
        public bool FailOpenOnStoreError { get; set; } = false;
    }
}
