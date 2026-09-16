using Rampage.Enums;
using Rampage.Management.DAL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.Web;
using System;

namespace Rampage.Management.BLL
{
    /// <summary>
    /// 管理员权限业务逻辑
    /// Administrator permissions business logic
    /// </summary>
    public interface IAdminPermissionService : ISuperService<AdminPermission, IAdminPermissionDao>
    {
        /// <summary>
        /// 识别指定管理员对指定路径的授权
        /// Identify the authorisation of the specified administrator for the specified path
        /// </summary>
        /// <param name="adminId"></param>
        /// <param name="path"></param>
        /// <param name="controllerName"></param>
        /// <param name="actionName"></param>
        /// <param name="areaName"></param>
        /// <returns></returns>
        ResultCode IdentifyAuthorization(int adminId, string path, string controllerName, string actionName, string? areaName = null);

        /// <summary>
        /// 获取指定管理员的授权功能列表
        /// Obtain the list of authorised functions for the specified administrator
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        List<PermissionActionInfo> GetAuthorizedActionListOfAdmin(int adminId);

        /// <summary>
        /// 从缓存获取指定管理员的授权功能列表
        /// Obtain the list of authorised functions for the specified administrator from cache
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        List<PermissionActionInfo> GetAuthorizedActionListOfAdminFromCache(int adminId);

        /// <summary>
        /// 获取指定管理员的授权功能列表
        /// Obtain the list of authorised functions for the specified administrator
        /// </summary>
        /// <param name="admin"></param>
        /// <returns></returns>
        List<PermissionActionInfo> GetAuthorizedActionListOfAdmin(AdminDto admin);

        /// <summary>
        /// 从缓存获取指定管理员的授权功能列表
        /// Obtain the list of authorised functions for the specified administrator from cache
        /// </summary>
        /// <param name="admin"></param>
        /// <returns></returns>
        List<PermissionActionInfo> GetAuthorizedActionListOfAdminFromCache(AdminDto admin);

        /// <summary>
        /// 获取指定管理员的授权列表
        /// Obtain the authorised list of the specified administrator
        /// </summary>
        /// <param name="adminId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        List<AdminPermissionDto> GetAdminPermissionList(int adminId, CommonStatus? status = null);

        /// <summary>
        /// 更新指定管理员的授权资源
        /// Update the authorised resources for the specified administrator
        /// </summary>
        /// <param name="adminId"></param>
        /// <param name="resourceIds"></param>
        void UpdateAdminPermissionListForAdmin(int adminId, List<string> resourceIds);

    }
}
