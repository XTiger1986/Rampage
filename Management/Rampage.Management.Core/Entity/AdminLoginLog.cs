using Rampage.Data;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>管理员登录日志（映射 admin_login_log 表） / Administrator login log (mapped to admin_login_log)</summary>
    [TableMapping("admin_login_log")]
    public class AdminLoginLog
    {
        
        /// <summary>主键ID（自增） / Primary key ID (auto increment)</summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public long Id { get; set; }
        
        /// <summary>管理员ID / Administrator ID</summary>
        [FieldMapping("admin_id")]
        public int AdminId { get; set; }

        /// <summary>登录时间(UTC) / Login time (UTC)</summary>
        [FieldMapping("login_time")]
        public DateTime LoginTime { get; set; }
        
        /// <summary>登录IP / Login IP address</summary>
        [FieldMapping("login_ip")]
        public string? LoginIp { get; set; }
        
        /// <summary>登录客户端(User-Agent) / Login user agent</summary>
        [FieldMapping("user_agent")]
        public string? UserAgent { get; set; }
        
    }
}
