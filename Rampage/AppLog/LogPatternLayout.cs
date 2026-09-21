using System;

namespace Rampage.AppLog
{
    /// <summary>
    /// 支持 %SystemLog{属性名} 占位符的 log4net 布局：在 log4net 配置中将 layout 类型指定为本类，
    /// 即可用 %SystemLog{Message}、%SystemLog{Class} 等输出 SystemLog 实体的属性。
    /// / log4net PatternLayout supporting the %SystemLog{property} placeholder: register this layout type in the
    /// log4net config to render SystemLog entity properties such as %SystemLog{Message} or %SystemLog{Class}.
    /// </summary>
    public class LogPatternLayout : log4net.Layout.PatternLayout
    {
        /// <summary>
        /// 构造函数：注册名为 SystemLog 的转换器。
        /// / Constructor: registers the converter named "SystemLog".
        /// </summary>
        public LogPatternLayout() : base()
        {
            this.AddConverter("SystemLog", typeof(LogPatternLayoutConverter));
        }
    }
}
