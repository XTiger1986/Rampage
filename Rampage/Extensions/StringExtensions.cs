using Rampage.Utilities;
using System;

namespace Rampage.Extensions
{
    /// <summary>
    /// String Extensions
    /// </summary>
    public static class StringExtensions
    {

        /// <summary>
		/// case insensitive, Compare strings using ordinal (binary) sort rules.
		/// </summary>
		/// <param name="s1">string 1</param>
		/// <param name="s2">string 2</param>
		/// <returns></returns>
        public static bool EqualsIgnoreCase(this string? s1, string? s2)
        {
            return StringTool.EqualsIgnoreCase(s1, s2);
        }

        /// <summary>
		/// case insensitive, check does a text start with a specified string, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="text">text to check</param>
		/// <param name="lookfor">string to look for</param>
		/// <returns></returns>
        public static bool StartsWithIgnoreCase(this string? text, string? lookfor)
        {
            return StringTool.StartsWithIgnoreCase(text, lookfor);
        }

        /// <summary>
		/// case insensitive, check does a text end with a specified string, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="text">text to check</param>
		/// <param name="lookfor">string to look for</param>
		/// <returns></returns>
        public static bool EndsWithIgnoreCase(this string? text, string? lookfor)
        {
            return StringTool.EndsWithIgnoreCase(text, lookfor);
        }

    }
}
