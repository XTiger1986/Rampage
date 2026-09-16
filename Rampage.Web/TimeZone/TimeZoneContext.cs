using Rampage.Web.Extensions;
using System;

namespace Rampage.Web.TimeZone
{
    public class TimeZoneContext
    {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
