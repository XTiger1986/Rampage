
using Rampage.Web.Extensions;
using System;
using System.Collections.Concurrent;

namespace Rampage.Web
{
    /// <summary>
    /// Web Global variables/constants, etc
    /// </summary>
    public class WebGlobals
    {

        static WebGlobals()
        {
            ErrorTemplatesWebDirectory = "html\\errors\\";
        }

        public static readonly string HEADER_KEY_POWERBY = "powered-by";

        public static readonly string PAGE_MONITOR_KEY = "CONTEXT_PROCESS";

        /// <summary>
        /// WebSite Root Url，example：http://www.1986z.com
        /// </summary>
        public static string WebRoot
        {
            get
            {
                return RequestContext.Current == null ? string.Empty : RequestContext.Current.Request.HostUrl();
            }
        }

        /// <summary>
        /// Html Templates of Web Error Pages (absolute file path is: "{WebHostEnvironment.WebRootPath}{ErrorTemplatesWebDirectory}{HttpStatusCode}.html")
        /// </summary>
        public static string ErrorTemplatesWebDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// Html Templates of Web Error Pages html (Key: HttpStatusCode, Value: Template Html)
        /// </summary>
        public static ConcurrentDictionary<string, string> ErrorWebTemplates
        {
            get;
        } = new ConcurrentDictionary<string, string>();

        /// <summary>
        ///
        /// </summary>
        /// <param name="code"></param>
        /// <param name="html"></param>
        public static void SetErrorWebTemplate(string code, string html)
        {
            ErrorWebTemplates[code] = html;
        }

        public static string AuthorizationHeaderKey = "Authorization";

        public static string ClientTimeZoneHeaderKey = "RampageClientTimeZone";

    }


    /// <summary>
    /// frequently-used Http Request Methods
    /// </summary>
    public enum HttpRequestMethod
    {
        /// <summary>
        /// all (include GET & POST)
        /// </summary>
        All = 0,
        /// <summary>
        /// 
        /// </summary>
        Get = 1,
        /// <summary>
        /// 
        /// </summary>
        Post = 2
    }

}
