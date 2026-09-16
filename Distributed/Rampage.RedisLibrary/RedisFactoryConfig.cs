using System;

namespace Rampage.RedisLibrary
{
    public class RedisFactoryConfig
    {

        /// <summary>
        /// Custom redis connection string decoder. if not specified, use plaintext
        /// </summary>
        public static IRedisDecoder? ConnectionStringDecoder
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, RedisConfig>? Clients
        {
            get;
            internal set;
        }

    }
}
