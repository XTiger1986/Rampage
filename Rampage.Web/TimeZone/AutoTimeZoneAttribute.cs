using System;

namespace Rampage.Web.TimeZone
{
    /// <summary>
    /// 标记类或属性需要进行自动时区转换
    /// Classes or attributes need to be automatically converted to the right timezone
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public class AutoTimeZoneAttribute : Attribute
    {
        /// <summary>强制指定原始时区ID，默认使用本地时区</summary>
        public string? SourceTimeZoneId { get; set; }
        /// <summary>输出格式，默认 yyyy-MM-dd HH:mm:ss</summary>
        public string OutputFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    }
}
