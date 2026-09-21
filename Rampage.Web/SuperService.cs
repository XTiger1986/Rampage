using Microsoft.AspNetCore.Http;
using Rampage.DependencyInjection;
using Rampage.Hierarchy;
using Rampage.Utilities;
using Rampage.Web.Caching;
using Rampage.Web.Extensions;
using Rampage.Web.TimeZone;
using System;

namespace Rampage.Web
{

    /// <summary>
    /// Web 服务基础接口：提供两级缓存（请求级 + 进程级）的数据读取 / Base interface for web services: provides two-level (per-request + in-process) cached data reads
    /// </summary>
    public interface ISuperService
    {
        /// <summary>
        /// 按缓存键模板读取数据：请求级缓存未命中时回溯进程级缓存 / Reads data via the cache key template: falls back to the in-process cache when the per-request cache misses
        /// </summary>
        /// <typeparam name="T">数据类型 / The data type</typeparam>
        /// <param name="dataFunc">缓存未命中时产生数据的委托 / Delegate that produces the data on a cache miss</param>
        /// <param name="cacheKeyTemplate">缓存键模板（string.Format 复合格式）/ Cache key template (string.Format composite format)</param>
        /// <param name="expiredSeconds">进程级缓存过期秒数；null 使用框架默认值 / In-process cache lifetime in seconds; null uses the framework default</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / Arguments for the cache key template</param>
        /// <returns>缓存或新产生的数据 / The cached or newly produced data</returns>
        T? HandleWebCache<T>(Func<T> dataFunc, string cacheKeyTemplate, double? expiredSeconds = null, params object?[]? cacheKeyArgs);
    }

    /// <summary>
    /// 带模型与 DAO 类型的 Web 服务接口 / Web service interface with model and DAO generic types
    /// </summary>
    /// <typeparam name="M">业务模型类型 / The business model type</typeparam>
    /// <typeparam name="D">数据访问对象类型 / The DAO type</typeparam>
    public interface ISuperService<M, D> : IBaseService<M, D>
        where M : class
        where D : ISuperDao<M>
    {
        /// <summary>
        /// 按缓存键模板读取数据：请求级缓存未命中时回溯进程级缓存 / Reads data via the cache key template: falls back to the in-process cache when the per-request cache misses
        /// </summary>
        /// <typeparam name="T">数据类型 / The data type</typeparam>
        /// <param name="dataFunc">缓存未命中时产生数据的委托 / Delegate that produces the data on a cache miss</param>
        /// <param name="cacheKeyTemplate">缓存键模板（string.Format 复合格式）/ Cache key template (string.Format composite format)</param>
        /// <param name="expiredSeconds">进程级缓存过期秒数；null 使用框架默认值 / In-process cache lifetime in seconds; null uses the framework default</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / Arguments for the cache key template</param>
        /// <returns>缓存或新产生的数据 / The cached or newly produced data</returns>
        T? HandleWebCache<T>(Func<T> dataFunc, string cacheKeyTemplate, double? expiredSeconds = null, params object?[]? cacheKeyArgs);
    }

    /// <summary>
    /// Web 业务服务基类：在 BaseService 之上提供 HttpContext、客户端时区与两级缓存等 Web 能力 / Base class for web services: adds HttpContext, client time zone and two-level caching on top of BaseService
    /// </summary>
    public abstract class SuperService : BaseService, ISuperService
    {
        /// <summary>
        /// 当前请求的 HttpContext（经 RequestContext 获取，非 Web 环境为 null）/ The HttpContext of the current request (via RequestContext; null outside a web environment)
        /// </summary>
        protected virtual HttpContext HttpContext => RequestContext.Current!;

        /// <summary>
        /// 请求级缓存（基于 HttpContext.Items），由 [Auto] 自动注入 / Per-request cache (backed by HttpContext.Items), injected automatically via [Auto]
        /// </summary>
        [Auto]
        protected IContextCache ContextCache
        {
            get;
            set;
        } = default!;


        private TimeZoneInfo? _clientTimeZone;
        /// <summary>
        /// 客户端时区：优先取 TimeZoneContext 从请求头解析的结果，解析失败回退服务器本地时区 / Client time zone: resolved by TimeZoneContext from the request header, falling back to the server local time zone
        /// </summary>
        protected virtual TimeZoneInfo ClientTimeZone
        {
            get
            {
                if (_clientTimeZone == null)
                {
                    _clientTimeZone = TimeZoneContext.GetClientTimeZone();
                }
                if (_clientTimeZone == null)
                {
                    _clientTimeZone = TimeZoneInfo.Local;
                }
                return _clientTimeZone;
            }
            set
            {
                _clientTimeZone = value;
            }
        }

        /// <summary>
        /// 按缓存键模板读取数据（两级缓存） / Reads data via the cache key template (two-level cache)
        /// </summary>
        /// <remarks>
        /// 先查请求级缓存（ContextCache / HttpContext.Items），未命中时调用基类 HandleCache（进程级 MemoryCache），
        /// 命中后写回请求级缓存；同一请求内多次读取只穿透一次。
        /// The per-request cache (ContextCache / HttpContext.Items) is checked first; a miss falls through to the base HandleCache
        /// (in-process MemoryCache) and the result is written back to the per-request cache, so only the first read in a request goes through.
        /// </remarks>
        /// <typeparam name="T">数据类型 / The data type</typeparam>
        /// <param name="dataFunc">缓存未命中时产生数据的委托 / Delegate that produces the data on a cache miss</param>
        /// <param name="cacheKeyTemplate">缓存键模板（string.Format 复合格式）/ Cache key template (string.Format composite format)</param>
        /// <param name="expiredSeconds">进程级缓存过期秒数；null 使用框架默认值 / In-process cache lifetime in seconds; null uses the framework default</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / Arguments for the cache key template</param>
        /// <returns>缓存或新产生的数据 / The cached or newly produced data</returns>
        public T? HandleWebCache<T>(Func<T> dataFunc, string cacheKeyTemplate, double? expiredSeconds = null, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });

            T? result;
            if (!ContextCache.TryGetValue<T>(cacheKey, out result))
            {
                result = base.HandleCache<T>(dataFunc, cacheKeyTemplate, expiredSeconds, cacheKeyArgs);
                if (result != null)
                {
                    ContextCache.Set(cacheKey, result);
                }
            }
            return result;
        }

        /// <summary>
        /// 按缓存键模板读取数据（仅请求级缓存） / Reads data via the cache key template (per-request cache only)
        /// </summary>
        /// <remarks>仅在当前请求内缓存（ContextCache），不写进程级缓存，适合请求内临时复用的中间数据 / Cached only within the current request (ContextCache), never in the in-process cache; suited to intermediate data reused within one request</remarks>
        /// <typeparam name="T">数据类型 / The data type</typeparam>
        /// <param name="dataFunc">缓存未命中时产生数据的委托 / Delegate that produces the data on a cache miss</param>
        /// <param name="cacheKeyTemplate">缓存键模板（string.Format 复合格式）/ Cache key template (string.Format composite format)</param>
        /// <param name="expiredSeconds">当前实现忽略该参数（请求级缓存随请求结束销毁）/ Ignored by the current implementation (the per-request cache dies with the request)</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / Arguments for the cache key template</param>
        /// <returns>缓存或新产生的数据 / The cached or newly produced data</returns>
        protected T? HandleContextCache<T>(Func<T> dataFunc, string cacheKeyTemplate, double? expiredSeconds = null, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });

            T? result;
            if (!ContextCache.TryGetValue<T>(cacheKey, out result))
            {
                result = dataFunc();
                if (result != null)
                {
                    ContextCache.Set(cacheKey, result);
                }
            }
            return result;
        }
        /// <summary>
        /// 清除指定键的两级缓存（请求级 + 进程级） / Removes the key from both cache levels (per-request + in-process)
        /// </summary>
        /// <param name="cacheKeyTemplate">缓存键模板（string.Format 复合格式）/ Cache key template (string.Format composite format)</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / Arguments for the cache key template</param>
        protected void RemoveWebCache(string cacheKeyTemplate, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });
            ContextCache.Remove(cacheKey);
            MemoryCache.Remove(cacheKey);
        }
        /// <summary>
        /// 按前缀清除两层缓存（请求级 + 进程级） / Removes entries with the key prefix from both cache levels (per-request + in-process)
        /// </summary>
        /// <param name="cacheKeyPrefix">缓存键前缀 / The cache key prefix</param>
        protected void RemoveWebCachesByPrefix(string cacheKeyPrefix)
        {
            ContextCache.RemoveBySearch(cacheKeyPrefix);
            MemoryCache.RemoveBySearch(cacheKeyPrefix);
        }

    }

    /// <summary>
    /// 带模型与 DAO 类型的 Web 业务服务基类：在 BaseService&lt;M, D&gt; 之上提供 HttpContext、客户端时区与两级缓存等 Web 能力 / Generic base class for web services: adds HttpContext, client time zone and two-level caching on top of BaseService&lt;M, D&gt;
    /// </summary>
    /// <typeparam name="M">业务模型类型 / The business model type</typeparam>
    /// <typeparam name="D">数据访问对象类型 / The DAO type</typeparam>
    public abstract class SuperService<M, D> : BaseService<M, D>, ISuperService<M, D>
         where M : class
        where D : ISuperDao<M>
    {

        /// <summary>
        /// 构造函数 / Constructor
        /// </summary>
        public SuperService() : base()
        {
        }


        /// <summary>
        /// 当前请求的 HttpContext（经 RequestContext 获取，非 Web 环境为 null）/ The HttpContext of the current request (via RequestContext; null outside a web environment)
        /// </summary>
        protected virtual HttpContext HttpContext => RequestContext.Current!;

        /// <summary>
        /// 请求级缓存（基于 HttpContext.Items），由 [Auto] 自动注入 / Per-request cache (backed by HttpContext.Items), injected automatically via [Auto]
        /// </summary>
        [Auto]
        protected IContextCache ContextCache
        {
            get;
            set;
        } = default!;


        private TimeZoneInfo? _clientTimeZone;
        /// <summary>
        /// 客户端时区：优先取 TimeZoneContext 从请求头解析的结果，解析失败回退服务器本地时区 / Client time zone: resolved by TimeZoneContext from the request header, falling back to the server local time zone
        /// </summary>
        protected virtual TimeZoneInfo ClientTimeZone
        {
            get
            {
                if (_clientTimeZone == null)
                {
                    _clientTimeZone = TimeZoneContext.GetClientTimeZone();
                    if (_clientTimeZone == null)
                    {
                        _clientTimeZone = TimeZoneInfo.Local;
                    }
                }
                return _clientTimeZone;
            }
            set
            {
                _clientTimeZone = value;
            }
        }

        /// <summary>
        /// 客户端当前时间（按客户端时区换算 UtcNow）/ The client's current time (UtcNow converted to the client time zone)
        /// </summary>
        public virtual DateTime ClientNow
        {
            get
            {
                return DateTimeTool.UtcToLocal(UtcNow, ClientTimeZone);
            }
        }

        /// <summary>
        /// 按缓存键模板读取数据（两级缓存） / Reads data via the cache key template (two-level cache)
        /// </summary>
        /// <remarks>
        /// 先查请求级缓存（ContextCache / HttpContext.Items），未命中时调用基类 HandleCache（进程级 MemoryCache），
        /// 命中后写回请求级缓存；同一请求内多次读取只穿透一次。
        /// The per-request cache (ContextCache / HttpContext.Items) is checked first; a miss falls through to the base HandleCache
        /// (in-process MemoryCache) and the result is written back to the per-request cache, so only the first read in a request goes through.
        /// </remarks>
        /// <typeparam name="T">数据类型 / The data type</typeparam>
        /// <param name="dataFunc">缓存未命中时产生数据的委托 / Delegate that produces the data on a cache miss</param>
        /// <param name="cacheKeyTemplate">缓存键模板（string.Format 复合格式）/ Cache key template (string.Format composite format)</param>
        /// <param name="expiredSeconds">进程级缓存过期秒数；null 使用框架默认值 / In-process cache lifetime in seconds; null uses the framework default</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / Arguments for the cache key template</param>
        /// <returns>缓存或新产生的数据 / The cached or newly produced data</returns>
        public T? HandleWebCache<T>(Func<T> dataFunc, string cacheKeyTemplate, double? expiredSeconds = null, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });

            T? result;
            if (!ContextCache.TryGetValue<T>(cacheKey, out result))
            {
                result = base.HandleCache<T>(dataFunc, cacheKeyTemplate, expiredSeconds, cacheKeyArgs);
                if (result != null)
                {
                    ContextCache.Set(cacheKey, result);
                }
            }
            return result;
        }
        /// <summary>
        /// 清除指定键的两级缓存（请求级 + 进程级） / Removes the key from both cache levels (per-request + in-process)
        /// </summary>
        /// <param name="cacheKeyTemplate">缓存键模板（string.Format 复合格式）/ Cache key template (string.Format composite format)</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / Arguments for the cache key template</param>
        protected void RemoveWebCache(string cacheKeyTemplate, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });
            ContextCache.Remove(cacheKey);
            MemoryCache.Remove(cacheKey);
        }
        /// <summary>
        /// 按前缀清除两层缓存（请求级 + 进程级） / Removes entries with the key prefix from both cache levels (per-request + in-process)
        /// </summary>
        /// <param name="cacheKeyPrefix">缓存键前缀 / The cache key prefix</param>
        protected void RemoveWebCacheByPrefix(string cacheKeyPrefix)
        {
            ContextCache.RemoveBySearch(cacheKeyPrefix);
            MemoryCache.RemoveBySearch(cacheKeyPrefix);
        }

        /// <summary>
        /// 按缓存键模板读取数据（仅请求级缓存） / Reads data via the cache key template (per-request cache only)
        /// </summary>
        /// <remarks>仅在当前请求内缓存（ContextCache），不写进程级缓存，适合请求内临时复用的中间数据 / Cached only within the current request (ContextCache), never in the in-process cache; suited to intermediate data reused within one request</remarks>
        /// <typeparam name="T">数据类型 / The data type</typeparam>
        /// <param name="dataFunc">缓存未命中时产生数据的委托 / Delegate that produces the data on a cache miss</param>
        /// <param name="cacheKeyTemplate">缓存键模板（string.Format 复合格式）/ Cache key template (string.Format composite format)</param>
        /// <param name="expiredSeconds">当前实现忽略该参数（请求级缓存随请求结束销毁）/ Ignored by the current implementation (the per-request cache dies with the request)</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / Arguments for the cache key template</param>
        /// <returns>缓存或新产生的数据 / The cached or newly produced data</returns>
        protected T? HandleContextCache<T>(Func<T> dataFunc, string cacheKeyTemplate, double? expiredSeconds = null, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });

            T? result;
            if (!ContextCache.TryGetValue<T>(cacheKey, out result))
            {
                result = dataFunc();
                if (result != null)
                {
                    ContextCache.Set(cacheKey, result);
                }
            }
            return result;
        }
        /// <summary>
        /// 清除请求级缓存中的指定键（不影响进程级缓存） / Removes the key from the per-request cache only (the in-process cache is untouched)
        /// </summary>
        /// <param name="cacheKeyTemplate">缓存键模板（string.Format 复合格式）/ Cache key template (string.Format composite format)</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / Arguments for the cache key template</param>
        protected void RemoveContextCache(string cacheKeyTemplate, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });
            ContextCache.Remove(cacheKey);
        }
        /// <summary>
        /// 按前缀清除请求级缓存（不影响进程级缓存） / Removes per-request cache entries with the key prefix (the in-process cache is untouched)
        /// </summary>
        /// <param name="cacheKeyPrefix">缓存键前缀 / The cache key prefix</param>
        protected void RemoveContextCacheByPrefix(string cacheKeyPrefix)
        {
            ContextCache.RemoveBySearch(cacheKeyPrefix);
        }

    }


}
