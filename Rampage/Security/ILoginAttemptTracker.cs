using System;

namespace Rampage.Security
{
    /// <summary>
    /// 登录失败计数器：用于暴力破解防护（按账号 + 按来源 IP 两个维度）。
    /// 进程内默认实现见 <see cref="MemoryLoginAttemptTracker"/>；集群部署请在扩展库中提供分布式实现并注册到 DI。
    /// / Failed-login tracker for brute-force protection (per account and per source IP).
    /// In-process default implementation: <see cref="MemoryLoginAttemptTracker"/>; provide a distributed implementation in an extension library and register it in DI for clustered deployments.
    /// </summary>
    public interface ILoginAttemptTracker
    {
        /// <summary>
        /// 当前是否应拒绝登录尝试（任一维度达到阈值）/ Whether the login attempt should currently be rejected (either dimension over its limit)
        /// </summary>
        /// <param name="scope">业务范围，如 "admin" / "user"，用于隔离不同登录入口 / Business scope such as "admin" / "user", isolating login entry points</param>
        /// <param name="account">账号标识（用户名/邮箱/手机号，建议小写）/ Account identifier (username/email/phone, lowercase recommended)</param>
        /// <param name="clientIp">来源 IP，可为空 / Source IP, may be null</param>
        /// <param name="retryAfter">被拒绝时距离解锁的剩余时间 / Remaining time until unlock when rejected</param>
        /// <returns>达到阈值返回 true / true when blocked</returns>
        bool IsBlocked(string scope, string? account, string? clientIp, out TimeSpan retryAfter);

        /// <summary>
        /// 记录一次失败 / Record one failed attempt
        /// </summary>
        /// <param name="scope">业务范围 / Business scope</param>
        /// <param name="account">账号标识，可为空 / Account identifier, may be null</param>
        /// <param name="clientIp">来源 IP，可为空 / Source IP, may be null</param>
        void RecordFailure(string scope, string? account, string? clientIp);

        /// <summary>
        /// 登录成功后清除该账号的失败计数（IP 维度计数保留，避免撞库者用一个正确账号洗白）
        /// / Clear the account's failure counters after a successful login (IP counters are kept so attackers cannot launder IP limits through one valid account)
        /// </summary>
        /// <param name="scope">业务范围 / Business scope</param>
        /// <param name="account">账号标识，可为空 / Account identifier, may be null</param>
        void Reset(string scope, string? account);
    }

    /// <summary>
    /// 登录防护阈值配置（配置节 LoginProtection）/ Login protection threshold options (config section LoginProtection)
    /// </summary>
    public class LoginProtectionOptions
    {
        /// <summary>
        /// 配置节名常量 "LoginProtection" / The config section name constant "LoginProtection"
        /// </summary>
        public const string SectionName = "LoginProtection";

        /// <summary>是否启用，默认 true / Whether enabled, default true</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>同一账号在窗口内允许的最大失败次数，默认 5 / Max failures per account within the window, default 5</summary>
        public int MaxFailuresPerAccount { get; set; } = 5;

        /// <summary>同一 IP 在窗口内允许的最大失败次数（覆盖撞库场景），默认 30 / Max failures per IP within the window (credential-stuffing coverage), default 30</summary>
        public int MaxFailuresPerIp { get; set; } = 30;

        /// <summary>计数窗口（秒），默认 900（15 分钟）；达到阈值后需等窗口过期才能再试 / Counting window in seconds, default 900 (15 min); once the limit is hit, retry is possible only after the window expires</summary>
        public int WindowSeconds { get; set; } = 900;

        /// <summary>
        /// 从 GlobalConfiguration 读取；缺省或读取异常时返回默认值
        /// / Load from GlobalConfiguration; returns defaults when missing or on error
        /// </summary>
        /// <returns>配置实例 / The options instance</returns>
        public static LoginProtectionOptions Load()
        {
            try
            {
                return GlobalConfiguration.GetSectionConfig<LoginProtectionOptions>(SectionName) ?? new LoginProtectionOptions();
            }
            catch
            {
                return new LoginProtectionOptions();
            }
        }
    }
}
