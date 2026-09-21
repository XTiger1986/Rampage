using Rampage.Enums;
using System;

namespace Rampage.Management
{


    /// <summary>后台管理全局变量 / Management global variables</summary>
    public class ManagementVars
    {

        /// <summary>后台认证配置名（JWT 配置的键，用于签发与校验令牌） / The management authentication config name (JWT config key used to issue and validate tokens)</summary>
        public static string AuthenticationConfigName = "Rampage.Management";


        /// <summary>后台菜单的最大深度（根节点深度为 0） / The maximum management menu depth (root depth is 0)</summary>
        public static readonly int ManageMenuMaxDepth = 2;

    }

}
