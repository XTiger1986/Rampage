using Rampage.Web.Extensions;
using System;

namespace Rampage.Web.TimeZone
{
    /// <summary>
    /// 客户端时区上下文，从请求头解析客户端时区 / Client time zone context resolved from request headers.
    /// </summary>
    /// <remarks>
    /// 客户端时区仅来自请求头：优先 RampageClientTimeZone（<see cref="WebGlobals.ClientTimeZoneHeaderKey"/>），其次 X-Timezone；不使用 Cookie。
    /// </remarks>
    public class TimeZoneContext
    {

        /// <summary>
        /// 获取客户端时区 / Gets the client time zone.
        /// </summary>
        /// <returns>请求头指定的时区；无法解析或无请求时回退服务器本地时区 / The time zone from the request headers; falls back to the server local time zone when unresolvable or outside a request.</returns>
        /// <remarks>
        /// 先取 <see cref="GetClientTimeZoneId"/> 的结果；若该 ID 无效且请求带 X-Timezone 头则再尝试之；两者均失败时返回 <see cref="TimeZoneInfo.Local"/>。
        /// </remarks>
        public static TimeZoneInfo GetClientTimeZone()
        {
            string? tzId = GetClientTimeZoneId();
            if (string.IsNullOrEmpty(tzId))
            {
                return TimeZoneInfo.Local;
            }
            TimeZoneInfo? tmpTimeZoneInfo = null;
            if (!TimeZoneInfo.TryFindSystemTimeZoneById(tzId, out tmpTimeZoneInfo))
            {
                if (RequestContext.Current != null && RequestContext.Current.Request.Headers.ContainsKey("X-Timezone"))
                {
                    string? xtzId = RequestContext.Current.Request.Headers["X-Timezone"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(xtzId))
                    {
                        TimeZoneInfo.TryFindSystemTimeZoneById(xtzId, out tmpTimeZoneInfo);
                    }
                }
            }
            if (tmpTimeZoneInfo == null)
            {
                tmpTimeZoneInfo = TimeZoneInfo.Local;
            }
            return tmpTimeZoneInfo;
        }

        /// <summary>
        /// 获取客户端时区 ID（原始请求头值，不做有效性校验）/ Gets the raw client time zone id from request headers without validation.
        /// </summary>
        /// <returns>RampageClientTimeZone 或 X-Timezone 请求头的首值；无请求或无相应头时为 null / The first value of the RampageClientTimeZone or X-Timezone header; null when there is no request or no matching header.</returns>
        public static string? GetClientTimeZoneId()
        {
            if (RequestContext.Current == null)
            {
                return null;
            }
            if (RequestContext.Current.Request.Headers.ContainsKey(WebGlobals.ClientTimeZoneHeaderKey))
            {
                return RequestContext.Current.Request.Headers[WebGlobals.ClientTimeZoneHeaderKey].FirstOrDefault();
            }
            else if (RequestContext.Current.Request.Headers.ContainsKey("X-Timezone"))
            {
                return RequestContext.Current.Request.Headers["X-Timezone"].FirstOrDefault();
            }
            return null;
        }

    }
}
