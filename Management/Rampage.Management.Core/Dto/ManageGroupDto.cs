using Rampage.Enums;
using Rampage.Utilities;
using Rampage.Validation;
using System;
using System.Text.Json.Serialization;

namespace Rampage.Management.Dto
{
    /// <summary>管理组数据传输对象 / Management group data transfer object</summary>
    public class ManageGroupDto
    {
        /// <summary>管理组列表缓存的键前缀 / Cache key prefix for the management group list</summary>
        public static readonly string CACHE_KEY_LIST_PFX = "ManageGroupList";
        /// <summary>管理组列表缓存的键模板（{0}=状态） / Cache key template for the management group list ({0} = status)</summary>
        public static readonly string CACHE_KEY_LIST_TPL = CACHE_KEY_LIST_PFX + "_{0}";

        /// <summary>主键ID / Primary key ID</summary>
        public int Id { get; set; }

        /// <summary>名称 / Name</summary>
        [NotWhiteSpaceString(MessageTemplate = "管理组名不能为空")]
        public string? Name { get; set; }

        /// <summary>描述 / Description</summary>
        [StringLength(Max = 200, MessageTemplate = "管理组描述不能超过{1}个字符")]
        public string? Description { get; set; }

        /// <summary>状态（启用/禁用） / Status (enabled/disabled)</summary>
        public CommonStatus Status { get; set; }

        /// <summary>创建时间(UTC) / Created time (UTC)</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC) / Last updated time (UTC)</summary>
        public DateTime UpdateTime { get; set; }




        /// <summary>授权资源标识集合（提交保存用） / Authorized resource identifiers (for saving)</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? ResourceIds { get; set; }

    }
}
