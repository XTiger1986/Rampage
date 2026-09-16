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
        /// <param name="group"></param>
        /// <returns></returns>
        R AddManageGroup(ManageGroupDto group);

        /// <summary>
        /// 更新管理组
        /// Update a management group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        R UpdateManageGroup(ManageGroupDto group);

        /// <summary>
        /// 修改管理组的状态
        /// Change the status of the management group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        R ChangeManageGroupStatus(int groupId, CommonStatus status);

        /// <summary>
        /// 删除管理组
        /// Delete a management group
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        R DeleteManageGroupById(int groupId);

        /// <summary>
        /// 获取指定管理组的授权列表
        /// Get the authorised list of the specified management group
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        List<ManageGroupPermissionDto> GetManageGroupPermissionList(int groupId, CommonStatus? status = null);

        /// <summary>
        /// 更新指定管理组的授权资源
        /// Update the authorised resources of the specified management group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="resourceIds"></param>
        void UpdateManageGroupPermissionListForGroup(int groupId, List<string> resourceIds);

        /// <summary>
        /// 根据id获取管理组
        /// Get the management group by ID
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        ManageGroupDto? GetManageGroupById(int groupId);

        /// <summary>
        /// 获取所有管理组列表
        /// Get the list of all management groups
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        List<ManageGroupDto> GetManageGroupList(CommonStatus? status = null);

        /// <summary>
        /// 从缓存获取所有管理组列表
        /// Get the list of all management groups from cache
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        List<ManageGroupDto> GetManageGroupListFromCache(CommonStatus? status = null);

    }
}
