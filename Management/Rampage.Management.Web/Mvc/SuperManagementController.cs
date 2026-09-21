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
        /// 当前请求的授权信息聚合（当前管理员、已授权功能/菜单、当前功能/菜单、面包屑与语言模块），装配后同时暴露到 ViewBag.CurrentAuthorization
        /// The aggregated authorization info for the current request (current admin, authorized actions/menus, current action/menu, breadcrumbs and language modules); also exposed as ViewBag.CurrentAuthorization after assembly
        /// </summary>
        protected CurrentAuthorizationInfo AuthorizationInfo
        {
            get; private set;
        } = default!;

        /// <summary>
        /// 当前登录的管理员 / The current logged-in admin
        /// </summary>
        protected AdminDto? CurrentAdmin
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前请求命中的功能（按 Area/Controller/Action 在已授权功能列表中匹配）/ The permission action of the current request (matched by area/controller/action within the authorized action list)
        /// </summary>
        protected PermissionActionInfo? CurrentAction
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前请求命中的管理菜单（按请求 Path 与菜单 Url 匹配）/ The management menu matching the current request (request path matched against the menu url)
        /// </summary>
        protected ManageMenuDto? CurrentMenu
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前管理员已授权的功能（Action）列表 / The authorized action list of the current admin
        /// </summary>
        protected List<PermissionActionInfo>? AuthorizedActionList
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前管理员已授权的菜单（树形）列表 / The authorized menu tree of the current admin
        /// </summary>
        protected List<ManageMenuDto>? AuthorizedMenuList
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前菜单的面包屑（已 Reverse 为顶级在前、当前菜单在后）/ The breadcrumbs of the current menu (reversed so the root comes first and the current menu last)
        /// </summary>
        protected List<MenuBreadCrumb>? MenuBreadCrumbList
        {
            get;
            private set;
        }

        /// <summary>
        /// <see cref="PermissionLanguage"/> 的后备字段 / Backing field of <see cref="PermissionLanguage"/>
        /// </summary>
        private LocalizationModule? _permissionLanguage;
        /// <summary>
        /// 权限文案语言模块（模块名 PermissionLoader.ResourceModule，惰性加载）
        /// The language module for permission texts (module name PermissionLoader.ResourceModule, lazily loaded)
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

        /// <summary>
        /// 前置处理：装配当前管理员、已授权功能/菜单列表、当前功能与当前菜单，构建菜单面包屑，并把授权信息写入 ViewBag 供布局视图使用
        /// Pre-processing: assembles the current admin, the authorized action/menu lists, the current action and menu, builds the menu breadcrumbs and exposes the authorization info to ViewBag for layout views
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
        /// <returns>恒为 true / Always true</returns>
        /// <remarks>当前功能在"已授权功能列表"中按 Area/Controller/Action 忽略大小写匹配；当前菜单按请求 Path 与菜单 Url 忽略大小写匹配，命中才生成面包屑。</remarks>
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


        /// <summary>
        /// 递归装载菜单面包屑：先加入当前节点，再沿 ParentId 向上回溯父级（调用方随后 Reverse 为顶级在前）
        /// Recursively loads the menu breadcrumbs: appends the current node first, then walks up along ParentId (the caller reverses afterwards so the root comes first)
        /// </summary>
        /// <param name="menuList">平铺菜单列表（按 Id 查找父级）/ The flat menu list (used to find the parent by id)</param>
        /// <param name="currentMenu">当前回溯到的菜单节点 / The menu node currently being visited</param>
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
