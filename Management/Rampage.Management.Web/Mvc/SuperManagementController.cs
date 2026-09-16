using Microsoft.AspNetCore.Mvc.Filters;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Localization;
using Rampage.Management.BLL;
using Rampage.Management.Dto;
using Rampage.Management.Permission;
using Rampage.Management.VO;
using Rampage.Management.Web.Mvc;
using Rampage.Model;
using Rampage.Utilities;
using Rampage.Web.Mvc;
using System;

namespace Rampage.Management.Mvc
{
    /// <summary>
    /// 管理系统控制器基类
    /// Management system controller base class
    /// </summary>
    [ManagementAuthorizationFilter]
    public abstract class SuperManagementController : SuperController
    {
        /// <summary>
        /// 
        /// </summary>
        [Auto]
        protected IAdminService AdminService = default!;
        [Auto]
        protected IAdminPermissionService AdminPermissionService = default!;
        [Auto]
        protected IManageMenuService ManageMenuService = default!;


        protected CurrentAuthorizationInfo AuthorizationInfo
        {
            get; private set;
        } = default!;

        /// <summary>
        /// 当前登录的管理员
        /// </summary>
        protected AdminDto? CurrentAdmin
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前请求的功能
        /// </summary>
        protected PermissionActionInfo? CurrentAction
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        protected ManageMenuDto? CurrentMenu
        {
            get;
            private set;
        }

        /// <summary>
        /// 授权请求的模块功能
        /// </summary>
        protected List<PermissionActionInfo>? AuthorizedActionList
        {
            get;
            private set;
        }

        /// <summary>
        /// 授权请求的模块功能
        /// </summary>
        protected List<ManageMenuDto>? AuthorizedMenuList
        {
            get;
            private set;
        }

        protected List<MenuBreadCrumb>? MenuBreadCrumbList
        {
            get;
            private set;
        }

        private LocalizationModule? _permissionLanguage;
        /// <summary>
        /// 
        /// </summary>
		protected virtual LocalizationModule PermissionLanguage
        {
            get
            {
                if (_permissionLanguage == null)
                {
                    _permissionLanguage = LocalizationManager.GetModule(PermissionLoader.ResourceModule);
                }
                return _permissionLanguage!;
            }
        }

        protected override bool BeforeActionExecuting(ActionExecutingContext context)
        {
            string path = context.HttpContext.Request.Path;
            List<ManageMenuDto> menuList = ManageMenuService.GetManageMenuListFromCache(CommonStatus.Enabled);

            R<AdminDto> loginR = AdminService.GetLoginAdmin();
            if (loginR.IsSuccess && loginR.Data != null)
            {
                CurrentAdmin = loginR.Data;
                AuthorizedActionList = AdminPermissionService.GetAuthorizedActionListOfAdminFromCache(CurrentAdmin.Id);
                AuthorizedMenuList = ManageMenuService.GetAuthorizedManageMenuTreeListOfAdminFromCache(CurrentAdmin.Id);
            }

            if (AuthorizedActionList != null && AuthorizedActionList.Any())
            {
                CurrentAction = AuthorizedActionList.Find(a => StringTool.EqualsIgnoreCase(a.ControllerName, ControllerName) && StringTool.EqualsIgnoreCase(a.ActionName, ActionName) && StringTool.EqualsIgnoreCase(a.AreaName, AreaName));
            }

            if (menuList.Any())
            {
                CurrentMenu = menuList.Find(m => StringTool.EqualsIgnoreCase(m.Url, path));
                if (CurrentMenu != null)
                {
                    MenuBreadCrumbList = new List<MenuBreadCrumb>();
                    LoadMenuBreadCrumbList(menuList, CurrentMenu);
                    MenuBreadCrumbList.Reverse();
                }
            }

            ViewBag.CurrentAdmin = CurrentAdmin;
            ViewBag.ActionDescriptor = ControllerActionDescriptor;
            ViewBag.AuthorizedActionList = AuthorizedActionList;
            ViewBag.AuthorizedMenuList = AuthorizedMenuList;
            ViewBag.CurrentAction = CurrentAction;

            AuthorizationInfo = new CurrentAuthorizationInfo
            {
                CurrentAdmin = CurrentAdmin,
                AuthorizedActionList = AuthorizedActionList,
                AuthorizedMenuList = AuthorizedMenuList,
                ActionDescriptor = ControllerActionDescriptor,
                CurrentAction = CurrentAction,
                CurrentMenu = CurrentMenu,
                MenuBreadCrumbList = MenuBreadCrumbList,
                GlobalLanguage = GlobalLanguage,
                PermissionLanguage = PermissionLanguage
            };
            ViewBag.CurrentAuthorization = AuthorizationInfo;

            return true;
        }


        private void LoadMenuBreadCrumbList(List<ManageMenuDto> menuList, ManageMenuDto? currentMenu)
        {
            if (MenuBreadCrumbList == null)
            {
                MenuBreadCrumbList = new List<MenuBreadCrumb>();
            }
            if (currentMenu != null)
            {
                MenuBreadCrumbList.Add(new MenuBreadCrumb
                {
                    Id = currentMenu.Id,
                    Title = currentMenu.Title,
                    ParentId = currentMenu.ParentId,
                    Url = currentMenu.Url,
                    ActionInfo = currentMenu.ActionInfo
                });

                if (currentMenu.ParentId > 0)
                {
                    ManageMenuDto? parentMenu = menuList.Find(m => m.Id == currentMenu.ParentId);
                    LoadMenuBreadCrumbList(menuList, parentMenu);
                }
            }
        }

    }
}
