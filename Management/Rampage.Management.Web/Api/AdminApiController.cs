using Microsoft.AspNetCore.Mvc;
using Rampage.Data;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Log;
using Rampage.Management.BLL;
using Rampage.Management.Dto;
using Rampage.Management.Filter;
using Rampage.Management.Mvc;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.Reflection;
using Rampage.Validation;
using Rampage.Web.Component;
using System;

namespace Rampage.Management.Web.Api
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/admin")]
    public class AdminApiController : SuperManagementApiController
    {
        [Auto]
        protected IManageGroupService ManageGroupService = default!;

        #region ManageGroup

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [PermissionAction(Title = "Query Management Group List", Type = PermissionActionType.Query, Resource = "GroupManagement")]
        [HttpGet("group/list")]
        public R QueryGetManageGroupList(CommonStatus? status = null)
        {
            List<ManageGroupDto> list = ManageGroupService.GetManageGroupList(status);
            return R.Success(list);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        [PermissionAction(Title = "Query Specified Management Group", Type = PermissionActionType.Query, Resource = "GroupManagement")]
        [HttpGet("group/get/{groupId}")]
        public R QueryGetManageGroup(int groupId)
        {
            ManageGroupDto? manageGroup = ManageGroupService.GetManageGroupById(groupId);
            return R.Success(manageGroup);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
        [PermissionAction(Title = "Query Authorized Action List", Type = PermissionActionType.Query, Resource = "AdminManagement")]
        [HttpGet("action/authorized/list")]
        public R QueryGetAuthorizedActionList()
        {
            List<PermissionActionInfo> list = AdminPermissionService.GetAuthorizedActionListOfAdminFromCache(CurrentAdmin!.Id);
            return R.Success(list);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        [PermissionAction(Title = "Query Specified Administrator", Type = PermissionActionType.Query, Resource = "AdminManagement")]
        [HttpGet("get/{adminId}")]
        public R QueryGetAdmin([FromRoute] int adminId)
        {
            AdminDto? admin = AdminService.GetAdminById(adminId);
            return R.Success(admin);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [PermissionAction(Title = "Query Management Menu Tree", Type = PermissionActionType.Query, Resource = "ManageMenuManagement")]
        [HttpGet("menu/tree")]
        public R QueryGetManageMenuTreeList(CommonStatus? status = null)
        {
            List<ManageMenuDto> list = ManageMenuService.GetManageMenuTreeList(status);
            return R.Success(list);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [PermissionAction(Title = "Query Management Menu List", Type = PermissionActionType.Query, Resource = "ManageMenuManagement")]
        [HttpGet("menu/list")]
        public R QueryGetManageMenuList(CommonStatus? status = null)
        {
            List<ManageMenuDto> list = ManageMenuService.GetManageMenuList(status);
            return R.Success(list);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        [PermissionAction(Title = "Query Specified Management Menu", Type = PermissionActionType.Query, Resource = "ManageMenuManagement")]
        [HttpGet("menu/get/{menuId}")]
        public R QueryGetManageMenu(int menuId)
        {
            ManageMenuDto? menu = ManageMenuService.GetManageMenuById(menuId);
            return R.Success(menu);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [PermissionAction(Title = "Query Authorized Management Menu Tree", Type = PermissionActionType.Query, Resource = "ManageMenuManagement")]
        [HttpGet("menu/authorized/list")]
        public R QueryGetAuthorizedManageMenuTreeList()
        {
            List<ManageMenuDto> list = ManageMenuService.GetAuthorizedManageMenuTreeListOfAdminFromCache(CurrentAdmin!.Id);
            return R.Success(list);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
