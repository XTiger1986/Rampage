using System;
using System.Collections.Generic;
using System.Threading;

namespace Rampage.RateLimit
{
    /// <summary>
    /// 进程内滑动窗口限流器（单机部署或实例级限流使用；键数据长期不活跃时随清理回收）。
    /// / In-process sliding-window rate limiter (for single-machine or per-instance limiting; idle keys are reclaimed by cleanup).
    /// </summary>
    /// <remarks>
    /// 每 key 维护一个请求时间戳队列（每 key 一把锁，全局字典另用 _windowsLock 保护）；限流路径不设后台任务，
    /// 清理仅在放行分支顺带触发。/ Each key holds a timestamp queue (one lock per key; the global dictionary uses a
    /// separate _windowsLock); there is no background task — cleanup runs only from the allow path.
    /// </remarks>
    public class MemoryRateLimiter : IRateLimiter
    {
        private sealed class WindowState
        {
            public readonly object Lock = new object();
            public Queue<long> Timestamps = new Queue<long>();
            public long LastAccessTicks;
        }

        private readonly Dictionary<string, WindowState> _windows = new Dictionary<string, WindowState>();
        private readonly object _windowsLock = new object();
        private long _lastCleanupTicks;

        /// <summary>
        /// 滑动窗口限流：先出队 &lt;= now−window 的过期时间戳，队列长度达到 limit 即拒绝；limit&lt;=0 恒拒绝。
        /// / Sliding-window limiting: timestamps &lt;= now−window are dequeued first, and a queue already holding
        /// limit entries rejects; limit &lt;= 0 always rejects.
        /// </summary>
        /// <param name="key">限流键 / rate-limit key</param>
        /// <param name="limit">窗口内允许的最大次数（<=0 恒拒绝）/ max calls per window (&lt;= 0 always rejects)</param>
        /// <param name="window">窗口时长 / window duration</param>
        /// <returns>拒绝时 ResetAtUnixMs = 窗口内最老时间戳 + 窗口时长；放行时 Remaining = limit − 窗口内计数（含本次）、
        /// ResetAtUnixMs = now + 窗口时长 / on reject ResetAtUnixMs = oldest timestamp + window; on allow
        /// Remaining = limit − in-window count (including this call) and ResetAtUnixMs = now + window</returns>
        /// <exception cref="ArgumentException">key 为 null 或空 / key is null or empty</exception>
        public RateLimitResult Limit(string key, int limit, TimeSpan window)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Rate limit key is required.", nameof(key));
            }
            if (limit <= 0)
            {
                return RateLimitResult.Reject(CurrentUnixMs() + (long)window.TotalMilliseconds);
            }

            WindowState state = GetState(key);
            long nowTicks = DateTime.UtcNow.Ticks;
            long windowTicks = window.Ticks;
            long nowMs = CurrentUnixMs();

            lock (state.Lock)
            {
                state.LastAccessTicks = nowTicks;
                while (state.Timestamps.Count > 0 && state.Timestamps.Peek() <= nowTicks - windowTicks)
                {
                    state.Timestamps.Dequeue();
                }
                if (state.Timestamps.Count >= limit)
                {
                    long oldestTicks = state.Timestamps.Peek();
                    long resetAtMs = nowMs + (long)((oldestTicks + windowTicks - nowTicks) / TimeSpan.TicksPerMillisecond);
                    return RateLimitResult.Reject(resetAtMs);
                }
                state.Timestamps.Enqueue(nowTicks);
                CleanupIdleWindows(nowTicks);
                return RateLimitResult.Allow(limit - state.Timestamps.Count, nowMs + (long)window.TotalMilliseconds);
            }
        }

        private WindowState GetState(string key)
        {
            lock (_windowsLock)
            {
                if (!_windows.TryGetValue(key, out WindowState? state))
                {
                    state = new WindowState();
                    _windows.Add(key, state);
                }
                return state;
            }
        }

        /// <summary>清理长期不活跃的键，防止键表无限增长（仅在 Allow 放行分支被调用；每 1 分钟最多执行一次，删除 1 小时未活跃的键）。/ Remove long-idle keys to keep the key table bounded (invoked only from the allow path; at most once per minute, removing keys idle for over an hour).</summary>
        private void CleanupIdleWindows(long nowTicks)
        {
            if (Interlocked.Read(ref _lastCleanupTicks) != 0 && nowTicks - _lastCleanupTicks < TimeSpan.TicksPerMinute)
            {
                return;
            }
            Interlocked.Exchange(ref _lastCleanupTicks, nowTicks);
            lock (_windowsLock)
            {
                List<string>? idleKeys = null;
                foreach (KeyValuePair<string, WindowState> kv in _windows)
                {
                    if (nowTicks - kv.Value.LastAccessTicks > TimeSpan.TicksPerHour)
                    {
                        (idleKeys ??= new List<string>()).Add(kv.Key);
                    }
                }
                if (idleKeys != null)
                {
                    foreach (string key in idleKeys)
                    {
                        _windows.Remove(key);
                    }
                }
            }
        }

        private static long CurrentUnixMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
