using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// Redis 连接串解码器接口，用于解密配置中存放的加密连接串。
    /// Decoder interface for Redis connection strings, used to decrypt encrypted strings held in configuration.
    /// </summary>
    public interface IRedisDecoder
    {

        /// <summary>
        /// 解码（解密）Redis 连接串 / Decode (decrypt) a Redis connection string
        /// </summary>
        /// <param name="encodedConnectionString">加密的连接串 / The encoded (encrypted) connection string</param>
        /// <returns>明文连接串 / The decoded plaintext connection string</returns>
        string DecodeConnectionString(string encodedConnectionString);

    }
}
