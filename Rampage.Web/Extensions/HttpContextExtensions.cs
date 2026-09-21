using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Primitives;
using Rampage.Utilities;
using System;

namespace Rampage.Web.Extensions
{
    /// <summary>
    /// HttpContext/HttpRequest 相关扩展方法 / Extension methods for HttpContext/HttpRequest.
    /// </summary>
    public static class HttpContextExtensions
    {

        /// <summary>
        /// 获取表单项的值 / Gets a form item value.
        /// </summary>
        /// <param name="req">当前请求 / The current request.</param>
        /// <param name="key">表单键名 / The form key.</param>
        /// <returns>表单项值；条件不满足或键不存在时返回 <see cref="StringValues.Empty"/> / The form value; <see cref="StringValues.Empty"/> when the preconditions fail or the key is absent.</returns>
        /// <remarks>
        /// 仅当请求为 POST 且 Content-Type 为 application/x-www-form-urlencoded（精确匹配）或 multipart/form-data（前缀匹配）时才读取表单，
        /// 其余情况（含 WebSocket 等非表单请求，此时 Form 属性不可用）一律返回空值而不触碰 req.Form。
        /// </remarks>
        public static StringValues GetForm(this HttpRequest req, string key)
        {
            StringValues contentTypeValues = req.Headers["Content-Type"];
            string? contentType = contentTypeValues.FirstOrDefault();
            bool isFormEncoded = contentType == "application/x-www-form-urlencoded";
            // multipart 的 Content-Type 带 boundary 后缀，需前缀匹配
            bool isMultipart = contentType != null && contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase);
            if (isFormEncoded || isMultipart)
            {
                if (StringTool.EqualsIgnoreCase(req.Method, "POST") && req.Form != null && req.Form.ContainsKey(key))
                    return req.Form[key];
            }
            return StringValues.Empty;
        }

        /// <summary>
        /// 请求的完整原始 URL（含 scheme 与 host 的绝对地址），例如：http://www.1986z.com/Product/?productId=123&amp;color=red / The full raw request URL (absolute, with scheme and host), e.g. http://www.1986z.com/Product/?productId=123&amp;color=red.
        /// </summary>
        /// <param name="req">当前请求 / The current request.</param>
        /// <returns>绝对 URL / The absolute URL.</returns>
        /// <remarks>端口为空或 80 时不拼接端口号，其余情况显式拼接。注意：返回的是绝对 URL 而非相对路径。</remarks>
        public static string RawUrl(this HttpRequest req)
        {
            return string.Concat(req.Scheme, "://", req.Host.Host, (req.Host.Port == null || req.Host.Port == 80 ? "" : $":{req.Host.Port}"), req.Path.Value, req.QueryString.Value);
        }

        /// <summary>
        /// 请求的来源页 Referer / The Referer header of the request.
        /// </summary>
        /// <param name="req">当前请求 / The current request.</param>
        /// <returns>Referer 值；无该请求头时返回 <see cref="string.Empty"/> / The referrer; <see cref="string.Empty"/> when the header is absent.</returns>
        public static string UrlReferrer(this HttpRequest req)
        {
            var refs = req.Headers["Referer"];
            var referrer = refs.FirstOrDefault();
            return referrer ?? string.Empty;
        }

        /// <summary>
        /// 请求的 User-Agent / The User-Agent header of the request.
        /// </summary>
        /// <param name="req">当前请求 / The current request.</param>
        /// <returns>User-Agent 值；无该请求头时返回 <see cref="string.Empty"/> / The user agent; <see cref="string.Empty"/> when the header is absent.</returns>
        public static string UserAgent(this HttpRequest req)
        {
            var uas = req.Headers["User-Agent"];
            var ua = uas.FirstOrDefault();
            return ua ?? string.Empty;
        }

        /// <summary>
        /// 请求的完整绝对 URL，例如：http://www.1986z.com/Product/?productId=123&amp;color=red / The absolute request URL, e.g. http://www.1986z.com/Product/?productId=123&amp;color=red.
        /// </summary>
        /// <param name="req">当前请求 / The current request.</param>
        /// <returns>绝对 URL / The absolute URL.</returns>
        /// <remarks>由 HostUrl 拼接 Path 与 QueryString 而成。</remarks>
        public static string AbsoluteUrl(this HttpRequest req)
        {
            return string.Concat(req.HostUrl(), req.Path, req.QueryString.Value);
        }

        /// <summary>
        /// root site url, for example：http://www.1986z.com
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static string HostUrl(this HttpRequest req)
        {
            return string.Concat(req.Scheme, "://", req.Host, req.Protocol == "80" ? string.Empty : ":" + req.Protocol);
        }

        /// <summary>
        /// 判断请求是否 AJAX 请求（X-Requested-With: XMLHttpRequest，忽略大小写）/ Determines whether the request is an AJAX request (X-Requested-With: XMLHttpRequest, case-insensitive).
        /// </summary>
        /// <param name="request">当前请求 / The current request.</param>
        /// <returns>是 AJAX 请求返回 true / true for AJAX requests.</returns>
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.Headers != null && request.Headers.ContainsKey("X-Requested-With"))
                return StringTool.EqualsIgnoreCase(request.Headers["X-Requested-With"], "XMLHttpRequest");

            return false;
        }

        /// <summary>
        /// 将上传文件保存到目标路径 / Saves the uploaded file to the target path.
        /// </summary>
        /// <param name="file">上传文件 / The uploaded file.</param>
        /// <param name="savePath">目标路径（目录不存在时自动创建）/ The target path (the directory is created when absent).</param>
        /// <param name="overwrite">是否覆盖同名文件，默认 false（同名时抛异常）/ Whether to overwrite an existing file; default false (throws when the file exists).</param>
        public static void SaveAs(this IFormFile file, string savePath, bool overwrite = false)
        {
            string dirPath = Path.GetDirectoryName(savePath)!;
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            using (Stream stream = file.OpenReadStream())
            {
                stream.Position = 0;
                byte[] fileBuff = new byte[stream.Length];
                stream.Read(fileBuff, 0, Convert.ToInt32(stream.Length));

                using (FileStream fs = new FileStream(savePath, overwrite ? FileMode.Create : FileMode.CreateNew))
                {
                    fs.Write(fileBuff, 0, fileBuff.Length);
                }
            }
        }

        /// <summary>
        /// 读取上传文件的全部字节 / Reads all bytes of the uploaded file.
        /// </summary>
        /// <param name="file">上传文件 / The uploaded file.</param>
        /// <returns>文件内容字节数组 / The file content as a byte array.</returns>
        public static byte[] GetBytes(this IFormFile file)
        {
            int size = Convert.ToInt32(file.Length);
            byte[] buff = new byte[size];
            Stream stream = file.OpenReadStream();
            stream.Position = 0;
            stream.Read(buff, 0, size);
            return buff;
        }

        /// <summary>
        /// 获取当前端点对应的控制器 Action 描述 / Gets the controller action descriptor of the current endpoint.
        /// </summary>
        /// <param name="httpContext">当前请求上下文 / The current HTTP context.</param>
        /// <returns>Action 描述信息 / The action descriptor.</returns>
        /// <remarks>要求请求已路由到控制器 Action，否则 GetEndpoint/元数据为 null 时会抛异常。</remarks>
        public static ActionDescriptor GetActionDescriptor(this HttpContext httpContext)
        {
            var endpoint = httpContext.GetEndpoint()!;
            return endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()!;
        }


    }
}
