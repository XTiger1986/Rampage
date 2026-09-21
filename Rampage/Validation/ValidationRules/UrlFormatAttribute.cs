using System;

namespace Rampage.Validation
{
    /// <summary>
    /// URL 格式校验：必须是绝对 URI、带主机且协议在 AllowedSchemes 内（默认 http/https，可扩展）
    /// / URL format validation: must be an absolute URI with a host and a scheme listed in AllowedSchemes (http/https by default)
    /// </summary>
    /// <remarks>长度不超过 2048；空白值按 Required 决定通过与否 / Length capped at 2048; blank values resolve by Required</remarks>
    public class UrlFormatAttribute : ValidationAttributeBase
    {
        private const int MaxLength = 2048;

        /// <summary>
        /// 允许的协议（小写），默认 http、https / Allowed schemes (lowercase), http and https by default
        /// </summary>
        public string[] AllowedSchemes { get; set; } = new[] { Uri.UriSchemeHttp, Uri.UriSchemeHttps };

        /// <summary>
        /// 验证值是否为合法 URL / Check that the value is a valid URL
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>匹配格式返回 true / true when the format matches</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }
            string? s = v.ToString();
            if (string.IsNullOrWhiteSpace(s))
            {
                return !Required;
            }
            if (s.Length > MaxLength)
            {
                return false;
            }
            if (!Uri.TryCreate(s, UriKind.Absolute, out Uri? uri))
            {
                return false;
            }
            if (string.IsNullOrEmpty(uri.Host))
            {
                return false;
            }
            foreach (string scheme in AllowedSchemes)
            {
                if (string.Equals(uri.Scheme, scheme, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
