using Rampage.DependencyInjection;
using Rampage.Management.BLL;
using Rampage.Management.Dto;
using Rampage.Management.Web.Mvc;
using Rampage.Model;
using Rampage.Web.Mvc;
using System;

namespace Rampage.Management.Web.Api
{
    /// <summary>
    /// 管理系统 API 控制器基类：通过 <see cref="ManagementAuthorizationFilter"/> 统一执行登录与权限校验，并在初始化时装配当前登录管理员
    /// Base class for management API controllers: enforces login and permission checks centrally via <see cref="ManagementAuthorizationFilter"/> and loads the current logged-in admin during initialization
    /// </summary>
    [ManagementAuthorizationFilter]
    public abstract class SuperManagementApiController : SuperApiController
    {

        /// <summary>
        /// 管理员服务，由 [Auto] 属性注入 / The admin service, injected via [Auto]
        /// </summary>
        [Auto]
        protected IAdminService AdminService = default!;
        /// <summary>
        /// 管理员权限服务，由 [Auto] 属性注入 / The admin permission service, injected via [Auto]
        /// </summary>
        [Auto]
        protected IAdminPermissionService AdminPermissionService = default!;
        /// <summary>
        /// 管理菜单服务，由 [Auto] 属性注入 / The management menu service, injected via [Auto]
        /// </summary>
        [Auto]
        protected IManageMenuService ManageMenuService = default!;

        /// <summary>
        /// 当前登录的管理员（未登录或登录失效时为 null）/ The current logged-in admin (null when not logged in or the login is invalid)
        /// </summary>
        protected AdminDto? CurrentAdmin
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始化：执行基类初始化后读取当前登录管理员并装配 <see cref="CurrentAdmin"/>（读取失败不抛异常，仅保持 null）
        /// Initializes: after the base initialization, loads the current logged-in admin into <see cref="CurrentAdmin"/> (a failed read only leaves it null rather than throwing)
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            R<AdminDto> loginR = AdminService.GetLoginAdmin();
            if (loginR.IsSuccess)
            {
                CurrentAdmin = loginR.Data;
            }
        }


    }
}
