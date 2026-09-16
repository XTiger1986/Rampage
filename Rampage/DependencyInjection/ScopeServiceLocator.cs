using Microsoft.Extensions.DependencyInjection;
using Rampage.Data.Client;
using Rampage.DependencyInjection;
using Rampage.Reflection;
using Rampage.Utilities;
using System;

namespace Rampage.Web.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public class ScopeServiceLocator : IScopedServiceLocator
    {

        public ScopeServiceLocator()
        {
            // DI 回退路径优先绑定当前环境 Scope（RampageScope.Use/Begin 已绑定时），
            // 仅在无环境 Scope 时才使用全局 Scope，避免拿到错误作用域的解析器
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

        public ScopeServiceLocator(IServiceScope scope)
        {
            ServiceScope = scope;
            ServiceProvider = scope.ServiceProvider;
        }

        public IServiceScope ServiceScope
        {
            get; private set;
        }

        /// <summary>
        /// HttpContext ServiceProvider
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>() where T : notnull
        {
            return ServiceScope.GetService<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>(string serviceName) where T : notnull
        {
            return ServiceScope.GetService<T>(serviceName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetList<T>() where T : notnull
        {
            return ServiceScope.GetServiceList<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Get(Type type)
        {
            return ServiceScope.GetService(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public object Get(string serviceName)
        {
            return ServiceScope.GetService(serviceName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Get(string serviceName, Type type)
        {
            return ServiceScope.GetService(serviceName, type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<object?> GetList(Type type)
        {
            return ServiceScope.GetServiceList(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? TryGet<T>() where T : notnull
        {
            return ServiceScope.TryGetService<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public T? TryGet<T>(string serviceName)
        {
            return ServiceScope.TryGetService<T>(serviceName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object? TryGet(Type type)
        {
            return ServiceScope.TryGetService(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public object? TryGet(string serviceName)
        {
            return ServiceScope.TryGetService(serviceName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object? TryGet(string serviceName, Type type)
        {
            return ServiceScope.TryGetService(serviceName, type);
        }

        /// <summary>
        /// 包装已有 IServiceProvider 的最小 IServiceScope 适配器（不持有、不释放任何资源）
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
