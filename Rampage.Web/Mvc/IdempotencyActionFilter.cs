using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Rampage.Idempotency;
using Rampage.Model;
using System;

namespace Rampage.Web.Mvc
{
    /// <summary>
    /// 写接口可选幂等：请求头 Idempotency-Key。 / Optional idempotency for write endpoints via the Idempotency-Key request header.
    /// 首次成功：缓存完整 R 的 JSON；重复：原样回放（兼容旧缓存标记 "1"）。
    /// First success caches the full R JSON; repeats replay it verbatim (the legacy cache marker "1" is tolerated).
    /// 类职责：框架级 HTTP 幂等拦截，存储与键规则由 DI 注入。
    /// This class is the framework-level HTTP idempotency interceptor; the store and key rules are injected via DI.
    /// </summary>
    /// <remarks>
    /// 存储键 = 请求路径 + 身份维度（"u:用户ID" 或 "ip:客户端IP"，可经 <see cref="IdempotencyOptions.BindToIdentity"/> 关闭）+ 截断到
    /// <see cref="IdempotencyOptions.MaxKeyLength"/> 的请求头值。同 Key 并发请求（ProcessingMarker 占位）返回 OperationTooFrequent；
    /// 存储故障默认 fail-closed 返回 503，可经 <see cref="IdempotencyOptions.FailOpenOnStoreError"/> 改为放行。
    /// The store key = request path + identity dimension ("u:userId" or "ip:clientIP", removable via <see cref="IdempotencyOptions.BindToIdentity"/>)
    /// + the header value truncated to <see cref="IdempotencyOptions.MaxKeyLength"/>. Concurrent requests sharing a key (ProcessingMarker placeholder)
    /// get OperationTooFrequent; store failures fail closed with 503 by default, switchable to fail-open via <see cref="IdempotencyOptions.FailOpenOnStoreError"/>.
    /// </remarks>
    public class IdempotencyActionFilter : IAsyncActionFilter
    {
        /// <summary>默认幂等请求头名称 / The default idempotency header name</summary>
        public const string DefaultHeaderName = "Idempotency-Key";

        private readonly IIdempotencyStore _store;
        private readonly IIdempotencyKeyBuilder _keyBuilder;
        private readonly IdempotencyOptions _options;

        /// <summary>
        /// 构造函数 / Constructor
        /// </summary>
        /// <param name="store">幂等结果存储 / The idempotency result store</param>
        /// <param name="keyBuilder">存储键构造器 / The store key builder</param>
        /// <param name="options">可选配置；null 使用默认值 / Optional options; null uses the defaults</param>
        public IdempotencyActionFilter(
            IIdempotencyStore store,
            IIdempotencyKeyBuilder keyBuilder,
            IOptions<IdempotencyOptions>? options = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _keyBuilder = keyBuilder ?? throw new ArgumentNullException(nameof(keyBuilder));
            _options = options?.Value ?? new IdempotencyOptions();
        }

        /// <summary>
        /// Action 执行前的幂等拦截：读取/截断请求头 → 查缓存（回放或冲突）→ 原子占位 → 执行业务 → 仅成功 R 落缓存 / Idempotency interception before the action: reads/truncates the header → checks the cache (replay or conflict) → atomically claims → runs the action → caches only successful R results
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
        /// <param name="next">后续执行委托 / The next execution delegate</param>
        /// <returns>异步任务 / A task representing the operation</returns>
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
        /// 存储故障处理：返回 true 表示按 fail-open 放行继续执行；返回 false 表示已写入拒绝响应，调用方应直接 return。 / Handles store failures: true means continue (fail-open); false means a rejection response was written and the caller must return.
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
        /// 解析调用方身份：优先已认证用户（NameIdentifier / nameid / sub / Identity.Name），未登录回退到客户端 IP。 / Resolves the caller identity: an authenticated user first (NameIdentifier / nameid / sub / Identity.Name), falling back to the client IP.
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

        /// <summary>同 Key 并发/处理中冲突响应（OperationTooFrequent）。 / Conflict response for concurrent/in-flight requests sharing a key (OperationTooFrequent).</summary>
        public static IActionResult CreateConflictResult()
        {
            return new JsonResult(R.Create(ResultCode.OperationTooFrequent, "相同幂等键的请求正在处理中，请稍后重试"));
        }

        /// <summary>幂等存储不可用（fail-closed）响应，HTTP 503。 / Store-unavailable (fail-closed) response, HTTP 503.</summary>
        public static IActionResult CreateStoreUnavailableResult()
        {
            return new JsonResult(R.Create(ResultCode.OperationTooFrequent, "幂等校验服务暂不可用，请稍后重试"))
            {
                StatusCode = 503
            };
        }

        /// <summary>
        /// 回放缓存正文；旧标记 "1" 回退为带 Idempotent 标记的成功响应（{Idempotent:true}）。 / Replays the cached body; the legacy "1" marker degrades to a success response carrying {Idempotent:true}.
        /// </summary>
        /// <param name="cached">缓存中的响应正文或标记 / The cached response body or marker</param>
        /// <returns>回放响应 / The replay response</returns>
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

    /// <summary>HTTP 幂等 Filter 配置。 / Options for the HTTP idempotency filter.</summary>
    public class IdempotencyOptions
    {
        /// <summary>请求头名称，默认 Idempotency-Key。 / The request header name; Idempotency-Key by default.</summary>
        public string HeaderName { get; set; } = IdempotencyActionFilter.DefaultHeaderName;

        /// <summary>成功响应缓存 TTL，默认 24 小时。 / TTL of cached successful responses; 24 hours by default.</summary>
        public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(24);

        /// <summary>客户端键最大长度（超出截断），默认 64。 / Maximum client key length (truncated beyond); 64 by default.</summary>
        public int MaxKeyLength { get; set; } = 64;

        /// <summary>
        /// 是否把调用方身份（用户标识，未登录则为客户端 IP）拼入幂等键，默认 true。
        /// 关闭后同一路径下不同用户的相同 Idempotency-Key 会互相回放响应。
        /// Whether to bind the caller identity (user id, or client IP when anonymous) into the idempotency key; true by default.
        /// When disabled, the same Idempotency-Key on one path replays responses across different users.
        /// </summary>
        public bool BindToIdentity { get; set; } = true;

        /// <summary>
        /// 幂等存储不可用时是否放行（fail-open），默认 false：返回 503 让客户端重试，避免重复执行写操作。
        /// Whether to fail open when the idempotency store is unavailable; false by default: respond 503 so the client retries instead of executing the write twice.
        /// </summary>
        public bool FailOpenOnStoreError { get; set; } = false;
    }
}
