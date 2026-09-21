using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rampage.Management.Configuration;
using Rampage.Management.Mvc;
using Rampage.Management.Web.Api;
using Rampage.Web;

namespace Rampage.Management.Web
{
    /// <summary>
    /// 管理系统应用：继承 RampageWebApplication，在应用初始化完成后把本库的控制器类型与过滤器类型回注到 ManagementConfig 静态属性
    /// Management application: extends RampageWebApplication and feeds this library's controller and filter types back into the static ManagementConfig properties once the application is initialized
    /// </summary>
    public class RampageManagementApplication : RampageWebApplication
    {

        /// <summary>
        /// 构造函数：订阅 ApplicationInitialized，把控制器基类、可扫描控制器与匿名/仅需登录过滤器类型写入 ManagementConfig 静态属性
        /// Constructor: subscribes ApplicationInitialized to write the controller base types, scannable controllers and the anonymous/login-only filter types into the static ManagementConfig properties
        /// </summary>
        public RampageManagementApplication() : base()
        {
            ApplicationInitialized += (app, provider) =>
            {
                // 静态配置桥接（非 DI）：PermissionLoader 的静态扫描依赖这些类型，须在首次触发权限加载前就位 / Static configuration bridge (not DI): PermissionLoader's static scan depends on these types, so they must be set before the first permission load
                ManagementConfig.ActionViewBaseControllerType = typeof(Controller);
                ManagementConfig.WebApiBaseControllerType = typeof(ControllerBase);
                ManagementConfig.AssignableControllerTypes = new List<Type>
                {
                    typeof(SuperManagementController),
                    typeof(SuperManagementApiController)
                };
                ManagementConfig.DefinedAnonymousFilterType = typeof(AllowAnonymousAttribute);
                ManagementConfig.DefinedLoggedInFilterType = typeof(PermissionLoggedInFilter);
            };
        }

    }
}
