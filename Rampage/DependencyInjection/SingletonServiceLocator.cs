using Microsoft.Extensions.DependencyInjection;
using Rampage.Data.Client;
using Rampage.Reflection;
using Rampage.Utilities;
using System;

namespace Rampage.DependencyInjection
{

    /// <summary>
    /// 单例服务定位器：基于根容器（根 IServiceProvider）的静态服务解析入口 / Singleton service locator: static resolution entry point over the root container.
    /// </summary>
    /// <remarks>
    /// 供 [Singleton] 注入与无 Scope 场景使用；根容器由启动引导阶段通过 <see cref="SetProvider"/> 设置。
    /// Used by [Singleton] injection and scope-less scenarios; the root container is assigned during bootstrap via <see cref="SetProvider"/>.
    /// </remarks>
    public class SingletonServiceLocator
    {

        /// <summary>
        /// 根服务提供者 / The root service provider.
        /// </summary>
        public static IServiceProvider ServiceProvider
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 设置根服务提供者（启动引导阶段调用一次）/ Sets the root service provider (called once during bootstrap).
        /// </summary>
        /// <param name="serviceProvider">根服务提供者 / The root service provider.</param>
        public static void SetProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 从根容器获取指定类型的服务（优先条件注册实现）；不存在则抛出异常 / Gets a service of the specified type from the root container (conditionally-registered implementation first); throws when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例 / The service instance.</returns>
        public static T GetService<T>() where T : notnull
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
        /// 按服务名从根容器获取指定类型的服务；不存在则抛出异常 / Gets the named service of the specified type from the root container; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The service name is unregistered or no instance is found.</exception>
        public static T GetService<T>(string serviceName) where T : notnull
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
        /// 从根容器获取指定类型的全部服务实例列表 / Gets all registered instances of the specified type from the root container.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>实例列表 / The instance list.</returns>
        public static List<T> GetList<T>() where T : notnull
        {
            IEnumerable<T> ss = ServiceProvider.GetServices<T>();
            if (ss != null)
            {
                return ss.ToList();
            }
            return new List<T>();
        }

        /// <summary>
        /// 从根容器获取指定类型的服务（优先条件注册实现）；不存在则抛出异常 / Gets a service of the specified type from the root container (conditionally-registered implementation first); throws when not registered.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        public static object GetService(Type type)
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
        /// 按服务名从根容器获取服务：先按实现类型直取，失败再遍历其接口逐个匹配；找不到则抛出异常 / Gets a named service from the root container: direct by implementation type first, then matched through its interfaces; throws when not found.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The service name is unregistered or no instance is found.</exception>
        public static object GetService(string serviceName)
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
        /// 按服务名从根容器获取指定类型的服务；不存在则抛出异常 / Gets the named service of the specified type from the root container; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The service name is unregistered or no instance is found.</exception>
        public static object GetService(string serviceName, Type type)
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
        /// 从根容器获取指定类型的全部服务实例列表 / Gets all registered instances of the specified type from the root container.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>实例列表 / The instance list.</returns>
        public static List<object?> GetList(Type type)
        {
            IEnumerable<object?> ss = ServiceProvider.GetServices(type);
            if (ss != null)
            {
                return ss.ToList();
            }
            return new List<object?>();
        }

        /// <summary>
        /// 尝试从根容器获取指定类型的服务（优先条件注册实现）；不存在返回 null / Tries to get a service of the specified type from the root container (conditionally-registered implementation first); returns null when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public static T? TryGetService<T>() where T : notnull
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
        /// 尝试按服务名从根容器获取指定类型的服务；不存在返回 null / Tries to get the named service of the specified type from the root container; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public static T? TryGetService<T>(string serviceName)
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
        /// 尝试从根容器获取指定类型的服务（优先条件注册实现）；不存在返回 null / Tries to get a service of the specified type from the root container (conditionally-registered implementation first); returns null when not registered.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public static object? TryGetService(Type type)
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
        /// 尝试按服务名从根容器获取服务；不存在返回 null / Tries to get the named service from the root container; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public static object? TryGetService(string serviceName)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                return null;
            }

            return ServiceProvider.GetService(mappingType);
        }

        /// <summary>
        /// 尝试按服务名从根容器获取指定类型的服务；不存在返回 null / Tries to get the named service of the specified type from the root container; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        /// <remarks>
        /// 当 <paramref name="type"/> 可赋值为 <see cref="DatabaseClient"/> 时走名称前缀特判：
        /// 去掉服务名的 DatabaseClient 前缀得到数据库名，按实例的 <c>Name</c> 匹配，而非按实现类型全名匹配。
        /// When <paramref name="type"/> is assignable to <see cref="DatabaseClient"/>, a name-prefix special case applies:
        /// the DatabaseClient prefix is stripped from the service name and instances are matched by their <c>Name</c> instead of by implementation type full name.
        /// </remarks>
        public static object? TryGetService(string serviceName, Type type)
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

            // DatabaseClient 特判：服务名 = 前缀 + 数据库名，按实例 Name 匹配 / DatabaseClient special case: service name = prefix + database name, matched by instance Name
            if (typeof(DatabaseClient).IsAssignableFrom(type))
            {
                string dbName = serviceName.Substring(DatabaseClient.SERVICE_NAME_PREFIX.Length);
                foreach (var service in serviceList)
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
