using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 用户名格式：不能为纯数字不能包含特殊字符
    /// </summary>
    public class UsernameFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            string pattern = @"^(?!^\d+$)(?!^\W+$)[^#!*><\/=\)\(&\^@,\.\:;'""\s-]{3,20}$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

    }
}
