using Microsoft.Extensions.DependencyInjection;
using Rampage.AppLog;
using Rampage.Reflection;
using System;
using System.Reflection;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 自动注入执行器：将宿主实例上 [Auto]/[Singleton] 标注的属性/字段解析并写入 / Auto-injection executor that resolves and assigns [Auto]/[Singleton]-attributed properties/fields on a host instance.
    /// </summary>
    /// <remarks>
    /// 支持 <c>Lazy&lt;T&gt;</c> 成员：构造期不解析 T，首次访问 <c>.Value</c> 时才从定位器取服务（可打破互依赖环）。
    /// <c>Lazy&lt;T&gt;</c> members are supported: T is not resolved at build time but on first <c>.Value</c> access, which can break mutual-dependency cycles.
    /// 解析顺序：先按成员类型（含 [Singleton] 时走根容器），失败后按该类型实现的各接口逐一回退尝试。
    /// Resolution order: first by member type (root container for [Singleton]), then falls back through each interface implemented by the type.
    /// </remarks>
    public class AutoInjecter : IDisposable
    {
        /// <summary>
        /// 快速失败开关，默认 true：解析失败抛 <see cref="AutoInjectException"/>；false 时仅记录错误日志 / Fail-fast switch, true by default: resolution failures throw <see cref="AutoInjectException"/>; when false they are only logged.
        /// </summary>
        public static bool FailFast { get; set; } = true;

        /// <summary>构造期绑定的作用域服务定位器 / The scoped service locator bound at construction time.</summary>
        protected IScopedServiceLocator ServiceLocator = default!;

        private AutoInjecter() { }

        /// <summary>
        /// 以指定定位器创建注入器 / Creates the injecter with the given locator.
        /// </summary>
        /// <param name="serviceLocator">作用域服务定位器 / The scoped service locator.</param>
        /// <exception cref="ArgumentNullException">serviceLocator 为 null / serviceLocator is null.</exception>
        public AutoInjecter(IScopedServiceLocator serviceLocator)
        {
            ServiceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
        }

        /// <summary>
        /// 对宿主实例执行属性/字段注入 / Performs property/field injection on the host instance.
        /// </summary>
        /// <param name="host">待注入的宿主实例 / The host instance to inject.</param>
        /// <exception cref="ArgumentNullException">host 为 null / host is null.</exception>
        /// <exception cref="AutoInjectException">FailFast 开启且某成员解析失败 / FailFast is on and a member failed to resolve.</exception>
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

        /// <summary>按成员类型解析：Lazy&lt;T&gt; 包装为延迟解析，其余立即解析 / Resolves by member type: Lazy&lt;T&gt; is wrapped for deferred resolution, everything else resolves eagerly.</summary>
        private object? Resolve(Auto auto, Type serviceType)
        {
            if (AutoInjectCycleDetector.TryUnwrapLazy(serviceType, out Type elementType)
                && serviceType != elementType)
            {
                return CreateLazy(auto, elementType);
            }

            return ResolveCore(auto, serviceType);
        }

        /// <summary>通过反射调用泛型 CreateLazyCore 构造 Lazy&lt;T&gt;（编译期元素类型未知）/ Builds Lazy&lt;T&gt; by reflectively invoking the generic CreateLazyCore (element type unknown at compile time).</summary>
        private object CreateLazy(Auto auto, Type elementType)
        {
            MethodInfo method = typeof(AutoInjecter)
                .GetMethod(nameof(CreateLazyCore), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(elementType);
            return method.Invoke(this, new object[] { auto })!;
        }

        /// <summary>Lazy 工厂：推迟到首次 .Value 才解析，打破构造期互相依赖 / Lazy factory: defers resolution to first .Value access to break construction-time mutual dependencies.</summary>
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

        /// <summary>
        /// 核心解析：先按 [Auto] 名称（若有）解析，再按类型解析；均失败时按该类型实现的接口逐一回退 / Core resolution: by [Auto] name (if any) first, then by type; falls back through the type's implemented interfaces when both fail.
        /// </summary>
        /// <param name="auto">注入特性实例 / The injection attribute instance.</param>
        /// <param name="serviceType">目标服务类型 / The target service type.</param>
        /// <returns>解析到的实例，失败返回 null / The resolved instance, or null on failure.</returns>
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

            // 类型直查失败 → 接口回退解析链：按成员类型实现的各接口逐个尝试
            // Direct type lookup failed -> interface fallback chain: try each interface implemented by the member type
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

        /// <summary>失败出口：FailFast 开启抛 <see cref="AutoInjectException"/>，否则仅记录错误日志 / Failure sink: throws <see cref="AutoInjectException"/> when FailFast is on, otherwise only logs the error.</summary>
        private static void Fail(Type hostType, string memberName, Type serviceType, string message, Exception? inner = null)
        {
            if (FailFast)
            {
                throw new AutoInjectException(hostType, memberName, serviceType, message, inner);
            }

            RampageLogger.Error(message, inner ?? new InvalidOperationException(message));
        }

        /// <summary>
        /// 释放资源（当前为空实现，仅为满足 IDisposable）/ Disposes resources (currently a no-op kept only to satisfy IDisposable).
        /// </summary>
        public void Dispose()
        {
        }
    }
}
