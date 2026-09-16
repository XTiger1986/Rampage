using Microsoft.Extensions.Caching.Memory;
using System;

namespace Rampage.Caching
{
    /// <summary>
    /// 单机缓存类接口
    /// StandAlone In-Memory Cache Interface
    /// </summary>
    public interface IStandAloneCache : IDisposable
    {
        /// <summary>
        /// 获取缓存提供器
        /// Get In-Memory Cache Provider
        /// </summary>
        /// <returns></returns>
        IMemoryCache GetMemoryCacheProvider();

        /// <summary>
        /// 获取所有缓存键名列表
        /// get all cache keys
        /// </summary>
        /// <returns></returns>
        List<string> GetAllKeys();

        /// <summary>
        /// 获取指定前缀的缓存键名列表
        /// get cache keys by prefix
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        List<string> GetKeys(string prefix);

        /// <summary>
        /// 获取指定键名的缓存数据
        /// Retrieve cached data for the specified key name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        T? Get<T>(string cacheKey);

        /// <summary>
        /// 尝试获取指定键名的缓存数据
        /// Try Retrieve cached data for the specified key name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool TryGet<T>(string cacheKey, out T? data);

        /// <summary>
        /// 尝试获取指定键名的缓存数据
        /// Try Retrieve cached data for the specified key name
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool TryGet(string cacheKey, out object? data);

        /// <summary>
        /// 设置缓存数据至指定键名 (若已经存在指定键名缓存则覆盖)
        /// Add a cache data (replace the original cache data when the cache data for that item already exists)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        void Set<T>(string cacheKey, T data);

        /// <summary>
        /// 设置缓存数据至指定键名 (若已经存在指定键名缓存则覆盖)
        /// Add a cache data (replace the original cache data when the cache data for that item already exists)
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        void Set(string cacheKey, object data);

        /// <summary>
        /// 设置缓存数据至指定键名 (若已经存在指定键名缓存则覆盖)
        /// Add a cache data (replace the original cache data when the cache data for that item already exists)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        /// <param name="expiredSeconds">configure cache data expire seconds</param>
        void Set<T>(string cacheKey, T data, double expiredSeconds);

        /// <summary>
        /// 设置缓存数据至指定键名 (若已经存在指定键名缓存则覆盖)
        /// Add a cache data (replace the original cache data when the cache data for that item already exists)
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        /// <param name="expiredSeconds">configure cache data expire seconds</param>
        void Set(string cacheKey, object data, double expiredSeconds);

        /// <summary>
        /// 清除指定键名的缓存
        /// Clear cache for specified key
        /// </summary>
        /// <param name="cacheKey"></param>
        void Remove(string cacheKey);

        /// <summary>
        /// 清除指定键名前缀的缓存
        /// Clear cache for specified key prefix
        /// </summary>
        /// <param name="prefix"></param>
        void RemoveBySearch(string prefix);

        /// <summary>
        /// 清除所有缓存数据
        /// Clear all cache data
        /// </summary>
        void ClearCache();

    }
}
