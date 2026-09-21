using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>管理员个人授权（映射 admin_permission 表） / Administrator personal permission (mapped to admin_permission)</summary>
    /// <remarks>资源级授权：Resource 对应权限系统中的资源组标识，与管理组授权取并集生效。/ Resource-level grant: Resource is a permission resource-group identifier, unioned with the management-group permissions.</remarks>
    [TableMapping("admin_permission")]
    public class AdminPermission
    {
        
        /// <summary>主键ID（自增） / Primary key ID (auto increment)</summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }
        
        /// <summary>管理员ID / Administrator ID</summary>
        [FieldMapping("admin_id")]
        public int AdminId { get; set; }

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
