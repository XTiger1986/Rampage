using Rampage.DependencyInjection;
using System;

namespace Rampage
{
    /// <summary>
    /// 兼容只读视图：请使用 <see cref="RampageScope"/> 绑定/释放环境 Scope。
    /// </summary>
    [Obsolete("Use Rampage.DependencyInjection.RampageScope (Use/Begin/UseGlobal) instead.")]
    public static class AsyncLocalContext
    {
        public static IServiceProvider? CurrentServiceProvider => RampageScope.CurrentProvider;

        public static IScopedServiceLocator? CurrentServiceLocator => RampageScope.CurrentLocator;
    }
}
