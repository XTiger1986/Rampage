using System;

namespace Rampage
{
    /// <summary>
    /// Rampage 应用上下文契约 / Rampage application context contract
    /// </summary>
    public interface IRampageApplicationContext
    {

        /// <summary>
        /// 当前应用实例 / The current application instance
        /// </summary>
        IRampageApplication Application { get; }

    }
}
