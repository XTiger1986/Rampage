using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 基于 Redis SET NX + TTL 的分布式锁；释放时 Lua 比对 token 防止误删他人锁。
    /// Redis distributed lock based on SET NX + TTL; released via Lua token comparison to avoid deleting someone else's lock.
    /// 默认启用看门狗自动续期（每 max(expiry/3, 500ms) 一次，仅 expiry≥3s 时启用），
    /// 避免临界区耗时超过 TTL 后锁过期导致双持有；
    /// By default a watchdog auto-renews the lock every max(expiry/3, 500ms) (enabled only when expiry >= 3s),
    /// preventing a second holder after the critical section outlives the TTL.
    /// 续期失败（锁已丢失）时自动停止续期并置 IsAcquired=false，由剩余 TTL 兜底释放。
    /// When renewal fails (lock lost) renewal stops and IsAcquired becomes false, with the remaining TTL as the final fallback.
    /// 必须使用 using 及时 Dispose，否则续期会让锁一直被持有。
    /// Dispose promptly with using, otherwise renewal keeps the lock held indefinitely.
    /// </summary>
    public sealed class RedisDistributedLock : IDistributedLock
    {
        // 看门狗启用的最小 TTL（3s）：更短的锁自动续期开销大于收益 / Minimum TTL (3s) for enabling the watchdog; for shorter locks renewal costs more than it saves
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
                // 续期周期取 expiry/3 且不小于 500ms，避免短 TTL 高频打 Redis / Renew every expiry/3 but at least 500ms apart, to avoid hammering Redis on short TTLs
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

        /// <summary>获取时设置的过期时间（看门狗续期到的目标 TTL）/ Expiry configured at acquisition (the TTL the watchdog renews to)</summary>
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
                    // 锁已丢失（过期或被他人持有）：停止续期，TTL 兜底 / Lock lost (expired or taken over): stop renewing, TTL is the fallback
                    StopRenewTimer();
                    IsAcquired = false;
                }
            }
            catch
            {
                // Redis 抖动时下个周期重试；连续失败最终由 TTL 兜底 / On Redis hiccups retry next period; repeated failures eventually fall back to TTL
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
        /// 尝试获取锁，失败返回 null（重试等待用 Thread.Sleep 阻塞）。
        /// Try to acquire the lock; returns null on failure (retry waits block via Thread.Sleep).
        /// </summary>
        /// <param name="redis">Redis 客户端 / Redis client</param>
        /// <param name="key">逻辑键（自动加 Prefix）/ Logical key (prefix applied automatically)</param>
        /// <param name="expiry">TTL，需大于业务临界区耗时 / TTL; must exceed the critical section duration</param>
        /// <param name="retryCount">失败后重试次数（不含首次）/ Retry count after the first attempt (excludes it)</param>
        /// <param name="retryDelay">重试间隔，默认 50ms / Retry delay, 50ms by default</param>
        /// <param name="autoRenew">是否启用看门狗自动续期（默认开启，仅 expiry≥3s 生效）/ Enable watchdog auto-renewal (default on; effective only when expiry >= 3s)</param>
        /// <returns>获取成功返回锁句柄；失败返回 null / The lock handle on success; null on failure</returns>
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
        /// Asynchronously try to acquire the lock (retry waits use Task.Delay, no thread blocked); returns null on failure.
        /// </summary>
        /// <param name="redis">Redis 客户端 / Redis client</param>
        /// <param name="key">逻辑键（自动加 Prefix）/ Logical key (prefix applied automatically)</param>
        /// <param name="expiry">TTL，需大于业务临界区耗时 / TTL; must exceed the critical section duration</param>
        /// <param name="retryCount">失败后重试次数（不含首次）/ Retry count after the first attempt (excludes it)</param>
        /// <param name="retryDelay">重试间隔，默认 50ms / Retry delay, 50ms by default</param>
        /// <param name="autoRenew">是否启用看门狗自动续期（默认开启，仅 expiry≥3s 生效）/ Enable watchdog auto-renewal (default on; effective only when expiry >= 3s)</param>
        /// <returns>获取成功返回锁句柄；失败返回 null / The lock handle on success; null on failure</returns>
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
                    // 释放失败不抛：锁将在 TTL 后自动过期 / Swallow release failures: the lock auto-expires via TTL
                }
                IsAcquired = false;
            }
        }
    }
}
