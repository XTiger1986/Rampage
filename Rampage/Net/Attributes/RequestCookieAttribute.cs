using System;

namespace Rampage.Net
{
    /// <summary>
    /// Cookie 参数标注：参数值以参数名（或特性 Name）作为 Cookie 名随请求发送（发送时手工拼接 Cookie 请求头）
    /// Cookie parameter attribute: sends the argument as a cookie named after the parameter (or the attribute Name); cookies are manually joined into the Cookie request header when sending
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestCookieAttribute : Attribute
    {
        /// <summary>Cookie 名称：缺省时取参数名 / The cookie name; defaults to the parameter name</summary>
        public virtual string? Name
        {
            get; set;
        }

    }
}
