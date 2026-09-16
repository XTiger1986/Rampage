using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 自定义正则表达式格式
    /// </summary>
    public class RegexFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            if (string.IsNullOrEmpty(Pattern))
            {
                return true; 
            }

            Regex regex = new Regex(Pattern, Options != null ? Options.Value : RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

        /// <summary>
        /// 
        /// </summary>
        public string? Pattern
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public RegexOptions? Options
        {
            get; set;
        }

    }
}
