using Rampage.Data.Filter;
using Rampage.Enums;
using System;

namespace Rampage.Management.Filter
{
    /// <summary>
    /// 管理员筛选器
    /// </summary>
    [Filter]
    public class AdminFilter : DataFilter
    {

        /// <summary>
        /// 
        /// </summary>
        public int? AdminId
        {
            get; set;
        }

        /// <summary>
        /// 状态
        /// </summary>
        public CommonStatus? Status
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like, FieldName = "username", OrFields = "true_name,mobile,email")]
        public string? Keywords
        {
            get; set;
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public ValueScope<DateTime?>? CreateTime
        {
            get; set;
        }

    }
}
