using Rampage.Enums;
using Rampage.Utilities;
using Rampage.Validation;
using System;
using System.Text.Json.Serialization;

namespace Rampage.Management.Dto
{
    /// <summary>
    /// 管理组
    /// </summary>
    public class ManageGroupDto
    {
        public static readonly string CACHE_KEY_LIST_PFX = "ManageGroupList";
        public static readonly string CACHE_KEY_LIST_TPL = CACHE_KEY_LIST_PFX + "_{0}";

        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [NotWhiteSpaceString(MessageTemplate = "管理组名不能为空")]
        public string? Name { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [StringLength(Max = 200, MessageTemplate = "管理组描述不能超过{1}个字符")]
        public string? Description { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public CommonStatus Status { get; set; }

        /// <summary>
        /// (UTC)
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// (UTC)
        /// </summary>
        public DateTime UpdateTime { get; set; }




        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? ResourceIds { get; set; }

    }
}
