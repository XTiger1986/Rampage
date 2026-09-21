using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    
    /// <summary>管理员（映射 admin 表） / Administrator (mapped to the admin table)</summary>
    [TableMapping("admin")]
    public class Admin
    {
        
        /// <summary>主键ID（自增） / Primary key ID (auto increment)</summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>用户名 / Username</summary>
        [FieldMapping("username")]
        public string? Username { get; set; }

        /// <summary>密码（PBKDF2 哈希） / Password (PBKDF2 hash)</summary>
        [FieldMapping("password")]
        public string? Password { get; set; }
        
        /// <summary>真实姓名 / Real full name</summary>
        [FieldMapping("true_name")]
        public string? TrueName { get; set; }
        
        /// <summary>手机号 / Mobile number</summary>
        [FieldMapping("mobile")]
        public string? Mobile { get; set; }
        
        /// <summary>邮箱地址 / Email address</summary>
        [FieldMapping("email")]
        public string? Email { get; set; }
        
        /// <summary>头像（相对路径） / Avatar (relative path)</summary>
        [FieldMapping("avatar")]
        public string? Avatar { get; set; }
        
        /// <summary>性别 / Gender</summary>
        [FieldMapping("gender")]
        public UserGender Gender { get; set; }
        
        /// <summary>状态（启用/禁用） / Status (enabled/disabled)</summary>
        [FieldMapping("status")]
        public CommonStatus Status { get; set; }
        
        /// <summary>是否超级管理员（超管拥有全部权限） / Whether this is a super administrator (masters hold all permissions)</summary>
        [FieldMapping("is_master")]
        public bool IsMaster { get; set; }

        /// <summary>最后登录时间(UTC) / Last login time (UTC)</summary>
        [FieldMapping("login_time")]
        public DateTime LoginTime { get; set; }
        
        /// <summary>最后登录IP / Last login IP address</summary>
        [FieldMapping("login_ip")]
        public string? LoginIp { get; set; }

        /// <summary>创建时间(UTC) / Created time (UTC)</summary>
        [FieldMapping("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC) / Last updated time (UTC)</summary>
        [FieldMapping("update_time")]
        public DateTime UpdateTime { get; set; }


    }
}
