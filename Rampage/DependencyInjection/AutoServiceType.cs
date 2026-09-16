using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public class AutoServiceType
    {
        /// <summary>
        /// 
        /// </summary>
        public Type Type
        {
            get; set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        public ServiceLifetime Lifetime
        {
            get; set;
        }

    }
}
