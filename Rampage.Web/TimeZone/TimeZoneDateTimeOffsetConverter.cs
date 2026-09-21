using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rampage.Web.TimeZone
{
    /// <summary>
    /// DateTimeOffset 的时区转换 JSON 转换器（只转换输出，读取保持默认行为）/ Time-zone JSON converter for DateTimeOffset (converts on write only; read keeps default behavior).
    /// </summary>
    public class TimeZoneDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        private readonly string _sourceTimeZoneId;
        private readonly string _outputFormat;

        /// <summary>
        /// 以源时区 ID 与输出格式初始化转换器 / Initializes the converter with a source time zone id and an output format.
        /// </summary>
        /// <param name="sourceTimeZoneId">源时区 ID / The source time zone id.</param>
        /// <param name="outputFormat">输出时间格式 / The output time format.</param>
        public TimeZoneDateTimeOffsetConverter(string sourceTimeZoneId, string outputFormat)
        {
            _sourceTimeZoneId = sourceTimeZoneId;
            _outputFormat = outputFormat;
        }

        /// <summary>
        /// 读取时不做时区转换，按默认行为解析 / Reads without time zone conversion, using default parsing.
        /// </summary>
        /// <param name="reader">JSON 读取器 / The JSON reader.</param>
        /// <param name="typeToConvert">目标类型 / The target type.</param>
        /// <param name="options">序列化选项 / The serializer options.</param>
        /// <returns>解析出的 DateTimeOffset / The parsed DateTimeOffset.</returns>
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTimeOffset();
        }

        /// <summary>
        /// 写出时将值的日期时间部分视作源时区时间（忽略原始偏移），转换为客户端时区（请求头 RampageClientTimeZone/X-Timezone）并按输出格式化为字符串 / On write, treats the date-time part as source-time-zone time (ignoring the original offset), converts it to the client time zone (RampageClientTimeZone/X-Timezone header) and writes it as a formatted string.
        /// </summary>
        /// <param name="writer">JSON 写入器 / The JSON writer.</param>
        /// <param name="value">要写出的值 / The value to write.</param>
        /// <param name="options">序列化选项 / The serializer options.</param>
        /// <remarks>未指定客户端时区时按原值仅做格式化输出。</remarks>
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            string? targetTimeZoneId = TimeZoneContext.GetClientTimeZoneId();
            if (string.IsNullOrEmpty(targetTimeZoneId))
            {
                writer.WriteStringValue(value.ToString(_outputFormat));
                return;
            }

            // 将 DateTimeOffset 的日期时间视为源时区时间，忽略原始偏移
            DateTime dateTime = value.DateTime; // Kind 通常为 Unspecified
            DateTime converted = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, _sourceTimeZoneId, targetTimeZoneId);

            writer.WriteStringValue(converted.ToString(_outputFormat));
        }
    }
}
