using System;
using System.Linq;

namespace Rampage.Web.Cors
{
    /// <summary>
    /// CORS 白名单配置（配置节 Cors）。 / CORS allowlist options (the Cors configuration section).
    /// 类职责：从配置加载允许的 Origin，供 Web 宿主注册 CORS 策略。
    /// Responsibility: loads the allowed origins from configuration for the web host to register CORS policies.
    /// </summary>
    public class CorsOptions
    {
        /// <summary>配置节名称 / The configuration section name</summary>
        public const string SectionName = "Cors";

        /// <summary>
        /// 是否启用 CORS 中间件与策略注册。 / Whether to enable the CORS middleware and policy registration.
        /// 同域后台（如 ManagementSite 自调用 /api）可设 false，避免无意义的跨域策略干扰。
        /// Same-origin backends (e.g. ManagementSite calling its own /api) may set false to avoid pointless cross-origin policies.
        /// WebApi 等需跨域的宿主保持 true。 Keep true for cross-origin hosts such as WebApi.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 允许的完整 Origin 列表，如 https://www.example.com（含协议与端口，不含尾斜杠更佳）。 / The full allowed origin list, e.g. https://www.example.com (scheme and port included; a trailing slash is best avoided).
        /// </summary>
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 是否额外放行 localhost / 127.0.0.1（任意端口）。开发默认 true，生产建议 false。 / Whether to additionally allow localhost / 127.0.0.1 (any port). Defaults to true for development; recommended false in production.
        /// </summary>
        public bool AllowLocalhost { get; set; } = true;

        /// <summary>从 GlobalConfiguration 读取 Cors 节；缺省返回安全默认值。 / Loads the Cors section from GlobalConfiguration; returns safe defaults when absent.</summary>
        /// <returns>CORS 配置 / The CORS options</returns>
        public static CorsOptions Load()
        {
            try
            {
                CorsOptions? cfg = GlobalConfiguration.GetSectionConfig<CorsOptions>(SectionName);
                if (cfg == null)
                {
                    return new CorsOptions();
                }
                cfg.AllowedOrigins ??= Array.Empty<string>();
                return cfg;
            }
            catch
            {
                return new CorsOptions();
            }
        }

        /// <summary>
        /// 判断请求 Origin 是否允许（精确匹配 AllowedOrigins；可选放行本机）。 / Decides whether a request origin is allowed (exact match against AllowedOrigins; localhost optionally allowed).
        /// </summary>
        /// <param name="origin">请求 Origin 头的值 / The value of the Origin request header</param>
        /// <returns>true 表示允许 / True when allowed</returns>
        public bool IsOriginAllowed(string? origin)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                return false;
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }

            if (AllowLocalhost && (uri.Host is "localhost" or "127.0.0.1"))
            {
                return true;
            }

            string normalized = NormalizeOrigin(origin);
            return AllowedOrigins.Any(a =>
                !string.IsNullOrWhiteSpace(a)
                && string.Equals(NormalizeOrigin(a), normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeOrigin(string origin)
        {
            return origin.Trim().TrimEnd('/');
        }
    }
}
