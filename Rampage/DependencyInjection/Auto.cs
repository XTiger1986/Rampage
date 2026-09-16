using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// Dependency Auto Injection Attribute。
    /// 成员类型可为 T 或 Lazy&lt;T&gt;（后者延迟解析，用于打破互依赖）。
    /// 禁止两个服务用立即 [Auto] 互相注入；启动期会检测环并失败。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class Auto : Attribute
    {

        public Auto() { }

        public Auto(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// custom injection service name, if not defined, use class type name
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// relative class property whick auto inject to
        /// </summary>
        internal PropertyInfo? Property
        {
            get; set;
        }

        /// <summary>
        /// relative class field whick auto inject to
        /// </summary>
        internal FieldInfo? Field
        {
            get; set;
        }

        private ServiceLifetime _lifetime = ServiceLifetime.Scoped;
        /// <summary>
        /// 
        /// </summary>
        protected internal virtual ServiceLifetime Lifetime
        {
            get { return _lifetime; }
        }


    }
}
