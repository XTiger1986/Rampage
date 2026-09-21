using System;

namespace Rampage.Net
{
    /// <summary>
    /// 请求参数标注（参数未标注任何参数特性时的默认行为）：GET/非 POST 请求将参数拼入 query string；POST 请求将参数放入表单（multipart 模式下进入 form-data）
    /// Request parameter attribute (also the default when a parameter carries no parameter attribute): appends the argument to the query string for GET/non-POST requests, or into the form for POST (form-data in multipart mode)
    /// </summary>
    /// <remarks>
    /// 值类型/字符串/枚举/Guid 参数按名拼接；IEnumerable 参数逐项以同名追加；其余对象转换为字典后逐属性拼接；参数值为 null 时直接跳过。
    /// 与 [RequestBody] 同用时参数仍进入 query string（POST 下也会同时并入 JSON body）。
    /// Value types/strings/enums/Guid are appended by name; an IEnumerable is appended item by item under the same name; other objects are flattened
    /// property by property after conversion to a dictionary; null argument values are skipped.
    /// When combined with [RequestBody], the argument still goes to the query string (and on POST it is also merged into the JSON body).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestParamAttribute : Attribute
    {
        /// <summary>请求参数名：缺省时取参数名 / The request parameter name; defaults to the parameter name</summary>
        public virtual string? Name
        {
            get; set;
        }

    }
}
