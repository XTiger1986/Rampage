using Rampage.RateLimit;
using StackExchange.Redis;
using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 基于 Redis 的分布式固定窗口限流器（Lua 原子 INCR + PEXPIRE；多实例共享额度）。
    /// Redis-backed distributed fixed-window rate limiter (atomic Lua INCR + PEXPIRE; quota shared across instances).
    /// Redis 不可用时按放行处理（fail-open），由业务侧兜底。
    /// When Redis is unavailable, requests are allowed (fail-open); the business side must provide its own backstop.
    /// </summary>
    public class RedisRateLimiter : IRateLimiter
    {
        /// <summary>
        /// 固定窗口限流 Lua 脚本：首次 INCR 得 1 时用 PEXPIRE 设定窗口 TTL，返回当前计数与窗口剩余毫秒。
        /// Fixed-window rate limit Lua script: when the first INCR yields 1 it sets the window TTL via PEXPIRE, returning the current count and remaining milliseconds.
        /// </summary>
        private const string RateLimitScript = @"
local current = redis.call('INCR', KEYS[1])
if current == 1 then
  redis.call('PEXPIRE', KEYS[1], ARGV[1])
end
local ttl = redis.call('PTTL', KEYS[1])
return {current, ttl}";

        /// <summary>
        /// 执行限流判定：同一窗口内第 limit+1 次请求被拒绝。
        /// Apply the rate limit verdict: the (limit+1)-th request within a window is rejected.
        /// </summary>
        /// <param name="key">限流键（自动加 Prefix，多实例共享同一计数）/ Rate limit key (prefix applied; all instances share one counter)</param>
        /// <param name="limit">窗口内允许的最大次数 / Maximum requests allowed per window</param>
        /// <param name="window">窗口时长 / Window duration</param>
        /// <returns>限流判定结果（含重置时间与剩余额度）/ The verdict (with reset time and remaining quota)</returns>
        /// <remarks>
        /// limit≤0 一律拒绝；Redis 客户端缺失或异常时放行（fail-open），剩余额度报 int.MaxValue。
        /// limit &lt;= 0 always rejects; a missing Redis client or any Redis failure allows the request (fail-open) with remaining quota int.MaxValue.
        /// </remarks>
        public RateLimitResult Limit(string key, int limit, TimeSpan window)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Rate limit key is required.", nameof(key));
            }
            if (limit <= 0)
            {
                return RateLimitResult.Reject(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)window.TotalMilliseconds);
            }

            try
            {
                RedisHelper? redis = RedisFactory.GetClient();
                if (redis == null)
                {
                    return RateLimitResult.Allow(int.MaxValue, 0);
                }
                RedisResult result = redis.Db.ScriptEvaluate(
                    RateLimitScript,
                    new RedisKey[] { redis.GetFullKey(key) },
                    new RedisValue[] { (long)window.TotalMilliseconds });
                RedisValue[] parts = (RedisValue[])result!;
                long current = (long)parts[0];
                long ttlMs = (long)parts[1];
                long resetAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (ttlMs > 0 ? ttlMs : (long)window.TotalMilliseconds);
                if (current > limit)
                {
                    return RateLimitResult.Reject(resetAt);
                }
                return RateLimitResult.Allow((int)(limit - current), resetAt);
            }
            catch
            {
                // Redis 不可用时放行，避免限流组件故障拖垮业务 / Allow when Redis is down, so a limiter outage never takes the business down
                return RateLimitResult.Allow(int.MaxValue, 0);
            }
        }
    }
}
