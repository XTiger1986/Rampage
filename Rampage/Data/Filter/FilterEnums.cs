using System;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器项连接符类型
    /// </summary>
    public enum FilterConnectorType
    {
        /// <summary>
        /// 所有筛选项使用AND连接
        /// </summary>
        And = 1,
        /// <summary>
        /// 所有筛选项使用OR连接
        /// </summary>
        Or = 2
    }

    /// <summary>
    /// 筛选器项操作符类型
    /// </summary>
    public enum FilterOperatorType
    {
        /// <summary>
        /// 等于
        /// </summary>
        Equal = 1,
        /// <summary>
        /// 不等于
        /// </summary>
        NotEqual = 2,
        /// <summary>
        /// 大于
        /// </summary>
        GreaterThan = 3,
        /// <summary>
        /// 大于等于
        /// </summary>
        GreaterThanOrEqual = 4,
        /// <summary>
        /// 小于
        /// </summary>
        LessThan = 5,
        /// <summary>
        /// 小于等于
        /// </summary>
        LessThanOrEqual = 6,
        /// <summary>
        /// 左开右闭区间
        /// </summary>
        LeftOpenScope = 7,
        /// <summary>
        /// 左闭右开区间
        /// </summary>
        RightOpenScope = 8,

        /// <summary>
        /// 左模糊匹配
        /// </summary>
        LeftLike = 97,
        /// <summary>
        /// 右模糊匹配
        /// </summary>
        RightLike = 98,
        /// <summary>
        /// 全模糊匹配
        /// </summary>
        Like = 99
    }
}
