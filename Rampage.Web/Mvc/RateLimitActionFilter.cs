using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rampage.Model;
using Rampage.RateLimit;
using System;
using System.Reflection;

namespace Rampage.Web.Mvc
{
    /// <summary>
    /// 接口限流标记：作用于 Action 或 Controller（Action 优先）。 / Rate limit marker applicable to an action or controller (action takes precedence).
    /// 需注册 RateLimitActionFilter（services.AddRampageRateLimitFilter() 后 opt.Filters.Add&lt;RateLimitActionFilter&gt;() 或 [ServiceFilter]）。
    /// The RateLimitActionFilter must be registered (services.AddRampageRateLimitFilter() then opt.Filters.Add&lt;RateLimitActionFilter&gt;() or [ServiceFilter]).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RateLimitAttribute : Attribute
    {
        /// <summary>业务限流键（全局唯一即可，框架会自动追加维度后缀）/ Business rate limit key (globally unique; the framework appends dimension suffixes automatically)</summary>
        public string Key { get; }

        /// <summary>窗口内允许的最大请求次数 / Maximum requests allowed within the window</summary>
        public int Limit { get; }

        /// <summary>窗口时长（秒）/ Window duration in seconds</summary>
        public int WindowSeconds { get; }

        /// <summary>是否按客户端 IP 分桶（默认 true；false 时全部调用方共享同一额度）/ Whether to bucket per client IP (default true; false shares one quota among all callers)</summary>
        public bool PerIp { get; set; } = true;

        /// <summary>
        /// 构造函数 / Constructor
        /// </summary>
        /// <param name="key">业务限流键 / Business rate limit key</param>
        /// <param name="limit">窗口内允许的最大请求次数 / Maximum requests allowed within the window</param>
        /// <param name="windowSeconds">窗口时长（秒）/ Window duration in seconds</param>
        public RateLimitAttribute(string key, int limit, int windowSeconds)
        {
            Key = key;
            Limit = limit;
            WindowSeconds = windowSeconds;
        }
    }

    /// <summary>
    /// 限流过滤器：命中 <see cref="RateLimitAttribute"/> 时按配置扣减额度，超限返回 OperationTooFrequent(5002) 并携带 Retry-After 响应头。 / Rate limit filter: consumes quota per the <see cref="RateLimitAttribute"/> configuration and returns OperationTooFrequent (5002) with a Retry-After header when exceeded.
    /// </summary>
    public class RateLimitActionFilter : IAsyncActionFilter
    {
        private readonly IRateLimiter _rateLimiter;

        /// <summary>
        /// 构造函数 / Constructor
        /// </summary>
        /// <param name="rateLimiter">限流器实现 / The rate limiter implementation</param>
        public RateLimitActionFilter(IRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        }

        /// <summary>
        /// Action 执行前限流：无标记直接放行；超限返回 OperationTooFrequent JSON 并设置 Retry-After / Rate limits before the action executes: passes through without the marker; returns an OperationTooFrequent JSON and sets Retry-After when the quota is exceeded
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
        /// <param name="next">后续执行委托 / The next execution delegate</param>
        /// <returns>异步任务 / A task representing the operation</returns>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            RateLimitAttribute? attribute = null;
            if (context.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerDescriptor)
            {
                attribute = controllerDescriptor.MethodInfo.GetCustomAttribute<RateLimitAttribute>()
                    ?? controllerDescriptor.ControllerTypeInfo.GetCustomAttribute<RateLimitAttribute>();
            }
            if (attribute == null)
            {
                await next();
                return;
            }

            string key = attribute.Key;
            if (attribute.PerIp)
            {
                string? ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                key = key + "|" + (string.IsNullOrEmpty(ip) ? "unknown" : ip);
            }

            RateLimitResult result = _rateLimiter.Limit(key, attribute.Limit, TimeSpan.FromSeconds(attribute.WindowSeconds));
            if (!result.Allowed)
            {
                long retryAfterSeconds = Math.Max(1, (result.ResetAtUnixMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 999) / 1000);
                context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
                context.Result = new JsonResult(R.Create(ResultCode.OperationTooFrequent, "请求过于频繁，请稍后重试"))
                {
                    StatusCode = 200
                };
                return;
            }

            await next();
        }
    }
}
