using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// Dependency Auto Injection Attribute (Singleton)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class Singleton : Auto
    {

        public Singleton() : base() { }

        public Singleton(string name) : base(name) { }

        /// <summary>
        /// 
        /// </summary>
        protected internal override ServiceLifetime Lifetime
        {
            get { return ServiceLifetime.Singleton; }
        }
    }
}
