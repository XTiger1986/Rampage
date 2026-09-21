using System;
using System.Globalization;

namespace Rampage.Utilities
{
    /// <summary>
    /// 日期时间工具（纪元换算、时区转换、多格式解析与格式化） /
    /// DateTime utilities (epoch conversion, time zone conversion, multi-format parsing and formatting)
    /// </summary>
    public class DateTimeTool
    {

        /// <summary>
        /// 取 1970-01-01 00:00:00 UTC 纪元时间并转换到指定时区（返回该时区墙钟表示） /
        /// Get the 1970-01-01 00:00:00 UTC epoch converted into the given time zone (as that zone's wall-clock time)
        /// </summary>
        /// <param name="timeZone">目标时区，null 时取本地时区 / The target time zone; the local zone when null</param>
        /// <returns>指定时区下的纪元时间 / The epoch time in the given zone</returns>
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
        /// 当前时间戳（距 1970-01-01 00:00:00 UTC 的秒数，以本地时区为纪元基准） /
        /// Current timestamp (seconds since 1970-01-01 00:00:00 UTC, with the local zone as the epoch base)
        /// </summary>
        /// <returns>秒级时间戳 / The second-precision timestamp</returns>
        public static long CurrentTimeSeconds()
        {
            return ToTimeSeconds(DateTime.Now);
        }

        /// <summary>
        /// 当前时间戳（距 1970-01-01 00:00:00 UTC 的毫秒数） /
        /// Current timestamp (milliseconds since 1970-01-01 00:00:00 UTC)
        /// </summary>
        /// <param name="timeZone">纪元基准时区，null 时取本地时区 / The epoch base time zone; the local zone when null</param>
        /// <returns>毫秒级时间戳 / The millisecond-precision timestamp</returns>
        public static long CurrentTimeMillis(TimeZoneInfo? timeZone = null)
        {
            return ToTimeMillis(DateTime.Now, timeZone);
        }

        /// <summary>
        /// 将 DateTime 转为秒级时间戳（纪元基准默认取本地时区） /
        /// Convert a DateTime to a second-precision timestamp (the epoch base defaults to the local time zone)
        /// </summary>
        /// <param name="dateTime">待转换时间 / The DateTime to convert</param>
        /// <param name="timeZone">纪元基准时区，null 时取本地时区 / The epoch base time zone; the local zone when null</param>
        /// <returns>秒级时间戳 / The second-precision timestamp</returns>
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
        /// 将秒级时间戳转为 DateTime（纪元基准默认取本地时区） /
        /// Convert a second-precision timestamp to a DateTime (the epoch base defaults to the local time zone)
        /// </summary>
        /// <param name="timestamp">秒级时间戳 / The second-precision timestamp</param>
        /// <param name="timeZone">纪元基准时区，null 时取本地时区 / The epoch base time zone; the local zone when null</param>
        /// <returns>对应的 DateTime / The corresponding DateTime</returns>
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
        /// 将 DateTime 转为毫秒级时间戳（纪元基准默认取本地时区） /
        /// Convert a DateTime to a millisecond-precision timestamp (the epoch base defaults to the local time zone)
        /// </summary>
        /// <param name="dateTime">待转换时间 / The DateTime to convert</param>
        /// <param name="timeZone">纪元基准时区，null 时取本地时区 / The epoch base time zone; the local zone when null</param>
        /// <returns>毫秒级时间戳 / The millisecond-precision timestamp</returns>
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
        /// 将毫秒级时间戳转为 DateTime（纪元基准默认取本地时区） /
        /// Convert a millisecond-precision timestamp to a DateTime (the epoch base defaults to the local time zone)
        /// </summary>
        /// <param name="timestamp">毫秒级时间戳 / The millisecond-precision timestamp</param>
        /// <param name="timeZone">纪元基准时区，null 时取本地时区 / The epoch base time zone; the local zone when null</param>
        /// <returns>对应的 DateTime / The corresponding DateTime</returns>
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
        /// 将 UTC 时间转换为本地时区时间 /
        /// Convert a UTC DateTime to the local time zone
        /// </summary>
        /// <remarks>先重建为 UTC Kind 再转换，入参原有的 Kind 标记被忽略。The value is rebuilt as UTC Kind first, so the original Kind flag is ignored.</remarks>
        /// <param name="utc">UTC 时间 / The UTC time</param>
        /// <returns>本地时区时间 / The local-zone time</returns>
        public static DateTime UtcToLocal(DateTime utc)
        {
            DateTime utcTime = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, utc.Microsecond, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTime(utcTime, TimeZoneInfo.Local);
        }

        /// <summary>
        /// 将 UTC 时间转换为指定时区时间 /
        /// Convert a UTC DateTime to the given time zone
        /// </summary>
        /// <remarks>先重建为 UTC Kind 再转换，入参原有的 Kind 标记被忽略。The value is rebuilt as UTC Kind first, so the original Kind flag is ignored.</remarks>
        /// <param name="utc">UTC 时间 / The UTC time</param>
        /// <param name="local">目标时区 / The target time zone</param>
        /// <returns>目标时区时间 / The time in the target zone</returns>
        public static DateTime UtcToLocal(DateTime utc, TimeZoneInfo local)
        {
            DateTime utcTime = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, utc.Microsecond, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTime(utcTime, local);
        }

        /// <summary>
        /// 将本地时间转换为 UTC 时间 /
        /// Convert a local DateTime to UTC
        /// </summary>
        /// <remarks>先重建为 Local Kind 再转换，入参原有的 Kind 标记被忽略。The value is rebuilt as Local Kind first, so the original Kind flag is ignored.</remarks>
        /// <param name="time">本地时间 / The local time</param>
        /// <returns>UTC 时间 / The UTC time</returns>
        public static DateTime LocalToUtc(DateTime time)
        {
            DateTime localTime = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Microsecond, DateTimeKind.Local);
            return TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Utc);
        }

        /// <summary>
        /// 将指定时区的本地时间转换为 UTC 时间（按 Unspecified 时间处理） /
        /// Convert a wall-clock time of the given zone to UTC (treated as an Unspecified-kind time)
        /// </summary>
        /// <param name="time">时区 local 下的时间 / The time in zone local</param>
        /// <param name="local">时间所在时区 / The zone the time belongs to</param>
        /// <returns>UTC 时间 / The UTC time</returns>
        public static DateTime LocalToUtc(DateTime time, TimeZoneInfo local)
        {
            DateTime localTime = TimeZoneInfo.ConvertTime(new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Microsecond, DateTimeKind.Unspecified), local);
            return TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Utc);
        }


        private static readonly HashSet<string> DEFAULT_DATETIME_FORMATS = new HashSet<string>
        {
            // ISO 8601 格式 / ISO 8601 format
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ss.fffffff",
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",
        
            // 常用格式 / Common formats
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss.fff",
        
            // 日期格式 / Date formats
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "yyyyMMdd",
        
            // DateTimeOffset 格式 / DateTimeOffset formats
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ss.fffzzz",
        
            // RFC 1123 格式 / RFC 1123
            "r",
            "R",
        
            // Unix 时间戳 / Unix timestamp
            "unix"
        };

        /// <summary>
        /// 尝试解析时间字符串：先按 Unix 时间戳（纯数字，长度 ≤10 视为秒级、更长视为毫秒级），
        /// 再按内置格式与自定义格式精确匹配（含放宽空白/假定本地的二次尝试），最后回退通用解析 /
        /// Try to parse a datetime string: first as a Unix timestamp (pure digits, up to 10 characters as
        /// seconds, longer as milliseconds), then exactly against built-in and custom formats (with a relaxed
        /// second pass allowing whitespace / assuming local), finally falling back to a general parse
        /// </summary>
        /// <param name="dateTimeString">待解析字符串 / The string to parse</param>
        /// <param name="customFormats">自定义格式串，追加在内置格式之后 / Custom format strings appended after the built-ins</param>
        /// <param name="dateTime">解析结果，失败时为 default / The parsed value, default on failure</param>
        /// <param name="detectedFormat">命中的格式（如 "unix-seconds"、"unix-milliseconds"、"yyyy-MM-dd"、"r"、"auto"），失败时为 null / The matched format (e.g. "unix-seconds", "unix-milliseconds", "yyyy-MM-dd", "r", "auto"), null on failure</param>
        /// <returns>是否解析成功 / true when parsed successfully</returns>
        public static bool TryParseDateTimeString(string dateTimeString, string[]? customFormats,
            out DateTime dateTime, out string? detectedFormat)
        {
            dateTime = default;
            detectedFormat = null;

            if (string.IsNullOrEmpty(dateTimeString))
                return false;

            // 先尝试 Unix 时间戳（秒或毫秒）/ try Unix timestamp first (seconds or milliseconds)
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

            // 合并默认格式和自定义格式 / merge built-in and custom formats
            var allFormats = DEFAULT_DATETIME_FORMATS.ToList();
            if (customFormats != null)
            {
                allFormats.AddRange(customFormats.Where(f => !string.IsNullOrEmpty(f)));
            }

            // 逐个尝试每种格式 / try each format in turn
            foreach (var format in allFormats)
            {
                // 处理特殊格式（RFC 1123）/ handle special formats (RFC 1123)
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

                // 标准格式解析 / standard exact-format parse
                if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out dateTime))
                {
                    detectedFormat = format;
                    return true;
                }

                // 放宽条件再试（允许空白、假定本地）/ retry relaxed (allow whitespace, assume local)
                if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dateTime))
                {
                    detectedFormat = format;
                    return true;
                }
            }

            // 最后尝试通用解析 / finally fall back to a general parse
            if (DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                detectedFormat = "auto";
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试解析带时区偏移的时间字符串：先按 Unix 时间戳（长度 ≤10 秒级、更长毫秒级），
        /// 再尝试通用 DateTimeOffset 解析，最后按内置/自定义格式精确匹配 /
        /// Try to parse a datetime string with offset: first as a Unix timestamp (up to 10 characters as
        /// seconds, longer as milliseconds), then with a general DateTimeOffset parse, finally exactly
        /// against built-in/custom formats
        /// </summary>
        /// <param name="dateTimeString">待解析字符串 / The string to parse</param>
        /// <param name="customFormats">自定义格式串，追加在内置格式之后 / Custom format strings appended after the built-ins</param>
        /// <param name="dateTimeOffset">解析结果，失败时为 default / The parsed value, default on failure</param>
        /// <param name="detectedFormat">命中的格式（如 "unix-seconds"、"unix-milliseconds"、"auto"），失败时为 null / The matched format (e.g. "unix-seconds", "unix-milliseconds", "auto"), null on failure</param>
        /// <returns>是否解析成功 / true when parsed successfully</returns>
        public static bool TryParseDateTimeOffsetString(string dateTimeString, string[]? customFormats,
            out DateTimeOffset dateTimeOffset, out string? detectedFormat)
        {
            dateTimeOffset = default;
            detectedFormat = null;

            if (string.IsNullOrEmpty(dateTimeString))
                return false;

            // 先尝试 Unix 时间戳 / try Unix timestamp first
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

            // 先尝试通用 DateTimeOffset 解析 / try a general DateTimeOffset parse first
            if (DateTimeOffset.TryParse(dateTimeString, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out dateTimeOffset))
            {
                detectedFormat = "auto";
                return true;
            }

            // 再逐个尝试精确格式 / then try each exact format
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
        /// 按指定格式将 DateTime 格式化为字符串（InvariantCulture）；"unix"/"unix-seconds"/"unix-milliseconds" 输出 Unix 时间戳 /
        /// Format a DateTime with the given format (InvariantCulture); "unix"/"unix-seconds"/"unix-milliseconds" emit a Unix timestamp
        /// </summary>
        /// <remarks>convertToString 为 false 或 format 为空时返回 null。Returns null when convertToString is false or format is empty.</remarks>
        /// <param name="dateTime">待格式化时间 / The value to format</param>
        /// <param name="format">格式串或 unix 特殊值 / The format string or a unix keyword</param>
        /// <param name="convertToString">是否转换为字符串 / Whether to convert to a string</param>
        /// <returns>格式化结果，不转换时为 null / The formatted string, or null when not converting</returns>
        public static string? FormatDateTime(DateTime dateTime, string? format, bool convertToString)
        {
            if (!convertToString || string.IsNullOrEmpty(format))
                return null;

            // 特殊格式处理（unix 关键字）/ handle special formats (the unix keywords)
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

        /// <summary>
        /// 按指定格式将 DateTimeOffset 格式化为字符串（InvariantCulture）；"unix"/"unix-seconds"/"unix-milliseconds" 输出 Unix 时间戳 /
        /// Format a DateTimeOffset with the given format (InvariantCulture); "unix"/"unix-seconds"/"unix-milliseconds" emit a Unix timestamp
        /// </summary>
        /// <remarks>convertToString 为 false 或 format 为空时返回 null。Returns null when convertToString is false or format is empty.</remarks>
        /// <param name="dateTimeOffset">待格式化时间 / The value to format</param>
        /// <param name="format">格式串或 unix 特殊值 / The format string or a unix keyword</param>
        /// <param name="convertToString">是否转换为字符串 / Whether to convert to a string</param>
        /// <returns>格式化结果，不转换时为 null / The formatted string, or null when not converting</returns>
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
                // 带 offset 的 ISO 8601 / ISO 8601 with offset
                "yyyy-MM-ddTHH:mm:sszzz",
                "yyyy-MM-ddTHH:mm:ss.fffzzz",
                "yyyy-MM-ddTHH:mm:ss.fffffffzzz",
                "yyyy-MM-dd HH:mm:sszzz",
            
                // RFC 1123 格式 / RFC 1123
                "r",
                "R",
            
                // 标准格式 / Standard formats
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
        /// 判断字符串是否可解析为时间（Unix 时间戳或任一支持的格式） /
        /// Determine whether the string parses as a datetime (Unix timestamp or any supported format)
        /// </summary>
        /// <remarks>任何可整型解析的字符串都会被视为 Unix 时间戳而返回 true。Any string parseable as an integer is treated as a Unix timestamp and returns true.</remarks>
        /// <param name="value">待判断字符串 / The string to check</param>
        /// <param name="customFormats">自定义格式串 / Custom format strings</param>
        /// <returns>可解析返回 true / true when it parses</returns>
        public static bool IsDateTimeString(string value, string[]? customFormats = null)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // 检查是否是 Unix 时间戳 / check for a Unix timestamp
            if (long.TryParse(value, out _))
                return true;

            // 尝试解析 / try parsing
            return TryParseDateTimeString(value, customFormats, out _, out _) ||
                   TryParseDateTimeOffsetString(value, customFormats, out _, out _);
        }

    }
}
