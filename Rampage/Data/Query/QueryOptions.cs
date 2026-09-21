using System;

namespace Rampage.Data.Query
{
    /// <summary>
    /// SQL 查询设置（面向数据表名 + 原生 SQL 片段的低层查询选项）
    /// SQL query options (low-level options of table name plus raw SQL fragments)
    /// </summary>
    public class QueryOptions
    {

        /// <summary>
        /// SELECT 字段部分的 SQL 片段，为 null 时默认查询全部字段
        /// SQL fragment of the SELECT field list; null selects all fields
        /// </summary>
        public string? Select
        {
            get;
            set;
        }

        /// <summary>
        /// 要查询的数据表名
        /// Name of the table to query
        /// </summary>
        public string? TableName
        {
            get;
            set;
        }

        /// <summary>
        /// WHERE 条件 SQL 片段（不含 WHERE 关键字）
        /// WHERE condition SQL fragment (without the WHERE keyword)
        /// </summary>
        public string? Condition
        {
            get;
            set;
        }

        /// <summary>
        /// GROUP BY 子句 SQL 片段（不含 GROUP BY 关键字）
        /// GROUP BY clause SQL fragment (without the GROUP BY keyword)
        /// </summary>
        public string? GroupBy
        {
            get;
            set;
        }

        /// <summary>
        /// ORDER BY 子句 SQL 片段（不含 ORDER BY 关键字）
        /// ORDER BY clause SQL fragment (without the ORDER BY keyword)
        /// </summary>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// 要查询的数据条数
        /// Maximum number of rows to query
        /// </summary>
        public long? Limit
        {
            get;
            set;
        }

        /// <summary>
        /// 数据查询偏移行数（从 0 开始）
        /// Row offset of the query (starting from 0)
        /// </summary>
        public long? Offset
        {
            get;
            set;
        }

    }

    /// <summary>
    /// SQL 查询设置（强类型版本，表名由 <typeparamref name="M"/> 的模型映射决定）
    /// SQL query options (strongly-typed; the table name comes from the model mapping of <typeparamref name="M"/>)
    /// </summary>
    /// <typeparam name="M">模型类型 / Model type</typeparam>
    public class QueryOptions<M> where M : class
    {

        /// <summary>
        /// SELECT 字段部分的 SQL 片段，为 null 时默认查询全部字段
        /// SQL fragment of the SELECT field list; null selects all fields
        /// </summary>
        public string? Select
        {
            get;
            set;
        }

        /// <summary>
        /// WHERE 条件 SQL 片段（不含 WHERE 关键字）
        /// WHERE condition SQL fragment (without the WHERE keyword)
        /// </summary>
        public string? Condition
        {
            get;
            set;
        }

        /// <summary>
        /// GROUP BY 子句 SQL 片段（不含 GROUP BY 关键字）
        /// GROUP BY clause SQL fragment (without the GROUP BY keyword)
        /// </summary>
        public string? GroupBy
        {
            get;
            set;
        }

        /// <summary>
        /// ORDER BY 子句 SQL 片段（不含 ORDER BY 关键字）
        /// ORDER BY clause SQL fragment (without the ORDER BY keyword)
        /// </summary>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// 要查询的数据条数
        /// Maximum number of rows to query
        /// </summary>
        public long? Limit
        {
            get;
            set;
        }

        /// <summary>
        /// 数据查询偏移行数（从 0 开始）
        /// Row offset of the query (starting from 0)
        /// </summary>
        public long? Offset
        {
            get;
            set;
        }

    }
}
