using System;
using System.Text.RegularExpressions;

namespace Rampage.Validation
{
    /// <summary>
    /// 邮箱格式校验（RFC 5322 简化版：local@domain.tld，禁止空白与连续点）
    /// / Email format validation (simplified RFC 5322: local@domain.tld, whitespace and consecutive dots rejected)
    /// </summary>
    /// <remarks>总长不超过 254；正则预编译并带 200ms 匹配超时；空白值按 Required 决定通过与否 / Total length capped at 254; precompiled regex with a 200 ms match timeout; blank values resolve by Required</remarks>
    public class EmailFormatAttribute : ValidationAttributeBase
    {
        private const int MaxLength = 254;

        // 预编译 + 匹配超时，避免每次校验重新构造 Regex 以及恶意输入导致的回溯放大 / Precompiled + match timeout: avoids re-creating the Regex each check and bounds backtracking on hostile input
        private static readonly Regex EmailRegex = new Regex(
            @"^(?!\.)[A-Za-z0-9!#$%&'*+/=?^_`{|}~.-]{1,64}(?<!\.)@(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(200));

        /// <summary>
        /// 验证值是否为合法邮箱地址 / Check that the value is a valid email address
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>匹配格式返回 true；超长、含连续点或匹配超时返回 false / true when the format matches; false when too long, containing consecutive dots, or timing out</returns>
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
