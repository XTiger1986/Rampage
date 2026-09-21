using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 依赖自动注入特性（Scoped 解析）/ Dependency auto-injection attribute (Scoped resolution).
    /// </summary>
    /// <remarks>
    /// 成员类型可为 T 或 <c>Lazy&lt;T&gt;</c>：后者延迟到首次访问 <c>.Value</c> 时才解析，用于打破互相依赖（注入环）。
    /// The member type may be T or <c>Lazy&lt;T&gt;</c>; the latter defers resolution to first <c>.Value</c> access and is used to break mutual-dependency cycles.
    /// 禁止两个服务用立即解析的 [Auto] 互相注入——启动期环检测会直接失败。
    /// Two services must not eagerly inject each other via [Auto]; the startup cycle detector fails fast on such cycles.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class Auto : Attribute
    {

        /// <summary>初始化不指定服务名的自动注入（按成员类型解析）/ Initializes auto-injection without a service name (resolved by member type).</summary>
        public Auto() { }

        /// <summary>初始化指定服务名的自动注入 / Initializes auto-injection with a service name.</summary>
        /// <param name="name">注入服务名 / The service name to resolve.</param>
        public Auto(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// 自定义注入服务名；未指定时按成员类型解析 / Custom service name to inject; when unset, resolution falls back to the member type.
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// 该特性标注的宿主类属性（由 Schema 扫描时回填）/ The host-class property this attribute is applied to (back-filled during schema scanning).
        /// </summary>
        internal PropertyInfo? Property
        {
            get; set;
        }

        /// <summary>
        /// 该特性标注的宿主类字段（由 Schema 扫描时回填）/ The host-class field this attribute is applied to (back-filled during schema scanning).
        /// </summary>
        internal FieldInfo? Field
        {
            get; set;
        }

        private ServiceLifetime _lifetime = ServiceLifetime.Scoped;
        /// <summary>
        /// 解析该成员时使用的生命周期，默认 Scoped / The service lifetime used to resolve this member; Scoped by default.
        /// [Singleton] 派生特性将其重写为 Singleton（从根容器解析）。
        /// The [Singleton] derived attribute overrides it to Singleton (resolved from the root container).
        /// </summary>
        protected internal virtual ServiceLifetime Lifetime
        {
            get { return _lifetime; }
        }


    }
}
