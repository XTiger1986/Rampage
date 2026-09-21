using System;

namespace Rampage.Net
{
    /// <summary>
    /// POST 请求标注：[RequestParam] 参数进入表单、[RequestBody] 参数触发 JSON body 提交、[RequestFormFile] 参数触发 multipart/form-data 上传
    /// POST request attribute: [RequestParam] arguments go into the form, [RequestBody] switches to a JSON body, and [RequestFormFile] switches to multipart/form-data upload
    /// </summary>
    public class SuperPostAttribute : SuperRequestAttribute
    {
    }
}
