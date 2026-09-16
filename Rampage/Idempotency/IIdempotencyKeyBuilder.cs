namespace Rampage.Idempotency
{
    /// <summary>
    /// 将请求路径 + 客户端 Idempotency-Key 映射为存储键（可加业务前缀、用户维度等）。
    /// </summary>
    public interface IIdempotencyKeyBuilder
    {
        /// <param name="requestPath">HTTP 路径，如 /api/checkin/do/</param>
        /// <param name="clientKey">请求头中的幂等键（已裁剪长度）</param>
        string Build(string requestPath, string clientKey);
    }
}
