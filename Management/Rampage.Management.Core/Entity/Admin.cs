using Rampage.Data;
using Rampage.Enums;
using System;

namespace Rampage.Management.Entity
{
    
    /// <summary>
    /// 管理员
    /// Administrator
    /// </summary>
    [TableMapping("admin")]
    public class Admin
    {
        
        /// <summary>
        /// 
        /// </summary>
        [FieldMapping(PrimaryKey = true, AutoIncrement = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// 用户名
        /// Username
        /// </summary>
        [FieldMapping("username")]
        public string? Username { get; set; }

        /// <summary>
        /// 密码
        /// Password
        /// </summary>
        [FieldMapping("password")]
        public string? Password { get; set; }
        
        /// <summary>
        /// 真实姓名
        /// Real Full Name
        /// </summary>
        [FieldMapping("true_name")]
        public string? TrueName { get; set; }
        
        /// <summary>
        /// 手机
        /// Mobile Number
        /// </summary>
        [FieldMapping("mobile")]
        public string? Mobile { get; set; }
        
        /// <summary>
        /// 邮箱
        /// Email Address
        /// </summary>
        [FieldMapping("email")]
        public string? Email { get; set; }
        
        /// <summary>
        /// 头像
        /// Profile Picture
        /// </summary>
        [FieldMapping("avatar")]
        public string? Avatar { get; set; }
        
        /// <summary>
        /// 性别
        /// Gender
        /// </summary>
        [FieldMapping("gender")]
        public UserGender Gender { get; set; }
        
        /// <summary>
        /// 状态
        /// Status
        /// </summary>
        [FieldMapping("status")]
        public CommonStatus Status { get; set; }
        
        /// <summary>
        /// 超级管理员
        /// is a super administrator
        /// </summary>
        [FieldMapping("is_master")]
        public bool IsMaster { get; set; }

        /// <summary>
        /// 最后登录时间(UTC)
        /// last login time
        /// </summary>
        [FieldMapping("login_time")]
        public DateTime LoginTime { get; set; }
        
        /// <summary>
        /// 最后登录IP
        /// last login ip adress
        /// </summary>
        [FieldMapping("login_ip")]
        public string? LoginIp { get; set; }

        /// <summary>
        /// 创建时间(UTC)
        /// created time
        /// </summary>
        [FieldMapping("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC)
        /// last updated time
        /// </summary>
        [FieldMapping("update_time")]
        public DateTime UpdateTime { get; set; }


    }
}
