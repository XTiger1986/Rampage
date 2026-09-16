using System;
using System.Collections.Concurrent;

namespace Rampage.Security
{
    /// <summary>
    /// 进程内登录失败计数器（固定窗口）。单实例部署可直接使用；多实例下每个实例独立计数，阈值按实例数等比放宽。
    /// In-process failed-login tracker (fixed window).
    /// </summary>
    public class MemoryLoginAttemptTracker : ILoginAttemptTracker
    {
        private sealed class Counter
        {
            public int Count;
            public DateTime WindowEndUtc;
        }

        private readonly ConcurrentDictionary<string, Counter> _counters = new ConcurrentDictionary<string, Counter>(StringComparer.Ordinal);
        private readonly Func<LoginProtectionOptions> _optionsProvider;
        private DateTime _lastCleanupUtc = DateTime.UtcNow;

        /// <summary>
        /// 全局默认实例（未在 DI 注册 ILoginAttemptTracker 时由宿主直接使用）
        /// </summary>
        public static MemoryLoginAttemptTracker Default { get; } = new MemoryLoginAttemptTracker();

        public MemoryLoginAttemptTracker() : this(LoginProtectionOptions.Load)
        {
        }

        public MemoryLoginAttemptTracker(Func<LoginProtectionOptions> optionsProvider)
        {
            _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        }

        public bool IsBlocked(string scope, string? account, string? clientIp, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;
            LoginProtectionOptions options = _optionsProvider();
            if (!options.Enabled)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;
            if (IsOverLimit(AccountKey(scope, account), options.MaxFailuresPerAccount, now, out retryAfter))
            {
                return true;
            }
            if (IsOverLimit(IpKey(scope, clientIp), options.MaxFailuresPerIp, now, out retryAfter))
            {
                return true;
            }
            return false;
        }

        public void RecordFailure(string scope, string? account, string? clientIp)
        {
            LoginProtectionOptions options = _optionsProvider();
            if (!options.Enabled)
            {
                return;
            }
            DateTime now = DateTime.UtcNow;
            TimeSpan window = TimeSpan.FromSeconds(options.WindowSeconds > 0 ? options.WindowSeconds : 900);
            Increment(AccountKey(scope, account), now, window);
            Increment(IpKey(scope, clientIp), now, window);
            CleanupExpired(now);
        }

        public void Reset(string scope, string? account)
        {
            string? key = AccountKey(scope, account);
            if (key != null)
            {
                _counters.TryRemove(key, out _);
            }
        }

        private bool IsOverLimit(string? key, int limit, DateTime now, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;
            if (key == null || limit <= 0)
            {
                return false;
            }
            if (!_counters.TryGetValue(key, out Counter? counter))
            {
                return false;
            }
            lock (counter)
            {
                if (counter.WindowEndUtc <= now)
                {
                    _counters.TryRemove(key, out _);
                    return false;
                }
                if (counter.Count >= limit)
                {
                    retryAfter = counter.WindowEndUtc - now;
                    return true;
                }
            }
            return false;
        }

        private void Increment(string? key, DateTime now, TimeSpan window)
        {
            if (key == null)
            {
                return;
            }
            Counter counter = _counters.GetOrAdd(key, _ => new Counter { WindowEndUtc = now + window });
            lock (counter)
            {
                if (counter.WindowEndUtc <= now)
                {
                    counter.Count = 0;
                    counter.WindowEndUtc = now + window;
                }
                counter.Count++;
            }
        }

        private void CleanupExpired(DateTime now)
        {
            if (now - _lastCleanupUtc < TimeSpan.FromMinutes(1))
            {
                return;
            }
            _lastCleanupUtc = now;
            foreach (KeyValuePair<string, Counter> kv in _counters)
            {
                if (kv.Value.WindowEndUtc <= now)
                {
                    _counters.TryRemove(kv.Key, out _);
                }
            }
        }

        private static string? AccountKey(string scope, string? account)
        {
            return string.IsNullOrWhiteSpace(account) ? null : string.Concat(scope, ":acct:", account.Trim().ToLowerInvariant());
        }

        private static string? IpKey(string scope, string? clientIp)
        {
            return string.IsNullOrWhiteSpace(clientIp) ? null : string.Concat(scope, ":ip:", clientIp.Trim());
        }
    }
}
