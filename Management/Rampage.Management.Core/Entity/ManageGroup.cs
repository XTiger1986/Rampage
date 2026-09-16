using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>
    /// 管理组
    /// </summary>
    [TableMapping("manage_group")]
    public class ManageGroup
    {

        /// <summary>
        /// 
        /// </summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }
        
        /// <summary>
        /// 名称
        /// </summary>
        [FieldMapping("name")]
        public string? Name { get; set; }
        
        /// <summary>
        /// 描述
        /// </summary>
        [FieldMapping("description")]
        public string? Description { get; set; }
        
        /// <summary>
        /// 状态
        /// </summary>
        [FieldMapping("status")]
        public CommonStatus Status { get; set; }

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
