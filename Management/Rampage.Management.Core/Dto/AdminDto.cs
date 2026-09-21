using Rampage.Enums;
using Rampage.Utilities;
using Rampage.Validation;
using System;
using System.Text.Json.Serialization;

namespace Rampage.Management.Dto
{
    /// <summary>管理员数据传输对象 / Administrator data transfer object</summary>
    public class AdminDto
    {
        /// <summary>按ID缓存管理员的键前缀 / Cache key prefix for admins by ID</summary>
        public static readonly string CACHE_KEY_ADMIN_BYID_PFX = "AdminById";
        /// <summary>按ID缓存管理员的键模板（{0}=管理员ID） / Cache key template for admins by ID ({0} = admin ID)</summary>
        public static readonly string CACHE_KEY_ADMIN_BYID_TPL = CACHE_KEY_ADMIN_BYID_PFX + "_{0}";


        /// <summary>主键ID / Primary key ID</summary>
        public int Id { get; set; }

        /// <summary>用户名 / Username</summary>
        [NotWhiteSpaceString(MessageTemplate = "管理员用户名不能为空")]
        [UsernameFormat]
        [StringLength(Min = 2, Max = 16, MessageTemplate = "管理员用户名长度必须在{1}~{2}位之间")]
        public string? Username { get; set; }

        /// <summary>密码（读取时被清空，仅提交时携带） / Password (cleared when reading; only present on submit)</summary>
        [PasswordFormat]
        [StringLength(Min = 5, Max = 20, MessageTemplate = "管理员密码长度必须在{1}~{2}位之间")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Password { get; set; }

        /// <summary>真实姓名 / Real full name</summary>
        [StringLength(Max = 50, MessageTemplate = "管理员真实姓名不能超过{1}个字符")]
        public string? TrueName { get; set; }

        /// <summary>手机号 / Mobile number</summary>
        [MobileFormat(MessageTemplate = "管理员手机号格式不正确")]
        public string? Mobile { get; set; }

        /// <summary>邮箱地址 / Email address</summary>
        [EmailFormat(MessageTemplate = "管理员邮箱格式不正确")]
        public string? Email { get; set; }

        /// <summary>头像（相对路径） / Avatar (relative path)</summary>
        public string? Avatar { get; set; }

        /// <summary>性别 / Gender</summary>
        public UserGender Gender { get; set; }

        /// <summary>状态（启用/禁用） / Status (enabled/disabled)</summary>
        public CommonStatus Status { get; set; }

        /// <summary>是否超级管理员（超管拥有全部权限） / Whether this is a super administrator (masters hold all permissions)</summary>
        public bool IsMaster { get; set; }

        /// <summary>最后登录时间(UTC) / Last login time (UTC)</summary>
        public DateTime LoginTime { get; set; }

        /// <summary>最后登录IP / Last login IP address</summary>
        public string? LoginIp { get; set; }

        /// <summary>创建时间(UTC) / Created time (UTC)</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC) / Last updated time (UTC)</summary>
        public DateTime UpdateTime { get; set; }


        /// <summary>所属管理组ID集合 / IDs of the associated management groups</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? GroupIds { get; set; }

        /// <summary>授权资源标识集合（提交保存用） / Authorized resource identifiers (for saving)</summary>
        public List<string>? ResourceIds { get; set; }

        /// <summary>所属管理组列表 / List of associated management groups</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ManageGroupDto>? GroupList
        {
            get; set;
        }

        /// <summary>显示名称（真实姓名优先，否则用户名） / Display name (real name if set, otherwise username)</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DisplayName
        {
            get
            {
                return string.IsNullOrEmpty(TrueName) ? Username : TrueName;
            }
        }

        /// <summary>头像网址（带前导斜杠；无头像时为 null） / Avatar URL (with leading slash; null when no avatar)</summary>
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



