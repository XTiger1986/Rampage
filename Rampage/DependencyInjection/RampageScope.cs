using Microsoft.Extensions.DependencyInjection;
using Rampage.Web.DependencyInjection;
using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 环境 Scope（Web / Job / Console 通用）：基于 AsyncLocal 的当前作用域栈 / Ambient scope (Web / Job / Console): an AsyncLocal-based current-scope stack.
    /// </summary>
    /// <remarks>
    /// Scoped 服务必须从 <see cref="Current"/> 解析。Use/Begin/UseGlobal 压入新 Scope，返回的 IDisposable Dispose（ScopePop）时还原上一个 Scope；
    /// Begin 创建的 Scope 在 Dispose 时一并释放，Use/UseGlobal 只借用不释放。
    /// Scoped services must be resolved from <see cref="Current"/>. Use/Begin/UseGlobal push a new scope; disposing the returned IDisposable (ScopePop) restores the previous one.
    /// A scope created by Begin is disposed along with the pop, while Use/UseGlobal only borrow and never dispose it.
    /// </remarks>
    public static class RampageScope
    {
        private static readonly AsyncLocal<ScopeHandle?> _current = new();

        /// <summary>当前环境 Scope 句柄；未绑定为 null / The current ambient scope handle; null when unbound.</summary>
        public static ScopeHandle? Current => _current.Value;

        /// <summary>是否已绑定环境 Scope / Whether an ambient scope is bound.</summary>
        public static bool HasCurrent => _current.Value != null;

        /// <summary>当前环境 Scope 的 IServiceProvider；未绑定为 null / The current ambient scope's IServiceProvider; null when unbound.</summary>
        public static IServiceProvider? CurrentProvider => _current.Value?.ServiceProvider;

        /// <summary>当前环境 Scope 的服务定位器；未绑定为 null / The current ambient scope's service locator; null when unbound.</summary>
        public static IScopedServiceLocator? CurrentLocator => _current.Value?.Locator;

        /// <summary>
        /// 获取当前环境 Scope，未绑定则抛出异常 / Gets the current ambient scope; throws when unbound.
        /// </summary>
        /// <param name="caller">调用方名称，仅用于异常消息 / The caller name, used only in the exception message.</param>
        /// <returns>当前 Scope 句柄 / The current scope handle.</returns>
        /// <exception cref="InvalidOperationException">未绑定环境 Scope / No ambient scope is bound.</exception>
        public static ScopeHandle RequireCurrent(string? caller = null)
        {
            ScopeHandle? handle = _current.Value;
            if (handle != null)
            {
                return handle;
            }

            string who = string.IsNullOrEmpty(caller) ? "code" : caller;
            throw new InvalidOperationException($"{who}: RampageScope is not bound.");
        }

        /// <summary>
        /// 绑定已有 provider（如 HttpContext.RequestServices），不持有也不释放 / Binds an existing provider (e.g. HttpContext.RequestServices); neither owns nor disposes it.
        /// </summary>
        /// <param name="serviceProvider">待绑定的服务提供者 / The service provider to bind.</param>
        /// <param name="serviceLocator">配套定位器；为 null 时从 provider 解析 / The accompanying locator; resolved from the provider when null.</param>
        /// <returns>Dispose 时还原上一个 Scope 的句柄 / A token whose Dispose restores the previous scope.</returns>
        /// <exception cref="ArgumentNullException">serviceProvider 为 null / serviceProvider is null.</exception>
        /// <exception cref="InvalidOperationException">provider 上解析不到 IScopedServiceLocator / No IScopedServiceLocator available on the provider.</exception>
        public static IDisposable Use(IServiceProvider serviceProvider, IScopedServiceLocator? locator = null)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            locator ??= serviceProvider.GetService<IScopedServiceLocator>()
                ?? throw new InvalidOperationException(
                    "RampageScope.Use: IScopedServiceLocator is not available on the given ServiceProvider.");

            return Push(new ScopeHandle(serviceProvider, locator, ownedScope: null));
        }

        /// <summary>
        /// 从根容器创建并绑定新 Scope；Dispose 时还原环境并释放该 Scope / Creates and binds a new scope from the root provider; Dispose restores the ambient value and disposes the scope.
        /// </summary>
        /// <param name="rootProvider">根服务提供者 / The root service provider.</param>
        /// <returns>Dispose 时还原并释放的句柄 / A token that restores and disposes on Dispose.</returns>
        /// <exception cref="ArgumentNullException">rootProvider 为 null / rootProvider is null.</exception>
        public static IDisposable Begin(IServiceProvider rootProvider)
        {
            if (rootProvider == null)
            {
                throw new ArgumentNullException(nameof(rootProvider));
            }

            IServiceScope scope = rootProvider.CreateScope();
            IScopedServiceLocator locator = new ScopeServiceLocator(scope);
            return Push(new ScopeHandle(scope.ServiceProvider, locator, ownedScope: scope));
        }

        /// <summary>
        /// 绑定全局 Scope（GLOBAL_SCOPE，用于启动期注入等），借用不释放 / Binds the global scope (GLOBAL_SCOPE, for startup injection etc.); borrowed, never disposed.
        /// </summary>
        /// <returns>Dispose 时还原上一个 Scope 的句柄 / A token whose Dispose restores the previous scope.</returns>
        /// <exception cref="InvalidOperationException">GLOBAL_SCOPE 不可用 / The GLOBAL_SCOPE is not available.</exception>
        public static IDisposable UseGlobal()
        {
            IServiceScope? scope = null;
            try
            {
                scope = RampageBooter.Application.GetScope(Globals.GLOBAL_SCOPE_NAME);
            }
            catch
            {
                // ignored
            }

            if (scope == null)
            {
                throw new InvalidOperationException("RampageScope.UseGlobal: GLOBAL_SCOPE is not available.");
            }

            IScopedServiceLocator locator = new ScopeServiceLocator(scope);
            return Push(new ScopeHandle(scope.ServiceProvider, locator, ownedScope: null));
        }

        /// <summary>压栈：记录前值并绑定新句柄，返回可还原的 ScopePop / Push: records the previous value, binds the new handle, returns a restoring ScopePop.</summary>
        private static IDisposable Push(ScopeHandle handle)
        {
            ScopeHandle? previous = _current.Value;
            _current.Value = handle;
            return new ScopePop(previous, handle);
        }

        /// <summary>
        /// 环境 Scope 句柄：封装当前作用域的解析器与定位器 / Ambient scope handle wrapping the current scope's provider and locator.
        /// </summary>
        public sealed class ScopeHandle
        {
            internal ScopeHandle(IServiceProvider serviceProvider, IScopedServiceLocator locator, IServiceScope? ownedScope)
            {
                ServiceProvider = serviceProvider;
                Locator = locator;
                OwnedScope = ownedScope;
            }

            /// <summary>当前作用域的服务提供者 / The current scope's service provider.</summary>
            public IServiceProvider ServiceProvider { get; }
            /// <summary>当前作用域的服务定位器 / The current scope's service locator.</summary>
            public IScopedServiceLocator Locator { get; }
            /// <summary>
            /// Begin() 创建的自有 scope；Use/UseGlobal 借用时为 null / The scope owned by Begin(); null when borrowed via Use/UseGlobal.
            /// </summary>
            public IServiceScope? OwnedScope { get; }
        }

        /// <summary>
        /// Scope 弹栈器：Dispose 时还原 AsyncLocal 栈并释放自有 Scope / Scope popper: restores the AsyncLocal stack and disposes the owned scope on Dispose.
        /// </summary>
        private sealed class ScopePop : IDisposable
        {
            private readonly ScopeHandle? _previous;
            private readonly ScopeHandle _mine;
            private bool _disposed;

            public ScopePop(ScopeHandle? previous, ScopeHandle mine)
            {
                _previous = previous;
                _mine = mine;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                // 仅当栈顶仍是自己时才还原，避免误清后压入的 Scope / Restore only when still on top, so later-pushed scopes are not clobbered
                if (ReferenceEquals(_current.Value, _mine))
                {
                    _current.Value = _previous;
                }

                if (_mine.OwnedScope != null)
                {
                    try
                    {
                        _mine.OwnedScope.Dispose();
                    }
                    catch
                    {
                        // avoid masking original exceptions on unwind / 避免掩盖展开中的原始异常
                    }
                }
            }
        }
    }
}
