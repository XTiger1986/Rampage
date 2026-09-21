using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 宽松密码格式：5-20 位可见 ASCII 字符（无空格，不要求字符种类）
    /// / Loose password format: 5-20 printable ASCII characters (no spaces, no character-class requirement)
    /// </summary>
    /// <remarks>值为 null 时按 Required 决定通过与否 / A null value passes or fails according to Required</remarks>
    public class SimplePasswordFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否为合法密码 / Check that the value is a valid password
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>匹配格式返回 true / true when the format matches</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            // 仅无空格 / No spaces only
            string pattern = @"^[\x21-\x7E]{5,20}$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

    }
}
