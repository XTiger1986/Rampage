using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 数据库连接字符串解码器接口 / Database connection string decoder interface
    /// </summary>
    /// <remarks>
    /// 配置文件中的连接字符串可以密文形式保存；<see cref="DatabaseFactory.ReloadConfig"/> 读取配置时，
    /// 若设置了 <see cref="DatabaseFactoryConfig.ConnectionStringDecoder"/>，会先调用 <c>Decode</c> 还原为明文再使用，
    /// 未设置则视为明文。
    /// Connection strings may be stored encrypted in configuration; when
    /// <see cref="DatabaseFactoryConfig.ConnectionStringDecoder"/> is set, <see cref="DatabaseFactory.ReloadConfig"/>
    /// calls <c>Decode</c> to restore plaintext before use - otherwise the value is treated as plaintext.
    /// </remarks>
    public interface IConnectionStringDecoder
    {

        /// <summary>
        /// 解码加密的数据库连接字符串 / Decode an encrypted database connection string
        /// </summary>
        /// <param name="encodedConnectionString">加密（编码）后的连接字符串 / The encoded connection string</param>
        /// <returns>解码后的明文连接字符串 / The decoded plaintext connection string</returns>
        string Decode(string encodedConnectionString);

    }
}
