using Rampage.Enums;
using Rampage.Management.Permission;
using Rampage.Utilities;
using Rampage.Validation;
using System;
using System.Text.Json.Serialization;

namespace Rampage.Management.Dto
{
    public class ManageMenuDto
    {
        public static readonly string CACHE_KEY_LIST_PFX = "ManageMenu_List";
        public static readonly string CACHE_KEY_LIST_TPL = CACHE_KEY_LIST_PFX + "_{0}";
        public static readonly string CACHE_KEY_TREE_LIST = "ManageMenu_TreeList";
        public static readonly string CACHE_KEY_AUTHORIZED_TREE_LIST_PFX = "Authorized_ManageMenu_TreeList";
        public static readonly string CACHE_KEY_AUTHORIZED_TREE_LIST_TPL = CACHE_KEY_AUTHORIZED_TREE_LIST_PFX + "_{0}";

        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        [NotWhiteSpaceString(MessageTemplate = "菜单标题不能为空")]
        public string? Title { get; set; }

        /// <summary>
        /// 自定义样式名
        /// </summary>
        [StringLength(Max = 50, MessageTemplate = "菜单样式名不能超过{1}个字符")]
        public string? Css { get; set; }

        /// <summary>
        /// 自定义样式
        /// </summary>
        [StringLength(Max = 100, MessageTemplate = "菜单自定义样式不能超过{1}个字符")]
        public string? Style { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [StringLength(Max = 200, MessageTemplate = "菜单跳转地址不能超过{1}个字符")]
        public string? Url { get; set; }

        /// <summary>
        /// 排序值(值越大越优先)
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 上一级菜单
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// 对应功能Id
        /// </summary>
        public string? ActionId { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public CommonStatus Status { get; set; }

        /// <summary>
        /// 创建时间(UTC)
        /// created time
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC)
        /// last updated time
        /// </summary>
        public DateTime UpdateTime { get; set; }




        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ManageMenuDto>? MenuList { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PermissionActionInfo? ActionInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Depth
        {
            get;
            set;
        }


    }
}
