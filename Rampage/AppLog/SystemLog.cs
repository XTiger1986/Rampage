using System;

namespace Rampage.AppLog
{
    /// <summary>
    /// 系统日志实体：一条日志的完整数据（级别、消息、堆栈、调用方，及 B/S 场景下的客户端信息）。
    /// 作为 log4net 的消息对象传入，可在布局中用 %SystemLog{属性名} 输出各字段。
    /// / System log entity: the full data of one entry (level, message, stack, caller, and client info for web scenarios).
    /// It is passed to log4net as the message object; fields can be rendered via %SystemLog{property} in the layout.
    /// </summary>
    public class SystemLog
    {
        /// <summary>
        /// 日志唯一编号（GUID，"N" 格式）
        /// / Unique log id (GUID in "N" format)
        /// </summary>
        public string? Id
        {
            get;
            set;
        }

        /// <summary>
        /// 日志级别名称（log4net.Core.Level.Name）
        /// / Log level name (log4net.Core.Level.Name)
        /// </summary>
        public string? Level
        {
            get; set;
        }

        /// <summary>
        /// 原始请求地址（B/S 场景下有值）
        /// / Raw request URL (populated in web scenarios)
        /// </summary>
        public string? RawUrl
        {
            get; set;
        }

        /// <summary>
        /// 日志消息（含异常时为 消息+换行+异常 Message）
        /// / Log message (message + newline + exception Message when an exception is attached)
        /// </summary>
        public string? Message
        {
            get; set;
        }

        /// <summary>
        /// 日志源（异常的 Source，无异常时为 "-"）
        /// / Log source (the exception's Source, "-" when absent)
        /// </summary>
        public string? Source
        {
            get; set;
        }

        /// <summary>
        /// 堆栈跟踪（调用栈或异常堆栈）
        /// / Stack trace (call stack or exception stack)
        /// </summary>
        public string? StackTrace
        {
            get; set;
        }

        /// <summary>
        /// 触发日志的方法名
        /// / Name of the method that produced the log
        /// </summary>
        public string? Method
        {
            get; set;
        }

        /// <summary>
        /// 触发日志的类名
        /// / Name of the class that produced the log
        /// </summary>
        public string? Class
        {
            get; set;
        }

        /// <summary>
        /// 请求的 PostData（B/S 场景下有值）
        /// / Request post data (populated in web scenarios)
        /// </summary>
        public string? PostData
        {
            get; set;
        }

        /// <summary>
        /// 请求的 Cookie 数据（B/S 场景下有值）
        /// / Request cookie data (populated in web scenarios)
        /// </summary>
        public string? CookieData
        {
            get; set;
        }

        /// <summary>
        /// 请求头信息（B/S 场景下有值）
        /// / Request headers (populated in web scenarios)
        /// </summary>
        public string? Headers
        {
            get; set;
        }

        /// <summary>
        /// 请求的引用页地址（B/S 场景下有值）
        /// / Request referrer (populated in web scenarios)
        /// </summary>
        public string? Referrer
        {
            get; set;
        }

        /// <summary>
        /// 请求的客户端 IP 地址（B/S 场景下有值）
        /// / Client IP of the request (populated in web scenarios)
        /// </summary>
        public string? OperationIP
        {
            get; set;
        }

        /// <summary>
        /// 请求的客户端信息（如 User-Agent）
        /// / Client info of the request (e.g. User-Agent)
        /// </summary>
        public string? ClientInfo
        {
            get; set;
        }

        /// <summary>
        /// 日志记录时间（本地时区）
        /// / Log time (local time zone)
        /// </summary>
        public DateTime LogTime
        {
            get; set;
        }

        /// <summary>
        /// 日志记录时间（UTC）
        /// / Log time (UTC)
        /// </summary>
        public DateTime LogUtcTime
        {
            get; set;
        }

    }
}
