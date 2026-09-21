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
        /// <remarks>
        /// 鉴权流程：管理员不存在→未授权；被禁用→账号禁用；Action 未注册进权限系统→按 UnregisteredActionPolicy 处理（默认拒绝，超级管理员放行）；
        /// 超级管理员→放行；否则与该管理员的授权功能缓存列表（区域/控制器/视图忽略大小写）比对。
        /// Pipeline: missing admin -> Unauthorized; disabled -> DisabledAccount; unregistered action -> UnregisteredActionPolicy (deny by default, masters pass);
        /// master admin -> Success; otherwise matched against the admin's cached authorized action list (case-insensitive area/controller/action).
        /// </remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="path">请求路径（自动去除查询串） / The request path (query string stripped)</param>
        /// <param name="controllerName">控制器名 / Controller name</param>
        /// <param name="actionName">视图(Action)名 / Action name</param>
        /// <param name="areaName">区域名；可为 null / Area name; may be null</param>
        /// <returns>鉴权结果码 / The authorization result code</returns>
        ResultCode IdentifyAuthorization(int adminId, string path, string controllerName, string actionName, string? areaName = null);

        /// <summary>
        /// 获取指定管理员的授权功能列表
        /// Obtain the list of authorised functions for the specified administrator
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>授权功能列表；管理员不存在或被禁用时为空列表 / The authorized action list; an empty list if the admin is missing or disabled</returns>
        List<PermissionActionInfo> GetAuthorizedActionListOfAdmin(int adminId);

        /// <summary>
        /// 从缓存获取指定管理员的授权功能列表
        /// Obtain the list of authorised functions for the specified administrator from cache
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>带缓存的授权功能列表；管理员不存在或被禁用时为空列表 / The cached authorized action list; an empty list if the admin is missing or disabled</returns>
        List<PermissionActionInfo> GetAuthorizedActionListOfAdminFromCache(int adminId);

        /// <summary>
        /// 获取指定管理员的授权功能列表（组权限与个人权限的并集）
        /// Obtain the list of authorised functions for the specified administrator (union of group and personal permissions)
        /// </summary>
        /// <param name="admin">管理员DTO / The administrator DTO</param>
        /// <returns>授权功能列表；被禁用时为空列表，超级管理员为全部功能 / The authorized action list; empty when disabled, all actions for master admins</returns>
        List<PermissionActionInfo> GetAuthorizedActionListOfAdmin(AdminDto admin);

        /// <summary>
        /// 从缓存获取指定管理员的授权功能列表
        /// Obtain the list of authorised functions for the specified administrator from cache
        /// </summary>
        /// <param name="admin">管理员DTO / The administrator DTO</param>
        /// <returns>带缓存的授权功能列表 / The cached authorized action list</returns>
        List<PermissionActionInfo> GetAuthorizedActionListOfAdminFromCache(AdminDto admin);

        /// <summary>
        /// 获取指定管理员的授权列表
        /// Obtain the authorised list of the specified administrator
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>该管理员的授权记录列表 / The admin's permission records</returns>
        List<AdminPermissionDto> GetAdminPermissionList(int adminId, CommonStatus? status = null);

        /// <summary>
        /// 更新指定管理员的授权资源
        /// Update the authorised resources for the specified administrator
        /// </summary>
        /// <remarks>差量更新：新资源插入、被移除的资源置为禁用；完成后立即失效该管理员的授权功能缓存。
        /// Delta update: new resources are inserted, removed ones disabled; the admin's authorized-action cache is invalidated immediately afterwards.</remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <param name="resourceIds">资源标识集合 / The resource identifiers</param>
        void UpdateAdminPermissionListForAdmin(int adminId, List<string> resourceIds);

    }
}
