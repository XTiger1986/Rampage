using System;

namespace Rampage.Net
{
    /// <summary>
    /// 文件上传参数标注：仅限 POST 请求，参数值须为 byte[] 或 Stream，触发 multipart/form-data 方式提交
    /// File-upload parameter attribute: POST-only, the argument must be byte[] or Stream, and it switches the request to multipart/form-data
    /// </summary>
    /// <remarks>
    /// 用于非 POST 方法、与 [RequestBody] 同时使用，或参数值非 byte[]/Stream 时均抛出 SuperException。
    /// Using it on a non-POST method, together with [RequestBody], or with a value that is neither byte[] nor Stream throws a SuperException.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestFormFileAttribute : Attribute
    {
        /// <summary>表单字段名：缺省时取参数名 / The form field name; defaults to the parameter name</summary>
        public virtual string? Name
        {
            get; set;
        }

        /// <summary>上传文件名，缺省为 "UploadFile" / The uploaded file name, "UploadFile" when absent</summary>
        public string? FileName
        {
            get; set;
        }

    }
}
