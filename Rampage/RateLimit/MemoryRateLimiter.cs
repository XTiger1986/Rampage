using System;
using System.Collections.Generic;
using System.Threading;

namespace Rampage.RateLimit
{
    /// <summary>
    /// 进程内滑动窗口限流器（单机部署或实例级限流使用；键数据长期不活跃时随清理回收）
    /// </summary>
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

        /// <summary>清理长期不活跃的键，防止键表无限增长（简单起见在限流路径顺带触发）</summary>
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
