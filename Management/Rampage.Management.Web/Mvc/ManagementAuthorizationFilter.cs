using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Management.BLL;
using Rampage.Model;
using Rampage.Web.Mvc;
using System;
using System.Web;
using Rampage.Web.Extensions;
using Rampage.Management.Configuration;
using Rampage.Management.Dto;

namespace Rampage.Management.Web.Mvc
{
    public class ManagementAuthorizationFilter : ActionFilterBase
    {
        [Auto]
        protected IAdminService AdminService = default!;
        [Auto]
        protected IAdminPermissionService AdminPermissionService = default!;

        protected override bool BeforeActionExecuting(ActionExecutingContext filterContext)
        {
            bool allowAnonymous = ControllerActionDescriptor.MethodInfo.GetCustomAttributes(ManagementConfig.DefinedAnonymousFilterType!, true).FirstOrDefault() != null || ControllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes(ManagementConfig.DefinedAnonymousFilterType!, true).FirstOrDefault() != null;
            bool loggedInAllow = ControllerActionDescriptor.MethodInfo.GetCustomAttributes(ManagementConfig.DefinedLoggedInFilterType!, true).FirstOrDefault() != null || ControllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes(ManagementConfig.DefinedLoggedInFilterType!, true).FirstOrDefault() != null;
            // PermissionAction(Ignore = true) 表示“不纳入权限资源管理，但需登录”，与 PermissionLoggedInFilter 语义一致
            if (!loggedInAllow)
            {
                Rampage.Management.Permission.PermissionAction? permissionAction = ControllerActionDescriptor.MethodInfo
                    .GetCustomAttributes(typeof(Rampage.Management.Permission.PermissionAction), true)
                    .OfType<Rampage.Management.Permission.PermissionAction>()
                    .FirstOrDefault();
                loggedInAllow = permissionAction != null && permissionAction.Ignore;
            }

            string path = filterContext.HttpContext.Request.Path;
            string returnUrl = filterContext.HttpContext.Request.RawUrl();
            string redirectUrl = "~/Passport/SignIn?RedirectUrl=" + HttpUtility.UrlEncode(returnUrl);

            ResultCode code = ResultCode.Success;
            string? message = null;
            if (false == allowAnonymous)
            {
                R<AdminDto> loginR = AdminService.GetLoginAdmin();
                ResultCode loginResult = (ResultCode)loginR.Code;
                if (loginR.IsSuccess && loginR.Data != null)
                {
                    AdminDto currentAdmin = loginR.Data;
                    if (loggedInAllow)
                    {
                        if (currentAdmin.Status == CommonStatus.Disabled)
                        {
                            code = ResultCode.DisabledAccount;
                        }
                    }
                    else
                    {
                        code = AdminPermissionService.IdentifyAuthorization(currentAdmin.Id, path, ControllerName, ActionName, AreaName);
                    }
                }
                else
                {
                    switch (loginResult)
                    {
                        case ResultCode.InvalidToken:
                            code = ResultCode.InvalidToken;
                            break;
                        case ResultCode.TokenExpired:
                            code = ResultCode.TokenExpired;
                            break;
                        case ResultCode.RefreshToken:
                            code = ResultCode.RefreshToken;
                            break;
                        default:
                            code = ResultCode.NotLoggedIn;
                            break;
                    }
                }
            }

            if (code != ResultCode.Success)
            {
                if (IsAjax)
                {
                    filterContext.Result = new JsonResult(R.Create(code, message, null));
                }
                else
                {
                    filterContext.Result = new RedirectResult(redirectUrl);
                }
                return false;
            }

            return true;
        }
    }
}
