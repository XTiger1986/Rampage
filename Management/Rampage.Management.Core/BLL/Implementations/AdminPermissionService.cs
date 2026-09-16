using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Log;
using Rampage.Management.Configuration;
using Rampage.Management.DAL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.Reflection;
using Rampage.Utilities;
using System;

namespace Rampage.Management.BLL
{
    /// <summary>
    /// 
    /// </summary>
    public class AdminPermissionService : ManagementService<AdminPermission, IAdminPermissionDao>, IAdminPermissionService
    {
        

        [Auto]
        protected IAdminService AdminService = default!;


        public ResultCode IdentifyAuthorization(int adminId, string path, string controllerName, string actionName, string? areaName = null)
        {
            areaName = areaName ?? string.Empty;

            if (path.IndexOf("?") > 0)
            {
                path = path.Substring(0, path.IndexOf("?"));
            }

            PermissionActionInfo? permissionActionInfo = PermissionLoader.GetAction(areaName, controllerName, actionName);

            AdminDto? admin = AdminService.GetAdminByIdFromCache(adminId);
            if (admin == null)
            {
                return ResultCode.Unauthorized;
            }
            else
            {
                if (admin.Status == CommonStatus.Disabled)
                {
                    return ResultCode.DisabledAccount;
                }

                if (permissionActionInfo == null)
                {
                    // 未注册进权限系统的 Action：默认 fail-closed。仅登录即可访问的功能请显式标记 PermissionLoggedInFilter，
                    // 不纳入权限管理的功能请标记 PermissionAction(Ignore = true)（由 ManagementAuthorizationFilter 直接放行）。
                    if (ManagementConfig.UnregisteredActionPolicy == UnregisteredActionPolicy.AllowLoggedIn || admin.IsMaster)
                    {
                        return ResultCode.Success;
                    }
                    RampageLogger.Warn($"Permission denied for unregistered action {areaName}/{controllerName}/{actionName} (admin {adminId}). " +
                                       "Mark it with PermissionLoggedInFilter / PermissionAction(Ignore=true) or set ManagementConfig.UnregisteredActionPolicy.");
                    return ResultCode.Unauthorized;
                }

                if (admin.IsMaster)
                {
                    return ResultCode.Success;
                }

                List<PermissionActionInfo> actionList = GetAuthorizedActionListOfAdminFromCache(admin);
                if (actionList.FindIndex(action =>
                {
                    return StringTool.EqualsIgnoreCase(areaName, action.AreaName) 
                    && StringTool.EqualsIgnoreCase(controllerName, action.ControllerName) 
                    && StringTool.EqualsIgnoreCase(actionName, action.ActionName);
                }) < 0)
                {
                    return ResultCode.Unauthorized;
                }

                return ResultCode.Success;
            }
        }

        public List<PermissionActionInfo> GetAuthorizedActionListOfAdmin(int adminId)
        {
            AdminDto? admin = AdminService.GetAdminByIdFromCache(adminId);
            if (admin == null)
            {
                return new List<PermissionActionInfo>();
            }
            return GetAuthorizedActionListOfAdmin(admin);
        }

        public List<PermissionActionInfo> GetAuthorizedActionListOfAdminFromCache(int adminId)
        {
            return HandleWebCache<List<PermissionActionInfo>>(() =>
            {
                return GetAuthorizedActionListOfAdmin(adminId);
            }, PermissionActionInfo.CACHE_KEY_AUTHORIZED_LIST_TPL, 3600f, adminId)!;
        }

        public List<PermissionActionInfo> GetAuthorizedActionListOfAdmin(AdminDto admin)
        {
            int adminId = admin.Id;
            if (admin.Status == CommonStatus.Disabled)
            {
                return new List<PermissionActionInfo>();
            }
            List<PermissionActionInfo> actionList = new List<PermissionActionInfo>();
            List<PermissionActionInfo> allActionList = PermissionLoader.ActionList;
            if (admin.IsMaster)
            {
                return allActionList;
            }
            
            if (admin.GroupIds != null && admin.GroupIds.Any())
            {
                List<ManageGroupPermission> groupPermissionList = ManageGroupPermissionDao.SelectList(p => admin.GroupIds.Contains(p.GroupId) && p.Status == CommonStatus.Enabled);
                if (groupPermissionList.Any())
                {
                    IEnumerable<string> groupResourceList = groupPermissionList.Select(p => p.Resource!);
                    List<PermissionActionInfo> groupActionList = allActionList.FindAll(r => groupResourceList.Contains(r.Resource));
                    actionList.AddRange(groupActionList);
                }
            }
            List<AdminPermission> adminPermissionList = AdminPermissionDao.SelectList(p => p.AdminId == adminId);
            if (adminPermissionList.Any())
            {
                IEnumerable<string> adminResourceList = adminPermissionList.Select(p => p.Resource!);
                List<PermissionActionInfo> adminActionList = allActionList.FindAll(r => adminResourceList.Contains(r.Resource));
                actionList.AddRange(adminActionList);
            }

            return actionList;
        }

        public List<PermissionActionInfo> GetAuthorizedActionListOfAdminFromCache(AdminDto admin)
        {
            return HandleWebCache<List<PermissionActionInfo>>(() =>
            {
                return GetAuthorizedActionListOfAdmin(admin);
            }, PermissionActionInfo.CACHE_KEY_AUTHORIZED_LIST_TPL, 3600f, admin.Id)!;
        }

        public List<AdminPermissionDto> GetAdminPermissionList(int adminId, CommonStatus? status = null)
        {
            LambdaWrapper<AdminPermission> wrapper = new LambdaWrapper<AdminPermission>(d => d.AdminId == adminId);
            if (status != null)
            {
                wrapper.And(p => p.Status == status.Value);
            }
            return AdminPermissionDao.SelectList<AdminPermissionDto>(wrapper);
        }

        public void UpdateAdminPermissionListForAdmin(int adminId, List<string> resourceIds)
        {
            List<AdminPermissionDto> existsList = GetAdminPermissionList(adminId);
            List<string> existsResourceIdList = existsList.Select(p => p.Resource!).ToList();
            List<AdminPermission> toAddList = resourceIds.Where(r => !existsList.Any(p => StringTool.EqualsIgnoreCase(p.Resource, r))).Select(r =>
            {
                return new AdminPermission
                {
                    AdminId = adminId,
                    Resource = r,
                    Status = CommonStatus.Enabled,
                    CreateTime = UtcNow,
                    UpdateTime = UtcNow
                };
            }).ToList();
            List<AdminPermission> toDisabledList = existsList.Where(p => !resourceIds.Any(r => StringTool.EqualsIgnoreCase(r, p.Resource))).Select(p =>
            {
                p.Status = CommonStatus.Disabled;
                p.UpdateTime = UtcNow;
                AdminPermission entity = ReflectionTool.Copy<AdminPermissionDto, AdminPermission>(p);
                return entity;
            }).ToList();

            if (toAddList.Any())
            {
                AdminPermissionDao.InsertBatch(toAddList);
            }
            if (toDisabledList.Any())
            {
                AdminPermissionDao.UpdateBatch(toDisabledList);
            }

            // 权限变更后立即失效该管理员的授权列表缓存，避免撤权最长1小时后才生效
            RemoveWebCache(PermissionActionInfo.CACHE_KEY_AUTHORIZED_LIST_TPL, adminId);
        }

    }
}
