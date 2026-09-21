using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 严格密码格式：5-20 位可见 ASCII 字符（无空格），须同时包含数字、字母和特殊符号
    /// / Strict password format: 5-20 printable ASCII characters (no spaces), must contain digits, letters and special characters
    /// </summary>
    /// <remarks>
    /// 值为 null 时按 Required 决定通过与否。A null value passes or fails according to Required.
    /// </remarks>
    public class StrictPasswordFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否为合法严格密码 / Check that the value is a valid strict password
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>匹配格式返回 true / true when the format matches</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            // 必须有数字、字母、特殊符号，无空格 / Must contain digits, letters and special characters, no spaces
            string pattern = @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z0-9])[\x21-\x7E]{5,20}$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

    }
}
