using Rampage.Data;
using System;

namespace Rampage.Management.Entity
{
    /// <summary>
    /// 管理员登录记录
    /// </summary>
    [TableMapping("admin_login_log")]
    public class AdminLoginLog
    {
        
        /// <summary>
        /// 
        /// </summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public long Id { get; set; }
        
        /// <summary>
        /// 管理员id
        /// </summary>
        [FieldMapping("admin_id")]
        public int AdminId { get; set; }

        /// <summary>
        /// 登录时间(UTC)
        /// </summary>
        [FieldMapping("login_time")]
        public DateTime LoginTime { get; set; }
        
        /// <summary>
        /// 登录IP
        /// </summary>
        [FieldMapping("login_ip")]
        public string? LoginIp { get; set; }
        
        /// <summary>
        /// 登录客户端
        /// </summary>
        [FieldMapping("user_agent")]
        public string? UserAgent { get; set; }
        
    }
}
