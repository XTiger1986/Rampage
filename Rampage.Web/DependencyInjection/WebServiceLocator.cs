using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Rampage.Data.Client;
using Rampage.DependencyInjection;
using Rampage.Reflection;
using Rampage.Utilities;
using System;

namespace Rampage.Web.DependencyInjection
{
    /// <summary>
    /// Web 作用域服务定位器：优先从当前请求的 RequestServices 解析，无请求时回退 RampageScope /
    /// Web scoped service locator: resolves from the current request's RequestServices first, falling back to RampageScope outside a request.
    /// </summary>
    /// <remarks>由 RampageWebApplication 注册为 IScopedServiceLocator 的 Scoped 实现；支持按类型（含条件注入）与按服务名解析。</remarks>
    public class WebServiceLocator : IScopedServiceLocator
    {
        /// <summary>
        /// 请求上下文访问器 / The HTTP context accessor.
        /// </summary>
        protected readonly IHttpContextAccessor _contextAccessor;

        /// <summary>
        /// 初始化服务定位器 / Initializes the service locator.
        /// </summary>
        /// <param name="contextAccessor">请求上下文访问器 / The HTTP context accessor.</param>
        public WebServiceLocator(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// 当前请求的 RequestServices；无 HttpContext 时用 RampageScope（不缓存、不回退 GLOBAL）/
        /// The current request's RequestServices; uses RampageScope when there is no HttpContext (never cached, never falls back to GLOBAL).
        /// </summary>
        /// <exception cref="InvalidOperationException">两者均不可用时抛出 / Thrown when neither is available.</exception>
        public IServiceProvider ServiceProvider
        {
            get
            {
                HttpContext? http = _contextAccessor.HttpContext;
                if (http != null)
                {
                    return http.RequestServices;
                }

                if (RampageScope.CurrentProvider != null)
                {
                    return RampageScope.CurrentProvider;
                }

                throw new InvalidOperationException(
                    "WebServiceLocator: HttpContext is null and RampageScope is not bound.");
            }
        }

        /// <summary>
        /// 获取指定类型的服务，不存在则抛出异常 / Gets the service of the specified type; throws when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例 / The service instance.</returns>
        /// <remarks>若该类型在 AutoInjectionSchemaStore 中登记了条件实现类型，则在全部注册实例中按实现类全名挑选；否则走 GetRequiredService。</remarks>
        public T Get<T>() where T : notnull
        {
            Type baseType = typeof(T);
            Type? injectType = AutoInjectionSchemaStore.GetConditionalType(baseType);
            if (injectType != null)
            {
                T? tmpObj = ServiceProvider.GetServices<T>().FindServiceByFullName(injectType.FullName);
                if (tmpObj != null)
                {
                    return tmpObj;
                }
            }
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// 获取指定名称的服务并转为指定类型，不存在则抛出异常 / Gets the named service cast to the specified type; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名（须已登记到 AutoInjectionSchemaStore）/ The service name (must be registered in AutoInjectionSchemaStore).</param>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The name is unknown or no matching instance exists.</exception>
        public T Get<T>(string serviceName) where T : notnull
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                throw new ArgumentException($"Service '{serviceName}' does not exist!");
            }
            Type type = typeof(T);

            T? service = ServiceProvider.GetServices<T>().FindServiceByFullName(mappingType.FullName);
            if (service == null)
            {
                throw new ArgumentException($"Service '{type.FullName}' does not exist!");
            }

            return service;
        }

        /// <summary>
        /// 获取指定类型的全部服务实例列表 / Gets all registered instances of the specified type.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>实例列表（无注册时为空列表）/ The instance list (empty when none registered).</returns>
        public List<T> GetList<T>() where T : notnull
        {
            IEnumerable<T> ss = ServiceProvider.GetServices<T>();
            if (ss != null)
            {
                return ss.ToList();
            }
            return new List<T>();
        }

        /// <summary>
        /// 获取指定类型的服务，不存在则抛出异常 / Gets the service of the specified type; throws when not registered.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        /// <remarks>条件注入逻辑同 <see cref="Get{T}()"/>：登记了条件实现类型时按实现类全名挑选，否则走 GetRequiredService。</remarks>
        public object Get(Type type)
        {
            Type? injectType = AutoInjectionSchemaStore.GetConditionalType(type);
            if (injectType != null)
            {
                object? tmpObj = ServiceProvider.GetServices(type).FindServiceByFullName(injectType.FullName);
                if (tmpObj != null)
                {
                    return tmpObj;
                }
            }
            return ServiceProvider.GetRequiredService(type);
        }

        /// <summary>
        /// 获取指定名称的服务，不存在则抛出异常 / Gets the named service; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名（须已登记到 AutoInjectionSchemaStore）/ The service name (must be registered in AutoInjectionSchemaStore).</param>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The name is unknown or no matching instance exists.</exception>
        /// <remarks>映射类型无接口时直接按映射类型解析；有接口时逐个接口在注册实例中按实现类全名匹配。</remarks>
        public object Get(string serviceName)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                throw new ArgumentException($"Service '{serviceName}' does not exist!");
            }
            List<Type> interfaceTypeList = ReflectionTool.GetInterfaceList(mappingType);
            if (interfaceTypeList.Count == 0)
            {
                return ServiceProvider.GetRequiredService(mappingType);
            }
            else
            {
                foreach (Type interfaceType in interfaceTypeList)
                {
                    object? serviceObj = ServiceProvider.GetServices(interfaceType).FindServiceByFullName(mappingType.FullName);
                    if (serviceObj != null)
                    {
                        return serviceObj;
                    }
                }

                throw new ArgumentException($"Service '{serviceName}' does not exist!");
            }
        }

        /// <summary>
        /// 获取指定名称的服务并转为指定类型，不存在则抛出异常 / Gets the named service cast to the specified type; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名（须已登记到 AutoInjectionSchemaStore）/ The service name (must be registered in AutoInjectionSchemaStore).</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The name is unknown or no matching instance exists.</exception>
        public object Get(string serviceName, Type type)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                throw new ArgumentException($"Service '{serviceName}' does not exist!");
            }

            object? serviceObj = ServiceProvider.GetServices(type).FindServiceByFullName(mappingType.FullName);
            if (serviceObj == null)
            {
                throw new ArgumentException($"Service '{type.FullName}' does not exist!");
            }

            return serviceObj;
        }

        /// <summary>
        /// 获取指定类型的全部服务实例列表 / Gets all registered instances of the specified type.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>实例列表（无注册时为空列表）/ The instance list (empty when none registered).</returns>
        public List<object?> GetList(Type type)
        {
            IEnumerable<object?> ss = ServiceProvider.GetServices(type);
            if (ss != null)
            {
                return ss.ToList();
            }
            return new List<object?>();
        }

        /// <summary>
        /// 尝试获取指定类型的服务，不存在返回 null / Tries to get the service of the specified type; returns null when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        /// <remarks>条件注入逻辑同 <see cref="Get{T}()"/>，仅找不到时不抛异常而返回 null。</remarks>
        public T? TryGet<T>() where T : notnull
        {
            Type baseType = typeof(T);
            Type? injectType = AutoInjectionSchemaStore.GetConditionalType(baseType);
            if (injectType != null)
            {
                T? matched = ServiceProvider.GetServices<T>().FindServiceByFullName(injectType.FullName);
                if (matched != null)
                {
                    return matched;
                }
            }
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// 尝试获取指定名称的服务并转为指定类型，不存在返回 null / Tries to get the named service cast to the specified type; returns null when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public T? TryGet<T>(string serviceName)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                return default(T);
            }
            Type type = typeof(T);

            return ServiceProvider.GetServices<T>().FindServiceByFullName(mappingType.FullName);
        }

        /// <summary>
        /// 尝试获取指定类型的服务，不存在返回 null / Tries to get the service of the specified type; returns null when not registered.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public object? TryGet(Type type)
        {
            Type? injectType = AutoInjectionSchemaStore.GetConditionalType(type);
            if (injectType != null)
            {
                object? matchedObj = ServiceProvider.GetServices(type).FindServiceByFullName(injectType.FullName);
                if (matchedObj != null)
                {
                    return matchedObj;
                }
            }

            return ServiceProvider.GetService(type);
        }

        /// <summary>
        /// 尝试获取指定名称的服务，不存在返回 null / Tries to get the named service; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public object? TryGet(string serviceName)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                return null ;
            }

            return ServiceProvider.GetService(mappingType);
        }

        /// <summary>
        /// 尝试获取指定名称的服务并转为指定类型，不存在返回 null / Tries to get the named service cast to the specified type; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        /// <remarks>
        /// type 为 DatabaseClient（或其子类）时按数据库名匹配：去掉服务名的 DatabaseClient 服务名前缀后，
        /// 在全部 DatabaseClient 实例中按 Name 相等（忽略大小写）挑选；其余类型按映射实现类全名匹配。
        /// </remarks>
        public object? TryGet(string serviceName, Type type)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                return null;
            }
            List<object?> serviceList = ServiceProvider.GetServices(type).ToList();
            if (serviceList.Count == 0)
            {
                return null;
            }

            if (typeof(DatabaseClient).IsAssignableFrom(type))
            {
                string dbName = serviceName.Substring(DatabaseClient.SERVICE_NAME_PREFIX.Length);
                foreach(var service in serviceList)
                {
                    var dcServ = service as DatabaseClient;
                    if (dcServ != null)
                    {
                        if (StringTool.Equals(dcServ.Name, dbName))
                        {
                            return dcServ;
                        }
                    }
                }
                return null;
            }
            else
            {
                return serviceList.FindServiceByFullName(mappingType.FullName);
            }
        }

    }
}
