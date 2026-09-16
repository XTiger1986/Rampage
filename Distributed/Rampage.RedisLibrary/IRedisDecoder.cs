using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// redis string decoder interface
    /// </summary>
    public interface IRedisDecoder
    {

        /// <summary>
        /// decode encrypted redis connection string
        /// </summary>
        /// <param name="encodedConnectionString"></param>
        /// <returns></returns>
        string DecodeConnectionString(string encodedConnectionString);

    }
}
