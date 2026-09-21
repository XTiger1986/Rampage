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
    /// Web 工具类：客户端 IP 解析、Cookie 读写、请求模型绑定、请求体读取等 / Web utilities: client IP resolution, cookie helpers, request model binding and request body reading.
    /// </summary>
    public class WebTool
    {
        /// <summary>
        /// 获取当前请求的客户端 IP / Gets the client IP of the current request.
        /// </summary>
        /// <returns>客户端 IP；无请求上下文时为 null / The client IP; null when there is no request context.</returns>
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
        /// 客户端 IP 解析模式 / The client IP resolution mode.
        /// </summary>
        public enum ClientIpMode
        {
            /// <summary>
            /// 仅使用连接层 RemoteIpAddress（默认）。如需识别反向代理后的真实 IP，请在配置节 ForwardedHeaders 中启用
            /// UseForwardedHeaders 中间件，它会用受信任代理传来的 X-Forwarded-For 改写 RemoteIpAddress /
            /// Uses only the connection-level RemoteIpAddress (default). To recognize real IPs behind a reverse proxy,
            /// enable the UseForwardedHeaders middleware via the ForwardedHeaders config section.
            /// </summary>
            Connection = 0,

            /// <summary>
            /// 旧行为：无条件信任 X-Real-IP / X-Forwarded-For 等请求头。任何客户端都能伪造，仅用于兼容过渡 /
            /// Legacy behavior: unconditionally trusts X-Real-IP / X-Forwarded-For headers; forgeable by any client, kept only for compatibility.
            /// </summary>
            LegacyHeaders = 1,
        }

        /// <summary>
        /// 当前客户端 IP 解析模式，由 RampageWebApplication 依据配置节 ForwardedHeaders:ClientIpMode 设置 /
        /// The current client IP resolution mode, set by RampageWebApplication from the ForwardedHeaders:ClientIpMode config section.
        /// </summary>
        public static ClientIpMode ClientIpResolveMode { get; set; } = ClientIpMode.Connection;

        /// <summary>
        /// 获取客户端 IP。默认只信任连接层地址；反向代理场景请启用 ForwardedHeaders 配置而不是直接读请求头 /
        /// Gets the client IP. By default only the connection-level address is trusted; enable the ForwardedHeaders config in reverse-proxy scenarios instead of reading headers directly.
        /// </summary>
        /// <param name="request">当前请求 / The current request.</param>
        /// <returns>客户端 IP；取不到时为 null / The client IP; null when unavailable.</returns>
        /// <remarks>
        /// LegacyHeaders 模式下按 X-Real-IP、X-Forwarded-For（含旧式 HTTP_X_FORWARDED_FOR 键）顺序取首段并去掉端口，取不到再回退 RemoteIpAddress；
        /// IPv4 映射的 IPv6 地址会转换为 IPv4。
        /// </remarks>
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
        /// 判断是否本站或本地网址 / Determines whether the URL belongs to the current site or is a local URL.
        /// </summary>
        /// <param name="url">待检查的 URL（会先做 URL 解码）/ The URL to check (URL-decoded first).</param>
        /// <returns>本站/本地网址返回 true；无请求上下文返回 false / true for local URLs; false when there is no request context.</returns>
        /// <remarks>相对地址视为本站；绝对地址要求其 host 与当前请求的 Host 相同（忽略大小写比较，或以请求域名为后缀）。</remarks>
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

        /// <summary>
        /// 判断 IP 是否本机地址（回环或本机网卡地址，网卡列表惰性缓存）/ Determines whether the IP is a local address (loopback or a local network adapter address; the adapter list is lazily cached).
        /// </summary>
        /// <param name="ipAddress">IP 字符串 / The IP string.</param>
        /// <returns>是本机地址返回 true / true if the IP is local.</returns>
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
        /// 移除Cookie / Removes a cookie.
        /// </summary>
        /// <param name="cookieKey">键名 / The cookie key.</param>
        public static void RemoveCookie(string cookieKey)
        {
            RemoveCookie(cookieKey, null);
        }

        /// <summary>
        /// 移除Cookie / Removes a cookie.
        /// </summary>
        /// <param name="cookieKey">键名 / The cookie key.</param>
        /// <param name="domain">所属域 / The cookie domain.</param>
        /// <remarks>无请求上下文时不做任何事；会先后按指定域与默认方式各删除一次，以确保浏览器端过期。</remarks>
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
        /// 修改Cookie的值 如果不存在键则创建（不设置过期时间）/ Sets a cookie value, creating the key when absent (no expiry set).
        /// </summary>
        /// <param name="cookieKey">键名 / The cookie key.</param>
        /// <param name="cookieValue">键值，为 null 时移除该 Cookie / The cookie value; null removes the cookie.</param>
        public static void SetCookie(string cookieKey, string? cookieValue)
        {
            SetCookie(cookieKey, cookieValue, null);
        }

        /// <summary>
        /// 修改Cookie的值 如果不存在键则创建 / Sets a cookie value, creating the key when absent.
        /// </summary>
        /// <param name="cookieKey">键名 / The cookie key.</param>
        /// <param name="cookieValue">键值，为 null 时移除该 Cookie / The cookie value; null removes the cookie.</param>
        /// <param name="expires">过期时间 / The expiry time.</param>
        public static void SetCookie(string cookieKey, string? cookieValue, DateTime? expires)
        {
            SetCookie(cookieKey, cookieValue, null, expires);
        }

        /// <summary>
        /// 修改Cookie的值 如果不存在键则创建 / Sets a cookie value, creating the key when absent.
        /// </summary>
        /// <param name="cookieKey">键名 / The cookie key.</param>
        /// <param name="cookieValue">键值，为 null 时移除该 Cookie / The cookie value; null removes the cookie.</param>
        /// <param name="domain">所属域 / The cookie domain.</param>
        /// <param name="expires">过期时间 / The expiry time.</param>
        public static void SetCookie(string cookieKey, string? cookieValue, string? domain, DateTime? expires)
        {
            SetCookie(cookieKey, cookieValue, domain, expires, httpOnly: true, sameSite: SameSiteMode.Lax);
        }

        /// <summary>
        /// 修改Cookie的值 如果不存在键则创建 / Sets a cookie value, creating the key when absent.
        /// </summary>
        /// <param name="cookieKey">键名 / The cookie key.</param>
        /// <param name="cookieValue">键值，为 null 时移除该 Cookie / The cookie value; null removes the cookie.</param>
        /// <param name="domain">所属域 / The cookie domain.</param>
        /// <param name="expires">过期时间 / The expiry time.</param>
        /// <param name="httpOnly">是否 HttpOnly / Whether the cookie is HttpOnly.</param>
        /// <param name="sameSite">SameSite 策略 / The SameSite mode.</param>
        /// <param name="secure">是否 Secure；null 时随请求协议（HTTPS 请求下自动加 Secure）/ Whether the cookie is Secure; null follows the request scheme (Secure added automatically on HTTPS).</param>
        /// <remarks>
        /// 默认 HttpOnly=true、SameSite=Lax、Secure 随请求协议（HTTPS 请求下自动加 Secure），用于认证类 Cookie；
        /// 需要前端脚本读取的 Cookie 请显式传 httpOnly:false。
        /// </remarks>
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
        /// 获取Cookie / Gets a cookie value.
        /// </summary>
        /// <param name="cookieKey">键名 / The cookie key.</param>
        /// <returns>键值；无请求上下文、Cookie 不存在或值为空时返回 <see cref="string.Empty"/> / The cookie value; <see cref="string.Empty"/> when there is no request context, the cookie is missing, or the value is empty.</returns>
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
        /// 从GET请求中获取模型的数据 / Binds model data from the GET request (query string).
        /// </summary>
        /// <typeparam name="T">模型类型 / The model type.</typeparam>
        /// <param name="model">待填充的模型实例，可为 null / The model instance to fill; may be null.</param>
        /// <returns>填充后的模型 / The populated model.</returns>
        public static T RenderGetModel<T>(T model) where T : class, new()
        {
            return RenderRequestModel<T>(model, HttpRequestMethod.Get);
        }
        /// <summary>
        /// 从POST请求中获取模型的数据 / Binds model data from the POST request (form).
        /// </summary>
        /// <typeparam name="T">模型类型 / The model type.</typeparam>
        /// <param name="model">待填充的模型实例，可为 null / The model instance to fill; may be null.</param>
        /// <returns>填充后的模型 / The populated model.</returns>
        public static T RenderPostModel<T>(T model) where T : class, new()
        {
            return RenderRequestModel<T>(model, HttpRequestMethod.Post);
        }
        /// <summary>
        /// 从请求中获取模型的数据 / Binds model data from the request into the model.
        /// </summary>
        /// <typeparam name="T">模型类型 / The model type.</typeparam>
        /// <param name="model">待填充的模型实例，可为 null / The model instance to fill; may be null.</param>
        /// <param name="httpMethod">取值来源：Get 读 Query，Post 读 Form，All 先 Query 后 Form / The value source: Get reads Query, Post reads Form, All reads Query then Form.</param>
        /// <returns>填充后的模型 / The populated model.</returns>
        /// <remarks>
        /// 只绑定模型在数据库映射（ModelSchemaStore 的 TableMapping）中登记过的字段，未映射属性不参与绑定；
        /// 值经 StringTool.TryParse 按属性基础类型（含可空 unwrap）转换，转换失败的字段保持原值。
        /// 无请求上下文、请求与 httpMethod 不匹配（如 Post 模式但非 POST 请求）或模型无映射时返回新实例（原有值不保留）。
        /// </remarks>
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


        /// <summary>
        /// 异步读取整个请求体为 UTF-8 字符串 / Asynchronously reads the whole request body as a UTF-8 string.
        /// </summary>
        /// <param name="request">当前请求 / The current request.</param>
        /// <returns>请求体字符串 / The request body string.</returns>
        /// <remarks>调用 EnableBuffering 并在读取前后把流位置重置为 0，下游管道可再次读取请求体。</remarks>
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

        /// <summary>
        /// 同步读取整个请求体为 UTF-8 字符串 / Synchronously reads the whole request body as a UTF-8 string.
        /// </summary>
        /// <param name="request">当前请求 / The current request.</param>
        /// <returns>请求体字符串 / The request body string.</returns>
        /// <remarks>调用 EnableBuffering 并在读取前后把流位置重置为 0，下游管道可再次读取请求体。</remarks>
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
