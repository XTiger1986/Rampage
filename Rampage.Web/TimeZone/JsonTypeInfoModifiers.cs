using System;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Rampage.Web.TimeZone
{
    /// <summary>
    /// JSON 类型信息修改器，为标记了 <see cref="AutoTimeZoneAttribute"/> 的时间属性挂接时区转换器 / JSON type-info modifier that attaches time zone converters to time properties marked with <see cref="AutoTimeZoneAttribute"/>.
    /// </summary>
    /// <remarks>
    /// 由 RampageWebApplication 在 EnableAutoTimeZone=true（默认 false）时注册为 System.Text.Json 的 TypeInfoResolver Modifier；
    /// 仅影响序列化输出（Write），读取（Read）不做转换。
    /// </remarks>
    public static class JsonTypeInfoModifiers
    {
        /// <summary>
        /// 修改指定类型的 JSON 序列化信息，为符合条件的时间属性设置自定义转换器 / Modifies the JSON type info of a type by assigning custom converters to qualifying time properties.
        /// </summary>
        /// <param name="typeInfo">目标类型的 JSON 序列化信息 / The JSON type info of the target type.</param>
        /// <remarks>
        /// 只处理 DateTime/DateTimeOffset（含可空）属性；<see cref="ExcludeAutoZoneAttribute"/> 标记的属性跳过；
        /// 属性级 <see cref="AutoTimeZoneAttribute"/> 优先于类级；源时区取特性 SourceTimeZoneId，未指定时用服务器本地时区。
        /// </remarks>
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
