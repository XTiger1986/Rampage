using System;

namespace Rampage.Net
{
    /// <summary>
    /// PUT 请求标注：参数按 GET 规则拼入 query string（[RequestBody] 参数仍走 JSON body）
    /// PUT request attribute: arguments are appended to the query string like GET ([RequestBody] arguments still go through the JSON body)
    /// </summary>
    public class SuperPutAttribute : SuperRequestAttribute
    {
    }
}
