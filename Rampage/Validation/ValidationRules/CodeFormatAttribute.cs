using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 简码格式：仅由字母和数字组成，长度 2-20（不含下划线）/ Short code format: letters and digits only, length 2-20 (no underscores)
    /// </summary>
    /// <remarks>值为 null 时按 Required 决定通过与否 / A null value passes or fails according to Required</remarks>
    public class CodeFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否为合法简码 / Check that the value is a valid short code
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>匹配格式返回 true / true when the format matches</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            string pattern = @"^[a-zA-Z0-9]{2,20}$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

    }
}
