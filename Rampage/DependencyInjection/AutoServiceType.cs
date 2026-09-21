using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 自动注册的服务类型描述项（类型 + 生命周期）/ A service type entry collected for auto registration (type + lifetime).
    /// </summary>
    public class AutoServiceType
    {
        /// <summary>
        /// 服务类型 / The service type.
        /// </summary>
        public Type Type
        {
            get; set;
        } = default!;

        /// <summary>
        /// 注册生命周期 / The registration lifetime.
        /// </summary>
        public ServiceLifetime Lifetime
        {
            get; set;
        }

    }
}
