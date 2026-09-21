using Rampage.Management.Permission;
using System;

namespace Rampage.Management.VO
{
    /// <summary>
    /// 菜单面包屑节点 / A menu breadcrumb node
    /// </summary>
    public class MenuBreadCrumb
    {

        /// <summary>
        /// 菜单ID / The menu id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 父菜单ID / The parent menu id
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// 菜单标题 / The menu title
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 菜单链接 / The menu url
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// 菜单绑定的权限功能信息 / The permission action bound to the menu
        /// </summary>
        public PermissionActionInfo? ActionInfo { get; set; }

    }
}
