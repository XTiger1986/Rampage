using Rampage.Enums;
using Rampage.Utilities;
using Rampage.Validation;
using System;
using System.Text.Json.Serialization;

namespace Rampage.Management.Dto
{
    public class AdminDto
    {
        public static readonly string CACHE_KEY_ADMIN_BYID_PFX = "AdminById";
        public static readonly string CACHE_KEY_ADMIN_BYID_TPL = CACHE_KEY_ADMIN_BYID_PFX + "_{0}";


        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [NotWhiteSpaceString(MessageTemplate = "管理员用户名不能为空")]
        [UsernameFormat]
        [StringLength(Min = 2, Max = 16, MessageTemplate = "管理员用户名长度必须在{1}~{2}位之间")]
        public string? Username { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [PasswordFormat]
        [StringLength(Min = 5, Max = 20, MessageTemplate = "管理员密码长度必须在{1}~{2}位之间")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Password { get; set; }

        /// <summary>
        /// 真实姓名
        /// </summary>
        [StringLength(Max = 50, MessageTemplate = "管理员真实姓名不能超过{1}个字符")]
        public string? TrueName { get; set; }

        /// <summary>
        /// 手机
        /// </summary>
        [MobileFormat(MessageTemplate = "管理员手机号格式不正确")]
        public string? Mobile { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [EmailFormat(MessageTemplate = "管理员邮箱格式不正确")]
        public string? Email { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public UserGender Gender { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public CommonStatus Status { get; set; }

        /// <summary>
        /// 超级管理员
        /// </summary>
        public bool IsMaster { get; set; }

        /// <summary>
        /// 最后登录时间(UTC)
        /// last login time
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// 最后登录IP
        /// last login ip adress
        /// </summary>
        public string? LoginIp { get; set; }

        /// <summary>
        /// 创建时间(UTC)
        /// created time
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC)
        /// last updated time
        /// </summary>
        public DateTime UpdateTime { get; set; }


        /// <summary>
        /// 所属管理组
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? GroupIds { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<string>? ResourceIds { get; set; }

        /// <summary>
        /// 所属管理组列表
        /// List of associated management groups
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ManageGroupDto>? GroupList
        {
            get; set;
        }

        /// <summary>
        /// 显示名称
        /// Display Name
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DisplayName
        {
            get
            {
                return string.IsNullOrEmpty(TrueName) ? Username : TrueName;
            }
        }

        /// <summary>
        /// 头像网址
        /// Profile picture URL
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AvatarUrl
        {
            get
            {
                return !string.IsNullOrEmpty(this.Avatar) ? string.Concat("/", this.Avatar) : null;
            }
        }


    }
}



