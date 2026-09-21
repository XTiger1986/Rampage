using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>管理组授权（映射 manage_group_permission 表） / Management group permission (mapped to manage_group_permission)</summary>
    /// <remarks>资源级授权：组内所有管理员共享 Resource 对应的权限，与管理员个人授权取并集生效。/ Resource-level grant shared by all members of the group, unioned with each admin's personal permissions.</remarks>
    [TableMapping("manage_group_permission")]
    public class ManageGroupPermission
    {
        
        /// <summary>主键ID（自增） / Primary key ID (auto increment)</summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }
        
        /// <summary>管理组ID / Management group ID</summary>
        [FieldMapping("group_id")]
        public int GroupId { get; set; }

        /// <summary>资源标识（权限资源组） / Resource identifier (permission resource group)</summary>
        [FieldMapping("resource")]
        public string? Resource { get; set; }

        /// <summary>状态（启用/禁用，禁用即撤权） / Status (enabled/disabled; disabled means revoked)</summary>
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
