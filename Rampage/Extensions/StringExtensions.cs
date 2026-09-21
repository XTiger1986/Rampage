using Rampage.Utilities;
using System;

namespace Rampage.Extensions
{
    /// <summary>
    /// 字符串扩展方法 / String extensions
    /// </summary>
    public static class StringExtensions
    {

        /// <summary>
        /// 忽略大小写比较两个字符串是否相等（序数规则） / Case-insensitive equality comparison using ordinal rules
        /// </summary>
        /// <param name="s1">字符串 1 / First string</param>
        /// <param name="s2">字符串 2 / Second string</param>
        /// <returns>忽略大小写相等时返回 true / true when the strings are equal ignoring case</returns>
        public static bool EqualsIgnoreCase(this string? s1, string? s2)
        {
            return StringTool.EqualsIgnoreCase(s1, s2);
        }

        /// <summary>
        /// 忽略大小写判断文本是否以指定字符串开头（序数规则） / Case-insensitive ordinal check for a prefix
        /// </summary>
        /// <param name="text">待检查文本 / Text to check</param>
        /// <param name="lookfor">要查找的前缀 / Prefix to look for</param>
        /// <returns>以此前缀开头时返回 true / true when the text starts with the prefix ignoring case</returns>
        public static bool StartsWithIgnoreCase(this string? text, string? lookfor)
        {
            return StringTool.StartsWithIgnoreCase(text, lookfor);
        }

        /// <summary>
        /// 忽略大小写判断文本是否以指定字符串结尾（序数规则） / Case-insensitive ordinal check for a suffix
        /// </summary>
        /// <param name="text">待检查文本 / Text to check</param>
        /// <param name="lookfor">要查找的后缀 / Suffix to look for</param>
        /// <returns>以此后缀结尾时返回 true / true when the text ends with the suffix ignoring case</returns>
        public static bool EndsWithIgnoreCase(this string? text, string? lookfor)
        {
            return StringTool.EndsWithIgnoreCase(text, lookfor);
        }

    }
}
