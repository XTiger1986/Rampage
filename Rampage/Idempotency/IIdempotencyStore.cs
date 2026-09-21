using System;

namespace Rampage.Idempotency
{
    /// <summary>
    /// 幂等响应缓存存储（通常由 Redis 实现）。
    /// 存的是成功响应的完整 JSON 正文，重复请求直接回放，避免丢失 Data。
    /// / Idempotent-response cache store (typically Redis-backed). It caches the full JSON body of successful
    /// responses so repeated requests can be replayed directly without losing Data.
    /// </summary>
    public interface IIdempotencyStore
    {
        /// <summary>
        /// 读取已缓存的响应 JSON；未命中或存储不可用时返回 null。/ Read the cached response JSON; null on miss or when the store is unavailable.
        /// </summary>
        /// <param name="key">存储键 / store key</param>
        /// <returns>缓存的响应 JSON；未命中时为 null / the cached response JSON; null on miss</returns>
        string? Get(string key);

        /// <summary>
        /// 写入成功响应 JSON（覆盖写），并设置 TTL。/ Write the success-response JSON (overwriting) with a TTL.
        /// </summary>
        /// <param name="key">存储键 / store key</param>
        /// <param name="responseJson">响应正文 JSON / the response body JSON</param>
        /// <param name="ttl">存活时间 / time to live</param>
        void Set(string key, string responseJson, TimeSpan ttl);

        /// <summary>
        /// 原子占位（SET NX）：写入处理中标记；键已被占用时返回 false。
        /// 用于防止同 Key 并发请求重复执行业务。
        /// / Atomic placeholder (SET NX): writes the processing marker and returns false when the key is already taken;
        /// prevents concurrent same-key requests from double-executing the business logic.
        /// </summary>
        /// <param name="key">存储键 / store key</param>
        /// <param name="processingMarker">处理中标记 / the processing marker</param>
        /// <param name="ttl">占位存活时间 / placeholder TTL</param>
        /// <returns>占位成功返回 true；键已被占用返回 false / true when acquired; false when the key is already occupied</returns>
        bool TrySet(string key, string processingMarker, TimeSpan ttl);

        /// <summary>
        /// 删除键（业务执行失败时释放占位，允许重试）。/ Remove the key (releases the placeholder on business failure so retries can proceed).
        /// </summary>
        /// <param name="key">存储键 / store key</param>
        void Remove(string key);
    }
}
