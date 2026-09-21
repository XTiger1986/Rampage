using Rampage.Data.Filter;
using Rampage.Enums;
using System;

namespace Rampage.Management.Filter
{
    /// <summary>管理员筛选器 / Administrator filter</summary>
    [Filter]
    public class AdminFilter : DataFilter
    {

        /// <summary>管理员ID / Administrator ID</summary>
        public int? AdminId
        {
            get; set;
        }

        /// <summary>状态 / Status</summary>
        public CommonStatus? Status
        {
            get; set;
        }

        /// <summary>关键词（模糊匹配用户名/真实姓名/手机/邮箱） / Keyword (like-match on username, real name, mobile, email)</summary>
        [FilterItem(Operator = FilterOperatorType.Like, FieldName = "username", OrFields = "true_name,mobile,email")]
        public string? Keywords
        {
            get; set;
        }

        /// <summary>创建时间范围 / Created time scope</summary>
        public ValueScope<DateTime?>? CreateTime
        {
            get; set;
        }

    }
}
