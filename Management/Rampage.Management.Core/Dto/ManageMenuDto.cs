using Rampage.Enums;
using Rampage.Management.Permission;
using Rampage.Utilities;
using Rampage.Validation;
using System;
using System.Text.Json.Serialization;

namespace Rampage.Management.Dto
{
    /// <summary>后台菜单数据传输对象 / Management menu data transfer object</summary>
    public class ManageMenuDto
    {
        /// <summary>菜单列表缓存的键前缀 / Cache key prefix for the menu list</summary>
        public static readonly string CACHE_KEY_LIST_PFX = "ManageMenu_List";
        /// <summary>菜单列表缓存的键模板（{0}=状态） / Cache key template for the menu list ({0} = status)</summary>
        public static readonly string CACHE_KEY_LIST_TPL = CACHE_KEY_LIST_PFX + "_{0}";
        /// <summary>菜单树缓存的键 / Cache key for the menu tree</summary>
        public static readonly string CACHE_KEY_TREE_LIST = "ManageMenu_TreeList";
        /// <summary>授权菜单树缓存的键前缀 / Cache key prefix for the authorized menu trees</summary>
        public static readonly string CACHE_KEY_AUTHORIZED_TREE_LIST_PFX = "Authorized_ManageMenu_TreeList";
        /// <summary>授权菜单树缓存的键模板（{0}=管理员ID） / Cache key template for the authorized menu trees ({0} = admin ID)</summary>
        public static readonly string CACHE_KEY_AUTHORIZED_TREE_LIST_TPL = CACHE_KEY_AUTHORIZED_TREE_LIST_PFX + "_{0}";

        /// <summary>主键ID / Primary key ID</summary>
        public int Id { get; set; }

        /// <summary>标题 / Title</summary>
        [NotWhiteSpaceString(MessageTemplate = "菜单标题不能为空")]
        public string? Title { get; set; }

        /// <summary>自定义样式名（CSS 类名） / Custom CSS class name</summary>
        [StringLength(Max = 50, MessageTemplate = "菜单样式名不能超过{1}个字符")]
        public string? Css { get; set; }

        /// <summary>自定义样式（内联 style） / Custom inline style</summary>
        [StringLength(Max = 100, MessageTemplate = "菜单自定义样式不能超过{1}个字符")]
        public string? Style { get; set; }

        /// <summary>跳转地址 / Navigation URL</summary>
        [StringLength(Max = 200, MessageTemplate = "菜单跳转地址不能超过{1}个字符")]
        public string? Url { get; set; }

        /// <summary>排序值（值越大越靠前） / Sort order (larger = higher priority)</summary>
        public int SortOrder { get; set; }

        /// <summary>上一级菜单ID（0 为根节点） / Parent menu ID (0 = root)</summary>
        public int ParentId { get; set; }

        /// <summary>对应权限功能的ID（控制器完整类型名.方法名） / Associated permission action ID (controller full type name + method name)</summary>
        public string? ActionId { get; set; }

        /// <summary>状态（启用/禁用） / Status (enabled/disabled)</summary>
        public CommonStatus Status { get; set; }

        /// <summary>创建时间(UTC) / Created time (UTC)</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC) / Last updated time (UTC)</summary>
        public DateTime UpdateTime { get; set; }




        /// <summary>子菜单列表（树形结构） / Child menus (tree structure)</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ManageMenuDto>? MenuList { get; set; }

        /// <summary>关联的权限功能信息（按 ActionId 匹配） / The associated permission action info (matched by ActionId)</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PermissionActionInfo? ActionInfo { get; set; }

        /// <summary>在菜单树中的深度（根为 0） / Depth in the menu tree (root = 0)</summary>
        public int Depth
        {
            get;
            set;
        }


    }
}
