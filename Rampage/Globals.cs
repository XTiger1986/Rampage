using Rampage.Data.Client;
using Rampage.Data.Query;
using Rampage.Hierarchy;
using Rampage.Net;
using System;

namespace Rampage
{
    /// <summary>
    /// 全局变量、常量，等等
    /// Global variables,constants, etc
    /// </summary>
    public class Globals
    {

        public static readonly string FRAMEWORK_NAME = "Rampage.NET";

        public const string EX_KEY_INVALID_ARGUMENT = "InvalidArgument";
        
        public static readonly Type TYPE_HTTP_RESPONSE_MESSAGE = typeof(HttpResponseMessage);
        public static readonly Type TYPE_SUPER_HTTP_CLIENT_I_TYPE = typeof(ISuperHttpClient);
        public static readonly Type TYPE_SUPER_CLIENT_ATTR_TYPE = typeof(SuperClientAttribute);
        public static readonly Type TYPE_SUPER_DAO_I = typeof(ISuperDao<>);
        public static readonly Type TYPE_BASE_BO_I = typeof(IBaseService);
        public static readonly Type TYPE_CURD_EXECUTOR = typeof(CurdExecutor);
        public static readonly Type TYPE_SELECT_COMMAND = typeof(SelectCommand);
        public static readonly Type TYPE_SELECT_LIST_COMMAND = typeof(SelectListCommand);
        public static readonly Type TYPE_EXECUTE_COMMAND = typeof(ExecuteCommand);

        public static readonly DateTime DATE_TIME_00010101 = new DateTime(1, 1, 1, 0, 0, 0);

        public static readonly string GLOBAL_SCOPE_NAME = "GLOBAL_SCOPE";

    }
}
