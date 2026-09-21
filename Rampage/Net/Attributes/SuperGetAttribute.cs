using System;

namespace Rampage.Net
{
    /// <summary>
    /// GET 请求标注：资源路径缺省取方法名，参数默认拼入 query string
    /// GET request attribute: the resource path defaults to the method name and arguments go to the query string by default
    /// </summary>
    /// <remarks>方法未标注任何请求特性时同样按 GET 处理 / A method without any request attribute is also treated as GET</remarks>
    public class SuperGetAttribute : SuperRequestAttribute
    {
    }
}
