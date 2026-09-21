using Rampage.Idempotency;
using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 基于 Redis 的幂等响应存储。
    /// Redis-backed idempotent response store.
    /// </summary>
    /// <remarks>
    /// Redis 异常时全部静默处理：Get/Remove 变为空操作，Set 丢失写入，TrySet 直接返回 true（fail-open）——
    /// 与 Web 层幂等过滤器的 fail-closed 策略相反，存储不可用时请求会放行，需业务层自备幂等兜底。
    /// All Redis failures are swallowed: Get/Remove become no-ops, Set loses the write and TrySet returns true (fail-open) —
    /// the opposite of the Web-layer idempotency filter's fail-closed policy; requests pass through while the store is down, so the business layer needs its own idempotency backstop.
    /// </remarks>
    public class RedisIdempotencyStore : IIdempotencyStore
    {
        /// <summary>
        /// 读取已缓存的幂等响应 / Read a cached idempotent response
        /// </summary>
        /// <param name="key">幂等键（自动加 Prefix）/ Idempotency key (prefix applied automatically)</param>
        /// <returns>缓存的响应 JSON；键不存在或 Redis 异常返回 null / The cached response JSON; null when missing or on Redis failure</returns>
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

        /// <summary>
        /// 缓存幂等响应（覆盖写，异常静默丢弃）/ Cache an idempotent response (overwrite; failures silently dropped)
        /// </summary>
        /// <param name="key">幂等键 / Idempotency key</param>
        /// <param name="responseJson">响应 JSON / Response JSON</param>
        /// <param name="ttl">保存时长；≤0 时按 24 小时处理 / Retention; treated as 24 hours when &lt;= 0</param>
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

        /// <summary>
        /// 尝试写入"处理中"占位（StringSetNx）：首个请求占位成功，并发重复请求返回 false。
        /// Try to place the "processing" marker (StringSetNx): the first request wins, concurrent duplicates get false.
        /// </summary>
        /// <param name="key">幂等键 / Idempotency key</param>
        /// <param name="processingMarker">占位值（如"处理中"标记）/ Marker value (e.g. a processing sentinel)</param>
        /// <param name="ttl">占位时长；≤0 时按 24 小时处理 / Marker TTL; treated as 24 hours when &lt;= 0</param>
        /// <returns>true=占位成功或 Redis 异常（fail-open 放行）；false=已有占位 / true when the marker was set or Redis failed (fail-open); false when a marker already exists</returns>
        /// <remarks>
        /// 注意与 Web 层幂等过滤器相反：Redis 不可用时此处返回 true 放行请求（fail-open）。
        /// Note the opposite of the Web-layer idempotency filter: on Redis failure this returns true and lets the request through (fail-open).
        /// </remarks>
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

        /// <summary>
        /// 删除幂等缓存（异常静默忽略）/ Delete an idempotency cache entry (failures silently ignored)
        /// </summary>
        /// <param name="key">幂等键 / Idempotency key</param>
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
