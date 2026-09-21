using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>后台菜单（映射 manage_menu 表） / Management menu (mapped to manage_menu)</summary>
    /// <remarks>通过 ParentId 构成菜单树（0 为根节点）；ActionId 关联权限系统中的功能（PermissionActionInfo），用于按授权过滤菜单。/ Forms a tree via ParentId (0 = root); ActionId links to a permission action (PermissionActionInfo) used for authorization-based menu filtering.</remarks>
    [TableMapping("manage_menu")]
    public class ManageMenu
    {
        
        /// <summary>主键ID（自增） / Primary key ID (auto increment)</summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>标题 / Title</summary>
        [FieldMapping("title")]
        public string? Title { get; set; }

        /// <summary>自定义样式名（CSS 类名） / Custom CSS class name</summary>
        [FieldMapping("css")]
        public string? Css { get; set; }

        /// <summary>自定义样式（内联 style） / Custom inline style</summary>
        [FieldMapping("style")]
        public string? Style { get; set; }

        /// <summary>跳转地址 / Navigation URL</summary>
        [FieldMapping("url")]
        public string? Url { get; set; }

        /// <summary>排序值（值越大越靠前） / Sort order (larger = higher priority)</summary>
        [FieldMapping("sort_order")]
        public int SortOrder { get; set; }

        /// <summary>上一级菜单ID（0 为根节点） / Parent menu ID (0 = root)</summary>
        [FieldMapping("parent_id")]
        public int ParentId { get; set; }

        /// <summary>对应权限功能的ID（控制器完整类型名.方法名） / Associated permission action ID (controller full type name + method name)</summary>
        [FieldMapping("action_id")]
        public string? ActionId { get; set; }

        /// <summary>状态（启用/禁用） / Status (enabled/disabled)</summary>
        [FieldMapping("status")]
        public CommonStatus Status { get; set; }

        /// <summary>是否系统内置 / Whether system built-in</summary>
        [FieldMapping("is_sys")]
        public bool IsSys { get; set; }

        /// <summary>创建时间(UTC) / Created time (UTC)</summary>
        [FieldMapping("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC) / Last updated time (UTC)</summary>
        [FieldMapping("update_time")]
        public DateTime UpdateTime { get; set; }



    }
}
