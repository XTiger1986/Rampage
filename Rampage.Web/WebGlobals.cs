
using Rampage.Web.Extensions;
using System;
using System.Collections.Concurrent;

namespace Rampage.Web
{
    /// <summary>
    /// Web 全局变量与常量 / Web global variables and constants
    /// </summary>
    public class WebGlobals
    {

        static WebGlobals()
        {
            ErrorTemplatesWebDirectory = "html\\errors\\";
        }

        /// <summary>
        /// "powered-by" 响应头的键名（SuperMiddleware 会写入框架名）/ The response header key of "powered-by" (SuperMiddleware writes the framework name into it)
        /// </summary>
        public static readonly string HEADER_KEY_POWERBY = "powered-by";

        /// <summary>
        /// ContextMonitor 默认计时器键（页面处理耗时监控）/ The default ContextMonitor stopwatch key (page processing time)
        /// </summary>
        public static readonly string PAGE_MONITOR_KEY = "CONTEXT_PROCESS";

        /// <summary>
        /// 站点根 Url，如 http://www.1986z.com / Web site root URL, e.g. http://www.1986z.com
        /// </summary>
        /// <remarks>无 HTTP 上下文（如后台任务）时返回空字符串 / Returns an empty string when there is no HTTP context (e.g. background tasks)</remarks>
        public static string WebRoot
        {
            get
            {
                return RequestContext.Current == null ? string.Empty : RequestContext.Current.Request.HostUrl();
            }
        }

        /// <summary>
        /// 错误页 HTML 模板的 wwwroot 相对目录 / The wwwroot-relative directory of error page HTML templates
        /// (绝对路径为 "{WebHostEnvironment.WebRootPath}{ErrorTemplatesWebDirectory}{HttpStatusCode 或 default}.html" / absolute path: "{WebHostEnvironment.WebRootPath}{ErrorTemplatesWebDirectory}{HttpStatusCode}.html")
        /// </summary>
        public static string ErrorTemplatesWebDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// 内存中的错误页 HTML 模板（键：HttpStatusCode 字符串，值：模板 HTML；查找优先级最高）/ In-memory error page HTML templates (key: HttpStatusCode string, value: template HTML; highest lookup priority)
        /// </summary>
        public static ConcurrentDictionary<string, string> ErrorWebTemplates
        {
            get;
        } = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 设置内存错误页模板，覆盖文件模板与内置模板 / Sets an in-memory error page template, overriding file and built-in templates
        /// </summary>
        /// <param name="code">状态码字符串（如 "500"）/ The status code string (e.g. "500")</param>
        /// <param name="html">模板 HTML / The template HTML</param>
        public static void SetErrorWebTemplate(string code, string html)
        {
            ErrorWebTemplates[code] = html;
        }

        /// <summary>
        /// 认证请求头的键名（Authorization）/ The authentication header key (Authorization)
        /// </summary>
        public static string AuthorizationHeaderKey = "Authorization";

        /// <summary>
        /// 客户端时区请求头的键名（RampageClientTimeZone，值为时区 Id，供 TimeZoneContext 解析）/ The client time zone header key (RampageClientTimeZone; value is a time zone Id parsed by TimeZoneContext)
        /// </summary>
        public static string ClientTimeZoneHeaderKey = "RampageClientTimeZone";

    }


    /// <summary>
    /// 常用 HTTP 请求方法（用于 Get/Post 系列辅助方法选择读取范围）/ Frequently-used HTTP request methods (select where the Get/Post helpers read from)
    /// </summary>
    public enum HttpRequestMethod
    {
        /// <summary>
        /// 全部（先查 Query 再查 Form）/ All (query first, then form)
        /// </summary>
        All = 0,
        /// <summary>
        /// 查询字符串（GET）/ Query string (GET)
        /// </summary>
        Get = 1,
        /// <summary>
        /// 表单（POST）/ Form data (POST)
        /// </summary>
        Post = 2
    }

}
