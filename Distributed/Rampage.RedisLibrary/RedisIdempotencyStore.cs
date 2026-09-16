using Rampage.Idempotency;
using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 基于 Redis 的幂等响应存储。Redis 不可用时 Get/Set 静默失败（业务层需自备幂等）。
    /// </summary>
    public class RedisIdempotencyStore : IIdempotencyStore
    {
        public string? Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            try
            {
                RedisHelper? redis = RedisFactory.GetClient();
                return redis?.StringGet(key);
            }
            catch
            {
                return null;
            }
        }

        public void Set(string key, string responseJson, TimeSpan ttl)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrEmpty(responseJson))
            {
                return;
            }

            if (ttl <= TimeSpan.Zero)
            {
                ttl = TimeSpan.FromHours(24);
            }

            try
            {
                RedisHelper? redis = RedisFactory.GetClient();
                redis?.StringSet(key, responseJson, ttl);
            }
            catch
            {
                // ignore
            }
        }

        public bool TrySet(string key, string processingMarker, TimeSpan ttl)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrEmpty(processingMarker))
            {
                return false;
            }
            if (ttl <= TimeSpan.Zero)
            {
                ttl = TimeSpan.FromHours(24);
            }

            try
            {
                RedisHelper? redis = RedisFactory.GetClient();
                return redis != null && redis.StringSetNx(key, processingMarker, ttl);
            }
            catch
            {
                // 存储不可用时放行，由业务层幂等兜底
                return true;
            }
        }

        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }
            try
            {
                RedisHelper? redis = RedisFactory.GetClient();
                redis?.KeyDelete(key);
            }
            catch
            {
                // ignore
            }
        }
    }
}
