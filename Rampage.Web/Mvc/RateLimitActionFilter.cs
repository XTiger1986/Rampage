using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rampage.Model;
using Rampage.RateLimit;
using System;
using System.Reflection;

namespace Rampage.Web.Mvc
{
    /// <summary>
    /// 接口限流标记：作用于 Action 或 Controller（Action 优先）。
    /// 需注册 RateLimitActionFilter（services.AddRampageRateLimitFilter() 后 opt.Filters.Add&lt;RateLimitActionFilter&gt;() 或 [ServiceFilter]）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RateLimitAttribute : Attribute
    {
        /// <summary>业务限流键（全局唯一即可，框架会自动追加维度后缀）</summary>
        public string Key { get; }

        /// <summary>窗口内允许的最大请求次数</summary>
        public int Limit { get; }

        /// <summary>窗口时长（秒）</summary>
        public int WindowSeconds { get; }

        /// <summary>是否按客户端 IP 分桶（默认 true；false 时全部调用方共享同一额度）</summary>
        public bool PerIp { get; set; } = true;

        public RateLimitAttribute(string key, int limit, int windowSeconds)
        {
            Key = key;
            Limit = limit;
            WindowSeconds = windowSeconds;
        }
    }

    /// <summary>
    /// 限流过滤器：命中 <see cref="RateLimitAttribute"/> 时按配置扣减额度，超限返回 OperationTooFrequent(5002)。
    /// </summary>
    public class RateLimitActionFilter : IAsyncActionFilter
    {
        private readonly IRateLimiter _rateLimiter;

        public RateLimitActionFilter(IRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        }

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
