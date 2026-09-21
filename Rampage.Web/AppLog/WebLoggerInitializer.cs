using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Rampage.Utilities;
using Rampage.Web.Extensions;
using Rampage.Web.Utilities;
using System;

namespace Rampage.AppLog
{
    /// <summary>
    /// Web 日志初始化器，为系统日志补充当前请求的上下文信息 / Web logger initializer that enriches system logs with current-request context.
    /// </summary>
    public class WebLoggerInitializer
    {
        /// <summary>
        /// 当前请求 HttpContext（无请求时为 null）/ The current HttpContext (null outside a request).
        /// </summary>
        private static HttpContext? HttpContext
        {
            get { return RequestContext.Current; }
        }

        /// <summary>
        /// 初始化 Web 日志：向 LoggerFactory 注册写日志时的请求数据填充处理器 / Initializes web logging by registering a request-data handler with LoggerFactory.
        /// </summary>
        /// <remarks>
        /// 注册的处理器在写日志时为 SystemLog 填充：RawUrl、Headers、Cookies、Referrer、客户端 IP（WebTool.GetClientIP）、
        /// 以及 User-Agent 与关联 ID（CorrelationIdMiddleware.CurrentId，拼入 ClientInfo）；
        /// 仅 POST 且 Content-Type 为表单时才记录 PostData；无请求上下文（如后台任务）时不填充任何字段。
        /// </remarks>
        public static void Initialize()
        {
            LoggerFactory.SetLogClientDataHandler = delegate (SystemLog log)
            {
                if (HttpContext == null)
                {
                    return;
                }
                HttpRequest req = HttpContext.Request;
                Dictionary<string, string> headerDict = new Dictionary<string, string>();
                foreach (var headerKey in req.Headers.Keys)
                {
                    string? headerValue = req.Headers[headerKey];
                    if (!string.IsNullOrEmpty(headerValue) && !headerDict.ContainsKey(headerKey))
                    {
                        headerDict.Add(headerKey, headerValue);
                    }
                }
                string postDataString = string.Empty;
                if (StringTool.EqualsIgnoreCase(req.Method, "POST"))
                {
                    if ((StringTool.EqualsIgnoreCase(req.ContentType, "application/x-www-form-urlencoded") || (StringTool.EqualsIgnoreCase(req.ContentType, "multipart/form-data"))) && req.Form != null)
                    {
                        Dictionary<string, string> postDataDict = new Dictionary<string, string>();
                        foreach (var formName in req.Form.Keys)
                        {
                            string? itemValue = req.GetForm(formName);
                            if (!string.IsNullOrEmpty(itemValue) && !postDataDict.ContainsKey(formName))
                            {
                                postDataDict.Add(formName, itemValue);
                            }
                        }
                        postDataString = JsonConvert.SerializeObject(postDataDict);
                    }
                }
                Dictionary<string, string> cookieDict = new Dictionary<string, string>();
                foreach (var cookieKey in req.Cookies.Keys)
                {
                    var cookieValue = req.Cookies[cookieKey];
                    if (!string.IsNullOrEmpty(cookieValue) && !cookieDict.ContainsKey(cookieKey))
                    {
                        cookieDict.Add(cookieKey, cookieValue);
                    }
                }

                log.RawUrl = req.RawUrl();
                log.PostData = postDataString;
                log.CookieData = JsonConvert.SerializeObject(cookieDict);
                log.Headers = JsonConvert.SerializeObject(headerDict);
                log.Referrer = req.UrlReferrer();
                log.OperationIP = WebTool.GetClientIP();
                string? correlationId = Web.Middleware.CorrelationIdMiddleware.CurrentId;
                log.ClientInfo = string.Concat(req.UserAgent(), string.IsNullOrEmpty(correlationId) ? string.Empty : " [cid:" + correlationId + "]");
            };
        }

    }
}
