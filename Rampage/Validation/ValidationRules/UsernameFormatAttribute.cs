using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 用户名格式：3-20 位，不能为纯数字、不能包含特殊字符（# ! * &gt; &lt; / = ) ( &amp; ^ @ , . : ; ' " 空白及连字符等均被排除）
    /// / Username format: 3-20 chars, not pure digits, no special characters ( # ! * &gt; &lt; / = ) ( &amp; ^ @ , . : ; ' " whitespace and hyphen are all excluded)
    /// </summary>
    /// <remarks>值为 null 时按 Required 决定通过与否 / A null value passes or fails according to Required</remarks>
    public class UsernameFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否为合法用户名 / Check that the value is a valid username
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>匹配格式返回 true / true when the format matches</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            string pattern = @"^(?!^\d+$)(?!^\W+$)[^#!*><\/=\)\(&\^@,\.\:;'""\s-]{3,20}$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

    }
}
