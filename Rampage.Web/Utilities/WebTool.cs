using Microsoft.AspNetCore.Http;
using Rampage.Data;
using Rampage.Reflection;
using Rampage.Utilities;
using Rampage.Web.Extensions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;

namespace Rampage.Web.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class WebTool
    {
        public static string? GetClientIP()
        {
            HttpContext? context = RequestContext.Current;
            if (context == null)
            {
                return null;
            }
            HttpRequest request = context.Request;
            return GetClientIP(request);
        }
        /// <summary>
        /// 客户端 IP 解析模式
        /// </summary>
        public enum ClientIpMode
        {
            /// <summary>
            /// 仅使用连接层 RemoteIpAddress（默认）。如需识别反向代理后的真实 IP，请在配置节 ForwardedHeaders 中启用
            /// UseForwardedHeaders 中间件，它会用受信任代理传来的 X-Forwarded-For 改写 RemoteIpAddress。
            /// </summary>
            Connection = 0,

            /// <summary>
            /// 旧行为：无条件信任 X-Real-IP / X-Forwarded-For 等请求头。任何客户端都能伪造，仅用于兼容过渡。
            /// </summary>
            LegacyHeaders = 1,
        }

        /// <summary>
        /// 当前客户端 IP 解析模式，由 RampageWebApplication 依据配置节 ForwardedHeaders:ClientIpMode 设置。
        /// </summary>
        public static ClientIpMode ClientIpResolveMode { get; set; } = ClientIpMode.Connection;

        /// <summary>
        /// 获取客户端 IP。默认只信任连接层地址；反向代理场景请启用 ForwardedHeaders 配置而不是直接读请求头。
        /// </summary>
        /// <returns></returns>
        public static string? GetClientIP(HttpRequest request)
        {
            HttpContext context = request.HttpContext;
            string? userIP = null;

            if (ClientIpResolveMode == ClientIpMode.LegacyHeaders)
            {
                if (!string.IsNullOrWhiteSpace(request.Headers["X-Real-IP"]))
                {
                    userIP = request.Headers["X-Real-IP"];
                }
                if (string.IsNullOrWhiteSpace(userIP) && !string.IsNullOrWhiteSpace(request.Headers["HTTP_X_FORWARDED_FOR"]))
                {
                    userIP = request.Headers["HTTP_X_FORWARDED_FOR"];
                }
                if (string.IsNullOrWhiteSpace(userIP) && !string.IsNullOrEmpty(request.Headers["X-Forwarded-For"]))
                {
                    userIP = request.Headers["X-Forwarded-For"];
                }
                if (!string.IsNullOrWhiteSpace(userIP))
                {
                    // X-Forwarded-For 可能是 "client, proxy1, proxy2"，取第一段并去掉端口
                    int comma = userIP.IndexOf(',');
                    if (comma >= 0)
                    {
                        userIP = userIP.Substring(0, comma);
                    }
                    userIP = userIP.Trim();
                    if (IPAddress.TryParse(userIP, out IPAddress? parsed))
                    {
                        userIP = parsed.ToString();
                    }
                    else if (IPEndPoint.TryParse(userIP, out IPEndPoint? endPoint))
                    {
                        userIP = endPoint.Address.ToString();
                    }
                }
            }

            if (string.IsNullOrEmpty(userIP))
            {
                IPAddress? remote = context.Connection.RemoteIpAddress;
                if (remote != null && remote.IsIPv4MappedToIPv6)
                {
                    remote = remote.MapToIPv4();
                }
                userIP = remote?.ToString();
            }
            return userIP;
        }

        /// <summary>
        /// 是否本站或本地网址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsLocalUrl(string url)
        {
            bool result = true;
            HttpContext? context = RequestContext.Current;
            if (context == null)
            {
                return false;
            }
            HttpRequest request = context.Request;
            string selfHost = request.Host.Host;

            url = HttpUtility.UrlDecode(url);

            Uri? uri;
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
            {
                if (uri.IsAbsoluteUri)
                {
                    string host = uri.Host;
                    string localHost = request.Host.Value;

                    if (string.Compare(host, localHost, true) != 0 &&  false == StringTool.EndsWithIgnoreCase(host, selfHost))
                    {
                        result = false;
                    }
                }
            }


            return result;
        }

        private static readonly Lazy<HashSet<IPAddress>> _localIps = new(() =>
        {
            var ips = new HashSet<IPAddress>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                var addIp = ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
                ips.Add(addIp);
            }
            // 添加回环地址
            ips.Add(IPAddress.Loopback);
            ips.Add(IPAddress.IPv6Loopback);
            return ips;
        });

        public static bool IsLocalIpAddress(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress) || !IPAddress.TryParse(ipAddress, out var inputIp))
                return false;

            if (IPAddress.IsLoopback(inputIp))
                return true;

            if (inputIp.IsIPv4MappedToIPv6)
                inputIp = inputIp.MapToIPv4();

            return _localIps.Value.Contains(inputIp);
        }

        #region Cookie

        /// <summary>
        /// 移除Cookie
        /// </summary>
        /// <param name="cookieKey">键名</param>
        public static void RemoveCookie(string cookieKey)
        {
            RemoveCookie(cookieKey, null);
        }

        /// <summary>
        /// 移除Cookie
        /// </summary>
        /// <param name="cookieKey">键名</param>
        /// <param name="domain"></param>
        public static void RemoveCookie(string cookieKey, string? domain)
        {
            if (RequestContext.Current == null)
            {
                return;
            }
            CookieOptions options = new CookieOptions();
            if (!string.IsNullOrEmpty(domain))
                options.Domain = domain;

            HttpContext context = RequestContext.Current;
            HttpResponse response = context.Response;
            response.Cookies.Delete(cookieKey, options);
            response.Cookies.Delete(cookieKey);
        }

        /// <summary>
        /// 修改Cookie的值 如果不存在键则创建 (不设置过期时间)
        /// </summary>
        /// <param name="cookieKey"></param>
        /// <param name="cookieValue"></param>
        public static void SetCookie(string cookieKey, string? cookieValue)
        {
            SetCookie(cookieKey, cookieValue, null);
        }

        /// <summary>
        /// 修改Cookie的值 如果不存在键则创建
        /// </summary>
        /// <param name="cookieKey"></param>
        /// <param name="cookieValue"></param>
        /// <param name="expires">过期时间</param>
        public static void SetCookie(string cookieKey, string? cookieValue, DateTime? expires)
        {
            SetCookie(cookieKey, cookieValue, null, expires);
        }

        /// <summary>
        /// 修改Cookie的值 如果不存在键则创建
        /// </summary>
        /// <param name="cookieKey"></param>
        /// <param name="cookieValue"></param>
        /// <param name="expires">过期时间</param>
        /// <param name="domain"></param>
        public static void SetCookie(string cookieKey, string? cookieValue, string? domain, DateTime? expires)
        {
            SetCookie(cookieKey, cookieValue, domain, expires, httpOnly: true, sameSite: SameSiteMode.Lax);
        }

        /// <summary>
        /// 修改Cookie的值 如果不存在键则创建。
        /// 默认 HttpOnly=true、SameSite=Lax、Secure 随请求协议（HTTPS 请求下自动加 Secure），用于认证类 Cookie；
        /// 需要前端脚本读取的 Cookie 请显式传 httpOnly:false。
        /// </summary>
        public static void SetCookie(string cookieKey, string? cookieValue, string? domain, DateTime? expires, bool httpOnly, SameSiteMode sameSite, bool? secure = null)
        {
            HttpContext? context = RequestContext.Current;
            if (context == null)
            {
                return;
            }

            CookieOptions options = new CookieOptions
            {
                HttpOnly = httpOnly,
                SameSite = sameSite,
                Secure = secure ?? context.Request.IsHttps,
            };
            if (expires != null)
                options.Expires = expires.Value;
            if (!string.IsNullOrEmpty(domain))
                options.Domain = domain;

            HttpResponse response = context.Response;
            if (cookieValue == null)
                RemoveCookie(cookieKey, domain);
            else
                response.Cookies.Append(cookieKey, cookieValue, options);
        }

        /// <summary>
        /// 获取Cookie
        /// </summary>
        /// <param name="cookieKey"></param>
        /// <returns></returns>
        public static string GetCookie(string cookieKey)
        {
            HttpContext? context = RequestContext.Current;
            if (context == null)
            {
                return string.Empty;
            }
            HttpRequest request = context.Request;
            if (request.Cookies == null)
            {
                return string.Empty;
            }
            string? value;
            request.Cookies.TryGetValue(cookieKey, out value);
            if (string.IsNullOrEmpty(value))
                value = string.Empty;
            return value;
        }

        #endregion

        /// <summary>
        /// 从GET请求中获取模型的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public static T RenderGetModel<T>(T model) where T : class, new()
        {
            return RenderRequestModel<T>(model, HttpRequestMethod.Get);
        }
        /// <summary>
        /// 从POST请求中获取模型的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public static T RenderPostModel<T>(T model) where T : class, new()
        {
            return RenderRequestModel<T>(model, HttpRequestMethod.Post);
        }
        /// <summary>
        /// 从请求中获取模型的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="httpMethod"></param>
        /// <returns></returns>
        public static T RenderRequestModel<T>(T model, HttpRequestMethod httpMethod) where T : class, new()
        {
            HttpContext? context = RequestContext.Current;
            if (context == null)
            {
                return new T();
            }
            HttpRequest request = context.Request;
            if (request == null)
            {
                return new T();
            }
            if (httpMethod == HttpRequestMethod.Post && (string.Compare(request.Method, "POST", true) != 0 || request.Form == null))
            {
                return new T();
            }
            else if (request.Query == null)
            {
                return new T();
            }

            if (model == null)
            {
                model = new T();
            }
            TableMapping? schema = ModelSchemaStore.GetModelMapping<T>();
            if (schema == null)
                return new T();

            foreach (var column in schema.FieldMappingList!)
            {
                PropertyInfo prp = column.Property!;
                Type prpType = prp.PropertyType;
                string formName = column.PropertyName!;

                string? itemStrValue = null;
                switch (httpMethod)
                {
                    case HttpRequestMethod.Get:
                        if (request.Query.ContainsKey(formName))
                            itemStrValue = request.Query[formName];
                        break;
                    case HttpRequestMethod.Post:
                        if (request.Form.ContainsKey(formName))
                            itemStrValue = request.Form[formName];
                        break;
                    default:
                        if (request.Query.ContainsKey(formName))
                            itemStrValue = request.Query[formName];
                        else if (request.Form.ContainsKey(formName))
                            itemStrValue = request.Form[formName];
                        break;
                }
                if (itemStrValue == null)
                    continue;

                itemStrValue = itemStrValue.Trim();
                Type basePrpType = GenericTool.GetUnderlyingType(prpType);

                object? itemValue = null;
                bool isParsed = StringTool.TryParse(itemStrValue, basePrpType, out itemValue);
                if (isParsed && itemValue != null)
                {
                    EmitTool.SetPropertyValue(model, prp.Name, itemValue);
                    //prp.SetValue(model, itemValue, null);
                }
            }

            return model;
        }


        public static async Task<string> GetRequestBodyStringAsync(HttpRequest request)
        {
            request.EnableBuffering();

            using (var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true))
            {
                request.Body.Position = 0;
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                return body;
            }
        }

        public static string GetRequestBodyString(HttpRequest request)
        {
            request.EnableBuffering();

            using (var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true))
            {
                request.Body.Position = 0;
                var body = reader.ReadToEnd();
                request.Body.Position = 0;
                return body;
            }
        }

    }
}
