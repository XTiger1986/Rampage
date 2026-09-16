using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Rampage.Scheduling
{
    /**
// 定时任务
services.AddRampageScheduler();
scheduler.Register("daily-report", "0 3 * * *", () => BuildReport());
     * */

    /// <summary>
    /// 调度组件注册扩展
    /// </summary>
    public static class SchedulingServiceCollectionExtensions
    {
        /// <summary>
        /// 注册内置 Cron 调度器（进程内分钟级；随宿主自动启停）。
        /// 任务通过 IRampageScheduler.Register(name, cron, job) 注册。
        /// 如需接入 Quartz/Hangfire 等，实现 IRampageScheduler 后覆盖注册即可。
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRampageScheduler(this IServiceCollection services, Action<RampageSchedulerOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            RampageSchedulerOptions options = new RampageSchedulerOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            // 默认进程内锁；集群部署请另行注册分布式实现（如 Rampage.RedisLibrary 的 AddRampageRedisJobLock，会替换此默认项）
            services.TryAddSingleton<IJobLockProvider, LocalJobLockProvider>();
            services.AddSingleton<IRampageScheduler>(sp => new DefaultRampageScheduler(
                sp.GetService<IJobLockProvider>(),
                sp.GetService<RampageSchedulerOptions>()));
            services.AddHostedService<RampageSchedulerHostedService>();
            return services;
        }
    }
}
