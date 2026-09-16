using Rampage.Data.Filter;
using Rampage.Enums;
using System;

namespace Rampage.Management.Filter
{
    /// <summary>
    /// 功能筛选器
    /// </summary>
    [Filter]
    public class ManageActionFilter : DataFilter
    {

        /// <summary>
        /// 
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like, FieldName = "title", OrFields = "controller_name,action_name,description")]
        public string? Keywords
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int? ModuleId
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int? ParentId
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

    }
}
