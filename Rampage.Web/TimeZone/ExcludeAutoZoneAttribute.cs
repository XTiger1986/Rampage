using System;

namespace Rampage.Web.TimeZone
{
    /// <summary>
    /// 标记属性跳过自动时区转换 / Marks a property to skip automatic time zone conversion.
    /// </summary>
    /// <remarks>优先级高于类级与属性级的 <see cref="AutoTimeZoneAttribute"/>；仅在属性上有效。</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ExcludeAutoZoneAttribute : Attribute
    {
    }

}
