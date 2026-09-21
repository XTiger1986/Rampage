using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 手机号格式：至少 10 位纯数字（不区分国家/地区，不限上限）/ Mobile format: at least 10 digits (region-agnostic, no upper limit)
    /// </summary>
    /// <remarks>值为 null 时按 Required 决定通过与否 / A null value passes or fails according to Required</remarks>
    public class MobileFormatAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否为合法手机号 / Check that the value is a valid mobile number
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>匹配格式返回 true / true when the format matches</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            string pattern = @"^\d{10,}$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

            return regex.IsMatch(v.ToString()!);
        }

    }
}
