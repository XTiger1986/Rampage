using System;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器结构：一个筛选器类型的解析结果（类型、类级特性与筛选项列表）
    /// Filter schema: the parsed result of one filter type (type, class attribute and item list)
    /// </summary>
    public class FilterSchema
    {

        /// <summary>
        /// 类型
        /// The filter type
        /// </summary>
        public Type? ModelType
        {
            get; internal set;
        }

        /// <summary>
        /// 过滤器主信息（表名等）
        /// Main filter attribute (table name etc.)
        /// </summary>
        public FilterAttribute? Filter
        {
            get; internal set;
        }

        /// <summary>
        /// 过滤器字段信息（按 SortOrder 降序排列）
        /// Filter item attributes (ordered by SortOrder descending)
        /// </summary>
        public List<FilterItemAttribute>? ItemList
        {
            get; internal set;
        }

    }
}
