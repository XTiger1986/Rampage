using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>管理员与管理组的关联（映射 admin_in_group 表） / Association between an administrator and a management group (mapped to admin_in_group)</summary>
    [TableMapping("admin_in_group")]
    public class AdminInGroup
    {
        
        /// <summary>主键ID（自增） / Primary key ID (auto increment)</summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }
        
        /// <summary>管理员ID / Administrator ID</summary>
        [FieldMapping("admin_id")]
        public int AdminId { get; set; }
        
        /// <summary>所属管理组ID / Management group ID</summary>
        [FieldMapping("group_id")]
        public int GroupId { get; set; }
        
        /// <summary>状态（启用表示仍属于该组） / Status (enabled means still in the group)</summary>
        [FieldMapping("status")]
        public CommonStatus Status { get; set; }

        /// <summary>创建时间(UTC) / Created time (UTC)</summary>
        [FieldMapping("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC) / Last updated time (UTC)</summary>
        [FieldMapping("update_time")]
        public DateTime UpdateTime { get; set; }
        
    }
}
