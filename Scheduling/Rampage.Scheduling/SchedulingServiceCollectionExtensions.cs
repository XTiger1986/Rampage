using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Rampage.Scheduling
{
    /**
// 定时任务 / Scheduled jobs
services.AddRampageScheduler();
scheduler.Register("daily-report", "0 3 * * *", () => BuildReport());
     * */

    /// <summary>
    /// 调度组件注册扩展 / Registration extensions for the scheduling component
    /// </summary>
    public static class SchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 注册内置 Cron 调度器（进程内分钟级；随宿主自动启停）。 / Registers the built-in cron scheduler (in-process, minute-level; started and stopped automatically with the host).
        /// 任务通过 IRampageScheduler.Register(name, cron, job) 注册。 / Jobs are registered via IRampageScheduler.Register(name, cron, job).
        /// 如需接入 Quartz/Hangfire 等，实现 IRampageScheduler 后覆盖注册即可。 / To adopt Quartz/Hangfire or the like, implement IRampageScheduler and override the registration.
        /// </summary>
        /// <param name="services">服务集合 / The service collection</param>
        /// <returns>服务集合（便于链式调用） / The service collection, for chaining</returns>
        public static IServiceCollection AddRampageScheduler(this IServiceCollection services, Action<RampageSchedulerOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            RampageSchedulerOptions options = new RampageSchedulerOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            // 默认进程内锁；集群部署请另行注册分布式实现（如 Rampage.RedisLibrary 的 AddRampageRedisJobLock，会替换此默认项） / In-process lock by default; for clustered deployments register a distributed implementation separately (e.g. AddRampageRedisJobLock from Rampage.RedisLibrary, which replaces this default)
            services.TryAddSingleton<IJobLockProvider, LocalJobLockProvider>();
            services.AddSingleton<IRampageScheduler>(sp => new DefaultRampageScheduler(
                sp.GetService<IJobLockProvider>(),
                sp.GetService<RampageSchedulerOptions>()));
            services.AddHostedService<RampageSchedulerHostedService>();
            return services;
        }
    }
}
