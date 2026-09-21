using System;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器项连接符类型
    /// Connector type between filter items
    /// </summary>
    public enum FilterConnectorType
    {
        /// <summary>
        /// 所有筛选项使用AND连接
        /// Join filter items with AND
        /// </summary>
        And = 1,
        /// <summary>
        /// 所有筛选项使用OR连接
        /// Join filter items with OR
        /// </summary>
        Or = 2
    }

    /// <summary>
    /// 筛选器项操作符类型
    /// Operator type of a filter item
    /// </summary>
    public enum FilterOperatorType
    {
        /// <summary>
        /// 等于
        /// Equal
        /// </summary>
        Equal = 1,
        /// <summary>
        /// 不等于
        /// Not equal
        /// </summary>
        NotEqual = 2,
        /// <summary>
        /// 大于
        /// Greater than
        /// </summary>
        GreaterThan = 3,
        /// <summary>
        /// 大于等于
        /// Greater than or equal
        /// </summary>
        GreaterThanOrEqual = 4,
        /// <summary>
        /// 小于
        /// Less than
        /// </summary>
        LessThan = 5,
        /// <summary>
        /// 小于等于
        /// Less than or equal
        /// </summary>
        LessThanOrEqual = 6,
        /// <summary>
        /// 左开右闭区间（起始值用 &gt;，结束值用 &lt;=）
        /// Left-open right-closed range (start with &gt;, end with &lt;=)
        /// </summary>
        LeftOpenScope = 7,
        /// <summary>
        /// 左闭右开区间（起始值用 &gt;=，结束值用 &lt;）
        /// Left-closed right-open range (start with &gt;=, end with &lt;)
        /// </summary>
        RightOpenScope = 8,

        /// <summary>
        /// 左模糊匹配（%value）
        /// Left fuzzy match (%value)
        /// </summary>
        LeftLike = 97,
        /// <summary>
        /// 右模糊匹配（value%）
        /// Right fuzzy match (value%)
        /// </summary>
        RightLike = 98,
        /// <summary>
        /// 全模糊匹配（%value%）
        /// Full fuzzy match (%value%)
        /// </summary>
        Like = 99
    }
}
