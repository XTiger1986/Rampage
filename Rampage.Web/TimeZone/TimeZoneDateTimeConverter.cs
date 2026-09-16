using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rampage.Web.TimeZone
{
    public class TimeZoneDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _sourceTimeZoneId;
        private readonly string _outputFormat;

        public TimeZoneDateTimeConverter(string sourceTimeZoneId, string outputFormat)
        {
            _sourceTimeZoneId = sourceTimeZoneId;
            _outputFormat = outputFormat;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 只处理输出，读取按默认行为
            return reader.GetDateTime();
        }

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
