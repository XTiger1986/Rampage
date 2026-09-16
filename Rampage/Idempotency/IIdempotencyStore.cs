using System;

namespace Rampage.Idempotency
{
    /// <summary>
    /// 幂等响应缓存存储（通常由 Redis 实现）。
    /// 存的是成功响应的完整 JSON 正文，重复请求直接回放，避免丢失 Data。
    /// </summary>
    public interface IIdempotencyStore
    {
        /// <summary>
        /// 读取已缓存的响应 JSON；未命中或存储不可用时返回 null。
        /// </summary>
        string? Get(string key);

        /// <summary>
        /// 写入成功响应 JSON（覆盖写），并设置 TTL。
        /// </summary>
        void Set(string key, string responseJson, TimeSpan ttl);

        /// <summary>
        /// 原子占位（SET NX）：写入处理中标记；键已被占用时返回 false。
        /// 用于防止同 Key 并发请求重复执行业务。
        /// </summary>
        bool TrySet(string key, string processingMarker, TimeSpan ttl);

        /// <summary>
        /// 删除键（业务执行失败时释放占位，允许重试）。
        /// </summary>
        void Remove(string key);
    }
}
