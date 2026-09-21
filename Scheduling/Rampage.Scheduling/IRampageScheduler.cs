using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Scheduling
{
    /// <summary>
    /// 任务运行信息快照 / Snapshot of a job's runtime information
    /// </summary>
    public sealed class ScheduledJobInfo
    {
        public string Name { get; init; } = string.Empty;
        public string Cron { get; init; } = string.Empty;
        public bool Running { get; init; }
        public DateTime? LastRunTime { get; init; }
        public string? LastError { get; init; }
        /// <summary>按 Cron 计算的下一次触发时间（本地时间，分钟粒度） / Next trigger time computed from the cron (local time, minute granularity)</summary>
        public DateTime? NextRunTime { get; init; }
    }

    /// <summary>
    /// 调度器 Provider 接口：内置实现为进程内分钟级 Cron 调度（DefaultRampageScheduler）。 / Scheduler provider interface: the built-in implementation is the in-process minute-level cron scheduler (DefaultRampageScheduler).
    /// 后续如需接入 Quartz/Hangfire 等，实现本接口替换注册即可，任务注册代码零改动。 / To adopt Quartz/Hangfire or the like later, implement this interface and replace the registration - job registration code needs no changes.
    /// </summary>
    public interface IRampageScheduler
    {
        /// <summary>
        /// 注册 Cron 任务（可重复注册，同名覆盖；建议在 StartAsync 之前注册——启动后注册的任务自下一个分钟周期起生效，且不会触发立即执行） / Registers a cron job (re-registerable, same name overwrites; best registered before StartAsync - jobs added after start take effect from the next minute tick and never fire runImmediately)
        /// </summary>
        /// <param name="name">任务名（唯一） / Job name (unique)</param>
        /// <param name="cron">Cron 表达式（分 时 日 月 周，本地时间） / Cron expression (minute hour day-of-month month weekday, local time)</param>
        /// <param name="job">任务委托；同一任务上一次未跑完时本次触发自动跳过 / The job delegate; a trigger is skipped automatically while the previous run of the same job has not finished</param>
        /// <param name="runImmediately">启动时立即执行一次 / Run once immediately at startup</param>
        void Register(string name, string cron, Func<CancellationToken, Task> job, bool runImmediately = false);

        /// <summary>
        /// 注册同步版 Cron 任务 / Registers a synchronous cron job
        /// </summary>
        void Register(string name, string cron, Action job, bool runImmediately = false);

        /// <summary>
        /// 启动调度（重复调用安全） / Starts scheduling (safe to call repeatedly)
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 停止调度循环；在跑任务仅收到取消信号，不被等待 / Stops the scheduling loop; running jobs only receive the cancellation signal and are not awaited
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 获取全部任务运行信息快照 / Gets runtime-information snapshots of all jobs
        /// </summary>
        List<ScheduledJobInfo> GetJobs();
    }
}
