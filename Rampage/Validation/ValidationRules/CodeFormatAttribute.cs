using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// A shortcode format made up of only letters, numbers, and underscores
    /// 仅由字母数字下划线组成的简码格式
    /// </summary>
    public class CodeFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            string pattern = @"^[a-zA-Z0-9]{2,20}$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

    }
}
