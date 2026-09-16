using System;

namespace Rampage.Management.Configuration
{
    /// <summary>
    /// 未注册权限 Action 的访问策略
    /// </summary>
    public enum UnregisteredActionPolicy
    {
        /// <summary>拒绝（fail-closed）</summary>
        Deny = 0,

        /// <summary>已登录即可访问（旧行为）</summary>
        AllowLoggedIn = 1,
    }

    /// <summary>
    /// 管理系统配置
    /// </summary>
    public class ManagementConfig
    {

        static ManagementConfig()
        {
        }

        private static IList<string>? _namespaceList;
        /// <summary>
        /// 指定在命名空间列表中搜索权限控制器与视图
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

        /// <summary>
        /// 
        /// </summary>
        public static IList<Type>? AssignableControllerTypes
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Type? DefinedAnonymousFilterType
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Type? DefinedLoggedInFilterType
        {
            get;
            set;
        }

        public static Type? ActionViewBaseControllerType
        {
            get;
            set;
        }

        public static Type? WebApiBaseControllerType
        {
            get;
            set;
        }

        public static Type? ActionResultType
        {
            get;
            set;
        }

        public static Type? ApiResultType
        {
            get;
            set;
        }

        /// <summary>
        /// 未注册进权限系统（PermissionLoader 未收录）的 Action 的访问策略。
        /// Deny（默认，fail-closed）：拒绝访问，需显式标记 PermissionLoggedInFilter / AllowAnonymous / PermissionAction(Ignore=true)；
        /// AllowLoggedIn：旧行为，任何已登录管理员可访问（仅用于迁移过渡，启动期会输出未标记 Action 清单）。
        /// </summary>
        public static UnregisteredActionPolicy UnregisteredActionPolicy
        {
            get;
            set;
        } = UnregisteredActionPolicy.Deny;



        private static string _adminPasswordEncryptKey = "Rampage.Management.AdminPasswordEncryptKey";
        /// <summary>
        /// 管理员密码的加密密钥
        /// </summary>
        public static string AdminPasswordEncryptKey
        {
            get { return _adminPasswordEncryptKey; }
            set { _adminPasswordEncryptKey = value; }
        }

        private static double _rememberDays = 3;
        /// <summary>
        /// 登录记住密码的天数
        /// </summary>
        public static double RememberDays
        {
            get { return _rememberDays; }
            set { _rememberDays = value; }
        }

    }
}
