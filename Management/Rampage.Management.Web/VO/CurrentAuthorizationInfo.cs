using Microsoft.AspNetCore.Mvc.Controllers;
using Rampage.Localization;
using Rampage.Management.Dto;
using Rampage.Management.Permission;
using System;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 当前请求的授权信息聚合视图模型：由 SuperManagementController 装配并暴露给视图
    /// Aggregated authorization view model for the current request: assembled by SuperManagementController and exposed to views
    /// </summary>
    public class CurrentAuthorizationInfo
    {

        /// <summary>
        /// 当前登录的管理员 / The current logged-in admin
        /// </summary>
        public AdminDto? CurrentAdmin { get; set; }

        /// <summary>
        /// 已授权的功能（Action）列表 / The authorized action list
        /// </summary>
        public List<PermissionActionInfo>? AuthorizedActionList { get; set; }

        /// <summary>
        /// 已授权的菜单（树形）列表 / The authorized menu tree
        /// </summary>
        public List<ManageMenuDto>? AuthorizedMenuList { get; set; }

        /// <summary>
        /// 当前 Action 描述符 / The current action descriptor
        /// </summary>
        public ControllerActionDescriptor? ActionDescriptor { get; set; }

        /// <summary>
        /// 当前请求命中的管理菜单 / The management menu matching the current request
        /// </summary>
        public ManageMenuDto? CurrentMenu { get; set; }

        /// <summary>
        /// 菜单面包屑（顶级在前）/ The menu breadcrumbs (root first)
        /// </summary>
        public List<MenuBreadCrumb>? MenuBreadCrumbList { get; set; } 

        /// <summary>
        /// 当前请求命中的功能 / The permission action of the current request
        /// </summary>
        public PermissionActionInfo? CurrentAction { get; set; }

        /// <summary>
        /// 全局语言模块 / The global language module
        /// </summary>
        public LocalizationModule? GlobalLanguage
        {
            get;
            set;
        }

        /// <summary>
        /// 权限文案语言模块 / The language module for permission texts
        /// </summary>
        public LocalizationModule? PermissionLanguage
        {
            get;
            set;
        }

    }
}
