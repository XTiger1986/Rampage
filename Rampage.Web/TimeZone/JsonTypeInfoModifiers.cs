using System;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Rampage.Web.TimeZone
{
    public static class JsonTypeInfoModifiers
    {
        public static void ModifyJsonTypeInfo(JsonTypeInfo typeInfo)
        {
            // 获取类级别的 AutoTimeZoneAttribute（优先使用属性级别的设置）
            var classAttr = typeInfo.Type.GetCustomAttribute<AutoTimeZoneAttribute>(inherit: true);

            foreach (var property in typeInfo.Properties)
            {
                // 跳过非时间类型或已排除的属性
                if (property.AttributeProvider is not MemberInfo memberInfo)
                    continue;

                if (memberInfo.IsDefined(typeof(ExcludeAutoZoneAttribute), inherit: true))
                    continue;

                Type propType = property.PropertyType;
                bool isDateTime = propType == typeof(DateTime) || propType == typeof(DateTime?);
                bool isDateTimeOffset = propType == typeof(DateTimeOffset) || propType == typeof(DateTimeOffset?);

                if (!isDateTime && !isDateTimeOffset)
                    continue;

                // 优先取属性上的特性，否则取类级别的
                var attr = memberInfo.GetCustomAttribute<AutoTimeZoneAttribute>(inherit: true) ?? classAttr;
                if (attr == null)
                    continue;

                string sourceTz = attr.SourceTimeZoneId ?? TimeZoneInfo.Local.Id;
                string format = attr.OutputFormat;

                if (isDateTime)
                {
                    property.CustomConverter = new TimeZoneDateTimeConverter(sourceTz, format);
                }
                else if (isDateTimeOffset)
                {
                    property.CustomConverter = new TimeZoneDateTimeOffsetConverter(sourceTz, format);
                }
            }
        }
    }
}
