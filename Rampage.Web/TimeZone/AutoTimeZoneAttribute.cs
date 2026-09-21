using System;

namespace Rampage.Web.TimeZone
{
    /// <summary>
    /// 标记类或属性需要进行自动时区转换 / Marks a class or property for automatic time zone conversion.
    /// </summary>
    /// <remarks>
    /// 仅影响 JSON 序列化的输出（Write）：将值视作源时区时间，转换为客户端时区（请求头 RampageClientTimeZone/X-Timezone）并按 <see cref="OutputFormat"/> 格式化为字符串；
    /// 反序列化（Read）按默认行为解析、不做转换。需在 RampageWebApplication 上设置 EnableAutoTimeZone=true（默认 false）注册 JsonTypeInfoModifiers 后才会生效；
    /// 属性级特性优先于类级特性；<see cref="ExcludeAutoZoneAttribute"/> 标记的属性始终跳过。仅对 DateTime/DateTimeOffset（含可空）属性生效。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public class AutoTimeZoneAttribute : Attribute
    {
        /// <summary>强制指定原始时区ID，默认使用本地时区 / The source time zone id; defaults to the server local time zone.</summary>
        public string? SourceTimeZoneId { get; set; }
        /// <summary>输出格式，默认 yyyy-MM-dd HH:mm:ss / The output format; defaults to yyyy-MM-dd HH:mm:ss.</summary>
        public string OutputFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    }
}
