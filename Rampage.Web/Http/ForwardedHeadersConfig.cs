using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Rampage.Log;
using Rampage.Web.Utilities;
using System;
using System.Net;

namespace Rampage.Web.Http
{
    /// <summary>
    /// 反向代理转发头配置（配置�?ForwardedHeaders）�?
    /// 类职责：决定是否挂载 ASP.NET Core UseForwardedHeaders 中间件，以及 WebTool.GetClientIP 的解析模式�?
    /// 默认关闭：只信任连接�?RemoteIpAddress，任何客户端伪造的 X-Forwarded-For 都不会被采纳�?
    /// 部署�?NGINX 等反向代理之后时，将 Enabled 设为 true 并把代理地址加入 KnownProxies / KnownNetworks�?
    /// </summary>
    public class ForwardedHeadersConfig
    {
        public const string SectionName = "ForwardedHeaders";

        /// <summary>是否启用 UseForwardedHeaders 中间件，默认 false�?/summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 受信任的代理 IP 列表（如 "127.0.0.1", "10.0.0.5"）。为空且 TrustAllProxies=false 时使�?ASP.NET Core 默认（仅本机回环）�?
        /// </summary>
        public string[] KnownProxies { get; set; } = Array.Empty<string>();

        /// <summary>受信任的代理网段（CIDR，如 "10.0.0.0/8"）�?/summary>
        public string[] KnownNetworks { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 信任任意来源的转发头（清�?KnownProxies/KnownNetworks）。仅在代理层已确保覆盖客户端传入�?X-Forwarded-For 时使用�?
        /// </summary>
        public bool TrustAllProxies { get; set; } = false;

        /// <summary>X-Forwarded-For 最多处理的跳数，默�?1（一层代理）�?/summary>
        public int ForwardLimit { get; set; } = 1;

        /// <summary>
        /// WebTool.GetClientIP 解析模式�?Connection"（默认）�?"LegacyHeaders"（旧行为，无条件信任请求头，仅用于过渡）�?
        /// </summary>
        public string ClientIpMode { get; set; } = nameof(WebTool.ClientIpMode.Connection);

        /// <summary>�?GlobalConfiguration 读取 ForwardedHeaders 节；缺省返回安全默认值�?/summary>
        public static ForwardedHeadersConfig Load()
        {
            try
            {
                ForwardedHeadersConfig? cfg = GlobalConfiguration.GetSectionConfig<ForwardedHeadersConfig>(SectionName);
                if (cfg == null)
                {
                    return new ForwardedHeadersConfig();
                }
                cfg.KnownProxies ??= Array.Empty<string>();
                cfg.KnownNetworks ??= Array.Empty<string>();
                return cfg;
            }
            catch
            {
                return new ForwardedHeadersConfig();
            }
        }

        /// <summary>
        /// 按配置挂�?UseForwardedHeaders 并设�?WebTool 的客户端 IP 解析模式�?
        /// </summary>
        public static void Apply(IApplicationBuilder app)
        {
            ForwardedHeadersConfig cfg = Load();

            if (Enum.TryParse(cfg.ClientIpMode, true, out WebTool.ClientIpMode mode))
            {
                WebTool.ClientIpResolveMode = mode;
            }
            if (WebTool.ClientIpResolveMode == WebTool.ClientIpMode.LegacyHeaders)
            {
                RampageLogger.Warn("ForwardedHeaders:ClientIpMode=LegacyHeaders trusts X-Forwarded-For/X-Real-IP from any client; " +
                                   "switch to ForwardedHeaders:Enabled=true with KnownProxies as soon as possible.");
            }

            if (!cfg.Enabled)
            {
                return;
            }

            ForwardedHeadersOptions options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
                ForwardLimit = cfg.ForwardLimit > 0 ? cfg.ForwardLimit : 1,
            };

            if (cfg.TrustAllProxies)
            {
                options.KnownProxies.Clear();
                options.KnownNetworks.Clear();
            }
            else
            {
                foreach (string proxy in cfg.KnownProxies)
                {
                    if (IPAddress.TryParse(proxy?.Trim(), out IPAddress? ip))
                    {
                        options.KnownProxies.Add(ip);
                    }
                    else
                    {
                        RampageLogger.Warn($"ForwardedHeaders:KnownProxies contains an invalid IP '{proxy}'.");
                    }
                }
                foreach (string network in cfg.KnownNetworks)
                {
                    if (TryParseNetwork(network, out Microsoft.AspNetCore.HttpOverrides.IPNetwork? ipNetwork))
                    {
                        options.KnownNetworks.Add(ipNetwork!);
                    }
                    else
                    {
                        RampageLogger.Warn($"ForwardedHeaders:KnownNetworks contains an invalid CIDR '{network}'.");
                    }
                }
            }

            app.UseForwardedHeaders(options);
        }

        private static bool TryParseNetwork(string? cidr, out Microsoft.AspNetCore.HttpOverrides.IPNetwork? network)
        {
            network = null;
            if (string.IsNullOrWhiteSpace(cidr))
            {
                return false;
            }
            string[] parts = cidr.Trim().Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out IPAddress? prefix) || !int.TryParse(parts[1], out int length))
            {
                return false;
            }
            network = new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, length);
            return true;
        }
    }
}
