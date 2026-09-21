using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// RedisFactory 全局配置，对应 RedisFactory 配置节。
    /// Global settings for RedisFactory, bound to the RedisFactory configuration section.
    /// </summary>
    public class RedisFactoryConfig
    {

        /// <summary>
        /// 自定义连接串解码器；未设置时连接串按明文处理。
        /// Custom connection string decoder; connection strings are treated as plaintext when not set.
        /// </summary>
        /// <remarks>静态属性，须在首次访问 <see cref="RedisFactory"/>（即创建任何连接）之前设置 / Static; set it before the first access to <see cref="RedisFactory"/> (before any connection is created)</remarks>
        public static IRedisDecoder? ConnectionStringDecoder
        {
            get;
            set;
        }

        /// <summary>
        /// 命名客户端配置字典，键为 <see cref="RedisConfig.Name"/> / Named client configurations keyed by <see cref="RedisConfig.Name"/>
        /// </summary>
        public Dictionary<string, RedisConfig>? Clients
        {
            get;
            internal set;
        }

    }
}
