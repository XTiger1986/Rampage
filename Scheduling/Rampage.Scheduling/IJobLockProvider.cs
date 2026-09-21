using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Scheduling
{
    /// <summary>
    /// 任务互斥锁提供者：调度器在触发任务前先获取该任务的锁，确保多实例（集群）部署时同一任务同一时刻只在一个实例执行。
    /// 核心库内置 <see cref="LocalJobLockProvider"/>（进程内，始终成功）；集群部署请使用扩展库的分布式实现
    /// （如 Rampage.RedisLibrary 的 RedisJobLockProvider，通过 AddRampageRedisJobLock 注册）。
    /// Job mutex provider so that a job runs on only one instance at a time in a clustered deployment.
    /// </summary>
    public interface IJobLockProvider
    {
        /// <summary>
        /// 尝试获取任务锁。成功返回锁句柄（Dispose 释放）；已被其它实例持有返回 null。 / Tries to acquire the job lock. Returns a lock handle on success (released via Dispose); returns null if it is already held by another instance.
        /// </summary>
        /// <param name="jobName">任务名称 / Job name</param>
        /// <param name="expiry">锁的最长持有时间（防止实例崩溃后锁永久残留；分布式实现可自动续期） / Maximum hold time of the lock (prevents it from lingering forever after an instance crash; distributed implementations may auto-renew)</param>
        /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
        Task<IDisposable?> TryAcquireAsync(string jobName, TimeSpan expiry, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 进程内任务锁：不做跨实例互斥（单实例部署使用）。进程内的重入互斥由调度器自身的 Running 标记保证。 / In-process job lock: no cross-instance mutual exclusion (for single-instance deployments); in-process re-entrancy is prevented by the scheduler's own Running flag.
    /// </summary>
    public sealed class LocalJobLockProvider : IJobLockProvider
    {
        private sealed class NoopHandle : IDisposable
        {
            public static readonly NoopHandle Instance = new NoopHandle();
            public void Dispose() { }
        }

        public Task<IDisposable?> TryAcquireAsync(string jobName, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDisposable?>(NoopHandle.Instance);
        }
    }

    /// <summary>
    /// 调度器选项 / Scheduler options
    /// </summary>
    public sealed class RampageSchedulerOptions
    {
        /// <summary>
        /// 任务锁的默认持有时长，默认 10 分钟。分布式实现通常带自动续期，此值主要作为实例崩溃后的兜底过期时间。 / Default hold time of a job lock, 10 minutes by default. Distributed implementations usually auto-renew; this value mainly serves as the fallback expiry after an instance crash.
        /// </summary>
        public TimeSpan JobLockExpiry { get; set; } = TimeSpan.FromMinutes(10);
    }
}
