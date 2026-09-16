using Microsoft.Extensions.DependencyInjection;
using Rampage.Reflection;
using System;
using System.Reflection;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 将 [Auto]/[Singleton] 成员解析并写入宿主实例。
    /// 支持 <c>Lazy&lt;T&gt;</c>：构造期不解析 T，首次 <c>.Value</c> 时再取服务（可打破互依赖环）。
    /// </summary>
    public class AutoInjecter : IDisposable
    {
        /// <summary>默认 true：解析失败抛 <see cref="AutoInjectException"/>。</summary>
        public static bool FailFast { get; set; } = true;

        protected IScopedServiceLocator ServiceLocator = default!;

        private AutoInjecter() { }

        public AutoInjecter(IScopedServiceLocator serviceLocator)
        {
            ServiceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
        }

        public void Build(object host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            Type type = host.GetType();
            AutoInjectionSchema? autoSchema = AutoInjectionSchemaStore.GetSchema(type);
            if (autoSchema?.AutoList == null || autoSchema.AutoList.Count == 0)
            {
                return;
            }

            foreach (Auto auto in autoSchema.AutoList)
            {
                Type? serviceType = null;
                string memberName;
                if (auto.Property != null)
                {
                    serviceType = auto.Property.PropertyType;
                    memberName = auto.Property.Name;
                }
                else if (auto.Field != null)
                {
                    serviceType = auto.Field.FieldType;
                    memberName = auto.Field.Name;
                }
                else
                {
                    continue;
                }

                if (serviceType == null)
                {
                    continue;
                }

                object? service;
                try
                {
                    service = Resolve(auto, serviceType);
                }
                catch (Exception ex)
                {
                    Fail(type, memberName, serviceType,
                        $"Failed resolving {serviceType.FullName} for {type.FullName}.{memberName}", ex);
                    continue;
                }

                if (service == null)
                {
                    Fail(type, memberName, serviceType,
                        $"No service registered for {serviceType.FullName} ({type.FullName}.{memberName}).");
                    continue;
                }

                if (auto.Property != null)
                {
                    EmitTool.SetPropertyValue(host, auto.Property.Name, service);
                }
                else if (auto.Field != null)
                {
                    EmitTool.SetFieldValue(host, auto.Field.Name, service);
                }
            }
        }

        private object? Resolve(Auto auto, Type serviceType)
        {
            if (AutoInjectCycleDetector.TryUnwrapLazy(serviceType, out Type elementType)
                && serviceType != elementType)
            {
                return CreateLazy(auto, elementType);
            }

            return ResolveCore(auto, serviceType);
        }

        private object CreateLazy(Auto auto, Type elementType)
        {
            MethodInfo method = typeof(AutoInjecter)
                .GetMethod(nameof(CreateLazyCore), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(elementType);
            return method.Invoke(this, new object[] { auto })!;
        }

        private Lazy<T> CreateLazyCore<T>(Auto auto)
        {
            return new Lazy<T>(() =>
            {
                object? resolved = ResolveCore(auto, typeof(T));
                if (resolved == null)
                {
                    Fail(typeof(T), auto.Property?.Name ?? auto.Field?.Name ?? "Lazy", typeof(T),
                        $"No service registered for {typeof(T).FullName} (Lazy<>).");
                    return default!;
                }

                return (T)resolved;
            });
        }

        private object? ResolveCore(Auto auto, Type serviceType)
        {
            string? serviceName = auto.Name;
            object? service = null;

            if (!string.IsNullOrEmpty(serviceName))
            {
                if (auto.Lifetime == ServiceLifetime.Singleton)
                {
                    service = SingletonServiceLocator.TryGetService(serviceName, serviceType);
                }
                else
                {
                    service = ServiceLocator.TryGet(serviceName, serviceType);
                }
            }
            else
            {
                if (auto.Lifetime == ServiceLifetime.Singleton)
                {
                    service = SingletonServiceLocator.TryGetService(serviceType);
                }
                else
                {
                    service = ServiceLocator.TryGet(serviceType);
                }
            }

            if (service != null)
            {
                return service;
            }

            var interfaceTypes = ReflectionTool.GetInterfaceList(serviceType);
            if (interfaceTypes == null || interfaceTypes.Count == 0)
            {
                return null;
            }

            foreach (var interfaceType in interfaceTypes)
            {
                if (!string.IsNullOrEmpty(serviceName))
                {
                    if (auto.Lifetime == ServiceLifetime.Singleton)
                    {
                        service = SingletonServiceLocator.TryGetService(serviceName, interfaceType);
                    }
                    else
                    {
                        service = ServiceLocator.TryGet(serviceName, interfaceType);
                    }
                }
                else
                {
                    if (auto.Lifetime == ServiceLifetime.Singleton)
                    {
                        service = SingletonServiceLocator.TryGetService(interfaceType);
                    }
                    else
                    {
                        service = ServiceLocator.TryGet(interfaceType);
                    }
                }

                if (service != null)
                {
                    return service;
                }
            }

            return null;
        }

        private static void Fail(Type hostType, string memberName, Type serviceType, string message, Exception? inner = null)
        {
            if (FailFast)
            {
                throw new AutoInjectException(hostType, memberName, serviceType, message, inner);
            }

            Rampage.Log.RampageLogger.Error(message, inner ?? new InvalidOperationException(message));
        }

        public void Dispose()
        {
        }
    }
}
