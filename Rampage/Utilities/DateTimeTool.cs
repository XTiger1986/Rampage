using System;
using System.Globalization;

namespace Rampage.Utilities
{
    /// <summary>
    /// DateTime Utilities
    /// </summary>
    public class DateTimeTool
    {

        /// <summary>
        /// Get January 1, 1970 at 00:00:00 Coordinated Universal Time (UTC) and change to the TimeZoneInfo DateTime
        /// </summary>
        /// <param name="timeZone"></param>
        /// <returns></returns>
        public static DateTime EpochTime(TimeZoneInfo? timeZone = null)
        {
            if (timeZone == null)
            {
                timeZone = TimeZoneInfo.Local;
            }
            DateTime epochTime;
            if (timeZone != TimeZoneInfo.Utc)
            {
                epochTime = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), timeZone);
            }
            else
            {
                epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            return epochTime;
        }

        /// <summary>
        /// Generate Timestamp (The number of seconds that have passed since January 1, 1970 at 00:00:00 Coordinated Universal Time (UTC) until now)
        /// </summary>
        /// <returns></returns>
        public static long CurrentTimeSeconds()
        {
            return ToTimeSeconds(DateTime.Now);
        }

        /// <summary>
        ///  Generate Timestamp (The number of Milliseconds that have passed since January 1, 1970 at 00:00:00 Coordinated Universal Time (UTC) until now)
        /// </summary>
        /// <returns></returns>
        public static long CurrentTimeMillis(TimeZoneInfo? timeZone = null)
        {
            return ToTimeMillis(DateTime.Now, timeZone);
        }

        /// <summary>
        /// convert datetime to Timestamp seconds (default use local time)
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ToTimeSeconds(DateTime dateTime, TimeZoneInfo? timeZone = null)
        {
            if (timeZone == null)
            {
                timeZone = TimeZoneInfo.Local;
            }
            DateTime start;
            if (timeZone != TimeZoneInfo.Utc)
            {
                start = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), timeZone);
            }
            else
            {
                start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            long timestamp = Convert.ToInt64((dateTime - start).TotalSeconds);
            return timestamp;
        }

        /// <summary>
        /// timestamp (seconds) convert to datetime (default use local time)
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static DateTime TimeSecondsToDateTime(long timestamp, TimeZoneInfo? timeZone = null)
        {
            if (timeZone == null)
            {
                timeZone = TimeZoneInfo.Local;
            }
            DateTime start;
            if (timeZone != TimeZoneInfo.Utc)
            {
                start = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), timeZone);
            }
            else
            {
                start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            DateTime dt = start.AddSeconds(timestamp);
            return dt;
        }

        /// <summary>
        /// convert datetime to Timestamp milliseconds (default use local time)
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ToTimeMillis(DateTime dateTime, TimeZoneInfo? timeZone = null)
        {
            if (timeZone == null)
            {
                timeZone = TimeZoneInfo.Local;
            }
            DateTime start;
            if (timeZone != TimeZoneInfo.Utc)
            {
                start = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), timeZone);
            }
            else
            {
                start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            long timestamp = Convert.ToInt64((dateTime - start).TotalMilliseconds);
            return timestamp;
        }

        /// <summary>
        /// timestamp (milliseconds) convert to datetime (default use local time)
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static DateTime TimeMillisToDateTime(long timestamp, TimeZoneInfo? timeZone = null)
        {
            if (timeZone == null)
            {
                timeZone = TimeZoneInfo.Local;
            }
            DateTime start;
            if (timeZone != TimeZoneInfo.Utc)
            {
                start = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), timeZone);
            }
            else
            {
                start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            DateTime dt = start.AddMilliseconds(timestamp);
            return dt;
        }

        /// <summary>
        /// convert utc time to local time
        /// </summary>
        /// <param name="utc"></param>
        /// <returns></returns>
        public static DateTime UtcToLocal(DateTime utc)
        {
            DateTime utcTime = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, utc.Microsecond, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTime(utcTime, TimeZoneInfo.Local);
        }

        /// <summary>
        /// convert utc time to specified local time
        /// </summary>
        /// <param name="utc"></param>
        /// <returns></returns>
        public static DateTime UtcToLocal(DateTime utc, TimeZoneInfo local)
        {
            DateTime utcTime = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, utc.Microsecond, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTime(utcTime, local);
        }

        /// <summary>
        /// convert local time to utc time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime LocalToUtc(DateTime time)
        {
            DateTime localTime = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Microsecond, DateTimeKind.Local);
            return TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Utc);
        }

        /// <summary>
        /// convert specified local time to utc time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime LocalToUtc(DateTime time, TimeZoneInfo local)
        {
            DateTime localTime = TimeZoneInfo.ConvertTime(new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Microsecond, DateTimeKind.Unspecified), local);
            return TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Utc);
        }


        private static readonly HashSet<string> DEFAULT_DATETIME_FORMATS = new HashSet<string>
        {
            // ISO 8601 format
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ss.fffffff",
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",
        
            // Common format
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss.fff",
        
            // Date format
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "yyyyMMdd",
        
            // DateTimeOffset
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ss.fffzzz",
        
            // RFC 1123
            "r",
            "R",
        
            // Unix timestamp
            "unix"
        };

        /// <summary>
        /// 尝试解析时间字符串
        /// </summary>
        public static bool TryParseDateTimeString(string dateTimeString, string[]? customFormats,
            out DateTime dateTime, out string? detectedFormat)
        {
            dateTime = default;
            detectedFormat = null;

            if (string.IsNullOrEmpty(dateTimeString))
                return false;

            // 尝试 Unix 时间戳（秒或毫秒）
            if (long.TryParse(dateTimeString, out var timestamp))
            {
                if (dateTimeString.Length <= 10) // 秒级时间戳
                {
                    dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    detectedFormat = "unix-seconds";
                    return true;
                }
                else // 毫秒级时间戳
                {
                    dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                    detectedFormat = "unix-milliseconds";
                    return true;
                }
            }

            // 合并默认格式和自定义格式
            var allFormats = DEFAULT_DATETIME_FORMATS.ToList();
            if (customFormats != null)
            {
                allFormats.AddRange(customFormats.Where(f => !string.IsNullOrEmpty(f)));
            }

            // 尝试每种格式
            foreach (var format in allFormats)
            {
                // 处理特殊格式
                if (format == "r" || format == "R")
                {
                    if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces, out dateTime))
                    {
                        detectedFormat = format;
                        return true;
                    }
                    continue;
                }

                // 标准格式解析
                if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out dateTime))
                {
                    detectedFormat = format;
                    return true;
                }

                // 尝试放宽条件
                if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dateTime))
                {
                    detectedFormat = format;
                    return true;
                }
            }

            // 最后尝试通用解析
            if (DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                detectedFormat = "auto";
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试解析带时区的时间字符串
        /// </summary>
        public static bool TryParseDateTimeOffsetString(string dateTimeString, string[]? customFormats,
            out DateTimeOffset dateTimeOffset, out string? detectedFormat)
        {
            dateTimeOffset = default;
            detectedFormat = null;

            if (string.IsNullOrEmpty(dateTimeString))
                return false;

            // 尝试 Unix 时间戳
            if (long.TryParse(dateTimeString, out var timestamp))
            {
                if (dateTimeString.Length <= 10)
                {
                    dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                    detectedFormat = "unix-seconds";
                    return true;
                }
                else
                {
                    dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                    detectedFormat = "unix-milliseconds";
                    return true;
                }
            }

            // 检查是否已经是 DateTimeOffset 格式
            if (DateTimeOffset.TryParse(dateTimeString, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out dateTimeOffset))
            {
                detectedFormat = "auto";
                return true;
            }

            // 尝试精确格式
            var allFormats = GetDateTimeOffsetFormats(customFormats);
            foreach (var format in allFormats)
            {
                if (DateTimeOffset.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out dateTimeOffset))
                {
                    detectedFormat = format;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 格式化为字符串
        /// </summary>
        public static string? FormatDateTime(DateTime dateTime, string? format, bool convertToString)
        {
            if (!convertToString || string.IsNullOrEmpty(format))
                return null;

            // 特殊格式处理
            if (format == "unix" || format == "unix-seconds")
            {
                return new DateTimeOffset(dateTime).ToUnixTimeSeconds().ToString();
            }
            else if (format == "unix-milliseconds")
            {
                return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds().ToString();
            }

            return dateTime.ToString(format, CultureInfo.InvariantCulture);
        }

        public static string? FormatDateTimeOffset(DateTimeOffset dateTimeOffset, string format, bool convertToString)
        {
            if (!convertToString || string.IsNullOrEmpty(format))
                return null;

            if (format == "unix" || format == "unix-seconds")
            {
                return dateTimeOffset.ToUnixTimeSeconds().ToString();
            }
            else if (format == "unix-milliseconds")
            {
                return dateTimeOffset.ToUnixTimeMilliseconds().ToString();
            }

            return dateTimeOffset.ToString(format, CultureInfo.InvariantCulture);
        }

        private static List<string> GetDateTimeOffsetFormats(string[]? customFormats)
        {
            var formats = new List<string>
            {
                // ISO 8601 with offset
                "yyyy-MM-ddTHH:mm:sszzz",
                "yyyy-MM-ddTHH:mm:ss.fffzzz",
                "yyyy-MM-ddTHH:mm:ss.fffffffzzz",
                "yyyy-MM-dd HH:mm:sszzz",
            
                // RFC 1123
                "r",
                "R",
            
                // Standard formats
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss"
            };

            if (customFormats != null)
            {
                formats.AddRange(customFormats.Where(f => !string.IsNullOrEmpty(f)));
            }

            return formats;
        }

        /// <summary>
        /// 检查字符串是否为时间格式
        /// </summary>
        public static bool IsDateTimeString(string value, string[]? customFormats = null)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // 检查是否是 Unix 时间戳
            if (long.TryParse(value, out _))
                return true;

            // 尝试解析
            return TryParseDateTimeString(value, customFormats, out _, out _) ||
                   TryParseDateTimeOffsetString(value, customFormats, out _, out _);
        }

    }
}
