using System;

namespace Rampage.Web.TimeZone
{
    /// <summary>
    /// 标记属性跳过自动时区转换
    /// Mark the attribute to skip automatic time zone conversion
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ExcludeAutoZoneAttribute : Attribute
    {
    }

}
