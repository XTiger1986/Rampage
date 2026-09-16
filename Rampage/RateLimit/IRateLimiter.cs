using System;

namespace Rampage.RateLimit
{

    /*
示例：
// 限流（Startup）
services.AddRampageMemoryRateLimiter().AddRampageRateLimitFilter();   // 或 Redis 版
opt.Filters.Add<RateLimitActionFilter>();
[RateLimit("login", 5, 60)] public R Login(...)   // 每 IP 每分钟 5 次
    */

    /// <summary>
    /// 限流结果
    /// </summary>
    public sealed class RateLimitResult
    {
        /// <summary>是否放行</summary>
        public bool Allowed { get; init; }

        /// <summary>窗口内剩余额度</summary>
        public int Remaining { get; init; }

        /// <summary>窗口重置时间（Unix 毫秒）</summary>
        public long ResetAtUnixMs { get; init; }

        public static RateLimitResult Allow(int remaining, long resetAtUnixMs)
        {
            return new RateLimitResult { Allowed = true, Remaining = remaining, ResetAtUnixMs = resetAtUnixMs };
        }

        public static RateLimitResult Reject(long resetAtUnixMs)
        {
            return new RateLimitResult { Allowed = false, Remaining = 0, ResetAtUnixMs = resetAtUnixMs };
        }
    }

    /// <summary>
    /// 限流器（Provider 接口：进程内内存实现见 MemoryRateLimiter；分布式实现见 Rampage.RedisLibrary.RedisRateLimiter）
    /// </summary>
    public interface IRateLimiter
    {

        /// <summary>
        /// 原子扣减一个额度：窗口内未超限返回 Allowed=true，否则 Allowed=false
        /// </summary>
        /// <param name="key">限流键（需包含业务维度与用户/IP维度）</param>
        /// <param name="limit">窗口内允许的最大次数</param>
        /// <param name="window">窗口时长</param>
        /// <returns></returns>
        RateLimitResult Limit(string key, int limit, TimeSpan window);
    }
}
