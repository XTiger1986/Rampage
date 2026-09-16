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
    /// 
    /// </summary>
    public interface IManageMenuService : ISuperService<ManageMenu, IManageMenuDao>
    {

        /// <summary>
        /// 添加一个后台菜单
        /// Add a management menu
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        R AddManageMenu(ManageMenuDto menu);

        /// <summary>
        /// 更新一个后台菜单
        /// Update a management menu
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        R UpdateManageMenu(ManageMenuDto menu);

        /// <summary>
        /// 修改后台菜单的状态
        /// change status of a management menu by id
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        R ChangeManageMenuStatus(int menuId, CommonStatus status);

        /// <summary>
        /// 删除后台菜单
        /// delete a management menu
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        R DeleteManageMenuById(int menuId);

        /// <summary>
        /// 根据ID获取一个后台菜单
        /// Get a management menu by ID
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        ManageMenuDto? GetManageMenuById(int menuId);

        /// <summary>
        /// 从菜单数据树列表中获取指定ID的后台菜单
        /// Get the management menu with the specified ID from the menu data tree list
        /// </summary>
        /// <param name="treeList"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        ManageMenuDto? GetManageMenuInTree(List<ManageMenuDto>? treeList, int menuId);

        /// <summary>
        /// 获取后台菜单列表
        /// Get management menu list
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        List<ManageMenuDto> GetManageMenuList(CommonStatus? status = null);

        /// <summary>
        /// 从缓存获取后台菜单列表
        /// Get management menu list from cache
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        List<ManageMenuDto> GetManageMenuListFromCache(CommonStatus? status = null);

        /// <summary>
        /// 获取后台菜单数据树列表
        /// Get management menu data tree list
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        List<ManageMenuDto> GetManageMenuTreeList(CommonStatus? status = null);

        /// <summary>
        /// 获取指定管理员已被授权的后台菜单数据树列表
        /// Get the list of management menu data tree authorised for the specified administrator
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        List<ManageMenuDto> GetAuthorizedManageMenuTreeListOfAdmin(int adminId);

        /// <summary>
        /// 从缓存中获取指定管理员已被授权的后台菜单数据树列表
        /// Get the list of management menu data tree authorised for the specified administrator from cache
        /// </summary>
        /// <param name="adminId"></param>
        /// <returns></returns>
        List<ManageMenuDto> GetAuthorizedManageMenuTreeListOfAdminFromCache(int adminId);

    }
}
