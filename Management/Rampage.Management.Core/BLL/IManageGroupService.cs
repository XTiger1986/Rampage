using Rampage.Enums;
using Rampage.Management.DAL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Model;
using Rampage.Web;
using System;

namespace Rampage.Management.BLL
{
    /// <summary>
    /// 管理组业务逻辑
    /// Management Group business logic
    /// </summary>
    public interface IManageGroupService : ISuperService<ManageGroup, IManageGroupDao>
    {

        /// <summary>
        /// 添加一个管理组
        /// Add a management group
        /// </summary>
        /// <param name="group">管理组数据 / The management group data</param>
        /// <returns>成功时 Data 为带新ID的管理组 / On success Data contains the group with its new ID</returns>
        R AddManageGroup(ManageGroupDto group);

        /// <summary>
        /// 更新管理组
        /// Update a management group
        /// </summary>
        /// <param name="group">管理组数据 / The management group data</param>
        /// <returns>成功或失败（数据不存在时返回 DataNotExists） / Success or failure (DataNotExists when the group does not exist)</returns>
        R UpdateManageGroup(ManageGroupDto group);

        /// <summary>
        /// 修改管理组的状态
        /// Change the status of the management group
        /// </summary>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <param name="status">新状态 / The new status</param>
        /// <returns>修改成功或失败 / Update success or failure</returns>
        R ChangeManageGroupStatus(int groupId, CommonStatus status);

        /// <summary>
        /// 删除管理组（事务内同时删除组内管理员关联与组授权）
        /// Delete a management group (also deletes its admin memberships and group permissions in a transaction)
        /// </summary>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <returns>成功时 Data 为管理组ID / On success Data contains the group ID</returns>
        R DeleteManageGroupById(int groupId);

        /// <summary>
        /// 获取指定管理组的授权列表
        /// Get the authorised list of the specified management group
        /// </summary>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>该管理组的授权记录列表 / The group's permission records</returns>
        List<ManageGroupPermissionDto> GetManageGroupPermissionList(int groupId, CommonStatus? status = null);

        /// <summary>
        /// 更新指定管理组的授权资源
        /// Update the authorised resources of the specified management group
        /// </summary>
        /// <remarks>差量更新：新资源插入、被移除的资源置为禁用；组权限影响组内所有管理员，完成后统一失效全部授权功能缓存。
        /// Delta update: new resources are inserted, removed ones disabled; since group permissions affect all member admins, all authorized-action caches are invalidated afterwards.</remarks>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <param name="resourceIds">资源标识集合 / The resource identifiers</param>
        void UpdateManageGroupPermissionListForGroup(int groupId, List<string> resourceIds);

        /// <summary>
        /// 根据id获取管理组
        /// Get the management group by ID
        /// </summary>
        /// <param name="groupId">管理组ID / Management group ID</param>
        /// <returns>管理组DTO；不存在时为 null / The group DTO; null if not found</returns>
        ManageGroupDto? GetManageGroupById(int groupId);

        /// <summary>
        /// 获取所有管理组列表
        /// Get the list of all management groups
        /// </summary>
        /// <param name="status">状态过滤；null 为全部（按ID升序） / Status filter; null for all (ordered by ID ascending)</param>
        /// <returns>管理组列表 / The management group list</returns>
        List<ManageGroupDto> GetManageGroupList(CommonStatus? status = null);

        /// <summary>
        /// 从缓存获取所有管理组列表
        /// Get the list of all management groups from cache
        /// </summary>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>带缓存的管理组列表 / The cached management group list</returns>
        List<ManageGroupDto> GetManageGroupListFromCache(CommonStatus? status = null);

    }
}
