using System;

namespace Rampage.Net
{
    /// <summary>
    /// 请求体参数标注：触发将全部请求数据以 JSON body 方式提交（UseBody 模式）
    /// Request-body parameter attribute: switches the whole request data to a JSON body (UseBody mode)
    /// </summary>
    /// <remarks>
    /// 值类型/字符串/枚举/Guid 参数以参数名（或特性 Name）为键并入 JSON；复杂对象展开其属性逐项并入；
    /// 与 [RequestFormFile] 同时使用将抛出 SuperException。
    /// Value-type/string/enum/Guid arguments are keyed by the parameter name (or the attribute Name) into the JSON; complex objects are flattened property by property;
    /// combining it with [RequestFormFile] throws a SuperException.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestBodyAttribute : Attribute
    {
        /// <summary>请求体字段名：缺省时取参数名 / The body field name; defaults to the parameter name</summary>
        public virtual string? Name
        {
            get; set;
        }

    }
}
