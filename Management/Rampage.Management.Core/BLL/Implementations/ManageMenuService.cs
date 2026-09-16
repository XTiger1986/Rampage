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
    /// 
    /// </summary>
    public class ManageMenuService : ManagementService<ManageMenu, IManageMenuDao>, IManageMenuService
    {
        [Auto]
        protected IManageMenuDao ManageMenuDao = default!;

        [Auto]
        protected IAdminPermissionService AdminPermissionService = default!;




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

        public R ChangeManageMenuStatus(int menuId, CommonStatus status)
        {
            bool result = ManageMenuDao.UpdateById(menuId, c => c.Status, status);
            if (result)
            {
                RemoveManageMenuCaches();
            }

            return result ? R.Success() : R.Failure();
        }

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

        public void RemoveManageMenuCaches()
        {
            RemoveWebCacheByPrefix(ManageMenuDto.CACHE_KEY_AUTHORIZED_TREE_LIST_PFX);
            RemoveWebCacheByPrefix(ManageMenuDto.CACHE_KEY_LIST_PFX);
        }

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

        public List<ManageMenuDto> GetManageMenuListFromCache(CommonStatus? status = null)
        {
            return HandleWebCache<List<ManageMenuDto>>(() =>
            {
                return GetManageMenuList(status);
            }, ManageMenuDto.CACHE_KEY_LIST_TPL, MathTool.GenerateInt(3600, 5400), status)!;
        }

        public List<ManageMenuDto> GetManageMenuTreeList(CommonStatus? status = null)
        {
            List<ManageMenuDto> menuList = GetManageMenuList(status);
            List<ManageMenuDto> rootList = menuList.FindAll(c => c.ParentId == 0);
            RebuildManageMenuList(rootList, menuList);
            return rootList;
        }

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

        public List<ManageMenuDto> GetAuthorizedManageMenuTreeListOfAdminFromCache(int adminId)
        {
            return HandleWebCache<List<ManageMenuDto>>(() =>
            {
                return GetAuthorizedManageMenuTreeListOfAdmin(adminId);
            }, ManageMenuDto.CACHE_KEY_AUTHORIZED_TREE_LIST_TPL, MathTool.GenerateInt(5400, 7200), adminId)!;
        }

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
