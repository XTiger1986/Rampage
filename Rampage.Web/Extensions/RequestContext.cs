using Microsoft.AspNetCore.Http;
using Rampage.DependencyInjection;
using System;

namespace Rampage.Web.Extensions
{
    /// <summary>
    /// 当前 HTTP 请求上下文入口（静态便捷访问）。IHttpContextAccessor 须惰性解析，避免静态字段在宿主重建后仍指向旧实例 / Entry point for the current HTTP request context (static convenience). IHttpContextAccessor must be resolved lazily so static fields never outlive a host rebuild.
    /// </summary>
    public class RequestContext
    {
        /// <summary>
        /// 每次从根容器取 Singleton 的 IHttpContextAccessor（勿静态缓存，热重载/二次 Build 会换 Provider）/ Resolves the singleton IHttpContextAccessor from the root container each time (do not cache statically; hot reload / a second Build swaps the Provider).
        /// </summary>
        public static IHttpContextAccessor? ContextAccessor
        {
            get
            {
                try
                {
                    if (SingletonServiceLocator.ServiceProvider == null)
                    {
                        return null;
                    }
                    return SingletonServiceLocator.TryGetService<IHttpContextAccessor>();
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 当前请求 HttpContext；无请求时为 null / The HttpContext of the current request; null outside a request.
        /// </summary>
        public static HttpContext? Current
        {
            get
            {
                IHttpContextAccessor? accessor = ContextAccessor;
                return accessor?.HttpContext;
            }
        }
    }
}
