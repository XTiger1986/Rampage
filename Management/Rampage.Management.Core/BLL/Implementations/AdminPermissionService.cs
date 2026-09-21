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
    /// 管理员权限业务逻辑实现（核心鉴权与授权合并）
    /// Administrator permission business logic implementation (core authorization and permission merging)
    /// </summary>
    public class AdminPermissionService : ManagementService<AdminPermission, IAdminPermissionDao>, IAdminPermissionService
    {
        

        /// <summary>管理员业务逻辑（用于读取管理员与缓存） / The admin service (used to load admins and caches)</summary>
        [Auto]
        protected IAdminService AdminService = default!;


        /// <summary>
        /// 识别指定管理员对指定路径的授权
        /// Identify the authorisation of the specified administrator for the specified path
        /// </summary>
        /// <remarks>
        /// 鉴权流程：管理员不存在→未授权；被禁用→账号禁用；Action 未注册进权限系统→按 UnregisteredActionPolicy 处理（默认拒绝 fail-closed，超级管理员放行）；
        /// 超级管理员→放行；否则与该管理员的授权功能缓存列表（区域/控制器/视图忽略大小写）比对。
        /// Pipeline: missing admin -> Unauthorized; disabled -> DisabledAccount; unregistered action -> UnregisteredActionPolicy (deny by default, fail-closed, masters pass);
        /// master admin -> Success; otherwise matched against the admin's cached authorized action list (case-insensitive area/controller/action).
        /// </remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="path">请求路径（自动去除查询串） / The request path (query string stripped)</param>
        /// <param name="controllerName">控制器名 / Controller name</param>
        /// <param name="actionName">视图(Action)名 / Action name</param>
        /// <param name="areaName">区域名；可为 null / Area name; may be null</param>
        /// <returns>鉴权结果码 / The authorization result code</returns>
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

        /// <summary>获取指定管理员的授权功能列表 / Obtain the list of authorised functions for the specified administrator</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>授权功能列表；管理员不存在或被禁用时为空列表 / The authorized action list; an empty list if the admin is missing or disabled</returns>
        public List<PermissionActionInfo> GetAuthorizedActionListOfAdmin(int adminId)
        {
            AdminDto? admin = AdminService.GetAdminByIdFromCache(adminId);
            if (admin == null)
            {
                return new List<PermissionActionInfo>();
            }
            return GetAuthorizedActionListOfAdmin(admin);
        }

        /// <summary>从缓存获取指定管理员的授权功能列表（约 1 小时） / Obtain the list of authorised functions for the specified administrator from cache (~1 hour)</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>带缓存的授权功能列表 / The cached authorized action list</returns>
        public List<PermissionActionInfo> GetAuthorizedActionListOfAdminFromCache(int adminId)
        {
            return HandleWebCache<List<PermissionActionInfo>>(() =>
            {
                return GetAuthorizedActionListOfAdmin(adminId);
            }, PermissionActionInfo.CACHE_KEY_AUTHORIZED_LIST_TPL, 3600f, adminId)!;
        }

        /// <summary>
        /// 获取指定管理员的授权功能列表（组权限与个人权限的并集）
        /// Obtain the list of authorised functions for the specified administrator (union of group and personal permissions)
        /// </summary>
        /// <param name="admin">管理员DTO / The administrator DTO</param>
        /// <returns>授权功能列表；被禁用时为空列表，超级管理员为全部功能 / The authorized action list; empty when disabled, all actions for master admins</returns>
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
            
            // 授权 = 组权限 ∪ 个人权限：先并入启用状态的组权限，再并入个人授权（并集，不去重）
            // Authorization = group permissions ∪ personal permissions: enabled group permissions first, then personal grants (union, not deduplicated)
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

        /// <summary>从缓存获取指定管理员的授权功能列表（约 1 小时） / Obtain the list of authorised functions for the specified administrator from cache (~1 hour)</summary>
        /// <param name="admin">管理员DTO / The administrator DTO</param>
        /// <returns>带缓存的授权功能列表 / The cached authorized action list</returns>
        public List<PermissionActionInfo> GetAuthorizedActionListOfAdminFromCache(AdminDto admin)
        {
            return HandleWebCache<List<PermissionActionInfo>>(() =>
            {
                return GetAuthorizedActionListOfAdmin(admin);
            }, PermissionActionInfo.CACHE_KEY_AUTHORIZED_LIST_TPL, 3600f, admin.Id)!;
        }

        /// <summary>获取指定管理员的授权列表 / Obtain the authorised list of the specified administrator</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>该管理员的授权记录列表 / The admin's permission records</returns>
        public List<AdminPermissionDto> GetAdminPermissionList(int adminId, CommonStatus? status = null)
        {
            LambdaWrapper<AdminPermission> wrapper = new LambdaWrapper<AdminPermission>(d => d.AdminId == adminId);
            if (status != null)
            {
                wrapper.And(p => p.Status == status.Value);
            }
            return AdminPermissionDao.SelectList<AdminPermissionDto>(wrapper);
        }

        /// <summary>
        /// 更新指定管理员的授权资源
        /// Update the authorised resources for the specified administrator
        /// </summary>
        /// <remarks>差量更新：新资源插入、被移除的资源置为禁用；完成后立即失效该管理员的授权功能缓存。
        /// Delta update: new resources are inserted, removed ones disabled; the admin's authorized-action cache is invalidated immediately afterwards.</remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="resourceIds">资源标识集合 / The resource identifiers</param>
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
