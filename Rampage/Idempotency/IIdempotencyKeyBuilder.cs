namespace Rampage.Idempotency
{
    /// <summary>
    /// 将请求路径 + 客户端 Idempotency-Key 映射为存储键（可加业务前缀、用户维度等）。
    /// / Maps request path + client Idempotency-Key to the store key (may add business prefixes, user dimensions, etc.).
    /// </summary>
    public interface IIdempotencyKeyBuilder
    {
        /// <summary>
        /// 构建幂等存储键。/ Build the idempotency store key.
        /// </summary>
        /// <param name="requestPath">HTTP 路径，如 /api/checkin/do/ / HTTP path, e.g. /api/checkin/do/</param>
        /// <param name="clientKey">请求头中的幂等键（已裁剪长度）/ the idempotency key from the request header (length-trimmed)</param>
        /// <returns>存储键 / the store key</returns>
        string Build(string requestPath, string clientKey);
    }
}
