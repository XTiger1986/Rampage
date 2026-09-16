using Microsoft.Extensions.DependencyInjection;
using Rampage.Data.Client;
using Rampage.Reflection;
using Rampage.Utilities;
using System;

namespace Rampage.DependencyInjection
{

    /// <summary>
    /// 
    /// </summary>
    public class SingletonServiceLocator
    {

        /// <summary>
        /// 
        /// </summary>
        public static IServiceProvider ServiceProvider
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void SetProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceName"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
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
