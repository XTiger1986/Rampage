using log4net.Core;
using log4net.Layout.Pattern;
using log4net.Util;
using Rampage.Reflection;
using System;
using System.Reflection;

namespace Rampage.AppLog
{
    /// <summary>
    /// %SystemLog{属性名} 占位符转换器：用反射从 LoggingEvent.MessageObject（通常是 SystemLog 实体）读取 Option 指定的属性值输出。
    /// / Converter for the %SystemLog{property} placeholder: reflects the Option-named property off LoggingEvent.MessageObject
    /// (typically a SystemLog entity) and writes its value.
    /// </summary>
    public class LogPatternLayoutConverter : PatternLayoutConverter
    {

        /// <summary>
        /// 将消息对象的指定属性值写入输出：消息对象为 null 时输出 log4net 空值占位（"(null)"）；
        /// Option 为空时输出消息对象的 ToString()；属性不存在时输出空。
        /// / Write the Option-named property of the message object: a null message renders log4net's null text ("(null)");
        /// an empty Option renders the object's ToString(); a missing property renders nothing.
        /// </summary>
        /// <param name="writer">输出目标 / the writer to append to</param>
        /// <param name="loggingEvent">日志事件（MessageObject 为 SystemLog 实体）/ the logging event (MessageObject is the SystemLog entity)</param>
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            //获取传入的消息对象
            object? objMsg = loggingEvent.MessageObject;

            if (objMsg == null)
            {
                //如果对象为空输出log4net默认的null字符串
                writer.Write(SystemInfo.NullText);
                return;
            }
            if (string.IsNullOrEmpty(this.Option))
            {
                //如果属性为空，输出消息对象的ToString()
                writer.Write(objMsg.ToString());
                return;
            }

            Type type = objMsg.GetType();
            PropertyInfo? prpInfo = ReflectionTool.GetProperty(type, this.Option);
            if (prpInfo == null)
            {
                return;
            }
            object? val = prpInfo.GetValue(objMsg, null);
            writer.Write(val == null ? "" : val.ToString());
        }

    }
}
