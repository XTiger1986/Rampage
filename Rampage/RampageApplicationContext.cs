using System;
namespace Rampage
{
    /// <summary>
    /// 应用上下文：对 IRampageApplication 的薄包装，仅持有应用实例引用 / Application context: a thin wrapper that only holds the application instance
    /// </summary>
    public class RampageApplicationContext : IRampageApplicationContext
    {

        /// <summary>
        /// 以指定应用实例构造上下文 / Creates the context around the given application
        /// </summary>
        /// <param name="app">被包装的应用实例 / The wrapped application instance</param>
        public RampageApplicationContext(IRampageApplication app)
        {
            Application = app;
        }

        /// <summary>
        /// 被包装的应用实例 / The wrapped application instance
        /// </summary>
        public IRampageApplication Application { get; private set; }

    }
}
