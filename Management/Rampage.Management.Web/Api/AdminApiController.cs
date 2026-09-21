using Microsoft.AspNetCore.Mvc;
using Rampage.AppLog;
using Rampage.Data;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Management.BLL;
using Rampage.Management.Dto;
using Rampage.Management.Filter;
using Rampage.Management.Mvc;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.Validation;
using Rampage.Web.Component;
using System;

namespace Rampage.Management.Web.Api
{
    /// <summary>
    /// 管理后台 API 控制器（路由 api/admin）：提供管理组、管理员、管理菜单的查询与维护端点，全部纳入权限系统管控
    /// Management backend API controller (route api/admin): provides query and maintenance endpoints for management groups, administrators and management menus, all governed by the permission system
    /// </summary>
    [Route("api/admin")]
    public class AdminApiController : SuperManagementApiController
    {
        /// <summary>
        /// 管理组服务，由 [Auto] 属性注入 / The management group service, injected via [Auto]
        /// </summary>
        [Auto]
        protected IManageGroupService ManageGroupService = default!;

        #region ManageGroup

        /// <summary>
        /// 查询管理组列表 / Queries the management group list
        /// </summary>
        /// <param name="status">状态过滤，null 表示全部 / The status filter; null for all</param>
        /// <returns>管理组列表 / The management group list</returns>
        [PermissionAction(Title = "Query Management Group List", Type = PermissionActionType.Query, Resource = "GroupManagement")]
        [HttpGet("group/list")]
        public R QueryGetManageGroupList(CommonStatus? status = null)
        {
            List<ManageGroupDto> list = ManageGroupService.GetManageGroupList(status);
            return R.Success(list);
        }

        /// <summary>
        /// 查询指定管理组 / Queries the specified management group
        /// </summary>
        /// <param name="groupId">管理组ID / The management group id</param>
        /// <returns>管理组数据，不存在时 Data 为 null / The management group; Data is null when it does not exist</returns>
        [PermissionAction(Title = "Query Specified Management Group", Type = PermissionActionType.Query, Resource = "GroupManagement")]
        [HttpGet("group/get/{groupId}")]
        public R QueryGetManageGroup(int groupId)
        {
            ManageGroupDto? manageGroup = ManageGroupService.GetManageGroupById(groupId);
            return R.Success(manageGroup);
        }

        /// <summary>
        /// 保存管理组（Id 大于 0 更新，否则新增）/ Saves a management group (updates when Id &gt; 0, otherwise inserts)
        /// </summary>
        /// <param name="input">管理组数据 / The management group data</param>
        /// <returns>操作结果 / The operation result</returns>
        /// <remarks>仅"新增成功且提交了 ResourceIds"时才会同步该组的权限关联；更新（Id&gt;0）分支直接返回、不同步权限。异常统一捕获、记日志后以失败结果返回。</remarks>
        [PermissionAction(Title = "Save Management Group", Type = PermissionActionType.Operation, Resource = "GroupOperation")]
        [RecordOperationFilter]
        [HttpPost("group/save")]
        public R QuerySaveManageGroup([FromForm][ValidAttribute] ManageGroupDto input)
        {
            R operationResult;
            try
            {
                int groupId = input.Id;

                if (groupId > 0)
                {
                    operationResult = ManageGroupService.UpdateManageGroup(input);

                    return operationResult;
                }
                else
                {
                    operationResult = ManageGroupService.AddManageGroup(input);
                }

                if (operationResult.IsSuccess && input.ResourceIds != null)
                {
                    ManageGroupService.UpdateManageGroupPermissionListForGroup(input.Id, input.ResourceIds);
                }
                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 变更管理组状态（启用/禁用）/ Changes a management group's status (enable/disable)
        /// </summary>
        /// <param name="groupId">管理组ID / The management group id</param>
        /// <param name="status">目标状态 / The target status</param>
        /// <returns>操作结果 / The operation result</returns>
        [PermissionAction(Title = "Change Management Group Status", Type = PermissionActionType.Operation, Resource = "GroupOperation")]
        [RecordOperationFilter]
        [HttpPost("group/status/change")]
        public R QueryChangeManageGroupStatus(int groupId, CommonStatus status)
        {
            try
            {
                R result = ManageGroupService.ChangeManageGroupStatus(groupId, status);
                return result;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 删除指定管理组 / Deletes the specified management group
        /// </summary>
        /// <param name="groupId">管理组ID / The management group id</param>
        /// <returns>操作结果 / The operation result</returns>
        [PermissionAction(Title = "Delete Specified Management Group", Type = PermissionActionType.Delete, Resource = "GroupDelete")]
        [RecordOperationFilter]
        [HttpDelete("group/delete/{groupId}")]
        public R QueryDeleteManageGroup(int groupId)
        {
            try
            {
                R operationResult = ManageGroupService.DeleteManageGroupById(groupId);
                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        #endregion

        #region Admin

        /// <summary>
        /// 查询当前登录管理员已授权的功能（Action）列表 / Queries the authorized action list of the current logged-in admin
        /// </summary>
        /// <returns>已授权功能列表（读管理员缓存）/ The authorized action list (from the admin cache)</returns>
        [PermissionAction(Title = "Query Authorized Action List", Type = PermissionActionType.Query, Resource = "AdminManagement")]
        [HttpGet("action/authorized/list")]
        public R QueryGetAuthorizedActionList()
        {
            List<PermissionActionInfo> list = AdminPermissionService.GetAuthorizedActionListOfAdminFromCache(CurrentAdmin!.Id);
            return R.Success(list);
        }

        /// <summary>
        /// 分页查询管理员列表 / Queries the administrator list by page
        /// </summary>
        /// <returns>列表视图数据（数据列表、分页器与过滤条件）/ The list view data (items, pager and filter)</returns>
        /// <remarks>分页参数取自 QueryString：page=页码、ps=页大小（默认 20，上限 100）；过滤条件由请求数据渲染为 AdminFilter。</remarks>
        [PermissionAction(Title = "Query Administrator Page List", Type = PermissionActionType.Query, Resource = "AdminManagement")]
        [HttpGet("page")]
        public R QueryGetAdminPageList()
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
            return R.Success(viewModel);
        }

        /// <summary>
        /// 查询指定管理员 / Queries the specified administrator
        /// </summary>
        /// <param name="adminId">管理员ID / The administrator id</param>
        /// <returns>管理员数据，不存在时 Data 为 null / The administrator; Data is null when it does not exist</returns>
        [PermissionAction(Title = "Query Specified Administrator", Type = PermissionActionType.Query, Resource = "AdminManagement")]
        [HttpGet("get/{adminId}")]
        public R QueryGetAdmin([FromRoute] int adminId)
        {
            AdminDto? admin = AdminService.GetAdminById(adminId);
            return R.Success(admin);
        }

        /// <summary>
        /// 保存管理员（Id 大于 0 更新，否则新增）/ Saves an administrator (updates when Id &gt; 0, otherwise inserts)
        /// </summary>
        /// <param name="input">管理员数据 / The administrator data</param>
        /// <returns>操作结果 / The operation result</returns>
        /// <remarks>仅"新增成功且提交了 ResourceIds"时才会同步该管理员的权限关联；更新（Id&gt;0）分支直接返回、不同步权限。异常统一捕获、记日志后以失败结果返回。</remarks>
        [PermissionAction(Title = "Save Administrator", Type = PermissionActionType.Operation, Resource = "AdminOperation")]
        [RecordOperationFilter]
        [HttpPost("save")]
        public R QuerySaveAdmin([FromForm][ValidAttribute] AdminDto input)
        {
            R operationResult;
            try
            {
                int adminId = input.Id;

                if (adminId > 0)
                {
                    operationResult = AdminService.UpdateAdmin(input, input.GroupIds);
                    return operationResult;
                }
                else
                {
                    operationResult = AdminService.AddAdmin(input, input.GroupIds);
                }

                if (operationResult.IsSuccess)
                {
                    if (input.ResourceIds != null)
                    {
                        AdminPermissionService.UpdateAdminPermissionListForAdmin(input.Id, input.ResourceIds);
                    }
                }
                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 变更管理员状态（启用/禁用）/ Changes an administrator's status (enable/disable)
        /// </summary>
        /// <param name="menuId">管理员ID（参数名 menuId 为历史遗留，实际按管理员ID处理）/ The administrator id (the parameter name menuId is a legacy misnomer; it is treated as the administrator id)</param>
        /// <param name="status">目标状态 / The target status</param>
        /// <returns>操作结果 / The operation result</returns>
        [PermissionAction(Title = "Change Administrator Status", Type = PermissionActionType.Operation, Resource = "AdminOperation")]
        [RecordOperationFilter]
        [HttpPost("status/change")]
        public R QueryChangeAdminStatus(int menuId, CommonStatus status)
        {
            try
            {
                R result = AdminService.ChangeAdminStatus(menuId, status);
                return result;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 删除指定管理员 / Deletes the specified administrator
        /// </summary>
        /// <param name="adminId">管理员ID / The administrator id</param>
        /// <returns>操作结果 / The operation result</returns>
        [PermissionAction(Title = "Delete Specified Administrator", Type = PermissionActionType.Delete, Resource = "AdminDelete")]
        [RecordOperationFilter]
        [HttpDelete("delete/{adminId}")]
        public R QueryDeleteAdmin([FromRoute] int adminId)
        {
            try
            {
                R operationResult = AdminService.DeleteAdminById(adminId);
                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        #endregion

        #region ManageMenu

        /// <summary>
        /// 查询管理菜单树 / Queries the management menu tree
        /// </summary>
        /// <param name="status">状态过滤，null 表示全部 / The status filter; null for all</param>
        /// <returns>树形菜单列表 / The tree-shaped menu list</returns>
        [PermissionAction(Title = "Query Management Menu Tree", Type = PermissionActionType.Query, Resource = "ManageMenuManagement")]
        [HttpGet("menu/tree")]
        public R QueryGetManageMenuTreeList(CommonStatus? status = null)
        {
            List<ManageMenuDto> list = ManageMenuService.GetManageMenuTreeList(status);
            return R.Success(list);
        }

        /// <summary>
        /// 查询管理菜单平铺列表 / Queries the flat management menu list
        /// </summary>
        /// <param name="status">状态过滤，null 表示全部 / The status filter; null for all</param>
        /// <returns>菜单列表 / The menu list</returns>
        [PermissionAction(Title = "Query Management Menu List", Type = PermissionActionType.Query, Resource = "ManageMenuManagement")]
        [HttpGet("menu/list")]
        public R QueryGetManageMenuList(CommonStatus? status = null)
        {
            List<ManageMenuDto> list = ManageMenuService.GetManageMenuList(status);
            return R.Success(list);
        }

        /// <summary>
        /// 查询指定管理菜单 / Queries the specified management menu
        /// </summary>
        /// <param name="menuId">菜单ID / The menu id</param>
        /// <returns>菜单数据，不存在时 Data 为 null / The menu; Data is null when it does not exist</returns>
        [PermissionAction(Title = "Query Specified Management Menu", Type = PermissionActionType.Query, Resource = "ManageMenuManagement")]
        [HttpGet("menu/get/{menuId}")]
        public R QueryGetManageMenu(int menuId)
        {
            ManageMenuDto? menu = ManageMenuService.GetManageMenuById(menuId);
            return R.Success(menu);
        }

        /// <summary>
        /// 查询当前登录管理员已授权的菜单树 / Queries the authorized menu tree of the current logged-in admin
        /// </summary>
        /// <returns>已授权树形菜单列表（读管理员缓存）/ The authorized tree-shaped menu list (from the admin cache)</returns>
        [PermissionAction(Title = "Query Authorized Management Menu Tree", Type = PermissionActionType.Query, Resource = "ManageMenuManagement")]
        [HttpGet("menu/authorized/list")]
        public R QueryGetAuthorizedManageMenuTreeList()
        {
            List<ManageMenuDto> list = ManageMenuService.GetAuthorizedManageMenuTreeListOfAdminFromCache(CurrentAdmin!.Id);
            return R.Success(list);
        }

        /// <summary>
        /// 保存管理菜单（Id 大于 0 更新，否则新增）/ Saves a management menu (updates when Id &gt; 0, otherwise inserts)
        /// </summary>
        /// <param name="input">菜单数据 / The menu data</param>
        /// <returns>操作结果 / The operation result</returns>
        [PermissionAction(Title = "Save Management Menu", Type = PermissionActionType.Operation, Resource = "ManageMenuOperation")]
        [RecordOperationFilter]
        [HttpPost("menu/save")]
        public R QuerySaveManageMenu([FromForm][ValidAttribute] ManageMenuDto input)
        {
            R operationResult;
            try
            {
                int menuId = input.Id;

                if (menuId > 0)
                {
                    operationResult = ManageMenuService.UpdateManageMenu(input);
                    return operationResult;
                }
                else
                {
                    operationResult = ManageMenuService.AddManageMenu(input);
                }

                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 变更管理菜单状态（启用/禁用）/ Changes a management menu's status (enable/disable)
        /// </summary>
        /// <param name="menuId">菜单ID / The menu id</param>
        /// <param name="status">目标状态 / The target status</param>
        /// <returns>操作结果 / The operation result</returns>
        [PermissionAction(Title = "Change Management Menu Status", Type = PermissionActionType.Operation, Resource = "ManageMenuOperation")]
        [RecordOperationFilter]
        [HttpPost("menu/status/change")]
        public R QueryChangeManageMenuStatus(int menuId, CommonStatus status)
        {
            try
            {
                R result = ManageMenuService.ChangeManageMenuStatus(menuId, status);
                return result;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        /// <summary>
        /// 删除指定管理菜单 / Deletes the specified management menu
        /// </summary>
        /// <param name="menuId">菜单ID / The menu id</param>
        /// <returns>操作结果 / The operation result</returns>
        [PermissionAction(Title = "Delete Specified Management Menu", Type = PermissionActionType.Delete, Resource = "ManageMenuDelete")]
        [RecordOperationFilter]
        [HttpDelete("menu/delete/{menuId}")]
        public R QueryDeleteManageMenu(int menuId)
        {
            try
            {
                R operationResult = ManageMenuService.DeleteManageMenuById(menuId);
                return operationResult;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                return R.Failure(ex.Message);
            }
        }

        #endregion

    }
}
