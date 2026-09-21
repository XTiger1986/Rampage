using Rampage.Data.Client;
using Rampage.Data.Query;
using Rampage.Hierarchy;
using Rampage.Net;
using System;

namespace Rampage
{
    /// <summary>
    /// 全局变量与常量定义 / Global variables and constants
    /// </summary>
    public class Globals
    {

        /// <summary>
        /// 框架名称常量 / Framework name constant
        /// </summary>
        public static readonly string FRAMEWORK_NAME = "Rampage.NET";

        /// <summary>
        /// 非法参数异常的字典键 / Dictionary key for invalid-argument exceptions
        /// </summary>
        public const string EX_KEY_INVALID_ARGUMENT = "InvalidArgument";
        
        /// <summary>
        /// HttpResponseMessage 的 Type 缓存 / Cached Type of HttpResponseMessage
        /// </summary>
        public static readonly Type TYPE_HTTP_RESPONSE_MESSAGE = typeof(HttpResponseMessage);
        /// <summary>
        /// ISuperHttpClient 接口的 Type 缓存 / Cached Type of the ISuperHttpClient interface
        /// </summary>
        public static readonly Type TYPE_SUPER_HTTP_CLIENT_I_TYPE = typeof(ISuperHttpClient);
        /// <summary>
        /// SuperClientAttribute 的 Type 缓存 / Cached Type of SuperClientAttribute
        /// </summary>
        public static readonly Type TYPE_SUPER_CLIENT_ATTR_TYPE = typeof(SuperClientAttribute);
        /// <summary>
        /// ISuperDao&lt;&gt; 泛型接口定义的 Type 缓存 / Cached Type of the ISuperDao&lt;&gt; generic interface definition
        /// </summary>
        public static readonly Type TYPE_SUPER_DAO_I = typeof(ISuperDao<>);
        /// <summary>
        /// IBaseService 接口的 Type 缓存 / Cached Type of the IBaseService interface
        /// </summary>
        public static readonly Type TYPE_BASE_BO_I = typeof(IBaseService);
        /// <summary>
        /// CurdExecutor 的 Type 缓存 / Cached Type of CurdExecutor
        /// </summary>
        public static readonly Type TYPE_CURD_EXECUTOR = typeof(CurdExecutor);
        /// <summary>
        /// SelectCommand 的 Type 缓存 / Cached Type of SelectCommand
        /// </summary>
        public static readonly Type TYPE_SELECT_COMMAND = typeof(SelectCommand);
        /// <summary>
        /// SelectListCommand 的 Type 缓存 / Cached Type of SelectListCommand
        /// </summary>
        public static readonly Type TYPE_SELECT_LIST_COMMAND = typeof(SelectListCommand);
        /// <summary>
        /// ExecuteCommand 的 Type 缓存 / Cached Type of ExecuteCommand
        /// </summary>
        public static readonly Type TYPE_EXECUTE_COMMAND = typeof(ExecuteCommand);

        /// <summary>
        /// 0001-01-01 00:00:00 时间基准值 / The 0001-01-01 00:00:00 base DateTime
        /// </summary>
        public static readonly DateTime DATE_TIME_00010101 = new DateTime(1, 1, 1, 0, 0, 0);

        /// <summary>
        /// 全局作用域名称 / Name of the global service scope
        /// </summary>
        public static readonly string GLOBAL_SCOPE_NAME = "GLOBAL_SCOPE";

    }
}
