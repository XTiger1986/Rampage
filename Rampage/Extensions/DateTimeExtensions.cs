using System;

namespace Rampage.Extensions
{
    /// <summary>
    /// DateTime 扩展方法 / DateTime extensions
    /// </summary>
    public static class DateTimeExtensions
    {

        /// <summary>
        /// 格式化为 yyyy-MM-dd HH:mm:ss / Formats the value as yyyy-MM-dd HH:mm:ss
        /// </summary>
        /// <param name="dt">待格式化的时间 / DateTime to format</param>
        /// <returns>格式化后的字符串 / The formatted string</returns>
        public static string ToCommonString(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 格式化为 yyyy-MM-dd / Formats the value as yyyy-MM-dd
        /// </summary>
        /// <param name="date">待格式化的日期 / Date to format</param>
        /// <returns>格式化后的字符串 / The formatted string</returns>
        public static string ToCommonDate(this DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

    }
}
