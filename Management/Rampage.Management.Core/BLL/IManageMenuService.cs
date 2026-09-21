using Rampage.Enums;
using Rampage.Management.DAL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Model;
using Rampage.Web;
using System;

namespace Rampage.Management.BLL
{
    /// <summary>后台菜单业务逻辑（菜单树构建与按授权过滤） / Management menu business logic (menu tree building and authorization filtering)</summary>
    public interface IManageMenuService : ISuperService<ManageMenu, IManageMenuDao>
    {

        /// <summary>
        /// 添加一个后台菜单
        /// Add a management menu
        /// </summary>
        /// <param name="menu">菜单数据 / The menu data</param>
        /// <returns>成功时 Data 为带新ID的菜单 / On success Data contains the menu with its new ID</returns>
        R AddManageMenu(ManageMenuDto menu);

        /// <summary>
        /// 更新一个后台菜单
        /// Update a management menu
        /// </summary>
        /// <param name="menu">菜单数据 / The menu data</param>
        /// <returns>成功或失败（数据不存在时返回 DataNotExists） / Success or failure (DataNotExists when the menu does not exist)</returns>
        R UpdateManageMenu(ManageMenuDto menu);

        /// <summary>
        /// 修改后台菜单的状态
        /// change status of a management menu by id
        /// </summary>
        /// <param name="menuId">菜单ID / Menu ID</param>
        /// <param name="status">新状态 / The new status</param>
        /// <returns>修改成功或失败 / Update success or failure</returns>
        R ChangeManageMenuStatus(int menuId, CommonStatus status);

        /// <summary>
        /// 删除后台菜单
        /// delete a management menu
        /// </summary>
        /// <param name="menuId">菜单ID / Menu ID</param>
        /// <returns>删除成功或失败 / Deletion success or failure</returns>
        R DeleteManageMenuById(int menuId);

        /// <summary>
        /// 根据ID获取一个后台菜单
        /// Get a management menu by ID
        /// </summary>
        /// <param name="menuId">菜单ID / Menu ID</param>
        /// <returns>菜单DTO（含关联的功能信息）；不存在时为 null / The menu DTO (with the associated action info); null if not found</returns>
        ManageMenuDto? GetManageMenuById(int menuId);

        /// <summary>
        /// 从菜单数据树列表中获取指定ID的后台菜单
        /// Get the management menu with the specified ID from the menu data tree list
        /// </summary>
        /// <param name="treeList">菜单树列表 / The menu tree list</param>
        /// <param name="menuId">菜单ID / Menu ID</param>
        /// <returns>树中匹配的菜单；未找到时为 null / The matching menu in the tree; null if not found</returns>
        ManageMenuDto? GetManageMenuInTree(List<ManageMenuDto>? treeList, int menuId);

        /// <summary>
        /// 获取后台菜单列表
        /// Get management menu list
        /// </summary>
        /// <param name="status">状态过滤；null 为全部（按排序值降序） / Status filter; null for all (ordered by sort value descending)</param>
        /// <returns>后台菜单平铺列表（含关联的功能信息） / The flat management menu list (with associated action info)</returns>
        List<ManageMenuDto> GetManageMenuList(CommonStatus? status = null);

        /// <summary>
        /// 从缓存获取后台菜单列表
        /// Get management menu list from cache
        /// </summary>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>带缓存的菜单平铺列表 / The cached flat menu list</returns>
        List<ManageMenuDto> GetManageMenuListFromCache(CommonStatus? status = null);

        /// <summary>
        /// 获取后台菜单数据树列表
        /// Get management menu data tree list
        /// </summary>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>按 ParentId 组装的菜单树（根节点列表） / The menu tree assembled via ParentId (root nodes)</returns>
        List<ManageMenuDto> GetManageMenuTreeList(CommonStatus? status = null);

        /// <summary>
        /// 获取指定管理员已被授权的后台菜单数据树列表
        /// Get the list of management menu data tree authorised for the specified administrator
        /// </summary>
        /// <remarks>仅保留启用状态的菜单；绑定了 ActionId 的菜单须其功能在管理员的授权列表中才保留，未绑定 ActionId 的菜单始终保留。父菜单被过滤时其子菜单也不会出现在树中。
        /// Only enabled menus are kept; menus bound to an ActionId are kept only if that action is in the admin's authorized list, while menus without an ActionId are always kept. If a parent is filtered out its children disappear from the tree too.</remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>授权菜单树的根节点列表（超级管理员为完整菜单树） / The root nodes of the authorized menu tree (the full tree for master admins)</returns>
        List<ManageMenuDto> GetAuthorizedManageMenuTreeListOfAdmin(int adminId);

        /// <summary>
        /// 从缓存中获取指定管理员已被授权的后台菜单数据树列表
        /// Get the list of management menu data tree authorised for the specified administrator from cache
        /// </summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>带缓存的授权菜单树根节点列表 / The cached root nodes of the authorized menu tree</returns>
        List<ManageMenuDto> GetAuthorizedManageMenuTreeListOfAdminFromCache(int adminId);

    }
}
