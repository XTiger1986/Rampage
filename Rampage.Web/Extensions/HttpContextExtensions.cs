using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Primitives;
using Rampage.Utilities;
using System;

namespace Rampage.Web.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class HttpContextExtensions
    {

        /// <summary>
        /// get form item value
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
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
        /// request raw relative Url, for example：/Product/?productId=123&color=red
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static string RawUrl(this HttpRequest req)
        {
            return string.Concat(req.Scheme, "://", req.Host.Host, (req.Host.Port == null || req.Host.Port == 80 ? "" : $":{req.Host.Port}"), req.Path.Value, req.QueryString.Value);
        }

        /// <summary>
        /// request url Referer
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static string UrlReferrer(this HttpRequest req)
        {
            var refs = req.Headers["Referer"];
            var referrer = refs.FirstOrDefault();
            return referrer ?? string.Empty;
        }

        /// <summary>
        /// request User-Agent
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static string UserAgent(this HttpRequest req)
        {
            var uas = req.Headers["User-Agent"];
            var ua = uas.FirstOrDefault();
            return ua ?? string.Empty;
        }

        /// <summary>
        /// request raw Absolute Url, for example：http://www.1986z.com/Product/?productId=123&color=red
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
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
        /// check HttpRequest is ajax request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.Headers != null && request.Headers.ContainsKey("X-Requested-With"))
                return StringTool.EqualsIgnoreCase(request.Headers["X-Requested-With"], "XMLHttpRequest");

            return false;
        }

        /// <summary>
        /// save IFormFile to target path
        /// </summary>
        /// <param name="file"></param>
        /// <param name="savePath"></param>
        /// <param name="overwrite"></param>
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
        /// Get bytes from IFormFile
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static ActionDescriptor GetActionDescriptor(this HttpContext httpContext)
        {
            var endpoint = httpContext.GetEndpoint()!;
            return endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()!;
        }


    }
}
