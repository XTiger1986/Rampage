using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rampage.Web.TimeZone
{
    public class TimeZoneDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        private readonly string _sourceTimeZoneId;
        private readonly string _outputFormat;

        public TimeZoneDateTimeOffsetConverter(string sourceTimeZoneId, string outputFormat)
        {
            _sourceTimeZoneId = sourceTimeZoneId;
            _outputFormat = outputFormat;
        }

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTimeOffset();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            string? targetTimeZoneId = TimeZoneContext.GetClientTimeZoneId();
            if (string.IsNullOrEmpty(targetTimeZoneId))
            {
                writer.WriteStringValue(value.ToString(_outputFormat));
                return;
            }

            TimeZoneInfo sourceTz = TimeZoneInfo.FindSystemTimeZoneById(_sourceTimeZoneId);
            TimeZoneInfo targetTz = TimeZoneInfo.FindSystemTimeZoneById(targetTimeZoneId);

            // 将 DateTimeOffset 的日期时间视为源时区时间，忽略原始偏移
            DateTime dateTime = value.DateTime; // Kind 通常为 Unspecified
            DateTime converted = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, _sourceTimeZoneId, targetTimeZoneId);

            writer.WriteStringValue(converted.ToString(_outputFormat));
        }
    }
}
