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
    /// <summary>
    /// 管理系统鉴权过滤器：统一执行登录校验与权限校验，未通过时 Ajax 请求返回携带错误码的 JSON，普通请求重定向到登录页
    /// Management authorization filter: enforces login and permission checks centrally; on failure an Ajax request gets a JSON payload with the error code while a normal request is redirected to the sign-in page
    /// </summary>
    public class ManagementAuthorizationFilter : ActionFilterBase
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
        /// 鉴权前置处理：标记匿名的入口直接放行；仅需登录的入口只校验账号禁用状态；其余入口执行 Area/Controller/Action 级权限判定
        /// Authorization pre-check: entries marked anonymous pass; login-only entries merely verify the disabled status; all other entries run Area/Controller/Action level permission identification
        /// </summary>
        /// <param name="filterContext">Action 执行上下文 / The action executing context</param>
        /// <returns>鉴权通过返回 true；未通过时写入短路结果（Ajax 为 JSON、普通请求为登录页重定向）并返回 false / True when authorized; otherwise a short-circuit result is written (JSON for Ajax, a sign-in redirect otherwise) and false is returned</returns>
        /// <remarks>
        /// 匿名与"仅需登录"的判定分别读取 ManagementConfig.DefinedAnonymousFilterType / DefinedLoggedInFilterType 标记类型，方法级与控制器级均生效。
        /// 登录态读取失败时按具体错误码（InvalidToken / TokenExpired / RefreshToken）返回，其余情况视为未登录（NotLoggedIn）。
        /// 重定向地址为 ~/Passport/SignIn?RedirectUrl=（原始 URL 经编码），用于登录后跳回。
        /// </remarks>
        protected override bool BeforeActionExecuting(ActionExecutingContext filterContext)
        {
            bool allowAnonymous = ControllerActionDescriptor.MethodInfo.GetCustomAttributes(ManagementConfig.DefinedAnonymousFilterType!, true).FirstOrDefault() != null || ControllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes(ManagementConfig.DefinedAnonymousFilterType!, true).FirstOrDefault() != null;
            bool loggedInAllow = ControllerActionDescriptor.MethodInfo.GetCustomAttributes(ManagementConfig.DefinedLoggedInFilterType!, true).FirstOrDefault() != null || ControllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes(ManagementConfig.DefinedLoggedInFilterType!, true).FirstOrDefault() != null;
            // PermissionAction(Ignore = true) 表示“不纳入权限资源管理，但需登录”，与 PermissionLoggedInFilter 语义一致 / PermissionAction(Ignore = true) means "excluded from permission resources but login required", the same semantics as PermissionLoggedInFilter
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
