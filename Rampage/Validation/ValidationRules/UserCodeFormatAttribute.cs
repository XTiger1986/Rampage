using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 用户简码格式：由字母、数字、下划线组成（3-20 位），不能为纯数字、不能包含特殊字符
    /// / User short code: letters, digits and underscores only (3-20 chars); pure digits and special characters are rejected
    /// </summary>
    /// <remarks>值为 null 时按 Required 决定通过与否 / A null value passes or fails according to Required</remarks>
    public class UserCodeFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否为合法用户简码 / Check that the value is a valid user code
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>匹配格式返回 true / true when the format matches</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            string pattern = @"^(?!^\d+$)(?!^\W+$)[a-zA-Z0-9_]{3,20}$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

    }
}
