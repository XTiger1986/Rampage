using Rampage.Log;
using Rampage.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 基于 <see cref="RedisDistributedLock"/> 的调度任务锁：多实例部署下同一任务同一时刻只在一个实例执行。
    /// Scheduler job lock based on <see cref="RedisDistributedLock"/>: with multiple instances deployed, a job runs on exactly one instance at a time.
    /// 锁带看门狗续期，长任务不会因 TTL 到期而被其它实例重复触发；实例崩溃后由 TTL 兜底释放。
    /// The lock carries watchdog renewal, so long jobs are not re-triggered on another instance when the TTL lapses; a crashed instance is released by TTL in the end.
    /// Redis 不可用时按 fail-closed 处理（本实例跳过本次触发并记录错误），避免所有实例同时"放行"导致重复执行。
    /// When Redis is unavailable it fails closed (this instance skips the trigger and logs an error), preventing every instance from running the job at once.
    /// 注册方式：services.AddRampageScheduler(); services.AddRampageRedisJobLock();
    /// Register with: services.AddRampageScheduler(); services.AddRampageRedisJobLock();
    /// </summary>
    public sealed class RedisJobLockProvider : IJobLockProvider
    {
        private readonly string? _redisClientName;
        private readonly string _keyPrefix;

        /// <summary>构造调度任务锁提供者 / Construct the scheduler job lock provider</summary>
        /// <param name="redisClientName">RedisFactory 中的客户端名称，null 表示第一个客户端 / Client name in RedisFactory; null selects the first client</param>
        /// <param name="keyPrefix">锁键前缀，默认 "scheduler:job:"，空串回退默认值 / Lock key prefix, "scheduler:job:" by default; an empty string falls back to the default</param>
        public RedisJobLockProvider(string? redisClientName = null, string keyPrefix = "scheduler:job:")
        {
            _redisClientName = redisClientName;
            _keyPrefix = string.IsNullOrEmpty(keyPrefix) ? "scheduler:job:" : keyPrefix;
        }

        /// <summary>
        /// 尝试获取任务锁（不重试，看门狗自动续期）。
        /// Try to acquire the job lock (no retries; watchdog auto-renewal enabled).
        /// </summary>
        /// <param name="jobName">任务名（与 keyPrefix 拼成锁键）/ Job name (combined with keyPrefix into the lock key)</param>
        /// <param name="expiry">锁 TTL，即实例崩溃后任务被锁住的最长时间 / Lock TTL; the longest a crashed instance holds the lock</param>
        /// <param name="cancellationToken">当前未使用，仅为接口兼容保留 / Currently unused; kept for interface compatibility</param>
        /// <returns>获取成功返回锁句柄（Dispose 即释放）；Redis 不可用、获取失败或异常时返回 null（本实例跳过本次触发，fail-closed）/ The lock handle on success (Dispose releases it); null on unavailable Redis, failed acquisition or error (this instance skips the trigger — fail-closed)</returns>
        public async Task<IDisposable?> TryAcquireAsync(string jobName, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                throw new ArgumentException("Job name is required.", nameof(jobName));
            }

            RedisHelper? redis = RedisFactory.GetClient(_redisClientName);
            if (redis == null)
            {
                RampageLogger.Error($"RedisJobLockProvider: redis client '{_redisClientName ?? "(default)"}' is not available; job '{jobName}' skipped on this instance.",
                    new InvalidOperationException("Redis client not available"));
                return null;
            }

            try
            {
                return await RedisDistributedLock.TryAcquireAsync(redis, string.Concat(_keyPrefix, jobName), expiry, retryCount: 0, autoRenew: true)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RampageLogger.Error($"RedisJobLockProvider: failed to acquire lock for job '{jobName}'; skipped on this instance.", ex);
                return null;
            }
        }
    }
}
