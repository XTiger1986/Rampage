using Microsoft.Extensions.Caching.Memory;
using System;

namespace Rampage.Caching
{
    /// <summary>
    /// 单机缓存接口。/ Standalone in-memory cache interface.
    /// </summary>
    public interface IStandAloneCache : IDisposable
    {
        /// <summary>
        /// 获取缓存提供器。/ Get the in-memory cache provider.
        /// </summary>
        /// <returns>底层 IMemoryCache 实例 / the underlying IMemoryCache instance</returns>
        IMemoryCache GetMemoryCacheProvider();

        /// <summary>
        /// 获取所有缓存键名列表。/ Get all cache keys.
        /// </summary>
        /// <returns>全部缓存键名 / all cache keys</returns>
        List<string> GetAllKeys();

        /// <summary>
        /// 获取匹配指定前缀的缓存键名列表（忽略大小写的前缀匹配，或整键相等）。
        /// / Get the cache keys that case-insensitively start with the prefix or equal it exactly.
        /// </summary>
        /// <param name="prefix">键名前缀（或完整键名）/ key prefix (or full key)</param>
        /// <returns>匹配的键名列表 / the matched keys</returns>
        List<string> GetKeys(string prefix);

        /// <summary>
        /// 获取指定键名的缓存数据。/ Get the cached data for the specified key.
        /// </summary>
        /// <typeparam name="T">数据类型 / data type</typeparam>
        /// <param name="cacheKey">缓存键名 / cache key</param>
        /// <returns>缓存数据；未命中时为 default / the cached data; default on miss</returns>
        T? Get<T>(string cacheKey);

        /// <summary>
        /// 尝试获取指定键名的缓存数据。/ Try to get the cached data for the specified key.
        /// </summary>
        /// <typeparam name="T">数据类型 / data type</typeparam>
        /// <param name="cacheKey">缓存键名 / cache key</param>
        /// <param name="data">命中的缓存数据；未命中时为 null / the cached data; null on miss</param>
        /// <returns>命中返回 true / true when the key is hit</returns>
        bool TryGet<T>(string cacheKey, out T? data);

        /// <summary>
        /// 尝试获取指定键名的缓存数据（object 形式）。/ Try to get the cached data for the specified key as object.
        /// </summary>
        /// <param name="cacheKey">缓存键名 / cache key</param>
        /// <param name="data">命中的缓存数据；未命中时为 null / the cached data; null on miss</param>
        /// <returns>命中返回 true / true when the key is hit</returns>
        bool TryGet(string cacheKey, out object? data);

        /// <summary>
        /// 设置缓存数据至指定键名（若键已存在则覆盖）。
        /// / Set data for the key (existing data is replaced).
        /// </summary>
        /// <typeparam name="T">数据类型 / data type</typeparam>
        /// <param name="cacheKey">缓存键名 / cache key</param>
        /// <param name="data">缓存数据 / data to cache</param>
        void Set<T>(string cacheKey, T data);

        /// <summary>
        /// 设置缓存数据至指定键名（若键已存在则覆盖）。
        /// / Set data for the key (existing data is replaced).
        /// </summary>
        /// <param name="cacheKey">缓存键名 / cache key</param>
        /// <param name="data">缓存数据 / data to cache</param>
        void Set(string cacheKey, object data);

        /// <summary>
        /// 设置缓存数据至指定键名并指定过期秒数（若键已存在则覆盖）。
        /// / Set data for the key with an explicit TTL in seconds (existing data is replaced).
        /// </summary>
        /// <typeparam name="T">数据类型 / data type</typeparam>
        /// <param name="cacheKey">缓存键名 / cache key</param>
        /// <param name="data">缓存数据 / data to cache</param>
        /// <param name="expiredSeconds">过期秒数 / expiration in seconds</param>
        void Set<T>(string cacheKey, T data, double expiredSeconds);

        /// <summary>
        /// 设置缓存数据至指定键名并指定过期秒数（若键已存在则覆盖）。
        /// / Set data for the key with an explicit TTL in seconds (existing data is replaced).
        /// </summary>
        /// <param name="cacheKey">缓存键名 / cache key</param>
        /// <param name="data">缓存数据 / data to cache</param>
        /// <param name="expiredSeconds">过期秒数 / expiration in seconds</param>
        void Set(string cacheKey, object data, double expiredSeconds);

        /// <summary>
        /// 清除指定键名的缓存。/ Clear the cache entry for the specified key.
        /// </summary>
        /// <param name="cacheKey">缓存键名 / cache key</param>
        void Remove(string cacheKey);

        /// <summary>
        /// 清除键名匹配指定前缀的缓存（忽略大小写的前缀匹配，或整键相等）。
        /// / Clear entries whose key case-insensitively starts with the prefix or equals it exactly.
        /// </summary>
        /// <param name="prefix">键名前缀（或完整键名）/ key prefix (or full key)</param>
        void RemoveBySearch(string prefix);

        /// <summary>
        /// 清除所有缓存数据。/ Clear all cached data.
        /// </summary>
        void ClearCache();

    }
}
