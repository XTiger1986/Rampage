using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 邮箱格式校验（RFC 5322 简化版：local@domain.tld，禁止空白与连续点）
    /// </summary>
    public class EmailFormatAttribute : ValidationAttributeBase
    {
        private const int MaxLength = 254;

        // 预编译 + 匹配超时，避免每次校验重新构造 Regex 以及恶意输入导致的回溯放大
        private static readonly Regex EmailRegex = new Regex(
            @"^(?!\.)[A-Za-z0-9!#$%&'*+/=?^_`{|}~.-]{1,64}(?<!\.)@(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(200));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            string? s = v.ToString();
            if (string.IsNullOrWhiteSpace(s))
            {
                return !Required;
            }
            if (s.Length > MaxLength || s.Contains(".."))
            {
                return false;
            }
            try
            {
                return EmailRegex.IsMatch(s);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

    }
}
