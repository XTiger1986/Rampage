using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Rampage.DependencyInjection;
using Rampage.Json;
using Rampage.Log;
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
    /// default common middleware (handle web exceptions)
    /// </summary>
    public class CommonMiddleware : SuperMiddleware
	{
        protected readonly IWebHostEnvironment WebHostEnvironment;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <param name="webHostEnvironment"></param>
        public CommonMiddleware(RequestDelegate next, IWebHostEnvironment webHostEnvironment) : base(next)
        {
            WebHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        /// <summary>
        /// 是否向客户端返回原始异常消息。null（默认）= 仅 Development 环境返回；生产环境只返回通用文案 + 错误 Id 便于排查。
        /// SuperException（业务异常，消息为面向用户的本地化文案）始终原样返回。
        /// </summary>
        public static bool? ExposeExceptionDetails
        {
            get;
            set;
        }

        /// <summary>
        /// 生产环境返回给客户端的通用错误文案（可由宿主覆盖）
        /// </summary>
        public static string GenericErrorMessage
        {
            get;
            set;
        } = "服务器处理请求时发生错误，请稍后重试。";

        protected virtual bool ShouldExposeDetails(Exception exception)
        {
            if (exception is Rampage.ExceptionExtensions.SuperException)
            {
                return true;
            }
            return ExposeExceptionDetails ?? WebHostEnvironment.IsDevelopment();
        }

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
            string encodedId = WebUtility.HtmlEncode(logId ?? string.Empty);
            string encodedMessage = WebUtility.HtmlEncode(message ?? string.Empty);
            html = LogIdPlaceholder.Replace(html, _ => encodedId);
            html = LogMessagePlaceholder.Replace(html, _ => encodedMessage);
            return html;
        }

    }
}
