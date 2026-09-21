using Rampage.Data.Filter;
using System;

namespace Rampage.Data.Query
{
    /// <summary>
    /// 基于 <see cref="DataFilter"/> 筛选器对象的查询包装器（字段与运算符由筛选器属性上的特性声明）
    /// Query wrapper driven by a <see cref="DataFilter"/> object (fields and operators are declared by attributes on filter properties)
    /// </summary>
    /// <typeparam name="M">模型类型 / Model type</typeparam>
    public class FilterWrapper<M> : QueryWrapper<M> where M : class
    {
        /// <summary>
        /// 当前关联的数据筛选器，为 null 表示无条件
        /// Associated data filter; null means no condition
        /// </summary>
        public DataFilter? Filter
        {
            get;
            protected set;
        }

        /// <summary>
        /// 初始化一个不带筛选器的包装器
        /// Initialize a wrapper without a filter
        /// </summary>
        public FilterWrapper() : base()
        {
        }

        /// <summary>
        /// 以一个数据筛选器初始化包装器
        /// Initialize a wrapper with a data filter
        /// </summary>
        /// <param name="filter">数据筛选器 / Data filter</param>
        public FilterWrapper(DataFilter filter) : base()
        {
            Filter = filter;
        }


        /// <summary>
        /// 创建一个不带筛选器的 FilterWrapper 包装器
        /// Create an empty FilterWrapper without a filter
        /// </summary>
        /// <returns>新的包装器实例 / A new wrapper instance</returns>
        public static FilterWrapper<M> Empty()
        {
            return new FilterWrapper<M>();
        }

        /// <summary>
        /// 以一个数据筛选器创建 FilterWrapper 包装器
        /// Create a FilterWrapper with a data filter
        /// </summary>
        /// <param name="filter">数据筛选器 / Data filter</param>
        /// <returns>新的包装器实例 / A new wrapper instance</returns>
        public static FilterWrapper<M> Create(DataFilter filter)
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            return wrapper;
        }


    }
}
