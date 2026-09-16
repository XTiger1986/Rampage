using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Rampage.Utilities;
using System;
using System.Collections.Concurrent;

namespace Rampage.Caching
{
    /// <summary>
    /// 单机内存缓存类接口
    /// StandAlone In-Memory Cache Interface
    /// </summary>
    public class MemoryCacheManager : IStandAloneCache
    {
        /// <summary>
        /// 缓存键名集合（键表，Remove 精确移除指定键）。条目被淘汰/过期时通过 PostEvictionCallback 同步移除，避免键表无限增长。
        /// cache keys collection; kept in sync with evictions so it cannot grow unbounded
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> CacheKeys = new ConcurrentDictionary<string, byte>();

        private static readonly Lazy<IMemoryCache> _provider = new Lazy<IMemoryCache>(CreateProvider, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 全局共享的内存缓存提供者：所有 MemoryCacheManager 实例共用同一份缓存。
        /// 按 MemoryCacheConfig 设置 SizeLimit / CompactionPercentage / ExpirationScanFrequency。
        /// </summary>
        public static IMemoryCache MemoryCacheProvider
        {
            get { return _provider.Value; }
        }

        private static IMemoryCache CreateProvider()
        {
            MemoryCacheConfig config = SafeConfig();
            MemoryCacheOptions options = new MemoryCacheOptions();
            if (config.SizeLimit.HasValue && config.SizeLimit.Value > 0)
            {
                options.SizeLimit = config.SizeLimit.Value;
                options.CompactionPercentage = config.CompactionPercentage > 0 && config.CompactionPercentage < 1 ? config.CompactionPercentage : 0.2;
            }
            if (config.ExpirationScanFrequencySeconds > 0)
            {
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(config.ExpirationScanFrequencySeconds);
            }
            return new MemoryCache(Options.Create(options));
        }

        private static MemoryCacheConfig SafeConfig()
        {
            try
            {
                return GlobalConfiguration.MemoryCacheConfig;
            }
            catch
            {
                // 配置尚未初始化（如单元测试）时退回默认值
                return new MemoryCacheConfig { CacheKeyPrefix = "Rampage_" };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual IMemoryCache GetMemoryCacheProvider()
        {
            return MemoryCacheProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual string? KeyPrefix
        {
            get { return SafeConfig().CacheKeyPrefix; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public string HandleCacheKey(string cacheKey)
        {
            return KeyPrefix + cacheKey;
        }

        /// <summary>
        /// 构建条目选项：每条目 Size=1（SizeLimit 生效的前提）、默认过期、淘汰回调同步键表。
        /// </summary>
        private MemoryCacheEntryOptions BuildEntryOptions(string cacheKey, double? expiredSeconds)
        {
            MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
            {
                Size = 1
            };

            double? ttl = expiredSeconds;
            if (ttl == null || ttl <= 0)
            {
                double? defaultTtl = SafeConfig().DefaultExpirationSeconds;
                if (defaultTtl.HasValue && defaultTtl.Value > 0)
                {
                    ttl = defaultTtl.Value;
                }
            }
            if (ttl.HasValue && ttl.Value > 0)
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl.Value);
            }

            options.RegisterPostEvictionCallback(static (key, value, reason, state) =>
            {
                // Replaced 表示同键被覆盖写入，键仍然有效
                if (reason != EvictionReason.Replaced && state is string originalKey)
                {
                    CacheKeys.TryRemove(originalKey, out _);
                }
            }, cacheKey);

            return options;
        }

        #region Get

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual List<string> GetAllKeys()
        {
            return CacheKeys.Keys.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public virtual List<string> GetKeys(string prefix)
        {
            List<string> allKeys = GetAllKeys();
            return allKeys.FindAll(cacheKey => StringTool.StartsWithIgnoreCase(cacheKey, prefix) || string.Compare(cacheKey, prefix, true) == 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public virtual T? Get<T>(string cacheKey)
        {
            return MemoryCacheProvider.Get<T>(HandleCacheKey(cacheKey));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public virtual object? Get(string cacheKey)
        {
            return MemoryCacheProvider.Get(HandleCacheKey(cacheKey));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool TryGet<T>(string cacheKey, out T? data)
        {
            return MemoryCacheProvider.TryGetValue<T>(HandleCacheKey(cacheKey), out data);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool TryGet(string cacheKey, out object? data)
        {
            return MemoryCacheProvider.TryGetValue(HandleCacheKey(cacheKey), out data);
        }

        #endregion

        #region Set

        /// <summary>
        /// 写入缓存；未指定过期时间时使用 MemoryCacheConfig.DefaultExpirationSeconds（默认 1 小时）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        public virtual void Set<T>(string cacheKey, T data)
        {
            if (data == null)
            {
                return;
            }
            MemoryCacheProvider.Set(HandleCacheKey(cacheKey), data, BuildEntryOptions(cacheKey, null));
            CacheKeys.TryAdd(cacheKey, 0);
        }
        /// <summary>
        /// 写入缓存；未指定过期时间时使用 MemoryCacheConfig.DefaultExpirationSeconds（默认 1 小时）
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        public virtual void Set(string cacheKey, object data)
        {
            if (data == null)
            {
                return;
            }
            MemoryCacheProvider.Set(HandleCacheKey(cacheKey), data, BuildEntryOptions(cacheKey, null));
            CacheKeys.TryAdd(cacheKey, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        /// <param name="expiredSeconds"></param>
        public virtual void Set<T>(string cacheKey, T data, double expiredSeconds)
        {
            if (data == null)
            {
                return;
            }
            MemoryCacheProvider.Set<T>(HandleCacheKey(cacheKey), data, BuildEntryOptions(cacheKey, expiredSeconds));
            CacheKeys.TryAdd(cacheKey, 0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="data"></param>
        /// <param name="expiredSeconds"></param>
        public virtual void Set(string cacheKey, object data, double expiredSeconds)
        {
            if (data == null)
            {
                return;
            }
            MemoryCacheProvider.Set(HandleCacheKey(cacheKey), data, BuildEntryOptions(cacheKey, expiredSeconds));
            CacheKeys.TryAdd(cacheKey, 0);
        }

        #endregion

        #region Remove

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKey">cache key</param>
        public virtual void Remove(string cacheKey)
        {
            MemoryCacheProvider.Remove(HandleCacheKey(cacheKey));

            CacheKeys.TryRemove(cacheKey, out _);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix">cache key prefix</param>
        public virtual void RemoveBySearch(string prefix)
        {
            List<string> cacheKeys = GetAllKeys();
            foreach (string cacheKey in cacheKeys)
            {
                if (StringTool.StartsWithIgnoreCase(cacheKey, prefix) || string.Compare(cacheKey, prefix, true) == 0)
                {
                    MemoryCacheProvider.Remove(HandleCacheKey(cacheKey));

                    CacheKeys.TryRemove(cacheKey, out _);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ClearCache()
        {
            List<string> cacheKeys = GetAllKeys();
            foreach (string cacheKey in cacheKeys)
            {
                MemoryCacheProvider.Remove(HandleCacheKey(cacheKey));
            }
            CacheKeys.Clear();
        }

        #endregion

        /// <summary>
        /// 缓存提供者为全局共享，生命周期随进程，实例 Dispose 不释放
        /// </summary>
        public virtual void Dispose()
        {
        }

    }
}
