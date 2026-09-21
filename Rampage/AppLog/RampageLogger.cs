using System;

namespace Rampage.AppLog
{
    /// <summary>
    /// 框架内部使用的静态日志门面：所有成员转发到名为 "RampageLogger" 的默认 Logger。
    /// / Static logging facade used by framework internals; all members forward to the default Logger named "RampageLogger".
    /// </summary>
    public class RampageLogger
    {

        /// <summary>
        /// 静态构造函数中创建的默认 Logger 实例（名称 "RampageLogger"）。
        /// / The default Logger instance (named "RampageLogger") created in the static constructor.
        /// </summary>
        public static Logger InnerLogger
        {
            get;
            private set;
        }

        static RampageLogger()
        {
            InnerLogger = LoggerFactory.GetLogger("RampageLogger");
        }

        /// <summary>
        /// 记录 Info 级别日志（不采集调用栈）并返回日志实体。
        /// / Log at Info level (no stack capture) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        public static SystemLog Info(string message)
        {
            return InnerLogger.Info(message);
        }

        /// <summary>
        /// 记录 Info 级别日志（含异常，不采集调用栈）并返回日志实体。
        /// / Log at Info level with an exception (no stack capture) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public static SystemLog Info(string message, Exception ex)
        {
            return InnerLogger.Info(message, ex);
        }


        /// <summary>
        /// 记录 Debug 级别日志并返回日志实体（是否采集调用栈见 Logger.Debug 与 CaptureStackTraceForDebug）。
        /// / Log at Debug level and return the log entity (stack capture: see Logger.Debug and CaptureStackTraceForDebug).
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        public static SystemLog Debug(string message)
        {
            return InnerLogger.Debug(message);
        }

        /// <summary>
        /// 记录 Debug 级别日志（含异常）并返回日志实体。
        /// / Log at Debug level with an exception and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public static SystemLog Debug(string message, Exception ex)
        {
            return InnerLogger.Debug(message, ex);
        }

        /// <summary>
        /// 记录 Warn 级别日志（不采集调用栈）并返回日志实体。
        /// / Log at Warn level (no stack capture) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        public static SystemLog Warn(string message)
        {
            return InnerLogger.Warn(message);
        }

        /// <summary>
        /// 记录 Warn 级别日志（含异常，不采集调用栈）并返回日志实体。
        /// / Log at Warn level with an exception (no stack capture) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public static SystemLog Warn(string? message, Exception ex)
        {
            return InnerLogger.Warn(message, ex);
        }

        /// <summary>
        /// 记录 Error 级别日志（恒采集调用栈）并返回日志实体。
        /// / Log at Error level (stack always captured) and return the log entity.
        /// </summary>
        /// <param name="ex">异常对象 / the exception</param>
        public static SystemLog Error(Exception ex)
        {
            return InnerLogger.Error(ex);
        }

        /// <summary>
        /// 记录 Error 级别日志（含异常，恒采集调用栈）并返回日志实体。
        /// / Log at Error level with an exception (stack always captured) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public static SystemLog Error(string? message, Exception ex)
        {
            return InnerLogger.Error(message, ex);
        }

        /// <summary>
        /// 记录 Fatal 级别日志（恒采集调用栈）并返回日志实体。
        /// / Log at Fatal level (stack always captured) and return the log entity.
        /// </summary>
        /// <param name="ex">异常对象 / the exception</param>
        public static SystemLog Fatal(Exception ex)
        {
            return InnerLogger.Fatal(ex);
        }
        /// <summary>
        /// 记录 Fatal 级别日志（含异常，恒采集调用栈）并返回日志实体。
        /// / Log at Fatal level with an exception (stack always captured) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public static SystemLog Fatal(string message, Exception ex)
        {
            return InnerLogger.Fatal(message, ex);
        }

    }
}
