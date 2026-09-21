using Rampage.Data.Filter;
using System;

namespace Rampage.Management.Filter
{
    /// <summary>管理员操作日志筛选器 / Administrator operation log filter</summary>
    public class AdminOperationLogFilter : DataFilter
    {

        /// <summary>管理员ID / Administrator ID</summary>
        public int? AdminId
        {
            get; set;
        }

        /// <summary>操作名称（模糊匹配） / Operation name (like-match)</summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? OperationName
        {
            get; set;
        }

        /// <summary>控制器名（模糊匹配） / Controller name (like-match)</summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? ControllerName
        {
            get; set;
        }

        /// <summary>视图名(Action名，模糊匹配) / Action name (like-match)</summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? ActionName
        {
            get; set;
        }

        /// <summary>请求的地址（模糊匹配） / Requested URL (like-match)</summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? RawUrl
        {
            get; set;
        }

        /// <summary>请求的表单数据（模糊匹配） / Requested form data (like-match)</summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? PostData
        {
            get; set;
        }

        /// <summary>入口数据（模糊匹配） / Input data (like-match)</summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? DataIn
        {
            get; set;
        }

        /// <summary>出口数据（模糊匹配） / Output data (like-match)</summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? DataOut
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
