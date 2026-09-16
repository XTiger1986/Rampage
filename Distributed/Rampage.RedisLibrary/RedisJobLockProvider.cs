using Rampage.Log;
using Rampage.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 基于 <see cref="RedisDistributedLock"/> 的调度任务锁：多实例部署下同一任务同一时刻只在一个实例执行。
    /// 锁带看门狗续期，长任务不会因 TTL 到期而被其它实例重复触发；实例崩溃后由 TTL 兜底释放。
    /// Redis 不可用时按 fail-closed 处理（本实例跳过本次触发并记录错误），避免所有实例同时“放行”导致重复执行。
    /// 注册方式：services.AddRampageScheduler(); services.AddRampageRedisJobLock();
    /// </summary>
    public sealed class RedisJobLockProvider : IJobLockProvider
    {
        private readonly string? _redisClientName;
        private readonly string _keyPrefix;

        /// <param name="redisClientName">RedisFactory 中的客户端名称，null 表示第一个客户端</param>
        /// <param name="keyPrefix">锁键前缀，默认 "scheduler:job:"</param>
        public RedisJobLockProvider(string? redisClientName = null, string keyPrefix = "scheduler:job:")
        {
            _redisClientName = redisClientName;
            _keyPrefix = string.IsNullOrEmpty(keyPrefix) ? "scheduler:job:" : keyPrefix;
        }

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
