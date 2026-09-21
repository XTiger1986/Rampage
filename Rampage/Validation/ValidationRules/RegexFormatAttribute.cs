using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 自定义正则表达式格式验证 / Custom regular-expression format validation
    /// </summary>
    /// <remarks>
    /// Pattern 为空视为直接通过；未指定 Options 时默认 IgnoreCase | Singleline | IgnorePatternWhitespace；
    /// An empty Pattern passes immediately; when Options is unset the default is IgnoreCase | Singleline | IgnorePatternWhitespace;
    /// 值为 null 时按 Required 决定通过与否。A null value passes or fails according to Required.
    /// </remarks>
    public class RegexFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否匹配指定正则 / Check that the value matches the pattern
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>匹配返回 true / true when the value matches</returns>
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
        /// 正则表达式模式（空值视为通过）/ The regular expression pattern (empty passes)
        /// </summary>
        public string? Pattern
        {
            get; set;
        }

        /// <summary>
        /// 正则选项；未设置时使用默认组合 / Regex options; the default combination when unset
        /// </summary>
        public RegexOptions? Options
        {
            get; set;
        }

    }
}
