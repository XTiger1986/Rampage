using Microsoft.Extensions.DependencyInjection;
using Rampage.Web.DependencyInjection;
using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// Ambient Scope（Web / Job / Console）。Scoped 服务须从 Current 解析。
    /// </summary>
    public static class RampageScope
    {
        private static readonly AsyncLocal<ScopeHandle?> _current = new();

        public static ScopeHandle? Current => _current.Value;

        public static bool HasCurrent => _current.Value != null;

        public static IServiceProvider? CurrentProvider => _current.Value?.ServiceProvider;

        public static IScopedServiceLocator? CurrentLocator => _current.Value?.Locator;

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

        /// <summary>绑定已有 provider（如 RequestServices），不 Dispose。</summary>
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

        /// <summary>创建并绑定匿名 Scope；Dispose 时还原 Ambient。</summary>
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

        /// <summary>绑定 GLOBAL_SCOPE（启动注入等）。</summary>
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

        private static IDisposable Push(ScopeHandle handle)
        {
            ScopeHandle? previous = _current.Value;
            _current.Value = handle;
            return new ScopePop(previous, handle);
        }

        /// <summary>环境 Scope 句柄。</summary>
        public sealed class ScopeHandle
        {
            internal ScopeHandle(IServiceProvider serviceProvider, IScopedServiceLocator locator, IServiceScope? ownedScope)
            {
                ServiceProvider = serviceProvider;
                Locator = locator;
                OwnedScope = ownedScope;
            }

            public IServiceProvider ServiceProvider { get; }
            public IScopedServiceLocator Locator { get; }
            /// <summary>Begin() 创建的 scope；Use/UseGlobal 时为 null。</summary>
            public IServiceScope? OwnedScope { get; }
        }

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
                        // avoid masking original exceptions on unwind
                    }
                }
            }
        }
    }
}
