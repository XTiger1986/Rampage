using Microsoft.AspNetCore.Http;
using Rampage.DependencyInjection;
using System;

namespace Rampage.Web.Extensions
{
    /// <summary>
    /// 当前 HTTP 请求上下文。IHttpContextAccessor 须惰性解析，避免静态字段在宿主重建后仍指向旧实例。
    /// </summary>
    public class RequestContext
    {
        /// <summary>
        /// 每次从根容器取 Singleton 的 IHttpContextAccessor（勿静态缓存，热重载/二次 Build 会换 Provider）。
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
        /// 当前请求 HttpContext；无请求时为 null。
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
