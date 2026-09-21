using Rampage.DependencyInjection;
using System;

namespace Rampage
{
    /// <summary>
    /// 兼容只读视图：请改用 <see cref="RampageScope"/>（Use/Begin/UseGlobal）绑定与释放环境作用域 / Compatibility read-only view; use <see cref="RampageScope"/> (Use/Begin/UseGlobal) to bind and release ambient scopes instead
    /// </summary>
    [Obsolete("Use Rampage.DependencyInjection.RampageScope (Use/Begin/UseGlobal) instead.")]
    public static class AsyncLocalContext
    {
        /// <summary>
        /// 当前环境上下文的 IServiceProvider（只读转发到 RampageScope） / The ambient IServiceProvider (read-only forwarding to RampageScope)
        /// </summary>
        public static IServiceProvider? CurrentServiceProvider => RampageScope.CurrentProvider;

        /// <summary>
        /// 当前环境上下文的作用域服务定位器（只读转发到 RampageScope） / The ambient scoped service locator (read-only forwarding to RampageScope)
        /// </summary>
        public static IScopedServiceLocator? CurrentServiceLocator => RampageScope.CurrentLocator;
    }
}
