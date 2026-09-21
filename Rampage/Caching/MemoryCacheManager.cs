using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Rampage.Utilities;
using System;
using System.Collections.Concurrent;

namespace Rampage.Caching
{
    /// <summary>
    /// 单机内存缓存管理器：IStandAloneCache 的默认实现，包装 Microsoft.Extensions.Caching.Memory（非自研缓存）。
    /// / Standalone in-memory cache manager: the default IStandAloneCache implementation, wrapping Microsoft.Extensions.Caching.Memory.
    /// </summary>
    /// <remarks>
    /// 1) 底层 IMemoryCache 为全局静态 Lazy 单例，所有实例（含派生类）共享同一份缓存；Dispose 为空操作，不释放共享缓存。
    /// 2) 缓存键双轨制：实际存储键 = CacheKeyPrefix + 业务键，而 GetAllKeys/GetKeys 返回的键表记录是不带前缀的业务键。
    /// 3) 每条目 Size=1，靠 SizeLimit（默认 10 万）+ CompactionPercentage（默认 0.2）在超限时压缩淘汰，并有默认绝对过期（DefaultExpirationSeconds，默认 3600 秒）。
    /// 4) MemoryCacheConfig 热更新仅对 DefaultExpirationSeconds 与 CacheKeyPrefix 生效；SizeLimit 等在提供者创建后即固化。
    /// / 1) The underlying IMemoryCache is a global static lazy singleton shared by all instances; Dispose is a no-op.
    /// 2) Dual-track keys: the physical storage key is CacheKeyPrefix + business key, while GetAllKeys/GetKeys return the business keys without the prefix.
    /// 3) Each entry has Size=1; SizeLimit (default 100k) + CompactionPercentage (default 0.2) drive compaction, plus a default absolute expiration (default 3600s).
    /// 4) Hot-updating MemoryCacheConfig only affects DefaultExpirationSeconds and CacheKeyPrefix; SizeLimit etc. are frozen once the provider is created.
    /// </remarks>
    public class MemoryCacheManager : IStandAloneCache
    {
        /// <summary>
        /// 缓存键名集合（键表，存不带前缀的业务键；Remove 精确移除指定键）。
        /// 条目被淘汰/过期时通过 PostEvictionCallback（Replaced 除外）同步移除，避免键表无限增长。
        /// / Key table of business keys (without prefix); kept in sync via PostEvictionCallback (except Replaced) so it cannot grow unbounded.
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> CacheKeys = new ConcurrentDictionary<string, byte>();

        private static readonly Lazy<IMemoryCache> _provider = new Lazy<IMemoryCache>(CreateProvider, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 全局共享的内存缓存提供者：所有 MemoryCacheManager 实例共用同一份缓存。
        /// 按 MemoryCacheConfig 设置 SizeLimit / CompactionPercentage / ExpirationScanFrequency；
        /// 提供者由 Lazy 首次访问时创建，此后修改 SizeLimit 等配置不再生效。
        /// / The memory cache provider shared by all MemoryCacheManager instances, configured from MemoryCacheConfig
        /// (SizeLimit / CompactionPercentage / ExpirationScanFrequency); created lazily on first access,
        /// after which changes to SizeLimit etc. no longer take effect.
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
        /// 获取全局共享的内存缓存提供者。/ Get the globally shared memory cache provider.
        /// </summary>
        /// <returns>底层 IMemoryCache / the underlying IMemoryCache</returns>
        public virtual IMemoryCache GetMemoryCacheProvider()
        {
            return MemoryCacheProvider;
        }

        /// <summary>
        /// 当前生效的缓存键名前缀（每次读取 MemoryCacheConfig.CacheKeyPrefix，支持热更新）。
        /// / The effective cache key prefix (read from MemoryCacheConfig on each access; hot-updatable).
        /// </summary>
        public virtual string? KeyPrefix
        {
            get { return SafeConfig().CacheKeyPrefix; }
        }

        /// <summary>
        /// 生成实际存储键：KeyPrefix + 业务键。/ Build the physical storage key: KeyPrefix + business key.
        /// </summary>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        /// <returns>实际存储键 / the physical storage key</returns>
        public string HandleCacheKey(string cacheKey)
        {
            return KeyPrefix + cacheKey;
        }

        /// <summary>
        /// 构建条目选项：每条目 Size=1（SizeLimit 生效的前提）、绝对过期（未指定或 &lt;=0 时回落 DefaultExpirationSeconds）、
        /// 淘汰回调同步键表（Replaced 除外）。
        /// / Build entry options: Size=1 per entry (required by SizeLimit), absolute expiration (falling back to
        /// DefaultExpirationSeconds when unspecified or &lt;= 0), and an eviction callback keeping the key table in sync (except Replaced).
        /// </summary>
        /// <param name="cacheKey">业务键（用于回调定位键表记录）/ business key (locates the key-table record in the callback)</param>
        /// <param name="expiredSeconds">显式过期秒数；null 或 &lt;=0 时回落默认值 / explicit TTL in seconds; falls back to the default when null or &lt;= 0</param>
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
        /// 获取所有缓存键名列表（不含前缀的业务键）。/ Get all cache keys (business keys without the prefix).
        /// </summary>
        /// <returns>全部业务键 / all business keys</returns>
        public virtual List<string> GetAllKeys()
        {
            return CacheKeys.Keys.ToList();
        }

        /// <summary>
        /// 获取匹配指定前缀的缓存键名列表（忽略大小写的前缀匹配，或整键相等）。
        /// / Get the cache keys that case-insensitively start with the prefix or equal it exactly.
        /// </summary>
        /// <param name="prefix">键名前缀（或完整键名）/ key prefix (or full key)</param>
        /// <returns>匹配的键名列表 / the matched keys</returns>
        public virtual List<string> GetKeys(string prefix)
        {
            List<string> allKeys = GetAllKeys();
            return allKeys.FindAll(cacheKey => StringTool.StartsWithIgnoreCase(cacheKey, prefix) || string.Compare(cacheKey, prefix, true) == 0);
        }

        /// <summary>
        /// 获取指定键名的缓存数据。/ Get the cached data for the specified key.
        /// </summary>
        /// <typeparam name="T">数据类型 / data type</typeparam>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        /// <returns>缓存数据；未命中时为 default / the cached data; default on miss</returns>
        public virtual T? Get<T>(string cacheKey)
        {
            return MemoryCacheProvider.Get<T>(HandleCacheKey(cacheKey));
        }
        /// <summary>
        /// 获取指定键名的缓存数据（object 形式）。/ Get the cached data for the specified key as object.
        /// </summary>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        /// <returns>缓存数据；未命中时为 null / the cached data; null on miss</returns>
        public virtual object? Get(string cacheKey)
        {
            return MemoryCacheProvider.Get(HandleCacheKey(cacheKey));
        }

        /// <summary>
        /// 尝试获取指定键名的缓存数据。/ Try to get the cached data for the specified key.
        /// </summary>
        /// <typeparam name="T">数据类型 / data type</typeparam>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        /// <param name="data">命中的缓存数据；未命中时为 null / the cached data; null on miss</param>
        /// <returns>命中返回 true / true when the key is hit</returns>
        public virtual bool TryGet<T>(string cacheKey, out T? data)
        {
            return MemoryCacheProvider.TryGetValue<T>(HandleCacheKey(cacheKey), out data);
        }
        /// <summary>
        /// 尝试获取指定键名的缓存数据（object 形式）。/ Try to get the cached data for the specified key as object.
        /// </summary>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        /// <param name="data">命中的缓存数据；未命中时为 null / the cached data; null on miss</param>
        /// <returns>命中返回 true / true when the key is hit</returns>
        public virtual bool TryGet(string cacheKey, out object? data)
        {
            return MemoryCacheProvider.TryGetValue(HandleCacheKey(cacheKey), out data);
        }

        #endregion

        #region Set

        /// <summary>
        /// 写入缓存；未指定过期时间时使用 MemoryCacheConfig.DefaultExpirationSeconds（默认 1 小时）；data 为 null 时静默忽略。
        /// / Set a cache entry; uses MemoryCacheConfig.DefaultExpirationSeconds (default 1 hour) when no TTL is given; null data is silently ignored.
        /// </summary>
        /// <typeparam name="T">数据类型 / data type</typeparam>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        /// <param name="data">缓存数据 / data to cache</param>
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
        /// 写入缓存；未指定过期时间时使用 MemoryCacheConfig.DefaultExpirationSeconds（默认 1 小时）；data 为 null 时静默忽略。
        /// / Set a cache entry; uses MemoryCacheConfig.DefaultExpirationSeconds (default 1 hour) when no TTL is given; null data is silently ignored.
        /// </summary>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        /// <param name="data">缓存数据 / data to cache</param>
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
        /// 写入缓存并指定过期秒数；expiredSeconds 为 null 或 &lt;=0 时回落 DefaultExpirationSeconds；data 为 null 时静默忽略。
        /// / Set a cache entry with an explicit TTL in seconds; values &lt;= 0 fall back to DefaultExpirationSeconds; null data is silently ignored.
        /// </summary>
        /// <typeparam name="T">数据类型 / data type</typeparam>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        /// <param name="data">缓存数据 / data to cache</param>
        /// <param name="expiredSeconds">过期秒数 / TTL in seconds</param>
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
        /// 写入缓存并指定过期秒数；expiredSeconds &lt;=0 时回落 DefaultExpirationSeconds；data 为 null 时静默忽略。
        /// / Set a cache entry with an explicit TTL in seconds; values &lt;= 0 fall back to DefaultExpirationSeconds; null data is silently ignored.
        /// </summary>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        /// <param name="data">缓存数据 / data to cache</param>
        /// <param name="expiredSeconds">过期秒数 / TTL in seconds</param>
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
        /// 清除指定键名的缓存并同步移除键表记录。/ Remove the cache entry for the key and its key-table record.
        /// </summary>
        /// <param name="cacheKey">业务键（不带前缀）/ business key (without the prefix)</param>
        public virtual void Remove(string cacheKey)
        {
            MemoryCacheProvider.Remove(HandleCacheKey(cacheKey));

            CacheKeys.TryRemove(cacheKey, out _);
        }

        /// <summary>
        /// 清除键名匹配指定前缀的缓存（忽略大小写的前缀匹配，或整键相等），并同步移除对应键表记录。
        /// / Remove entries whose key case-insensitively starts with the prefix or equals it exactly, dropping their key-table records too.
        /// </summary>
        /// <param name="prefix">键名前缀（或完整键名）/ key prefix (or full key)</param>
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
        /// 清除所有缓存数据。/ Clear all cached data.
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
        /// 缓存提供者为全局共享，生命周期随进程，实例 Dispose 不释放（空操作）。
        /// / The provider is process-wide shared; instance Dispose never releases it (no-op).
        /// </summary>
        public virtual void Dispose()
        {
        }

    }
}
