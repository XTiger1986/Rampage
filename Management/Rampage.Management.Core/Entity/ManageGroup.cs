using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>管理组（映射 manage_group 表） / Management group (mapped to manage_group)</summary>
    [TableMapping("manage_group")]
    public class ManageGroup
    {

        /// <summary>主键ID（自增） / Primary key ID (auto increment)</summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }
        
        /// <summary>名称 / Name</summary>
        [FieldMapping("name")]
        public string? Name { get; set; }
        
        /// <summary>描述 / Description</summary>
        [FieldMapping("description")]
        public string? Description { get; set; }
        
        /// <summary>状态（启用/禁用） / Status (enabled/disabled)</summary>
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
