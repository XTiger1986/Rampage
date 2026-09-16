using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Scheduling
{
    /// <summary>
    /// 任务运行信息快照
    /// </summary>
    public sealed class ScheduledJobInfo
    {
        public string Name { get; init; } = string.Empty;
        public string Cron { get; init; } = string.Empty;
        public bool Running { get; init; }
        public DateTime? LastRunTime { get; init; }
        public string? LastError { get; init; }
        /// <summary>按 Cron 计算的下一次触发时间（本地时间，分钟粒度）</summary>
        public DateTime? NextRunTime { get; init; }
    }

    /// <summary>
    /// 调度器 Provider 接口：内置实现为进程内分钟级 Cron 调度（DefaultRampageScheduler）。
    /// 后续如需接入 Quartz/Hangfire 等，实现本接口替换注册即可，任务注册代码零改动。
    /// </summary>
    public interface IRampageScheduler
    {
        /// <summary>
        /// 注册 Cron 任务（可重复注册，同名覆盖；须在 StartAsync 之前注册）
        /// </summary>
        /// <param name="name">任务名（唯一）</param>
        /// <param name="cron">Cron 表达式（分 时 日 月 周，本地时间）</param>
        /// <param name="job">任务委托；同一任务上一次未跑完时本次触发自动跳过</param>
        /// <param name="runImmediately">启动时立即执行一次</param>
        void Register(string name, string cron, Func<CancellationToken, Task> job, bool runImmediately = false);

        /// <summary>
        /// 注册同步版 Cron 任务
        /// </summary>
        void Register(string name, string cron, Action job, bool runImmediately = false);

        /// <summary>
        /// 启动调度（重复调用安全）
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 停止调度并等待在跑任务结束
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 获取全部任务运行信息快照
        /// </summary>
        List<ScheduledJobInfo> GetJobs();
    }
}
