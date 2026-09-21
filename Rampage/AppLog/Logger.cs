using System;

namespace Rampage.AppLog
{
    /// <summary>
    /// 日志门面：包装 log4net.ILog，每条日志先构造 SystemLog 实体（含客户端数据与可选的调用栈）再交给 log4net 输出。
    /// / Logging facade wrapping log4net.ILog: each entry is built as a SystemLog entity (with client data and an optional stack trace) before being written to log4net.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// 被包装的 log4net 日志器。
        /// / The wrapped log4net logger.
        /// </summary>
        public log4net.ILog InnerLogger
        {
            get;
            private set;
        }

        /// <summary>
        /// 用 log4net 日志器构造 Logger 实例。
        /// / Construct a Logger wrapping a log4net logger.
        /// </summary>
        /// <param name="logger">被包装的 log4net 日志器 / the log4net logger to wrap</param>
        internal Logger(log4net.ILog logger)
        {
            InnerLogger = logger;
        }

        /// <summary>
        /// 构造一条 SystemLog：生成 GUID（"N" 格式）Id、本地与 UTC 时间、级别名并采集客户端数据；
        /// isTrace 为 true 时再采集调用栈与调用方类/方法名。
        /// / Build a SystemLog: assigns a GUID ("N" format) id, local and UTC times, the level name and client data;
        /// when isTrace is true the stack trace and the caller's class/method are captured as well.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="isTrace">是否采集调用栈（Info/Warn 传 false；Error/Fatal 恒为 true；Debug 受 CaptureStackTraceForDebug 控制）/ whether to capture the stack trace</param>
        /// <param name="level">log4net 日志级别 / the log4net level</param>
        /// <returns>填充好的日志实体（尚未写入 log4net）/ the populated log entity (not yet written to log4net)</returns>
        public SystemLog GetLog(string? message, bool isTrace, log4net.Core.Level level)
        {
            SystemLog log = new SystemLog();
            log.Id = Guid.NewGuid().ToString("N");
            log.LogTime = DateTime.Now;
            log.LogUtcTime = DateTime.UtcNow;
            log.Level = level.Name;
            log.Message = message;
            log.StackTrace = string.Empty;
            log.Class = string.Empty;
            log.Method = string.Empty;
            log.Source = string.Empty;
            LoggerFactory.SetLogClientData(log);
            if (isTrace)
            {
                LoggerFactory.SetLogStackTraceData(log);
            }
            return log;
        }

        /// <summary>
        /// 构造一条含异常的 SystemLog：ex 不为 null 时用异常数据填充空缺字段（消息、源、堆栈、类、方法）；
        /// 最终 Message 为「参数 message + 换行 + 异常 Message」。
        /// / Build a SystemLog with an exception: when ex is not null its data fills empty fields (message, source, stack, class, method);
        /// the final Message is "message + newline + ex.Message".
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="isTrace">是否采集调用栈（仅在 ex 为 null 时生效）/ whether to capture the stack trace (effective only when ex is null)</param>
        /// <param name="ex">异常对象 / the exception</param>
        /// <param name="level">log4net 日志级别 / the log4net level</param>
        /// <returns>填充好的日志实体（尚未写入 log4net）/ the populated log entity (not yet written to log4net)</returns>
        public SystemLog GetLog(string? message, bool isTrace, Exception ex, log4net.Core.Level level)
        {
            SystemLog log = new SystemLog();
            log.Id = Guid.NewGuid().ToString("N");
            log.LogTime = DateTime.Now;
            log.LogUtcTime = DateTime.UtcNow;
            log.Level = level.Name;
            log.StackTrace = string.Empty;
            log.Class = string.Empty;
            log.Method = string.Empty;
            log.Source = string.Empty;
            LoggerFactory.SetLogClientData(log);
            if (isTrace && ex == null)
            {
                LoggerFactory.SetLogStackTraceData(log);
            }
            if (ex != null)
            {
                LoggerFactory.SetLogExceptionData(log, ex);
            }
            if (!string.IsNullOrEmpty(message))
            {
                log.Message = string.Concat(message, Environment.NewLine, log.Message);
            }
            return log;
        }

        /// <summary>
        /// 记录 Info 级别日志（不采集调用栈）并返回日志实体。
        /// / Log at Info level (no stack capture) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        public SystemLog Info(string message)
        {
            SystemLog log = GetLog(message, false, log4net.Core.Level.Info);
            InnerLogger.Info(log);
            return log;
        }
        /// <summary>
        /// 记录 Info 级别日志（含异常，不采集调用栈）并返回日志实体。
        /// / Log at Info level with an exception (no stack capture) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public SystemLog Info(string? message, Exception ex)
        {
            SystemLog log = GetLog(message, false, ex, log4net.Core.Level.Info);
            InnerLogger.Info(log, ex);
            return log;
        }

        /// <summary>
        /// Debug 日志。级别未启用时不采集栈信息、不写 log4net 直接返回，避免生产环境为被丢弃的日志付出 StackTrace 采集成本；
        /// 级别启用时是否采集调用栈由 LoggerFactory.CaptureStackTraceForDebug（默认 false）控制。
        /// / Debug log: when the level is disabled the entity is returned without stack capture or writing to log4net,
        /// avoiding StackTrace cost for discarded logs; when enabled, capture follows LoggerFactory.CaptureStackTraceForDebug (default false).
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        public SystemLog Debug(string message)
        {
            if (!InnerLogger.IsDebugEnabled)
            {
                return GetLog(message, false, log4net.Core.Level.Debug);
            }
            SystemLog log = GetLog(message, LoggerFactory.CaptureStackTraceForDebug, log4net.Core.Level.Debug);
            InnerLogger.Debug(log);
            return log;
        }
        /// <summary>
        /// Debug 日志（含异常）。级别未启用时不采集栈信息、不写 log4net 直接返回；
        /// 级别启用时是否采集调用栈由 LoggerFactory.CaptureStackTraceForDebug（默认 false）控制。
        /// / Debug log with an exception: returns without stack capture or writing when the level is disabled;
        /// capture follows LoggerFactory.CaptureStackTraceForDebug when enabled.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public SystemLog Debug(string? message, Exception ex)
        {
            if (!InnerLogger.IsDebugEnabled)
            {
                return GetLog(message, false, ex, log4net.Core.Level.Debug);
            }
            SystemLog log = GetLog(message, LoggerFactory.CaptureStackTraceForDebug, ex, log4net.Core.Level.Debug);
            InnerLogger.Debug(log, ex);
            return log;
        }

        /// <summary>
        /// 记录 Warn 级别日志（不采集调用栈）并返回日志实体。
        /// / Log at Warn level (no stack capture) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        public SystemLog Warn(string message)
        {
            SystemLog log = GetLog(message, false, log4net.Core.Level.Warn);
            InnerLogger.Warn(log);
            return log;
        }

        /// <summary>
        /// 记录 Warn 级别日志（含异常，不采集调用栈）并返回日志实体。
        /// / Log at Warn level with an exception (no stack capture) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public SystemLog Warn(string? message, Exception ex)
        {
            SystemLog log = GetLog(message, false, ex, log4net.Core.Level.Warn);
            InnerLogger.Warn(log, ex);
            return log;
        }

        /// <summary>
        /// 记录 Error 级别日志（恒采集调用栈）并返回日志实体。
        /// / Log at Error level (stack always captured) and return the log entity.
        /// </summary>
        /// <param name="ex">异常对象 / the exception</param>
        public SystemLog Error(Exception ex)
        {
            SystemLog log = GetLog(null, true, ex, log4net.Core.Level.Error);
            InnerLogger.Error(log, ex);
            return log;
        }

        /// <summary>
        /// 记录 Error 级别日志（含异常，恒采集调用栈）并返回日志实体。
        /// / Log at Error level with an exception (stack always captured) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public SystemLog Error(string? message, Exception ex)
        {
            SystemLog log = GetLog(message, true, ex, log4net.Core.Level.Error);
            InnerLogger.Error(log, ex);
            return log;
        }

        /// <summary>
        /// 记录 Fatal 级别日志（恒采集调用栈）并返回日志实体。
        /// / Log at Fatal level (stack always captured) and return the log entity.
        /// </summary>
        /// <param name="ex">异常对象 / the exception</param>
        public SystemLog Fatal(Exception ex)
        {
            SystemLog log = GetLog(null, true, ex, log4net.Core.Level.Fatal);
            InnerLogger.Fatal(log, ex);
            return log;
        }

        /// <summary>
        /// 记录 Fatal 级别日志（含异常，恒采集调用栈）并返回日志实体。
        /// / Log at Fatal level with an exception (stack always captured) and return the log entity.
        /// </summary>
        /// <param name="message">日志消息 / log message</param>
        /// <param name="ex">异常对象 / the exception</param>
        public SystemLog Fatal(string? message, Exception ex)
        {
            SystemLog log = GetLog(message, true, ex, log4net.Core.Level.Fatal);
            InnerLogger.Fatal(log, ex);
            return log;
        }

    }
}
