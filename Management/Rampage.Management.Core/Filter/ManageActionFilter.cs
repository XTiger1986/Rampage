using Rampage.Data.Filter;
using Rampage.Enums;
using System;

namespace Rampage.Management.Filter
{
    /// <summary>权限功能(Action)筛选器 / Permission action filter</summary>
    [Filter]
    public class ManageActionFilter : DataFilter
    {

        /// <summary>关键词（模糊匹配标题/控制器名/视图名/说明） / Keyword (like-match on title, controller name, action name, description)</summary>
        [FilterItem(Operator = FilterOperatorType.Like, FieldName = "title", OrFields = "controller_name,action_name,description")]
        public string? Keywords
        {
            get; set;
        }

        /// <summary>模块ID / Module ID</summary>
        public int? ModuleId
        {
            get; set;
        }

        /// <summary>上一级功能ID / Parent action ID</summary>
        public int? ParentId
        {
            get; set;
        }

        /// <summary>状态 / Status</summary>
        public CommonStatus? Status
        {
            get; set;
        }

    }
}
