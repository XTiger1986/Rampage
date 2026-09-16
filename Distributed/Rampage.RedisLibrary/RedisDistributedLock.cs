using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 基于 Redis SET NX + TTL 的分布式锁；释放时 Lua 比对 token。
    /// 默认启用看门狗自动续期（每 expiry/3 续期一次），避免临界区超过 TTL 后锁过期导致双持有；
    /// 续期失败（锁已丢失）时自动停止并置 IsAcquired=false。
    /// 必须使用 using 及时 Dispose，否则续期会让锁一直被持有。
    /// </summary>
    public sealed class RedisDistributedLock : IDistributedLock
    {
        private const int MinRenewableExpiryMs = 3000;

        private readonly RedisHelper _redis;
        private Timer? _renewTimer;
        private int _renewing;
        private bool _disposed;

        private RedisDistributedLock(RedisHelper redis, string key, string token, TimeSpan expiry, bool autoRenew)
        {
            _redis = redis;
            Key = key;
            Token = token;
            Expiry = expiry;
            IsAcquired = true;

            if (autoRenew && expiry.TotalMilliseconds >= MinRenewableExpiryMs)
            {
                TimeSpan period = TimeSpan.FromMilliseconds(Math.Max(expiry.TotalMilliseconds / 3, 500));
                _renewTimer = new Timer(Renew, null, period, period);
            }
        }

        /// <inheritdoc />
        public bool IsAcquired { get; private set; }

        /// <inheritdoc />
        public string Key { get; }

        /// <inheritdoc />
        public string Token { get; }

        /// <summary>获取时设置的过期时间（看门狗续期到的目标 TTL）。</summary>
        public TimeSpan Expiry { get; }

        private void Renew(object? state)
        {
            if (Interlocked.Exchange(ref _renewing, 1) == 1)
            {
                return;
            }
            try
            {
                if (!_redis.LockExtend(Key, Token, Expiry))
                {
                    // 锁已丢失（过期或被他人持有）：停止续期，TTL 兜底
                    StopRenewTimer();
                    IsAcquired = false;
                }
            }
            catch
            {
                // Redis 抖动时下个周期重试；连续失败最终由 TTL 兜底
            }
            finally
            {
                Interlocked.Exchange(ref _renewing, 0);
            }
        }

        private void StopRenewTimer()
        {
            Timer? timer = _renewTimer;
            _renewTimer = null;
            timer?.Dispose();
        }

        /// <summary>
        /// 尝试获取锁；失败返回 null。
        /// </summary>
        /// <param name="redis">Redis 客户端</param>
        /// <param name="key">逻辑键</param>
        /// <param name="expiry">TTL</param>
        /// <param name="retryCount">失败后重试次数（不含首次）</param>
        /// <param name="retryDelay">重试间隔</param>
        /// <param name="autoRenew">是否启用看门狗自动续期（默认开启）</param>
        public static RedisDistributedLock? TryAcquire(
            RedisHelper redis,
            string key,
            TimeSpan expiry,
            int retryCount = 0,
            TimeSpan? retryDelay = null,
            bool autoRenew = true)
        {
            if (redis == null)
            {
                throw new ArgumentNullException(nameof(redis));
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Lock key is required.", nameof(key));
            }

            string token = Guid.NewGuid().ToString("N");
            TimeSpan delay = retryDelay ?? TimeSpan.FromMilliseconds(50);
            int attempts = Math.Max(0, retryCount) + 1;
            for (int i = 0; i < attempts; i++)
            {
                if (redis.LockTake(key, token, expiry))
                {
                    return new RedisDistributedLock(redis, key, token, expiry, autoRenew);
                }
                if (i < attempts - 1 && delay > TimeSpan.Zero)
                {
                    Thread.Sleep(delay);
                }
            }
            return null;
        }

        /// <summary>
        /// 异步尝试获取锁（重试等待用 Task.Delay，不阻塞线程池线程）；失败返回 null。
        /// </summary>
        public static async Task<RedisDistributedLock?> TryAcquireAsync(
            RedisHelper redis,
            string key,
            TimeSpan expiry,
            int retryCount = 0,
            TimeSpan? retryDelay = null,
            bool autoRenew = true)
        {
            if (redis == null)
            {
                throw new ArgumentNullException(nameof(redis));
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Lock key is required.", nameof(key));
            }

            string token = Guid.NewGuid().ToString("N");
            TimeSpan delay = retryDelay ?? TimeSpan.FromMilliseconds(50);
            int attempts = Math.Max(0, retryCount) + 1;
            for (int i = 0; i < attempts; i++)
            {
                if (await redis.LockTakeAsync(key, token, expiry).ConfigureAwait(false))
                {
                    return new RedisDistributedLock(redis, key, token, expiry, autoRenew);
                }
                if (i < attempts - 1 && delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }
            return null;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            StopRenewTimer();
            if (IsAcquired)
            {
                try
                {
                    _redis.LockRelease(Key, Token);
                }
                catch
                {
                    // 释放失败不抛：锁将在 TTL 后自动过期
                }
                IsAcquired = false;
            }
        }
    }
}
