using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rampage.Scheduling;
using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 调度器分布式锁注册扩展
    /// </summary>
    public static class RedisSchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 使用 Redis 分布式锁作为调度任务锁（替换 AddRampageScheduler 注册的进程内默认实现），
        /// 使多实例部署下同一 Cron 任务只在一个实例执行。
        /// </summary>
        /// <param name="services"></param>
        /// <param name="redisClientName">RedisFactory 客户端名称，null 表示第一个客户端</param>
        /// <param name="keyPrefix">锁键前缀</param>
        public static IServiceCollection AddRampageRedisJobLock(this IServiceCollection services, string? redisClientName = null, string keyPrefix = "scheduler:job:")
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            services.Replace(ServiceDescriptor.Singleton<IJobLockProvider>(_ => new RedisJobLockProvider(redisClientName, keyPrefix)));
            return services;
        }
    }
}
