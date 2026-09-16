using System;
using System.Linq;

namespace Rampage.Web.Cors
{
    /// <summary>
    /// CORS 白名单配置（配置节 Cors）。
    /// 类职责：从配置加载允许的 Origin，供 Web 宿主注册 CORS 策略。
    /// </summary>
    public class CorsOptions
    {
        public const string SectionName = "Cors";

        /// <summary>
        /// 是否启用 CORS 中间件与策略注册。
        /// 同域后台（如 ManagementSite 自调用 /api）可设 false，避免无意义的跨域策略干扰。
        /// WebApi 等需跨域的宿主保持 true。
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 允许的完整 Origin 列表，如 https://www.example.com（含协议与端口，不含尾斜杠更佳）。
        /// </summary>
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 是否额外放行 localhost / 127.0.0.1（任意端口）。开发默认 true，生产建议 false。
        /// </summary>
        public bool AllowLocalhost { get; set; } = true;

        /// <summary>从 GlobalConfiguration 读取 Cors 节；缺省返回安全默认值。</summary>
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
        /// 判断请求 Origin 是否允许（精确匹配 AllowedOrigins；可选放行本机）。
        /// </summary>
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
