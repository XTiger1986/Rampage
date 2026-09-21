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
    /// 后台菜单业务逻辑实现（菜单树构建与按授权过滤）
    /// Management menu business logic implementation (menu tree building and authorization filtering)
    /// </summary>
    public class ManageMenuService : ManagementService<ManageMenu, IManageMenuDao>, IManageMenuService
    {
        /// <summary>后台菜单数据访问 / Management menu data access</summary>
        [Auto]
        protected IManageMenuDao ManageMenuDao = default!;

        /// <summary>管理员权限业务逻辑（用于授权菜单过滤） / The admin permission service (used for authorized menu filtering)</summary>
        [Auto]
        protected IAdminPermissionService AdminPermissionService = default!;




        /// <summary>未填 Url 但绑定了 ActionId 时，由权限功能信息自动生成跳转地址 / When Url is empty but ActionId is set, derive the navigation URL from the permission action info</summary>
        /// <param name="menu">菜单数据 / The menu data</param>
        private void HandleManageMenu(ManageMenuDto menu)
        {
            if (!string.IsNullOrEmpty(menu.ActionId) && string.IsNullOrEmpty(menu.Url))
            {
                List<PermissionActionInfo> actionList = PermissionLoader.ActionList;
                PermissionActionInfo? actionInfo = actionList.Find(a => StringTool.EqualsIgnoreCase(a.ActionId, menu.ActionId));
                if (actionInfo != null)
                {
                    string url = string.Empty;
                    if (!string.IsNullOrEmpty(actionInfo.AreaName))
                    {
                        url += $"/{actionInfo.AreaName}";
                    }
                    url += $"/{actionInfo.ControllerName}/{actionInfo.ActionName}";
                    menu.Url = url;
                }
            }
        }

        /// <summary>添加一个后台菜单 / Add a management menu</summary>
        /// <param name="menu">菜单数据 / The menu data</param>
        /// <returns>成功时 Data 为带新ID的菜单 / On success Data contains the menu with its new ID</returns>
        public R AddManageMenu(ManageMenuDto menu)
        {
            HandleManageMenu(menu);
            if (menu.CreateTime <= DateTime.MinValue)
            {
                menu.CreateTime = UtcNow;
            }
            ManageMenu entity = ReflectionTool.Copy<ManageMenuDto, ManageMenu>(menu);

            bool result = ManageMenuDao.Insert(entity);
            if (result)
            {
                menu.Id = entity.Id;
                RemoveManageMenuCaches();
                return R.Success(menu);
            }
            return R.Failure();
        }

        /// <summary>更新一个后台菜单 / Update a management menu</summary>
        /// <param name="menu">菜单数据 / The menu data</param>
        /// <returns>成功或失败（数据不存在时返回 DataNotExists） / Success or failure (DataNotExists when the menu does not exist)</returns>
        public R UpdateManageMenu(ManageMenuDto menu)
        {
            ManageMenu? entity = SelectById(menu.Id);
            if (entity == null)
            {
                return R.Create(ResultCode.DataNotExists);
            }
            HandleManageMenu(menu);
            menu.UpdateTime = UtcNow;
            ReflectionTool.CopyProperties(menu, entity, true);
            bool result = ManageMenuDao.UpdateById(entity);
            if (result)
            {
                RemoveManageMenuCaches();
                return R.Success(menu);
            }
            return R.Failure();
        }

        /// <summary>修改后台菜单的状态 / Change the status of a management menu</summary>
        /// <param name="menuId">菜单ID / Menu ID</param>
        /// <param name="status">新状态 / The new status</param>
        /// <returns>修改成功或失败 / Update success or failure</returns>
        public R ChangeManageMenuStatus(int menuId, CommonStatus status)
        {
            bool result = ManageMenuDao.UpdateById(menuId, c => c.Status, status);
            if (result)
            {
                RemoveManageMenuCaches();
            }

            return result ? R.Success() : R.Failure();
        }

        /// <summary>删除后台菜单（仅删除菜单本身，不级联子菜单） / Delete a management menu (the menu itself only, children are not cascaded)</summary>
        /// <param name="menuId">菜单ID / Menu ID</param>
        /// <returns>删除成功或失败 / Deletion success or failure</returns>
        public R DeleteManageMenuById(int menuId)
        {
            bool result = ManageMenuDao.DeleteById(menuId);
            if (result)
            {
                RemoveManageMenuCaches();
                return R.Success();
            }
            return R.Failure();
        }

        /// <summary>移除后台菜单相关缓存（平铺列表与各管理员的授权菜单树） / Remove the management menu caches (the flat list and each admin's authorized menu tree)</summary>
        public void RemoveManageMenuCaches()
        {
            RemoveWebCacheByPrefix(ManageMenuDto.CACHE_KEY_AUTHORIZED_TREE_LIST_PFX);
            RemoveWebCacheByPrefix(ManageMenuDto.CACHE_KEY_LIST_PFX);
        }

        /// <summary>根据ID获取一个后台菜单（含关联的功能信息） / Get a management menu by ID (with the associated action info)</summary>
        /// <param name="menuId">菜单ID / Menu ID</param>
        /// <returns>菜单DTO；不存在时为 null / The menu DTO; null if not found</returns>
        public ManageMenuDto? GetManageMenuById(int menuId)
        {
            List<PermissionActionInfo> actionList = PermissionLoader.ActionList;
            ManageMenu? menu = ManageMenuDao.SelectById(menuId);
            if (menu != null)
            {
                ManageMenuDto dto = ReflectionTool.Copy<ManageMenu, ManageMenuDto>(menu);
                dto.ActionInfo = actionList.Find(a => StringTool.EqualsIgnoreCase(a.ActionId, menu.ActionId));
                return dto;
            }
            return null;
        }

        /// <summary>从菜单数据树列表中（递归）获取指定ID的后台菜单 / Recursively get the management menu with the specified ID from the menu tree list</summary>
        /// <param name="treeList">菜单树列表 / The menu tree list</param>
        /// <param name="menuId">菜单ID / Menu ID</param>
        /// <returns>树中匹配的菜单；未找到时为 null / The matching menu in the tree; null if not found</returns>
        public ManageMenuDto? GetManageMenuInTree(List<ManageMenuDto>? treeList, int menuId)
        {
            if (treeList == null || !treeList.Any())
            {
                return null;
            }
            ManageMenuDto? menu = treeList.Find(c => c.Id == menuId);
            if (menu == null)
            {
                foreach (ManageMenuDto item in treeList)
                {
                    menu = GetManageMenuInTree(item.MenuList, menuId);
                    if (menu != null)
                    {
                        return menu;
                    }
                }
            }
            return menu;
        }

        /// <summary>获取后台菜单列表（按排序值降序，含关联的功能信息） / Get the management menu list (ordered by sort value descending, with associated action info)</summary>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>后台菜单平铺列表 / The flat management menu list</returns>
        public List<ManageMenuDto> GetManageMenuList(CommonStatus? status = null)
        {
            List<PermissionActionInfo> actionList = PermissionLoader.ActionList;
            LambdaWrapper<ManageMenu> wrapper = new LambdaWrapper<ManageMenu>();
            if (status != null)
            {
                wrapper.And(m => m.Status == status.Value);
            }
            wrapper.OrderByDescending(c => c.SortOrder);
            List<ManageMenuDto> list = ManageMenuDao.SelectList<ManageMenuDto>(wrapper);
            foreach (ManageMenuDto menu in list)
            {
                if (!string.IsNullOrEmpty(menu.ActionId))
                {
                    menu.ActionInfo = actionList.Find(a => StringTool.EqualsIgnoreCase(a.ActionId, menu.ActionId));
                }
            }
            return list;
        }

        /// <summary>从缓存获取后台菜单列表（缓存时长在 3600~5400 秒间随机，避免集中过期） / Get the management menu list from cache (duration randomized between 3600 and 5400 seconds to avoid synchronized expiry)</summary>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>带缓存的菜单平铺列表 / The cached flat menu list</returns>
        public List<ManageMenuDto> GetManageMenuListFromCache(CommonStatus? status = null)
        {
            return HandleWebCache<List<ManageMenuDto>>(() =>
            {
                return GetManageMenuList(status);
            }, ManageMenuDto.CACHE_KEY_LIST_TPL, MathTool.GenerateInt(3600, 5400), status)!;
        }

        /// <summary>获取后台菜单数据树列表（ParentId=0 为根，递归挂接子菜单并记录深度） / Get the management menu data tree list (ParentId=0 roots; children attached recursively with depth recorded)</summary>
        /// <param name="status">状态过滤；null 为全部 / Status filter; null for all</param>
        /// <returns>菜单树根节点列表 / The root nodes of the menu tree</returns>
        public List<ManageMenuDto> GetManageMenuTreeList(CommonStatus? status = null)
        {
            List<ManageMenuDto> menuList = GetManageMenuList(status);
            List<ManageMenuDto> rootList = menuList.FindAll(c => c.ParentId == 0);
            RebuildManageMenuList(rootList, menuList);
            return rootList;
        }

        /// <summary>
        /// 获取指定管理员已被授权的后台菜单数据树列表
        /// Get the list of management menu data tree authorised for the specified administrator
        /// </summary>
        /// <remarks>仅保留启用状态的菜单；绑定了 ActionId 的菜单须其功能在管理员的授权列表中才保留，未绑定 ActionId 的菜单始终保留。父菜单被过滤时其子菜单也不会出现在树中。
        /// Only enabled menus are kept; menus bound to an ActionId are kept only if that action is in the admin's authorized list, while menus without an ActionId are always kept. If a parent is filtered out its children disappear from the tree too.</remarks>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>授权菜单树的根节点列表（超级管理员为完整菜单树） / The root nodes of the authorized menu tree (the full tree for master admins)</returns>
        public List<ManageMenuDto> GetAuthorizedManageMenuTreeListOfAdmin(int adminId)
        {
            List<PermissionActionInfo> actionList = PermissionLoader.ActionList;
            List<PermissionActionInfo> authorizedActionList = AdminPermissionService.GetAuthorizedActionListOfAdminFromCache(adminId);
            List<ManageMenuDto> menuList = GetManageMenuList();
            List<ManageMenuDto> authorizedMenuList = menuList.FindAll(m =>
            {
                if (m.Status != CommonStatus.Enabled)
                {
                    return false;
                }
                if (actionList.Any(a => StringTool.EqualsIgnoreCase(a.ActionId, m.ActionId)))
                {
                    return authorizedActionList.Any(a => StringTool.EqualsIgnoreCase(a.ActionId, m.ActionId));
                }
                return true;
            });
            List<ManageMenuDto> rootList = authorizedMenuList.FindAll(c => c.ParentId == 0);
            RebuildManageMenuList(rootList, authorizedMenuList);
            return rootList;
        }

        /// <summary>从缓存中获取指定管理员已被授权的后台菜单数据树列表（缓存时长在 5400~7200 秒间随机） / Get the list of management menu data tree authorised for the specified administrator from cache (duration randomized between 5400 and 7200 seconds)</summary>
        /// <param name="adminId">管理员ID / Administrator ID</param>
        /// <returns>带缓存的授权菜单树根节点列表 / The cached root nodes of the authorized menu tree</returns>
        public List<ManageMenuDto> GetAuthorizedManageMenuTreeListOfAdminFromCache(int adminId)
        {
            return HandleWebCache<List<ManageMenuDto>>(() =>
            {
                return GetAuthorizedManageMenuTreeListOfAdmin(adminId);
            }, ManageMenuDto.CACHE_KEY_AUTHORIZED_TREE_LIST_TPL, MathTool.GenerateInt(5400, 7200), adminId)!;
        }

        /// <summary>递归重建菜单树：挂接直接子菜单并写入当前深度 / Recursively rebuild the menu tree: attaches direct children and records the current depth</summary>
        /// <param name="list">当前层菜单列表 / The menu list at the current level</param>
        /// <param name="allList">全量菜单平铺列表 / The flat list of all menus</param>
        /// <param name="depth">当前深度（根为 0） / The current depth (root = 0)</param>
        private void RebuildManageMenuList(List<ManageMenuDto>? list, List<ManageMenuDto> allList, int depth = 0)
        {
            if (list == null || !list.Any())
            {
                return;
            }
            foreach (ManageMenuDto menu in list)
            {
                menu.MenuList = allList.FindAll(c => c.ParentId == menu.Id);
                menu.Depth = depth;

                RebuildManageMenuList(menu.MenuList, allList, depth + 1);
            }
        }


    }
}
