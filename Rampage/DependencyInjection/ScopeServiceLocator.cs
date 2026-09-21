using Microsoft.Extensions.DependencyInjection;
using Rampage.Data.Client;
using Rampage.DependencyInjection;
using Rampage.Reflection;
using Rampage.Utilities;
using System;

namespace Rampage.Web.DependencyInjection
{
    /// <summary>
    /// 作用域服务定位器：包装一个 IServiceScope 的解析实现 / Scoped service locator wrapping an IServiceScope for resolution.
    /// </summary>
    public class ScopeServiceLocator : IScopedServiceLocator
    {

        /// <summary>
        /// 无参构造（DI 回退路径）：优先绑定当前环境 Scope，无环境 Scope 时退回全局 Scope / Parameterless constructor (DI fallback path): binds the ambient scope when present, otherwise falls back to the global scope.
        /// </summary>
        public ScopeServiceLocator()
        {
            // DI 回退路径优先绑定当前环境 Scope（RampageScope.Use/Begin 已绑定时），
            // 仅在无环境 Scope 时才使用全局 Scope，避免拿到错误作用域的解析器
            // The DI fallback path binds the ambient scope first (when RampageScope.Use/Begin is bound);
            // only when no ambient scope exists does it use the global scope, avoiding a resolver of the wrong scope
            IServiceProvider? ambientProvider = RampageScope.CurrentProvider;
            if (ambientProvider != null)
            {
                ServiceScope = new ProviderScopeAdapter(ambientProvider);
                ServiceProvider = ambientProvider;
                return;
            }
            ServiceScope = RampageBooter.Application.GetScope();
            ServiceProvider = ServiceScope.ServiceProvider;
        }

        /// <summary>
        /// 以指定 Scope 构造定位器 / Constructs the locator around the given scope.
        /// </summary>
        /// <param name="scope">待包装的服务作用域 / The service scope to wrap.</param>
        public ScopeServiceLocator(IServiceScope scope)
        {
            ServiceScope = scope;
            ServiceProvider = scope.ServiceProvider;
        }

        /// <summary>
        /// 包装的作用域 / The wrapped service scope.
        /// </summary>
        public IServiceScope ServiceScope
        {
            get; private set;
        }

        /// <summary>
        /// 当前作用域的服务提供者（请求场景即 HttpContext 的 RequestServices）/ The current scope's service provider (RequestServices in request scenarios).
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取指定类型的服务，不存在则抛出异常 / Gets the service of the specified type; throws when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例 / The service instance.</returns>
        public T Get<T>() where T : notnull
        {
            return ServiceScope.GetService<T>();
        }

        /// <summary>
        /// 获取指定名称的服务并转为指定类型，不存在则抛出异常 / Gets the named service cast to the specified type; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例 / The service instance.</returns>
        public T Get<T>(string serviceName) where T : notnull
        {
            return ServiceScope.GetService<T>(serviceName);
        }

        /// <summary>
        /// 获取指定类型的全部服务实例列表 / Gets all registered instances of the specified type.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>实例列表 / The instance list.</returns>
        public List<T> GetList<T>() where T : notnull
        {
            return ServiceScope.GetServiceList<T>();
        }

        /// <summary>
        /// 获取指定类型的服务，不存在则抛出异常 / Gets the service of the specified type; throws when not registered.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        public object Get(Type type)
        {
            return ServiceScope.GetService(type);
        }

        /// <summary>
        /// 获取指定名称的服务，不存在则抛出异常 / Gets the named service; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例 / The service instance.</returns>
        /// <exception cref="ArgumentException">服务名未登记或找不到对应实例 / The service name is unregistered or no instance is found.</exception>
        public object Get(string serviceName)
        {
            return ServiceScope.GetService(serviceName);
        }

        /// <summary>
        /// 获取指定名称的服务并转为指定类型，不存在则抛出异常 / Gets the named service cast to the specified type; throws when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例 / The service instance.</returns>
        public object Get(string serviceName, Type type)
        {
            return ServiceScope.GetService(serviceName, type);
        }

        /// <summary>
        /// 获取指定类型的全部服务实例列表 / Gets all registered instances of the specified type.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>实例列表 / The instance list.</returns>
        public List<object?> GetList(Type type)
        {
            return ServiceScope.GetServiceList(type);
        }

        /// <summary>
        /// 尝试获取指定类型的服务，不存在返回 null / Tries to get the service of the specified type; returns null when not registered.
        /// </summary>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public T? TryGet<T>() where T : notnull
        {
            return ServiceScope.TryGetService<T>();
        }

        /// <summary>
        /// 尝试获取指定名称的服务并转为指定类型，不存在返回 null / Tries to get the named service cast to the specified type; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <typeparam name="T">服务类型 / The service type.</typeparam>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public T? TryGet<T>(string serviceName)
        {
            return ServiceScope.TryGetService<T>(serviceName);
        }

        /// <summary>
        /// 尝试获取指定类型的服务，不存在返回 null / Tries to get the service of the specified type; returns null when not registered.
        /// </summary>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public object? TryGet(Type type)
        {
            return ServiceScope.TryGetService(type);
        }

        /// <summary>
        /// 尝试获取指定名称的服务，不存在返回 null / Tries to get the named service; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public object? TryGet(string serviceName)
        {
            return ServiceScope.TryGetService(serviceName);
        }

        /// <summary>
        /// 尝试获取指定名称的服务并转为指定类型，不存在返回 null / Tries to get the named service cast to the specified type; returns null when not registered.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="type">服务类型 / The service type.</param>
        /// <returns>服务实例或 null / The service instance or null.</returns>
        public object? TryGet(string serviceName, Type type)
        {
            return ServiceScope.TryGetService(serviceName, type);
        }

        /// <summary>
        /// 包装已有 IServiceProvider 的最小 IServiceScope 适配器（不持有、不释放任何资源）/ Minimal IServiceScope adapter over an existing IServiceProvider (owns and disposes nothing).
        /// </summary>
        private sealed class ProviderScopeAdapter : IServiceScope
        {
            public ProviderScopeAdapter(IServiceProvider provider)
            {
                ServiceProvider = provider;
            }

            public IServiceProvider ServiceProvider { get; }

            public void Dispose()
            {
            }
        }

    }
}
