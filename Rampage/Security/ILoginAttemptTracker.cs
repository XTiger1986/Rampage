using System;

namespace Rampage.Security
{
    /// <summary>
    /// 登录失败计数器：用于暴力破解防护（按账号 + 按来源 IP 两个维度）。
    /// 进程内默认实现见 <see cref="MemoryLoginAttemptTracker"/>；集群部署请在扩展库中提供分布式实现并注册到 DI。
    /// Failed-login tracker for brute-force protection (per account and per source IP).
    /// </summary>
    public interface ILoginAttemptTracker
    {
        /// <summary>
        /// 当前是否应拒绝登录尝试（任一维度达到阈值）
        /// </summary>
        /// <param name="scope">业务范围，如 "admin" / "user"，用于隔离不同登录入口</param>
        /// <param name="account">账号标识（用户名/邮箱/手机号，建议小写）</param>
        /// <param name="clientIp">来源 IP，可为空</param>
        /// <param name="retryAfter">被拒绝时距离解锁的剩余时间</param>
        bool IsBlocked(string scope, string? account, string? clientIp, out TimeSpan retryAfter);

        /// <summary>
        /// 记录一次失败
        /// </summary>
        void RecordFailure(string scope, string? account, string? clientIp);

        /// <summary>
        /// 登录成功后清除该账号的失败计数（IP 维度计数保留，避免撞库者用一个正确账号洗白）
        /// </summary>
        void Reset(string scope, string? account);
    }

    /// <summary>
    /// 登录防护阈值配置（配置节 LoginProtection）
    /// </summary>
    public class LoginProtectionOptions
    {
        public const string SectionName = "LoginProtection";

        /// <summary>是否启用，默认 true</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>同一账号在窗口内允许的最大失败次数，默认 5</summary>
        public int MaxFailuresPerAccount { get; set; } = 5;

        /// <summary>同一 IP 在窗口内允许的最大失败次数（覆盖撞库场景），默认 30</summary>
        public int MaxFailuresPerIp { get; set; } = 30;

        /// <summary>计数窗口（秒），默认 900（15 分钟）；达到阈值后需等窗口过期才能再试</summary>
        public int WindowSeconds { get; set; } = 900;

        /// <summary>从 GlobalConfiguration 读取；缺省返回默认值</summary>
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
