using System;

namespace Rampage.Management.Configuration
{
    /// <summary>未注册权限 Action 的访问策略 / Access policy for actions not registered in the permission system</summary>
    public enum UnregisteredActionPolicy
    {
        /// <summary>拒绝（fail-closed） / Deny (fail-closed)</summary>
        Deny = 0,

        /// <summary>已登录即可访问（旧行为） / Any logged-in admin may access (legacy behavior)</summary>
        AllowLoggedIn = 1,
    }

    /// <summary>管理系统配置（静态配置项，由宿主应用在启动时设置） / Management system configuration (static options set by the host application at startup)</summary>
    public class ManagementConfig
    {

        static ManagementConfig()
        {
        }

        private static IList<string>? _namespaceList;
        /// <summary>
        /// 指定在命名空间列表中搜索权限控制器与视图（读取时自动补充 Rampage.Management.Mvc）
        /// Namespaces searched for permission controllers and actions (Rampage.Management.Mvc is appended automatically on read)
        /// </summary>
        public static IList<string> NamespaceList
        {
            get
            {
                if (_namespaceList == null)
                    _namespaceList = new List<string>();
                if (_namespaceList.Count > 0 && !_namespaceList.Contains("Rampage.Management.Mvc"))
                    _namespaceList.Add("Rampage.Management.Mvc");

                return _namespaceList;
            }
            set { _namespaceList = value; }
        }

        /// <summary>可作为权限控制器的基类型集合；控制器须可分配至其中之一才会被权限系统收集（宿主应用必须设置，否则收集时会抛异常） / Base controller types collected by the permission system; a controller is collected only if assignable to one of them (must be set by the host, otherwise collection throws)</summary>
        public static IList<Type>? AssignableControllerTypes
        {
            get;
            set;
        }

        /// <summary>匿名访问过滤器特性类型；标记了该特性的控制器不纳入权限系统（如 AllowAnonymous） / The anonymous-access filter attribute type; controllers marked with it are excluded from the permission system (e.g. AllowAnonymous)</summary>
        public static Type? DefinedAnonymousFilterType
        {
            get;
            set;
        }

        /// <summary>登录即可访问过滤器特性类型；标记了该特性的控制器不纳入权限系统，仅需登录 / The logged-in-only filter attribute type; controllers marked with it need only a login and are excluded from the permission system</summary>
        public static Type? DefinedLoggedInFilterType
        {
            get;
            set;
        }

        /// <summary>MVC 视图控制器的基类型，用于识别页面类 Action / The base type of MVC view controllers, used to recognize page actions</summary>
        public static Type? ActionViewBaseControllerType
        {
            get;
            set;
        }

        /// <summary>Web API 控制器的基类型，用于识别接口类 Action / The base type of Web API controllers, used to recognize API actions</summary>
        public static Type? WebApiBaseControllerType
        {
            get;
            set;
        }

        /// <summary>视图 Action 的返回类型（为空时默认 IActionResult），据此判定方法是否纳入权限系统 / The return type of view actions (defaults to IActionResult when null), used to decide whether a method joins the permission system</summary>
        public static Type? ActionResultType
        {
            get;
            set;
        }

        /// <summary>API Action 的返回类型（为空时默认 R），据此判定方法是否纳入权限系统 / The return type of API actions (defaults to R when null), used to decide whether a method joins the permission system</summary>
        public static Type? ApiResultType
        {
            get;
            set;
        }

        /// <summary>
        /// 未注册进权限系统（PermissionLoader 未收录）的 Action 的访问策略。
        /// Deny（默认，fail-closed）：拒绝访问，需显式标记 PermissionLoggedInFilter / AllowAnonymous / PermissionAction(Ignore=true)；
        /// AllowLoggedIn：旧行为，任何已登录管理员可访问（仅用于迁移过渡，启动期会输出未标记 Action 清单）。
        /// The access policy for actions not registered in the permission system (not collected by PermissionLoader).
        /// Deny (default, fail-closed): access is denied unless explicitly marked with PermissionLoggedInFilter / AllowAnonymous / PermissionAction(Ignore=true);
        /// AllowLoggedIn: legacy behavior where any logged-in admin may access (for migration only; unmarked actions are listed at startup).
        /// </summary>
        public static UnregisteredActionPolicy UnregisteredActionPolicy
        {
            get;
            set;
        } = UnregisteredActionPolicy.Deny;



        private static string _adminPasswordEncryptKey = "Rampage.Management.AdminPasswordEncryptKey";
        /// <summary>
        /// 管理员密码的加密密钥（参与安全戳派生，修改后所有已签发令牌失效）
        /// The encryption key for admin passwords (also used in security-stamp derivation; changing it invalidates all issued tokens)
        /// </summary>
        public static string AdminPasswordEncryptKey
        {
            get { return _adminPasswordEncryptKey; }
            set { _adminPasswordEncryptKey = value; }
        }

        private static double _rememberDays = 3;
        /// <summary>
        /// 登录记住密码的天数（令牌 Cookie 的持久化时长）
        /// Days to remember the login (persistence duration of the token cookie)
        /// </summary>
        public static double RememberDays
        {
            get { return _rememberDays; }
            set { _rememberDays = value; }
        }

    }
}
