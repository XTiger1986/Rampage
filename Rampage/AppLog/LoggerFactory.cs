using log4net.Config;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Rampage.AppLog
{
    /// <summary>
    /// 日志工厂：封装 log4net 的初始化与日志器缓存，并集中提供日志实体（SystemLog）各字段的填充逻辑。
    /// / Log factory: wraps log4net initialization and logger caching, and centralizes the fill-in logic for SystemLog entities.
    /// </summary>
    public class LoggerFactory
    {

        /// <summary>
        /// Configure 成功后由 log4net XmlConfigurator 返回的配置集合；未调用 Configure 前为 null。
        /// / The configuration collection returned by log4net's XmlConfigurator after Configure; null before Configure is called.
        /// </summary>
        public static ICollection? Config
        {
            get;
            private set;
        }

        /// <summary>
        /// 按名称缓存的 Logger 实例（GetLogger 对同名复用同一实例）。
        /// / Logger instances cached by name (GetLogger reuses the same instance for the same name).
        /// </summary>
        public static ConcurrentDictionary<string, Logger> Loggers
        {
            get;
        } = new ConcurrentDictionary<string, Logger>();

        /// <summary>
        /// 加载并监视 log4net 配置文件（XmlConfigurator.ConfigureAndWatch，文件变更自动生效）。
        /// / Load and watch the log4net config file (XmlConfigurator.ConfigureAndWatch; file changes are hot-applied).
        /// </summary>
        /// <param name="logConfigPath">log4net 配置文件路径 / path to the log4net config file</param>
        public static void Configure(string logConfigPath)
        {
            Config = XmlConfigurator.ConfigureAndWatch(new FileInfo(logConfigPath));
        }


        /// <summary>
        /// 为每条日志补充客户端数据（RawUrl、PostData、Cookie 等）的委托；未设置时相关字段被置为空字符串。
        /// / Delegate invoked on each log to fill client-side fields (RawUrl, PostData, cookies, ...); when unset those fields are set to empty strings.
        /// </summary>
        public static Action<SystemLog>? SetLogClientDataHandler
        {
            get;
            set;
        }

        /// <summary>
        /// Debug 级别日志是否采集调用栈（默认 false）。Error/Fatal 无异常对象时始终采集。
        /// Whether Debug-level logs capture the call stack (default false). Error/Fatal without an exception always do.
        /// </summary>
        public static bool CaptureStackTraceForDebug
        {
            get;
            set;
        } = false;

        /// <summary>
        /// 填充日志的调用栈、类名与方法名（仅覆盖空缺字段）：用 new StackTrace(2, false) 跳过本方法与 GetLog 两帧
        /// 且不采集文件行号，再跳过 Rampage.Log 命名空间内的帧，取真实调用方的类名与方法名。
        /// / Fill the log's stack trace, class and method (empty fields only): new StackTrace(2, false) skips this method
        /// and GetLog without file info, then frames inside the Rampage.Log namespace are skipped to find the real caller.
        /// </summary>
        /// <param name="log">待填充的日志实体 / the log entity to fill</param>
        internal static void SetLogStackTraceData(SystemLog log)
        {
            if (!string.IsNullOrEmpty(log.Class) && !string.IsNullOrEmpty(log.Method) && !string.IsNullOrEmpty(log.StackTrace))
            {
                return;
            }

            // 跳过 SetLogStackTraceData / GetLog 两帧；不采集文件行号信息（fNeedFileInfo=false 可省去 PDB 解析开销）
            StackTrace stackTrace = new StackTrace(2, false);
            if (stackTrace.FrameCount > 0)
            {
                if (string.IsNullOrEmpty(log.StackTrace))
                    log.StackTrace = stackTrace.ToString();

                // 跳过 Logger / RampageLogger 等日志门面自身的帧，取真正的调用方
                MethodBase? method = null;
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    MethodBase? candidate = stackTrace.GetFrame(i)?.GetMethod();
                    Type? declaring = candidate?.ReflectedType ?? candidate?.DeclaringType;
                    if (candidate == null || declaring == null)
                    {
                        continue;
                    }
                    if (declaring.Namespace != null && declaring.Namespace.StartsWith("Rampage.Log", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    method = candidate;
                    break;
                }
                if (method != null)
                {
                    if (string.IsNullOrEmpty(log.Class) && method.ReflectedType != null)
                    {
                        log.Class = method.ReflectedType.Name;
                    }
                    if (string.IsNullOrEmpty(log.Method))
                        log.Method = method.Name;
                }
            }
        }

        /// <summary>
        /// 用异常对象填充日志的消息、源、堆栈、类名与方法名（仅覆盖空缺字段）。
        /// / Fill the log's message, source, stack trace, class and method from the exception (empty fields only).
        /// </summary>
        /// <param name="log">待填充的日志实体 / the log entity to fill</param>
        /// <param name="ex">异常对象 / the exception</param>
        internal static void SetLogExceptionData(SystemLog log, Exception ex)
        {
            if (!string.IsNullOrEmpty(log.Message) && !string.IsNullOrEmpty(log.Source) && !string.IsNullOrEmpty(log.StackTrace) && !string.IsNullOrEmpty(log.Class) && !string.IsNullOrEmpty(log.Method))
                return;

            if (ex != null)
            {
                if (string.IsNullOrEmpty(log.Message))
                    log.Message = ex.Message;
                if (string.IsNullOrEmpty(log.Source))
                    log.Source = string.IsNullOrEmpty(ex.Source) ? "-" : ex.Source;
                if (string.IsNullOrEmpty(log.StackTrace))
                    log.StackTrace = ex.StackTrace;

                if (ex.TargetSite != null)
                {
                    if (string.IsNullOrEmpty(log.Class) && ex.TargetSite.DeclaringType != null)
                    {
                        log.Class = ex.TargetSite.DeclaringType.FullName;
                    }

                    if (string.IsNullOrEmpty(log.Method))
                        log.Method = ex.TargetSite.Name;
                }
            }
        }

        /// <summary>
        /// 填充日志的客户端数据：已注册 <see cref="SetLogClientDataHandler"/> 时调用之，否则将客户端相关字段置为空字符串。
        /// / Fill client data on the log: invokes <see cref="SetLogClientDataHandler"/> when registered, otherwise clears the client fields.
        /// </summary>
        /// <param name="log">待填充的日志实体 / the log entity to fill</param>
        internal static void SetLogClientData(SystemLog log)
        {
            if (SetLogClientDataHandler != null)
            {
                SetLogClientDataHandler.Invoke(log);
            }
            else
            {
                log.RawUrl = string.Empty;
                log.PostData = string.Empty;
                log.CookieData = string.Empty;
                log.Headers = string.Empty;
                log.Referrer = string.Empty;
                log.OperationIP = string.Empty;
                log.ClientInfo = string.Empty;
            }
        }

        /// <summary>
        /// 获取（或创建并缓存）指定名称的 Logger，内部包装 log4net 的同名日志器。
        /// / Get (or create and cache) a Logger by name, wrapping the log4net logger of the same name.
        /// </summary>
        /// <param name="loggerName">日志器名称，默认 "RampageLogger" / logger name, defaults to "RampageLogger"</param>
        /// <returns>与该名称对应的 Logger 实例（同名复用同一实例）/ the Logger for the name (the same name reuses the same instance)</returns>
        public static Logger GetLogger(string loggerName = "RampageLogger")
        {
            return Loggers.GetOrAdd(loggerName, name => new Logger(log4net.LogManager.GetLogger(name)));
        }

    }
}
