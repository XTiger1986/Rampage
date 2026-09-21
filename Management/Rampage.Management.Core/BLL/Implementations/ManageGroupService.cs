using Rampage.AppLog;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Enums;
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
    /// 管理组业务逻辑实现
    /// Management group business logic implementation
    /// </summary>
    public class ManageGroupService : ManagementService<ManageGroup, IManageGroupDao>, IManageGroupService
    {

        /// <summary>管理员-管理组关联数据访问（删除组时清理成员关系） / Admin-group membership data access (used to clean memberships when deleting a group)</summary>
        [Auto]
        protected IAdminInGroupDao AdminInGroupDao = default!;


        /// <summary>失效管理组列表缓存 / Invalidate the management group list cache</summary>
        private void RemoveManageGroupCache()
        {
            RemoveWebCacheByPrefix(ManageGroupDto.CACHE_KEY_LIST_PFX);
        }

        /// <summary>添加一个管理组 / Add a management group</summary>
        /// <param name="group">管理组数据 / The management group data</param>
        /// <returns>成功时 Data 为带新ID的管理组 / On success Data contains the group with its new ID</returns>
        public R AddManageGroup(ManageGroupDto group)
        {
            if (group.CreateTime <= DateTime.MinValue)
            {
                group.CreateTime = UtcNow;
            }
            ManageGroup entity = ReflectionTool.Copy<ManageGroupDto, ManageGroup>(group);
            
            bool result = ManageGroupDao.Insert(entity);
            if (result)
            {
                group.Id = entity.Id;
                RemoveManageGroupCache();
                return R.Success(group);
            }
            return R.Failure();
        }

        /// <summary>更新管理组 / Update a management group</summary>
        /// <param name="group">管理组数据 / The management group data</param>
        /// <returns>成功或失败（数据不存在时返回 DataNotExists） / Success or failure (DataNotExists when the group does not exist)</returns>
        public R UpdateManageGroup(ManageGroupDto group)
        {
            ManageGroup? entity = ManageGroupDao.SelectById(group.Id);
            if (entity == null)
            {
                return R.Create(ResultCode.DataNotExists);
            }
            group.UpdateTime = UtcNow;
            ReflectionTool.CopyProperties(group, entity, true);
            bool result = ManageGroupDao.UpdateById(entity);
            if (result)
            {
                RemoveManageGroupCache();
                return R.Success(group);
            }
            return R.Failure();
        }

        /// <summary>修改管理组的状态 / Change the status of the management group</summary>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <param name="status">新状态 / The new status</param>
        /// <returns>修改成功或失败 / Update success or failure</returns>
        public R ChangeManageGroupStatus(int groupId, CommonStatus status)
        {
            bool result = ManageGroupDao.UpdateById(groupId, c => c.Status, status);
            if (result)
            {
                RemoveManageGroupCache();
            }

            return result ? R.Success() : R.Failure();
        }

        /// <summary>
        /// 删除管理组（事务内同时删除组内管理员关联与组授权）
        /// Delete a management group (also deletes its admin memberships and group permissions in a transaction)
        /// </summary>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <returns>成功时 Data 为管理组ID / On success Data contains the group ID</returns>
        public R DeleteManageGroupById(int groupId)
        {
            try
            {
                BeginTransaction();

                AdminInGroupDao.Delete(d => d.GroupId == groupId);
                ManageGroupPermissionDao.Delete(d => d.GroupId == groupId);
                Delete(d => d.Id == groupId);

                CommitTransaction();

                RemoveManageGroupCache();
                return R.Success(groupId);
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                RampageLogger.Error(ex);

                return R.Failure();
            }
        }

        /// <summary>获取指定管理组的授权列表 / Get the authorised list of the specified management group</summary>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>该管理组的授权记录列表 / The group's permission records</returns>
        public List<ManageGroupPermissionDto> GetManageGroupPermissionList(int groupId, CommonStatus? status = null)
        {
            LambdaWrapper<ManageGroupPermission> wrapper = new LambdaWrapper<ManageGroupPermission>(d => d.GroupId == groupId);
            if (status != null)
            {
                wrapper.And(p => p.Status == status.Value);
            }
            return ManageGroupPermissionDao.SelectList<ManageGroupPermissionDto>(wrapper);
        }

        /// <summary>
        /// 更新指定管理组的授权资源
        /// Update the authorised resources of the specified management group
        /// </summary>
        /// <remarks>差量更新：新资源插入、被移除的资源置为禁用；组权限影响组内所有管理员，完成后统一失效全部授权功能缓存。
        /// Delta update: new resources are inserted, removed ones disabled; since group permissions affect all member admins, all authorized-action caches are invalidated afterwards.</remarks>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <param name="resourceIds">资源标识集合 / The resource identifiers</param>
        public void UpdateManageGroupPermissionListForGroup(int groupId, List<string> resourceIds)
        {
            List<ManageGroupPermissionDto> existsList = GetManageGroupPermissionList(groupId);
            List<string> existsResourceIdList = existsList.Select(p => p.Resource!).ToList();
            List<ManageGroupPermission> toAddList = resourceIds.Where(r => !existsList.Any(p => StringTool.EqualsIgnoreCase(p.Resource, r))).Select(r =>
            {
                return new ManageGroupPermission
                {
                    GroupId = groupId,
                    Resource = r,
                    Status = CommonStatus.Enabled,
                    CreateTime = UtcNow,
                    UpdateTime = UtcNow
                };
            }).ToList();
            List<ManageGroupPermission> toDisabledList = existsList.Where(p => !resourceIds.Any(r => StringTool.EqualsIgnoreCase(r, p.Resource))).Select(p =>
            {
                p.Status = CommonStatus.Disabled;
                p.UpdateTime = UtcNow;
                ManageGroupPermission manageGroupPermission = ReflectionTool.Copy<ManageGroupPermissionDto, ManageGroupPermission>(p);
                return manageGroupPermission;
            }).ToList();

            if (toAddList.Any())
            {
                ManageGroupPermissionDao.InsertBatch(toAddList);
            }
            if (toDisabledList.Any())
            {
                ManageGroupPermissionDao.UpdateBatch(toDisabledList);
            }

            // 组权限变更影响组内所有管理员的授权列表，统一失效授权缓存（管理操作低频，全量失效成本可接受）
            RemoveCacheByPrefix(PermissionActionInfo.CACHE_KEY_AUTHORIZED_LIST_PFX);
        }

        /// <summary>根据ID获取管理组 / Get the management group by ID</summary>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <returns>管理组DTO；不存在时为 null / The group DTO; null if not found</returns>
        public ManageGroupDto? GetManageGroupById(int groupId)
        {
            ManageGroup? group = ManageGroupDao.SelectById(groupId);
            if (group != null)
            {
                ManageGroupDto dto = ReflectionTool.Copy<ManageGroup, ManageGroupDto>(group);
                return dto;
            }
            return null;
        }

        /// <summary>获取管理组列表（按ID升序） / Get the management group list (ordered by ID ascending)</summary>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>管理组列表 / The management group list</returns>
        public List<ManageGroupDto> GetManageGroupList(CommonStatus? status = null)
        {
            LambdaWrapper<ManageGroup> wrapper = new LambdaWrapper<ManageGroup>();
            if (status != null)
            {
                wrapper.And(g => g.Status == status);
            }
            wrapper.OrderBy(g => g.Id);
            List<ManageGroupDto> list = ManageGroupDao.SelectList<ManageGroupDto>(wrapper);

            return list;
        }

        /// <summary>从缓存获取管理组列表（缓存时长在 3600~5400 秒间随机，避免集中过期） / Get the management group list from cache (duration randomized between 3600 and 5400 seconds to avoid synchronized expiry)</summary>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>带缓存的管理组列表 / The cached management group list</returns>
        public List<ManageGroupDto> GetManageGroupListFromCache(CommonStatus? status = null)
        {
            return HandleWebCache<List<ManageGroupDto>>(() =>
            {
                return GetManageGroupList(status);
            }, ManageGroupDto.CACHE_KEY_LIST_TPL, MathTool.GenerateInt(3600, 5400), status)!;
        }

    }
}
