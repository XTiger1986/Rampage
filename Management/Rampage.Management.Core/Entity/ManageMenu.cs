using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>
    /// 后台菜单
    /// </summary>
    [TableMapping("manage_menu")]
    public class ManageMenu
    {
        
        /// <summary>
        /// 
        /// </summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        [FieldMapping("title")]
        public string? Title { get; set; }

        /// <summary>
        /// 自定义样式名
        /// </summary>
        [FieldMapping("css")]
        public string? Css { get; set; }

        /// <summary>
        /// 自定义样式
        /// </summary>
        [FieldMapping("style")]
        public string? Style { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [FieldMapping("url")]
        public string? Url { get; set; }

        /// <summary>
        /// 排序值(值越大越优先)
        /// </summary>
        [FieldMapping("sort_order")]
        public int SortOrder { get; set; }

        /// <summary>
        /// 上一级菜单
        /// </summary>
        [FieldMapping("parent_id")]
        public int ParentId { get; set; }

        /// <summary>
        /// 对应功能Id
        /// </summary>
        [FieldMapping("action_id")]
        public string? ActionId { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [FieldMapping("status")]
        public CommonStatus Status { get; set; }

        /// <summary>
        /// 是否系统内置
        /// </summary>
        [FieldMapping("is_sys")]
        public bool IsSys { get; set; }

        /// <summary>
        /// 创建时间(UTC)
        /// </summary>
        [FieldMapping("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC)
        /// </summary>
        [FieldMapping("update_time")]
        public DateTime UpdateTime { get; set; }



    }
}
