using Rampage.RateLimit;
using StackExchange.Redis;
using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 基于 Redis 的分布式固定窗口限流器（Lua 原子 INCR + PEXPIRE；多实例共享额度）。
    /// Redis 不可用时按放行处理（fail-open），由业务侧兜底。
    /// </summary>
    public class RedisRateLimiter : IRateLimiter
    {
        private const string RateLimitScript = @"
local current = redis.call('INCR', KEYS[1])
if current == 1 then
  redis.call('PEXPIRE', KEYS[1], ARGV[1])
end
local ttl = redis.call('PTTL', KEYS[1])
return {current, ttl}";

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
                // Redis 不可用时放行，避免限流组件故障拖垮业务
                return RateLimitResult.Allow(int.MaxValue, 0);
            }
        }
    }
}
