using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rampage.Web.TimeZone
{
    /// <summary>
    /// DateTime 的时区转换 JSON 转换器（只转换输出，读取保持默认行为）/ Time-zone JSON converter for DateTime (converts on write only; read keeps default behavior).
    /// </summary>
    public class TimeZoneDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _sourceTimeZoneId;
        private readonly string _outputFormat;

        /// <summary>
        /// 以源时区 ID 与输出格式初始化转换器 / Initializes the converter with a source time zone id and an output format.
        /// </summary>
        /// <param name="sourceTimeZoneId">源时区 ID / The source time zone id.</param>
        /// <param name="outputFormat">输出时间格式 / The output time format.</param>
        public TimeZoneDateTimeConverter(string sourceTimeZoneId, string outputFormat)
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
        /// <returns>解析出的 DateTime / The parsed DateTime.</returns>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 只处理输出，读取按默认行为 / Write-side only; read keeps default behavior
            return reader.GetDateTime();
        }

        /// <summary>
        /// 写出时将值视作源时区时间，转换为客户端时区（请求头 RampageClientTimeZone/X-Timezone）并按输出格式化为字符串 / On write, treats the value as source-time-zone local time, converts it to the client time zone (RampageClientTimeZone/X-Timezone header) and writes it as a formatted string.
        /// </summary>
        /// <param name="writer">JSON 写入器 / The JSON writer.</param>
        /// <param name="value">要写出的值 / The value to write.</param>
        /// <param name="options">序列化选项 / The serializer options.</param>
        /// <remarks>未指定客户端时区时按原值仅做格式化输出；转换时忽略 <see cref="DateTime.Kind"/>，一律按源时区本地时间处理。</remarks>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            string? targetTimeZoneId = TimeZoneContext.GetClientTimeZoneId();
            if (string.IsNullOrEmpty(targetTimeZoneId))
            {
                // 无目标时区时直接按原值输出（可使用源时区格式化）
                writer.WriteStringValue(value.ToString(_outputFormat));
                return;
            }

            // 将 DateTime 视作源时区的本地时间（忽略 Kind）
            DateTime unspecified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            DateTime converted = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(unspecified, _sourceTimeZoneId, targetTimeZoneId);

            writer.WriteStringValue(converted.ToString(_outputFormat));
        }
    }
}
