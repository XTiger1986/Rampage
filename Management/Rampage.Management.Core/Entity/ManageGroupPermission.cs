using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>
    /// 管理组权限
    /// </summary>
    [TableMapping("manage_group_permission")]
    public class ManageGroupPermission
    {
        
        /// <summary>
        /// 
        /// </summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }
        
        /// <summary>
        /// 管理组ID
        /// </summary>
        [FieldMapping("group_id")]
        public int GroupId { get; set; }

        /// <summary>
        /// 资源标识
        /// </summary>
        [FieldMapping("resource")]
        public string? Resource { get; set; }

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
