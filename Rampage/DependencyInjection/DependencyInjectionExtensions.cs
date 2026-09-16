using Microsoft.Extensions.DependencyInjection;
using Rampage.Data.Client;
using Rampage.Reflection;
using Rampage.Utilities;
using System;

namespace Rampage.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {


        /// <summary>
        /// 单次枚举按实现类型全名查找服务，避免 Any+First/FirstOrDefault 双重枚举把全部实现实例化两遍
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="fullName"></param>
        /// <returns></returns>
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
        /// 单次枚举按实现类型全名查找服务（非泛型 GetServices(Type) 返回序列）
        /// </summary>
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
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceName"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
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
