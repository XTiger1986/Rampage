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
    /// 限流结果。/ Rate-limit decision result.
    /// </summary>
    public sealed class RateLimitResult
    {
        /// <summary>是否放行。/ Whether the request is allowed.</summary>
        public bool Allowed { get; init; }

        /// <summary>窗口内剩余额度：放行时 = limit − 窗口内已用次数（含本次）；拒绝时恒为 0。/ Remaining quota: on allow = limit − used-in-window (including this call); always 0 on reject.</summary>
        public int Remaining { get; init; }

        /// <summary>窗口重置时间（Unix 毫秒）：拒绝时 = 窗口内最老一次请求时间 + 窗口时长（最早释放一个额度的时刻）；放行时 = 当前时间 + 窗口时长。/ Window reset time (Unix ms): on reject = oldest timestamp in the window + window length (the moment one slot frees); on allow = now + window length.</summary>
        public long ResetAtUnixMs { get; init; }

        /// <summary>
        /// 构建放行结果。/ Build an allow result.
        /// </summary>
        /// <param name="remaining">剩余额度 / remaining quota</param>
        /// <param name="resetAtUnixMs">重置时间（Unix 毫秒，通常为 now + 窗口时长）/ reset time (Unix ms, typically now + window)</param>
        /// <returns>放行结果 / the allow result</returns>
        public static RateLimitResult Allow(int remaining, long resetAtUnixMs)
        {
            return new RateLimitResult { Allowed = true, Remaining = remaining, ResetAtUnixMs = resetAtUnixMs };
        }

        /// <summary>
        /// 构建拒绝结果（Remaining=0）。/ Build a reject result (Remaining=0).
        /// </summary>
        /// <param name="resetAtUnixMs">重置时间（Unix 毫秒，= 窗口内最老请求时间 + 窗口时长）/ reset time (Unix ms, = oldest request in window + window length)</param>
        /// <returns>拒绝结果 / the reject result</returns>
        public static RateLimitResult Reject(long resetAtUnixMs)
        {
            return new RateLimitResult { Allowed = false, Remaining = 0, ResetAtUnixMs = resetAtUnixMs };
        }
    }

    /// <summary>
    /// 限流器（Provider 接口：进程内内存实现见 MemoryRateLimiter；分布式实现见 Rampage.RedisLibrary.RedisRateLimiter）。
    /// / Rate limiter provider interface: see MemoryRateLimiter for the in-process implementation and
    /// Rampage.RedisLibrary.RedisRateLimiter for the distributed one.
    /// </summary>
    public interface IRateLimiter
    {

        /// <summary>
        /// 原子扣减一个额度：窗口内未超限返回 Allowed=true，否则 Allowed=false。
        /// / Atomically consume one slot: returns Allowed=true when the window is not exceeded, otherwise Allowed=false.
        /// </summary>
        /// <param name="key">限流键（需包含业务维度与用户/IP维度）/ rate-limit key (should combine business and user/IP dimensions)</param>
        /// <param name="limit">窗口内允许的最大次数；&lt;=0 恒拒绝 / max calls per window; &lt;= 0 always rejects</param>
        /// <param name="window">窗口时长 / window duration</param>
        /// <returns>限流结果（含剩余额度与重置时间）/ the decision (with remaining quota and reset time)</returns>
        RateLimitResult Limit(string key, int limit, TimeSpan window);
    }
}
