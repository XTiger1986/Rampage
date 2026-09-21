using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 依赖自动注入特性（Singleton 解析，从根容器取服务）/ Dependency auto-injection attribute (Singleton resolution from the root container).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class Singleton : Auto
    {

        /// <summary>初始化不指定服务名的单例注入（按成员类型解析）/ Initializes singleton injection without a service name (resolved by member type).</summary>
        public Singleton() : base() { }

        /// <summary>初始化指定服务名的单例注入 / Initializes singleton injection with a service name.</summary>
        /// <param name="name">注入服务名 / The service name to resolve.</param>
        public Singleton(string name) : base(name) { }

        /// <summary>
        /// 解析生命周期，固定为 Singleton（覆盖基类默认的 Scoped）/ The resolution lifetime, fixed to Singleton (overriding the base Scoped default).
        /// </summary>
        protected internal override ServiceLifetime Lifetime
        {
            get { return ServiceLifetime.Singleton; }
        }
    }
}
