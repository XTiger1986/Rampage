using Microsoft.Extensions.DependencyInjection;
using Rampage.Data.Client;
using Rampage.Reflection;
using Rampage.Utilities;
using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// IServiceScope 服务解析扩展：支持条件注册实现优先与服务名定位 / Service resolution extensions on IServiceScope with conditional-implementation preference and service-name lookup.
    /// </summary>
    public static class DependencyInjectionExtensions
    {


        /// <summary>
        /// 单次枚举按实现类型全名查找服务，避免 Any+First 双重枚举把全部实现实例化两遍 / Finds a service by implementation type full name in a single enumeration, avoiding double enumeration that would instantiate every implementation twice.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <param name="services">服务实例序列 / The service instance sequence.</param>
        /// <param name="fullName">实现类型全名 / The implementation type full name.</param>
        /// <returns>匹配实例；未找到返回 default / The matched instance, or default when not found.</returns>
        public static T? FindServiceByFullName<T>(this IEnumerable<T> services, string? fullName)
        {
            if (fullName == null)
            {
                return default;
            }
            foreach (T service in services)
            {
                if (service != null && StringTool.Equals(service.GetType().FullName, fullName))
                {
                    return service;
                }
            }
            return default;
        }

        /// <summary>
        /// 单次枚举按实现类型全名查找服务（用于非泛型 GetServices(Type) 返回序列）/ Finds a service by implementation type full name in a single enumeration (for non-generic GetServices(Type) sequences).
        /// </summary>
        /// <param name="services">服务实例序列 / The service instance sequence.</param>
        /// <param name="fullName">实现类型全名 / The implementation type full name.</param>
        /// <returns>匹配实例；未找到返回 null / The matched instance, or null when not found.</returns>
        public static object? FindServiceByFullName(this IEnumerable<object?> services, string? fullName)
        {
            if (fullName == null)
            {
                return null;
            }
            foreach (object? service in services)
            {
                if (service != null && StringTool.Equals(service.GetType().FullName, fullName))
                {
                    return service;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取指定类型的服务：优先条件注册实现，否则回退 GetRequiredService / Gets a service of the specified type: the conditionally-registered implementation first, otherwise GetRequiredService.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <returns>服务实例 / The service instance.</returns>
        public static T GetService<T>(this IServiceScope scope) where T : notnull
        {
            Type baseType = typeof(T);
            Type? injectType = AutoInjectionSchemaStore.GetConditionalType(baseType);
            if (injectType != null)
            {
                T? tmpObj = scope.ServiceProvider.GetServices<T>().FindServiceByFullName(injectType.FullName);
                if (tmpObj != null)
                {
                    return tmpObj;
                }
            }
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// 按服务名获取指定类型的服务；不存在则抛出异常 / Gets the named service of the specified type; throws when not registered.
        /// </summary>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The service name is unregistered or no instance is found.</exception>
        public static T GetService<T>(this IServiceScope scope, string serviceName) where T : notnull
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                throw new ArgumentException($"Service '{serviceName}' does not exist!");
            }
            Type type = typeof(T);

            T? service = scope.ServiceProvider.GetServices<T>().FindServiceByFullName(mappingType.FullName);
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
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <returns>实例列表 / The instance list.</returns>
        public static List<T> GetServiceList<T>(this IServiceScope scope) where T : notnull
        {
            IEnumerable<T> ss = scope.ServiceProvider.GetServices<T>();
            if (ss != null)
            {
                return ss.ToList();
            }
            return new List<T>();
        }

        /// <summary>
        /// 获取指定类型的服务：优先条件注册实现，否则回退 GetRequiredService / Gets a service of the specified type: the conditionally-registered implementation first, otherwise GetRequiredService.
        /// </summary>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        public static object GetService(this IServiceScope scope, Type type)
        {
            Type? injectType = AutoInjectionSchemaStore.GetConditionalType(type);
            if (injectType != null)
            {
                object? tmpObj = scope.ServiceProvider.GetServices(type).FindServiceByFullName(injectType.FullName);
                if (tmpObj != null)
                {
                    return tmpObj;
                }
            }
            return scope.ServiceProvider.GetRequiredService(type);
        }

        /// <summary>
        /// 按服务名获取服务：先按实现类型直取，失败再遍历其接口逐个匹配；找不到则抛出异常 / Gets a named service: direct by implementation type first, then matched through its interfaces; throws when not found.
        /// </summary>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The service name is unregistered or no instance is found.</exception>
        public static object GetService(this IServiceScope scope, string serviceName)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                throw new ArgumentException($"Service '{serviceName}' does not exist!");
            }
            List<Type> interfaceTypeList = ReflectionTool.GetInterfaceList(mappingType);
            if (interfaceTypeList.Count == 0)
            {
                return scope.ServiceProvider.GetRequiredService(mappingType);
            }
            else
            {
                foreach (Type interfaceType in interfaceTypeList)
                {
                    object? serviceObj = scope.ServiceProvider.GetServices(interfaceType).FindServiceByFullName(mappingType.FullName);
                    if (serviceObj != null)
                    {
                        return serviceObj;
                    }
                }

                throw new ArgumentException($"Service '{serviceName}' does not exist!");
            }
        }

        /// <summary>
        /// 按服务名获取指定类型的服务；不存在则抛出异常 / Gets the named service of the specified type; throws when not registered.
        /// </summary>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The service name is unregistered or no instance is found.</exception>
        public static object GetService(this IServiceScope scope, string serviceName, Type type)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                throw new ArgumentException($"Service '{serviceName}' does not exist!");
            }

            object? serviceObj = scope.ServiceProvider.GetServices(type).FindServiceByFullName(mappingType.FullName);
            if (serviceObj == null)
            {
                throw new ArgumentException($"Service '{type.FullName}' does not exist!");
            }

            return serviceObj;
        }

        /// <summary>
        /// 获取指定类型的全部服务实例列表 / Gets all registered instances of the specified type.
        /// </summary>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>实例列表 / The instance list.</returns>
        public static List<object?> GetServiceList(this IServiceScope scope,Type type)
        {
            IEnumerable<object?> ss = scope.ServiceProvider.GetServices(type);
            if (ss != null)
            {
                return ss.ToList();
            }
            return new List<object?>();
        }

        /// <summary>
        /// 尝试获取指定类型的服务：优先条件注册实现，否则回退 GetService；不存在返回 null / Tries to get a service of the specified type: the conditionally-registered implementation first, otherwise GetService; returns null when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public static T? TryGetService<T>(this IServiceScope scope) where T : notnull
        {
            Type baseType = typeof(T);
            Type? injectType = AutoInjectionSchemaStore.GetConditionalType(baseType);
            if (injectType != null)
            {
                T? matched = scope.ServiceProvider.GetServices<T>().FindServiceByFullName(injectType.FullName);
                if (matched != null)
                {
                    return matched;
                }
            }
            return scope.ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// 尝试按服务名获取指定类型的服务；不存在返回 null / Tries to get the named service of the specified type; returns null when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public static T? TryGetService<T>(this IServiceScope scope, string serviceName)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                return default(T);
            }
            Type type = typeof(T);

            return scope.ServiceProvider.GetServices<T>().FindServiceByFullName(mappingType.FullName);
        }

        /// <summary>
        /// 尝试获取指定类型的服务：优先条件注册实现，否则回退 GetService；不存在返回 null / Tries to get a service of the specified type: the conditionally-registered implementation first, otherwise GetService; returns null when not registered.
        /// </summary>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public static object? TryGetService(this IServiceScope scope, Type type)
        {
            Type? injectType = AutoInjectionSchemaStore.GetConditionalType(type);
            if (injectType != null)
            {
                object? matchedObj = scope.ServiceProvider.GetServices(type).FindServiceByFullName(injectType.FullName);
                if (matchedObj != null)
                {
                    return matchedObj;
                }
            }

            return scope.ServiceProvider.GetService(type);
        }

        /// <summary>
        /// 尝试按服务名获取服务（直接按映射类型解析）；不存在返回 null / Tries to get the named service (resolved directly by its mapped type); returns null when not registered.
        /// </summary>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public static object? TryGetService(this IServiceScope scope, string serviceName)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                return null;
            }

            return scope.ServiceProvider.GetService(mappingType);
        }

        /// <summary>
        /// 尝试按服务名获取指定类型的服务；不存在返回 null / Tries to get the named service of the specified type; returns null when not registered.
        /// </summary>
        /// <param name="scope">服务作用域 / The service scope.</param>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        /// <remarks>
        /// 当 <paramref name="type"/> 可赋值为 <see cref="DatabaseClient"/> 时走名称前缀特判：
        /// 去掉服务名的 DatabaseClient 前缀得到数据库名，按实例的 <c>Name</c> 匹配，而非按实现类型全名匹配。
        /// When <paramref name="type"/> is assignable to <see cref="DatabaseClient"/>, a name-prefix special case applies:
        /// the DatabaseClient prefix is stripped from the service name and instances are matched by their <c>Name</c> instead of by implementation type full name.
        /// </remarks>
        public static object? TryGetService(this IServiceScope scope, string serviceName, Type type)
        {
            Type? mappingType = AutoInjectionSchemaStore.GetServiceType(serviceName);
            if (mappingType == null)
            {
                return null;
            }
            var services = scope.ServiceProvider.GetServices(type);

            if (services == null || !services.Any())
            {
                return null;
            }

            // DatabaseClient 特判：服务名 = 前缀 + 数据库名，按实例 Name 匹配 / DatabaseClient special case: service name = prefix + database name, matched by instance Name
            if (typeof(DatabaseClient).IsAssignableFrom(type))
            {
                string dbName = serviceName.Substring(DatabaseClient.SERVICE_NAME_PREFIX.Length);
                foreach (var service in services)
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
                return services.FindServiceByFullName(mappingType.FullName);
            }
        }

    }
}
