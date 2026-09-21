using System;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器解析产出的单个 SQL 参数（参数名、运行时值与列映射）
    /// A single SQL parameter produced by filter parsing (name, runtime value and column mapping)
    /// </summary>
    public class FilterConditionParameter
    {

        /// <summary>
        /// 参数名（基础键，渲染时按 SQL 模板格式化）
        /// Parameter name (the base key, formatted per the SQL template when rendered)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 参数运行时值
        /// Runtime value of the parameter
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// 参数对应的列映射信息（含类型转换等元数据）
        /// Column mapping the parameter belongs to (metadata such as type conversion)
        /// </summary>
        public FieldMapping Column { get; set; } = default!;

    }
}
