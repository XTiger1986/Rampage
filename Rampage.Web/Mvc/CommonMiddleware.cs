using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Rampage.AppLog;
using Rampage.Json;
using Rampage.Model;
using Rampage.Utilities;
using Rampage.Web.Extensions;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Rampage.Web.Mvc
{
    /// <summary>
    /// 默认通用中间件：统一处理 Web 异常（Ajax 返回 R JSON，普通请求返回错误页 HTML） / Default common middleware: handles web exceptions uniformly (R JSON for Ajax, error page HTML for normal requests)
    /// </summary>
    /// <remarks>
    /// 错误页模板查找顺序：内存字典（WebGlobals.ErrorWebTemplates）→ wwwroot 下 {ErrorTemplatesWebDirectory}{状态码|default}.html → 内置 HTML；
    /// 模板占位符 {Log.Id}/{Log.Message} 会先 HTML 编码再替换，防止反射型 XSS。
    /// Error page template lookup order: the in-memory dictionary (WebGlobals.ErrorWebTemplates) → {ErrorTemplatesWebDirectory}{status|default}.html under wwwroot → the built-in HTML;
    /// the {Log.Id}/{Log.Message} placeholders are HTML-encoded before substitution to prevent reflected XSS.
    /// </remarks>
    public class CommonMiddleware : SuperMiddleware
	{
        /// <summary>
        /// 宿主环境信息（用于判断 Development 等环境）/ The host environment (used to detect Development, etc.)
        /// </summary>
        protected readonly IWebHostEnvironment WebHostEnvironment;

        /// <summary>
        /// 构造函数 / Constructor
        /// </summary>
        /// <param name="next">管道中的下一个委托 / The next delegate in the pipeline</param>
        /// <param name="webHostEnvironment">宿主环境信息 / The host environment</param>
        public CommonMiddleware(RequestDelegate next, IWebHostEnvironment webHostEnvironment) : base(next)
        {
            WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 是否向客户端返回原始异常消息。null（默认）= 仅 Development 环境返回；生产环境只返回通用文案 + 错误 Id 便于排查。
        /// SuperException（业务异常，消息为面向用户的本地化文案）始终原样返回。
        /// Whether to return the raw exception message to clients. null (default) = only in Development; production gets the
        /// generic message plus the error Id. SuperException (business exceptions with user-facing localized messages) is always returned as-is.
        /// </summary>
        public static bool? ExposeExceptionDetails
        {
            get;
            set;
        }

        /// <summary>
        /// 生产环境返回给客户端的通用错误文案（可由宿主覆盖） / The generic error message returned to clients in production (host-overridable)
        /// </summary>
        public static string GenericErrorMessage
        {
            get;
            set;
        } = "服务器处理请求时发生错误，请稍后重试。";

        /// <summary>
        /// 判断本次异常是否向客户端暴露原始消息：SuperException 始终暴露，其余取决于 <see cref="ExposeExceptionDetails"/> 或是否 Development 环境 / Decides whether the raw message is exposed for this exception: SuperException always is; others depend on <see cref="ExposeExceptionDetails"/> or the Development environment
        /// </summary>
        /// <param name="exception">捕获的异常 / The caught exception</param>
        /// <returns>true 表示返回原始异常消息 / True to return the raw exception message</returns>
        protected virtual bool ShouldExposeDetails(Exception exception)
        {
            if (exception is Rampage.ExceptionExtensions.SuperException)
            {
                return true;
            }
            return ExposeExceptionDetails ?? WebHostEnvironment.IsDevelopment();
        }

        /// <summary>
        /// 处理未捕获异常：记录日志并按请求类型响应——Ajax 返回 R JSON（Code=Failure），
        /// 普通请求返回错误页 HTML；HttpRequestException 沿用其状态码，其余按 500。
        /// Handles uncaught exceptions: logs and responds by request type - R JSON (Code=Failure) for Ajax,
        /// error page HTML otherwise; HttpRequestException keeps its status code, everything else maps to 500.
        /// </summary>
        /// <param name="context">当前 HTTP 上下文 / The current HTTP context</param>
        /// <param name="exception">捕获的异常 / The caught exception</param>
        /// <returns>异步任务 / A task representing the operation</returns>
        protected override async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            bool isAjax = context.Request.IsAjaxRequest();
            var response = context.Response;

            SystemLog sysLog;
            HttpStatusCode statusCode;

            bool isHttpException = false;
            if (exception is HttpRequestException)
            {
                isHttpException = true;
                HttpRequestException httpRequestException = (HttpRequestException)exception;
                if (httpRequestException.StatusCode != null)
                {
                    statusCode = httpRequestException.StatusCode.Value;
                }
                else
                {
                    statusCode = HttpStatusCode.InternalServerError;
                }
                sysLog = RampageLogger.InnerLogger.GetLog(exception.Message, true, exception, log4net.Core.Level.Info);
                sysLog.Id = string.Empty;
            }
            else
            {
                sysLog = RampageLogger.Error(exception);
                statusCode = HttpStatusCode.InternalServerError;
            }
            response.StatusCode = (int)statusCode;

            // 面向客户端的消息：开发环境/业务异常返回原文；否则只给通用文案 + 错误 Id，避免泄露连接串、表名、内部路径等
            // Client-facing message: raw text in Development or for business exceptions; otherwise the generic message + error Id to avoid leaking connection strings, table names, internal paths, etc.
            bool expose = ShouldExposeDetails(exception);
            string clientMessage = expose
                ? exception.Message
                : (string.IsNullOrEmpty(sysLog.Id) ? GenericErrorMessage : $"{GenericErrorMessage} (Error Id: {sysLog.Id})");

            if (isAjax)
            {
                var errorResponse = new R
                {
                    Code = (int)ResultCode.Failure,
                    Message = clientMessage
                };
                response.ContentType = "application/json";
                var result = JsonTool.Stringify(errorResponse);
                await response.WriteAsync(result);
            }
            else
            {
                string errorHtml = GetErrorHtml(isHttpException, statusCode);
                errorHtml = ParseErrorHtml(errorHtml, sysLog.Id, clientMessage);

                response.ContentType = "text/html";
                await response.WriteAsync(errorHtml);
            }

        }


        private string GetErrorHtml(bool isHttpException, HttpStatusCode statusCode)
        {
            string key = statusCode.ToString();
            if (WebGlobals.ErrorWebTemplates.ContainsKey(key))
            {
                return WebGlobals.ErrorWebTemplates[key];
            }

            string errorTemplateDirPath = Path.Combine(WebHostEnvironment.WebRootPath, WebGlobals.ErrorTemplatesWebDirectory);

            string? errorHtml = null;
            if (Directory.Exists(errorTemplateDirPath))
            {
                string errorTemplatePath = Path.Combine(errorTemplateDirPath, statusCode + ".html");
                if (!File.Exists(errorTemplatePath))
                {
                    errorTemplatePath = Path.Combine(errorTemplateDirPath, "default.html");
                }
                if (File.Exists(errorTemplatePath))
                {
                    errorHtml = FileTool.ReadFile(errorTemplatePath, Encoding.UTF8);
                }
            }

            if (string.IsNullOrEmpty(errorHtml))
            {
                errorHtml = !isHttpException ? GetDefaultServerErrorHtml() : GetDefaultHttpErrorHtml();
            }

            WebGlobals.SetErrorWebTemplate(key, errorHtml);
            return errorHtml;
        }

        private string _errorBgPath = "/images/error_bg.png";

        private string GetDefaultHttpErrorHtml()
        {
            string html = string.Concat(@"
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<title>Internal Server Error</title>
<style type=""text/css"">
body{margin:0;padding:0 100px;font:12px ""Segoe UI"",Helvetica,Arial,sans-serif;background:url(", _errorBgPath, @") repeat-x 0 -70px;}
h1{padding-left:70px;height:70px;line-height:70px;margin:0 0 0 -70px;font-size:130%;white-space:nowrap;background:url(", _errorBgPath, @") no-repeat;}
ul{margin:40px 0;padding:0 0 0 1.2em;font-size:120%;}
li{margin-bottom:0.2em;}
pre{margin:5px 10px 0 0;padding:10px;background:#ffe;white-space:pre;}
code{font-size:90%;font-family:Consolas,""Courier New"",monospace;}
a:link, a:visited{color:#06C;text-decoration:none;}
.red{color:#c00;}
</style>
</head>
<body>
<h1>Http Request Exception</h1>
<ul>
<li>Error Message: <code class=""red"">{Log.Message}</code></li>
</ul>
<p>We apologize for any inconvenience caused to you. </p>
</body>
</html>");

            return html;
        }

        private string GetDefaultServerErrorHtml()
        {
            string html = string.Concat(@"
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<title>Internal Server Error</title>
<style type=""text/css"">
body{margin:0;padding:0 100px;font:12px ""Segoe UI"",Helvetica,Arial,sans-serif;background:url(", _errorBgPath, @") repeat-x 0 -70px;}
h1{padding-left:70px;height:70px;line-height:70px;margin:0 0 0 -70px;font-size:130%;white-space:nowrap;background:url(", _errorBgPath, @") no-repeat;}
ul{margin:40px 0;padding:0 0 0 1.2em;font-size:120%;}
li{margin-bottom:0.2em;}
pre{margin:5px 10px 0 0;padding:10px;background:#ffe;white-space:pre;}
code{font-size:90%;font-family:Consolas,""Courier New"",monospace;}
a:link, a:visited{color:#06C;text-decoration:none;}
.red{color:#c00;}
</style>
</head>
<body>
<h1>an error has occurred and the error message has been recorded. Please contact the administrator</h1>
<ul>
<li>Error Message: <code class=""red"">{Log.Message}</code></li>
<li>Error Id: <code class=""red"">{Log.Id}</code></li>
</ul>
<p>We apologize for any inconvenience caused to you. </p>
</body>
</html>");

            return html;
        }

        private static readonly Regex LogIdPlaceholder = new Regex(@"\{Log\.Id\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LogMessagePlaceholder = new Regex(@"\{Log\.Message\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private string ParseErrorHtml(string html, string? logId, string? message)
        {
            // HTML 编码后再写入模板，防止异常消息中的用户输入造成反射型 XSS；使用 Replace 的委托形式避免 $ 被当作替换组
            // HTML-encode before substitution to stop reflected XSS via user input in exception messages; the delegate form of Replace avoids "$" being treated as a substitution group
            string encodedId = WebUtility.HtmlEncode(logId ?? string.Empty);
            string encodedMessage = WebUtility.HtmlEncode(message ?? string.Empty);
            html = LogIdPlaceholder.Replace(html, _ => encodedId);
            html = LogMessagePlaceholder.Replace(html, _ => encodedMessage);
            return html;
        }

    }
}
