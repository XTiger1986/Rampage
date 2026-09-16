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
    /// 
    /// </summary>
    public class AdminController : SuperManagementController
    {
        [Auto]
        protected IManageGroupService ManageGroupService = default!;
        [Auto]
        protected IAdminOperationLogService AdminOperationLogService = default!;


        #region ManageGroup

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
        [PermissionAction(Title = "Add Management Group", Type = PermissionActionType.Page, Resource = "GroupManagement")]
        public IActionResult Group_Add()
        {
            ViewBag.ResourceDict = PermissionLoader.ResourceDict;
            ViewBag.ResourceActionListDict = PermissionLoader.ResourceActionListDict;
            return VW("Group_Edit", new ManageGroupDto());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
        [PermissionAction(Title = "Add Administrator", Type = PermissionActionType.Page, Resource = "AdminManagement")]
        public IActionResult Admin_Add()
        {
            ViewBag.GroupList = ManageGroupService.GetManageGroupList(CommonStatus.Enabled);
            ViewBag.ResourceDict = PermissionLoader.ResourceDict;
            ViewBag.ResourceActionListDict = PermissionLoader.ResourceActionListDict;
            return VW("Admin_Edit", new AdminDto());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
        [PermissionAction(Title = "Add Management Menu", Type = PermissionActionType.Page, Resource = "ManageMenuManagement")]
        public IActionResult Menu_Add()
        {
            ViewBag.TreeManageMenuList = ManageMenuService.GetManageMenuTreeList();
            ViewBag.ResourceActionListDict = PermissionLoader.ResourceActionListDict;
            return VW("Menu_Edit", new ManageMenuDto());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
