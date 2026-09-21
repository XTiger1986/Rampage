using Microsoft.AspNetCore.Mvc;
using Rampage.Data;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.ExceptionExtensions;
using Rampage.Management.BLL;
using Rampage.Management.Core.BLL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Management.Filter;
using Rampage.Management.Permission;
using Rampage.Web.Component;
using System;
using System.Net;

namespace Rampage.Management.Mvc
{
    /// <summary>
    /// 管理后台页面控制器：管理组、管理员、管理菜单与管理员操作日志的列表及编辑页面
    /// Management backend page controller: list and edit pages for management groups, administrators, management menus and admin operation logs
    /// </summary>
    public class AdminController : SuperManagementController
    {
        /// <summary>
        /// 管理组服务，由 [Auto] 属性注入 / The management group service, injected via [Auto]
        /// </summary>
        [Auto]
        protected IManageGroupService ManageGroupService = default!;
        /// <summary>
        /// 管理员操作日志服务，由 [Auto] 属性注入 / The admin operation log service, injected via [Auto]
        /// </summary>
        [Auto]
        protected IAdminOperationLogService AdminOperationLogService = default!;


        #region ManageGroup

        /// <summary>
        /// 管理组列表页 / The management group list page
        /// </summary>
        /// <returns>列表视图（全量管理组，不分页）/ The list view (all management groups, unpaged)</returns>
        [PermissionAction(Title = "Management Group List", Type = PermissionActionType.Page, Resource = "GroupManagement")]
        public IActionResult Group_List()
        {
            List<ManageGroupDto> list = ManageGroupService.GetManageGroupList(null);

            ListViewData<ManageGroupDto> viewModel = new ListViewData<ManageGroupDto>
            {
                DataList = list
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 新增管理组页 / The add-management-group page
        /// </summary>
        /// <returns>复用 Group_Edit 视图（空模型），并注入权限资源字典供勾选权限 / Reuses the Group_Edit view (empty model) with the permission resource dictionaries injected for permission selection</returns>
        [PermissionAction(Title = "Add Management Group", Type = PermissionActionType.Page, Resource = "GroupManagement")]
        public IActionResult Group_Add()
        {
            ViewBag.ResourceDict = PermissionLoader.ResourceDict;
            ViewBag.ResourceActionListDict = PermissionLoader.ResourceActionListDict;
            return VW("Group_Edit", new ManageGroupDto());
        }

        /// <summary>
        /// 编辑管理组页 / The edit-management-group page
        /// </summary>
        /// <returns>复用 Group_Edit 视图，并注入该组已授权限与权限资源字典 / Reuses the Group_Edit view with the group's granted permissions and the permission resource dictionaries</returns>
        /// <remarks>管理组不存在时抛出 404（SuperHttpException）。</remarks>
        [PermissionAction(Title = "Modify Management Group", Type = PermissionActionType.Page, Resource = "GroupManagement")]
        public IActionResult Group_Modify()
        {
            int groupId = Get<int>("GroupId");
            ManageGroupDto? manageGroup = ManageGroupService.GetManageGroupById(groupId);
            if (manageGroup == null)
            {
                throw new SuperHttpException("SpecifiedManageGroup_NotFound", HttpStatusCode.NotFound);
            }
            ViewBag.ManageGroupPermissionList = ManageGroupService.GetManageGroupPermissionList(groupId, CommonStatus.Enabled);
            ViewBag.ResourceDict = PermissionLoader.ResourceDict;
            ViewBag.ResourceActionListDict = PermissionLoader.ResourceActionListDict;
            return VW("Group_Edit", manageGroup);
        }

        #endregion

        #region Admin

        /// <summary>
        /// 管理员列表页（分页）/ The administrator list page (paged)
        /// </summary>
        /// <returns>列表视图（数据、分页器与过滤条件）/ The list view (items, pager and filter)</returns>
        /// <remarks>分页参数取自 QueryString：page=页码、ps=页大小（默认 20，上限 100）；过滤条件由请求数据渲染为 AdminFilter。</remarks>
        [PermissionAction(Title = "Administrator List", Type = PermissionActionType.Page, Resource = "AdminManagement")]
        public IActionResult Admin_List()
        {
            PagerInfo pagerInfo = new PagerInfo();
            pagerInfo.PageIndex = Get<int>("page");
            pagerInfo.PageSize = Get<int>("ps", 20);
            if (pagerInfo.PageSize > 100)
            {
                pagerInfo.PageSize = 100;
            }

            AdminFilter filter = RenderModel<AdminFilter>();

            List<AdminDto> list = AdminService.GetAdminPageListByFilter(filter, pagerInfo);
            DataPager dataPager = new DataPager(pagerInfo);
            var viewModel = new ListViewData<AdminDto, AdminFilter>
            {
                DataList = list,
                DataPager = dataPager,
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 新增管理员页 / The add-administrator page
        /// </summary>
        /// <returns>复用 Admin_Edit 视图（空模型），并注入启用状态的管理组与权限资源字典 / Reuses the Admin_Edit view (empty model) with the enabled management groups and the permission resource dictionaries</returns>
        [PermissionAction(Title = "Add Administrator", Type = PermissionActionType.Page, Resource = "AdminManagement")]
        public IActionResult Admin_Add()
        {
            ViewBag.GroupList = ManageGroupService.GetManageGroupList(CommonStatus.Enabled);
            ViewBag.ResourceDict = PermissionLoader.ResourceDict;
            ViewBag.ResourceActionListDict = PermissionLoader.ResourceActionListDict;
            return VW("Admin_Edit", new AdminDto());
        }

        /// <summary>
        /// 编辑管理员页 / The edit-administrator page
        /// </summary>
        /// <returns>复用 Admin_Edit 视图，并注入管理组、该管理员已授权限与权限资源字典 / Reuses the Admin_Edit view with the management groups, the admin's granted permissions and the permission resource dictionaries</returns>
        /// <remarks>管理员不存在时抛出 404（SuperHttpException）。</remarks>
        [PermissionAction(Title = "Modify Administrator", Type = PermissionActionType.Page, Resource = "AdminManagement")]
        public IActionResult Admin_Modify()
        {
            int adminId = Get<int>("AdminId");
            AdminDto? admin = AdminService.GetAdminById(adminId);
            if (admin == null)
            {
                throw new SuperHttpException("SpecifiedAdministrator_NotFound", HttpStatusCode.NotFound);
            }
            ViewBag.GroupList = ManageGroupService.GetManageGroupList(CommonStatus.Enabled);
            ViewBag.AdminPermissionList = AdminPermissionService.GetAdminPermissionList(adminId, CommonStatus.Enabled);
            ViewBag.ResourceDict = PermissionLoader.ResourceDict;
            ViewBag.ResourceActionListDict = PermissionLoader.ResourceActionListDict;
            return VW("Admin_Edit", admin);
        }

        #endregion

        #region ManageMenu

        /// <summary>
        /// 管理菜单列表页（树形展示）/ The management menu list page (shown as a tree)
        /// </summary>
        /// <returns>列表视图（树形菜单数据）/ The list view (tree-shaped menu data)</returns>
        [PermissionAction(Title = "Management Menu List", Type = PermissionActionType.Page, Resource = "ManageMenuManagement")]
        public IActionResult Menu_List()
        {
            List<ManageMenuDto> list = ManageMenuService.GetManageMenuTreeList(null);

            var viewModel = new ListViewData<ManageMenuDto>
            {
                DataList = list
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 新增管理菜单页 / The add-management-menu page
        /// </summary>
        /// <returns>复用 Menu_Edit 视图（空模型），并注入完整菜单树供选择父级 / Reuses the Menu_Edit view (empty model) with the full menu tree for parent selection</returns>
        [PermissionAction(Title = "Add Management Menu", Type = PermissionActionType.Page, Resource = "ManageMenuManagement")]
        public IActionResult Menu_Add()
        {
            ViewBag.TreeManageMenuList = ManageMenuService.GetManageMenuTreeList();
            ViewBag.ResourceActionListDict = PermissionLoader.ResourceActionListDict;
            return VW("Menu_Edit", new ManageMenuDto());
        }

        /// <summary>
        /// 编辑管理菜单页 / The edit-management-menu page
        /// </summary>
        /// <returns>复用 Menu_Edit 视图，并注入菜单树与当前菜单数据 / Reuses the Menu_Edit view with the menu tree and the current menu data</returns>
        /// <remarks>先取整棵菜单树再从中定位目标菜单，不存在时抛出 404（SuperHttpException）。</remarks>
        [PermissionAction(Title = "Modify Management Menu", Type = PermissionActionType.Page, Resource = "ManageMenuManagement")]
        public IActionResult Menu_Modify()
        {
            int menuId = Get<int>("MenuId");

            List<ManageMenuDto> menuTreeList = ManageMenuService.GetManageMenuTreeList();
            ManageMenuDto? menu = ManageMenuService.GetManageMenuInTree(menuTreeList, menuId);
            if (menu == null)
            {
                throw new SuperHttpException("SpecifiedManageMenu_NotFound", HttpStatusCode.NotFound);
            }
            ViewBag.TreeManageMenuList = menuTreeList;
            ViewBag.ResourceActionListDict = PermissionLoader.ResourceActionListDict;
            return VW("Menu_Edit", menu);
        }

        #endregion

        #region AdminOperationLog

        /// <summary>
        /// 管理员操作日志列表页（分页）/ The admin operation log list page (paged)
        /// </summary>
        /// <returns>列表视图（数据、分页器与过滤条件）/ The list view (items, pager and filter)</returns>
        /// <remarks>分页参数取自 QueryString：page=页码、ps=页大小（默认 20，上限 100）。</remarks>
        [PermissionAction(Title = "Admin Operation Log List", Type = PermissionActionType.Page, Resource = "AdminLogManagement")]
        public IActionResult AdminOperationLog_List()
        {
            PagerInfo pagerInfo = new PagerInfo();
            pagerInfo.PageIndex = Get<int>("page");
            pagerInfo.PageSize = Get<int>("ps", 20);
            if (pagerInfo.PageSize > 100)
            {
                pagerInfo.PageSize = 100;
            }

            AdminOperationLogFilter filter = RenderModel<AdminOperationLogFilter>();

            List<AdminOperationLogDto> list = AdminOperationLogService.SearchAdminOperationLogPageList(pagerInfo, filter);
            DataPager dataPager = new DataPager(pagerInfo);
            var viewModel = new ListViewData<AdminOperationLogDto, AdminOperationLogFilter>
            {
                DataList = list,
                DataPager = dataPager,
                DataFilter = filter
            };
            return VW(viewModel);
        }

        /// <summary>
        /// 管理员操作日志详情页 / The admin operation log detail page
        /// </summary>
        /// <returns>详情视图 / The detail view</returns>
        /// <remarks>日志不存在时抛出 404（SuperHttpException）。</remarks>
        [PermissionAction(Title = "Admin Operation Log Detail", Type = PermissionActionType.Page, Resource = "AdminLogManagement")]
        public IActionResult AdminOperationLog_Detail()
        {
            int logId = Get<int>("LogId");
            AdminOperationLogDto? log = AdminOperationLogService.GetAdminOperationLogById(logId);
            if (log == null)
            {
                throw new SuperHttpException("SpecifiedAdminOperationLog_NotFound", HttpStatusCode.NotFound);
            }
            return VW(log);
        }

        #endregion
      
    }
}
