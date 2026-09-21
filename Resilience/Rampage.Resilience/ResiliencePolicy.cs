using System;

namespace Rampage.Resilience
{
    /// <summary>
    /// 弹性策略配置（重试 + 熔断 + 超时；不需要的能力将对应参数设为 0/null 即可关闭） / Resilience policy settings (retry + circuit breaker + timeout; disable unneeded capabilities by setting the corresponding parameter to 0/null)
    /// </summary>
    public sealed class ResiliencePolicy
    {
        /// <summary>失败后的最大重试次数（不含首次执行），0 表示不重试 / Maximum number of retries after a failure (excluding the initial execution); 0 means no retry</summary>
        public int MaxRetries { get; init; } = 3;

        /// <summary>重试基础延迟（指数退避时为第1次的延迟） / Base retry delay (the first retry's delay under exponential backoff)</summary>
        public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(100);

        /// <summary>是否指数退避（false 为固定延迟） / Whether to use exponential backoff (false means fixed delay)</summary>
        public bool UseExponentialBackoff { get; init; } = true;

        /// <summary>熔断阈值：连续失败达到该次数后熔断（&lt;=0 禁用熔断） / Breaker threshold: trips after this many consecutive failures (&lt;= 0 disables the breaker)</summary>
        public int BreakerFailureThreshold { get; init; } = 20;

        /// <summary>熔断时长：熔断打开后拒绝请求的时间窗，过后进入半开放行一次探测 / Break duration: the window during which an open circuit rejects requests; afterwards it goes half-open and admits one probe</summary>
        public TimeSpan BreakerBreakDuration { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>单次执行超时（null 表示不限制；触发超时按失败计并按重试策略处理） / Per-execution timeout (null means no limit; a timeout counts as a failure and follows the retry policy)</summary>
        public TimeSpan? Timeout { get; init; }

        public static ResiliencePolicy Default { get; } = new ResiliencePolicy();
    }
}
