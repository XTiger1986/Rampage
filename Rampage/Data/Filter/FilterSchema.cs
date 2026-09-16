using System;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器结构
    /// </summary>
    public class FilterSchema
    {

        /// <summary>
        /// 类型
        /// </summary>
        public Type? ModelType
        {
            get; internal set;
        }

        /// <summary>
        /// 过滤器主信息
        /// </summary>
        public FilterAttribute? Filter
        {
            get; internal set;
        }

        /// <summary>
        /// 过滤器字段信息
        /// </summary>
        public List<FilterItemAttribute>? ItemList
        {
            get; internal set;
        }

    }
}
