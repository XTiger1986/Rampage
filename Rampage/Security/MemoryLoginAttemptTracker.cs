using System;
using System.Collections.Concurrent;

namespace Rampage.Security
{
    /// <summary>
    /// 进程内登录失败计数器（固定窗口）。单实例部署可直接使用；多实例下每个实例独立计数，阈值按实例数等比放宽。
    /// / In-process failed-login tracker (fixed window). Usable as-is for single-instance deployments; in multi-instance setups each instance counts independently, so scale the limits by the instance count.
    /// </summary>
    /// <remarks>
    /// 固定窗口计数：账号与 IP 两个维度各自独立计数，窗口内失败达到阈值后封锁至窗口结束；
    /// Fixed-window counting: the account and IP dimensions count independently; once a limit is hit within the window the key stays blocked until the window expires;
    /// 过期条目在每次 RecordFailure 时顺带清理（每分钟至多一次）。
    /// Expired entries are cleaned opportunistically on each RecordFailure (at most once per minute).
    /// </remarks>
    public class MemoryLoginAttemptTracker : ILoginAttemptTracker
    {
        /// <summary>
        /// 单维度计数器：窗口内失败次数与窗口结束时间 / Per-key counter: failure count within the window and the window end time
        /// </summary>
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
        /// / Global default instance (used directly by the host when ILoginAttemptTracker is not registered in DI)
        /// </summary>
        public static MemoryLoginAttemptTracker Default { get; } = new MemoryLoginAttemptTracker();

        /// <summary>
        /// 使用全局配置构造（每次操作经 LoginProtectionOptions.Load 读取最新配置）
        /// / Construct with global configuration (options are re-read via LoginProtectionOptions.Load on each call)
        /// </summary>
        public MemoryLoginAttemptTracker() : this(LoginProtectionOptions.Load)
        {
        }

        /// <summary>
        /// 使用自定义选项提供器构造（便于测试注入）/ Construct with a custom options provider (handy for tests)
        /// </summary>
        /// <param name="optionsProvider">每次操作时返回最新配置的委托 / Delegate returning the latest options on each call</param>
        /// <exception cref="ArgumentNullException">optionsProvider 为 null / optionsProvider is null</exception>
        public MemoryLoginAttemptTracker(Func<LoginProtectionOptions> optionsProvider)
        {
            _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        }

        /// <summary>
        /// 判断账号或 IP 维度是否已超过失败阈值（先查账号维度再查 IP 维度）
        /// / Check whether the account or IP dimension is over its failure limit (account dimension first)
        /// </summary>
        /// <param name="scope">业务范围，用于隔离不同登录入口 / Business scope isolating login entry points</param>
        /// <param name="account">账号标识，可为空 / Account identifier, may be null</param>
        /// <param name="clientIp">来源 IP，可为空 / Source IP, may be null</param>
        /// <param name="retryAfter">被拒绝时距离解锁的剩余时间 / Remaining time until unlock when rejected</param>
        /// <returns>任一维度达到阈值返回 true / true when either dimension is blocked</returns>
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

        /// <summary>
        /// 在账号与 IP 两个维度各记录一次失败，并顺带清理过期计数
        /// / Record one failure on both the account and IP dimensions, with opportunistic cleanup
        /// </summary>
        /// <param name="scope">业务范围，用于隔离不同登录入口 / Business scope isolating login entry points</param>
        /// <param name="account">账号标识，可为空 / Account identifier, may be null</param>
        /// <param name="clientIp">来源 IP，可为空 / Source IP, may be null</param>
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

        /// <summary>
        /// 清除账号维度的失败计数（IP 维度计数保留）/ Clear the account-dimension counters (IP counters are kept)
        /// </summary>
        /// <param name="scope">业务范围 / Business scope</param>
        /// <param name="account">账号标识，可为空 / Account identifier, may be null</param>
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
