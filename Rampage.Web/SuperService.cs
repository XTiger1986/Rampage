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

    public interface ISuperService
    {
        T? HandleWebCache<T>(Func<T> dataFunc, string cacheKeyTemplate, double? expiredSeconds = null, params object?[]? cacheKeyArgs);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ISuperService<M, D> : IBaseService<M, D>
        where M : class
        where D : ISuperDao<M>
    {
        T? HandleWebCache<T>(Func<T> dataFunc, string cacheKeyTemplate, double? expiredSeconds = null, params object?[]? cacheKeyArgs);
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class SuperService : BaseService, ISuperService
    {
        protected virtual HttpContext HttpContext => RequestContext.Current!;

        /// <summary>
        /// 
        /// </summary>
        [Auto]
        protected IContextCache ContextCache
        {
            get;
            set;
        } = default!;


        private TimeZoneInfo? _clientTimeZone;
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
        /// 
        /// </summary>
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
        /// 
        /// </summary>
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
        /// 
        /// </summary>
        /// <param name="cacheKeyTemplate"></param>
        /// <param name="cacheKeyArgs"></param>
        protected void RemoveWebCache(string cacheKeyTemplate, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });
            ContextCache.Remove(cacheKey);
            MemoryCache.Remove(cacheKey);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKeyPrefix"></param>
        protected void RemoveWebCachesByPrefix(string cacheKeyPrefix)
        {
            ContextCache.RemoveBySearch(cacheKeyPrefix);
            MemoryCache.RemoveBySearch(cacheKeyPrefix);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="M"></typeparam>
    /// <typeparam name="D"></typeparam>
    public abstract class SuperService<M, D> : BaseService<M, D>, ISuperService<M, D>
         where M : class
        where D : ISuperDao<M>
    {

        /// <summary>
        /// 
        /// </summary>
        public SuperService() : base()
        {
        }


        protected virtual HttpContext HttpContext => RequestContext.Current!;

        /// <summary>
        /// 
        /// </summary>
        [Auto]
        protected IContextCache ContextCache
        {
            get;
            set;
        } = default!;


        private TimeZoneInfo? _clientTimeZone;
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
        /// 客户端当前时间
        /// </summary>
        public virtual DateTime ClientNow
        {
            get
            {
                return DateTimeTool.UtcToLocal(UtcNow, ClientTimeZone);
            }
        }

        /// <summary>
        /// 
        /// </summary>
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
        /// 
        /// </summary>
        /// <param name="cacheKeyTemplate"></param>
        /// <param name="cacheKeyArgs"></param>
        protected void RemoveWebCache(string cacheKeyTemplate, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });
            ContextCache.Remove(cacheKey);
            MemoryCache.Remove(cacheKey);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKeyPrefix"></param>
        protected void RemoveWebCacheByPrefix(string cacheKeyPrefix)
        {
            ContextCache.RemoveBySearch(cacheKeyPrefix);
            MemoryCache.RemoveBySearch(cacheKeyPrefix);
        }

        /// <summary>
        /// 
        /// </summary>
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
        /// 
        /// </summary>
        /// <param name="cacheKeyTemplate"></param>
        /// <param name="cacheKeyArgs"></param>
        protected void RemoveContextCache(string cacheKeyTemplate, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });
            ContextCache.Remove(cacheKey);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKeyPrefix"></param>
        protected void RemoveContextCacheByPrefix(string cacheKeyPrefix)
        {
            ContextCache.RemoveBySearch(cacheKeyPrefix);
        }

    }


}
