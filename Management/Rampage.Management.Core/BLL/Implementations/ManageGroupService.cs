using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Log;
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
    public class ManageGroupService : ManagementService<ManageGroup, IManageGroupDao>, IManageGroupService
    {

        [Auto]
        protected IAdminInGroupDao AdminInGroupDao = default!;


        private void RemoveManageGroupCache()
        {
            RemoveWebCacheByPrefix(ManageGroupDto.CACHE_KEY_LIST_PFX);
        }

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

        public R ChangeManageGroupStatus(int groupId, CommonStatus status)
        {
            bool result = ManageGroupDao.UpdateById(groupId, c => c.Status, status);
            if (result)
            {
                RemoveManageGroupCache();
            }

            return result ? R.Success() : R.Failure();
        }

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

        public List<ManageGroupPermissionDto> GetManageGroupPermissionList(int groupId, CommonStatus? status = null)
        {
            LambdaWrapper<ManageGroupPermission> wrapper = new LambdaWrapper<ManageGroupPermission>(d => d.GroupId == groupId);
            if (status != null)
            {
                wrapper.And(p => p.Status == status.Value);
            }
            return ManageGroupPermissionDao.SelectList<ManageGroupPermissionDto>(wrapper);
        }

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

        public List<ManageGroupDto> GetManageGroupListFromCache(CommonStatus? status = null)
        {
            return HandleWebCache<List<ManageGroupDto>>(() =>
            {
                return GetManageGroupList(status);
            }, ManageGroupDto.CACHE_KEY_LIST_TPL, MathTool.GenerateInt(3600, 5400), status)!;
        }

    }
}
