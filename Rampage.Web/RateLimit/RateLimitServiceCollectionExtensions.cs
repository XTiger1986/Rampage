using Microsoft.Extensions.DependencyInjection;
using Rampage.RateLimit;
using Rampage.Web.Mvc;
using System;

namespace Rampage.Web
{
    /// <summary>
    /// 限流组件注册扩展 / Registration extensions for the rate limit components.
    /// </summary>
    public static class RateLimitServiceCollectionExtensions
    {
        /// <summary>
        /// 注册限流过滤器（使用已注册的 IRateLimiter；需同时注册限流器实现：
        /// 单机用 AddRampageMemoryRateLimiter，分布式用 Rampage.RedisLibrary 的 AddRampageRedisRateLimiter）/
        /// Registers the rate limit filter (uses the registered IRateLimiter; also register a limiter implementation:
        /// AddRampageMemoryRateLimiter for a single node, or AddRampageRedisRateLimiter from Rampage.RedisLibrary for distributed deployments).
        /// </summary>
        /// <param name="services">服务集合 / The service collection.</param>
        /// <returns>服务集合 / The service collection.</returns>
        public static IServiceCollection AddRampageRateLimitFilter(this IServiceCollection services)
        {
            return services.AddScoped<RateLimitActionFilter>();
        }

        /// <summary>
        /// 注册进程内滑动窗口限流器（单机部署）/ Registers the in-process sliding-window rate limiter (single-node deployments).
        /// </summary>
        /// <param name="services">服务集合 / The service collection.</param>
        /// <returns>服务集合 / The service collection.</returns>
        /// <remarks>限流计数仅存在于当前进程内存，多实例部署请改用分布式限流器。</remarks>
        public static IServiceCollection AddRampageMemoryRateLimiter(this IServiceCollection services)
        {
            return services.AddSingleton<IRateLimiter, MemoryRateLimiter>();
        }
    }
}
