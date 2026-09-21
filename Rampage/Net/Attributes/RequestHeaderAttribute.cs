using System;

namespace Rampage.Net
{
    /// <summary>
    /// 请求头参数标注：参数值以参数名（或特性 Name）作为请求头名随请求发送
    /// Request-header parameter attribute: sends the argument as a request header named after the parameter (or the attribute Name)
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestHeaderAttribute : Attribute
    {
        /// <summary>请求头名称：缺省时取参数名 / The header name; defaults to the parameter name</summary>
        public virtual string? Name
        {
            get; set;
        }

    }
}
