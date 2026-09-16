using System;

namespace Rampage.Resilience
{
    /// <summary>
    /// 弹性策略配置（重试 + 熔断 + 超时；不需要的能力保持默认禁用/关闭即可）
    /// </summary>
    public sealed class ResiliencePolicy
    {
        /// <summary>失败后的最大重试次数（不含首次执行），0 表示不重试</summary>
        public int MaxRetries { get; init; } = 3;

        /// <summary>重试基础延迟（指数退避时为第1次的延迟）</summary>
        public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(100);

        /// <summary>是否指数退避（false 为固定延迟）</summary>
        public bool UseExponentialBackoff { get; init; } = true;

        /// <summary>熔断阈值：连续失败达到该次数后熔断（&lt;=0 禁用熔断）</summary>
        public int BreakerFailureThreshold { get; init; } = 20;

        /// <summary>熔断时长：熔断打开后拒绝请求的时间窗，过后进入半开放行一次探测</summary>
        public TimeSpan BreakerBreakDuration { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>单次执行超时（null 表示不限制；触发超时按失败计并按重试策略处理）</summary>
        public TimeSpan? Timeout { get; init; }

        public static ResiliencePolicy Default { get; } = new ResiliencePolicy();
    }
}
