using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 单个 Redis 客户端配置，对应 RedisFactory:Clients 配置节中的一项。
    /// Configuration of a single Redis client; one entry in the RedisFactory:Clients section.
    /// </summary>
    public class RedisConfig
    {

        /// <summary>客户端名称（Clients 字典的键）/ Client name (the key in the Clients dictionary)</summary>
        public string Name
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// Redis 连接串；可存放加密内容，由 <see cref="IRedisDecoder"/> 解密 / Redis connection string; may hold encrypted text decoded by <see cref="IRedisDecoder"/>
        /// </summary>
        public string ConnectionString
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// 键前缀；RedisHelper 对所有键自动拼接 "PrefixKey:" / Key prefix; RedisHelper prepends "PrefixKey:" to every key
        /// </summary>
        public string PrefixKey
        {
            get;
            set;
        } = string.Empty;

        private int _db = -1;
        /// <summary>
        /// 逻辑库编号；默认 -1 表示使用连接串指定的库 / Logical database number; -1 (default) uses the database from the connection string
        /// </summary>
        public int Db
        {
            get { return _db; }
            set { _db = value; }
        }

    }
}
