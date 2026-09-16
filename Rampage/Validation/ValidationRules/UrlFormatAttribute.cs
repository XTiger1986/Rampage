using System;

namespace Rampage.Validation
{
    /// <summary>
    /// URL 格式校验：必须是绝对 URI，且协议为 http/https（可通过 AllowedSchemes 扩展）
    /// </summary>
    public class UrlFormatAttribute : ValidationAttributeBase
    {
        private const int MaxLength = 2048;

        /// <summary>
        /// 允许的协议（小写），默认 http、https
        /// </summary>
        public string[] AllowedSchemes { get; set; } = new[] { Uri.UriSchemeHttp, Uri.UriSchemeHttps };

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
