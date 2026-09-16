using Microsoft.Extensions.DependencyInjection;
using Rampage.RateLimit;
using Rampage.Web.Mvc;
using System;

namespace Rampage.Web
{
    /// <summary>
    /// 限流组件注册扩展
    /// </summary>
    public static class RateLimitServiceCollectionExtensions
    {
        /// <summary>
        /// 注册限流过滤器（使用已注册的 IRateLimiter；需同时注册限流器实现：
        /// 单机用 AddRampageMemoryRateLimiter，分布式用 Rampage.RedisLibrary 的 AddRampageRedisRateLimiter）
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRampageRateLimitFilter(this IServiceCollection services)
        {
            return services.AddScoped<RateLimitActionFilter>();
        }

        /// <summary>
        /// 注册进程内滑动窗口限流器（单机部署）
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRampageMemoryRateLimiter(this IServiceCollection services)
        {
            return services.AddSingleton<IRateLimiter, MemoryRateLimiter>();
        }
    }
}
