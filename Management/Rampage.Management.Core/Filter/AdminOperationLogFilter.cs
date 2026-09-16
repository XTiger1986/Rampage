using Rampage.Data.Filter;
using System;

namespace Rampage.Management.Filter
{
    /// <summary>
    /// 类说明
    /// </summary>
    public class AdminOperationLogFilter : DataFilter
    {

        /// <summary>
        /// 
        /// </summary>
        public int? AdminId
        {
            get; set;
        }

        /// <summary>
        /// 操作名称
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? OperationName
        {
            get; set;
        }

        /// <summary>
        /// 控制器名
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? ControllerName
        {
            get; set;
        }

        /// <summary>
        /// 视图名
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? ActionName
        {
            get; set;
        }

        /// <summary>
        /// 请求的地址
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? RawUrl
        {
            get; set;
        }

        /// <summary>
        /// 请求的表单数据
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? PostData
        {
            get; set;
        }

        /// <summary>
        /// 输入数据
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? DataIn
        {
            get; set;
        }

        /// <summary>
        /// 输出数据
        /// </summary>
        [FilterItem(Operator = FilterOperatorType.Like)]
        public string? DataOut
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
