using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// Strict Password Format
    /// 严格密码格式
    /// </summary>
    public class StrictPasswordFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            // 必须有数字、字母、特殊符号，无空格
            string pattern = @"^(?=.*[A-Za-z])(?=.*\\d)(?=.*[^A-Za-z0-9])[\\x21-\\x7E]{5,20}$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

    }
}
