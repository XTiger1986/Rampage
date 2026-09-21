using Rampage.Data.Expressions;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Query
{
    /// <summary>
    /// 要进行查询的数据表字段包装集
    /// Field packaging set of the data table to be queried
    /// </summary>
    public class QueryFields
    {

        /// <summary>
        /// 数据表字段名列表
        /// List of table field names
        /// </summary>
        public List<string> Fields
        {
            get; protected set;
        }

        /// <summary>
        /// 已收录的字段数量
        /// Number of fields collected
        /// </summary>
        public int Count
        {
            get
            {
                return Fields.Count;
            }
        }


        /// <summary>
        /// 是否查询所有字段
        /// Whether to query all fields
        /// </summary>
        public bool IsAll
        {
            get;
            protected set;
        }

        /// <summary>
        /// 查询字段前缀的表别名（如生成 alias.Column）
        /// Table alias prefixed to query fields (produces alias.Column)
        /// </summary>
        public string? TableAlias
        {
            get;
            set;
        }

        /// <summary>
        /// 初始化字段集合
        /// Initialize the field collection
        /// </summary>
        /// <param name="all">
        /// 是否查询所有字段
        /// Whether to query all fields
        /// </param>
        public QueryFields(bool all = true)
        {
            IsAll = all;
            Fields = new List<string>();
        }

        /// <summary>
        /// 以指定字段名列表初始化字段集合（null 或空数组视为查询所有字段）
        /// Initialize with an explicit field name list (null or empty means query all fields)
        /// </summary>
        /// <param name="resultFields">字段名数组 / Field names</param>
        public QueryFields(params string[] resultFields)
        {
            Fields = new List<string>();
            if (resultFields != null && resultFields.Length > 0)
            {
                Fields.AddRange(resultFields);
            }
            else
            {
                IsAll = true;
            }
        }

        /// <summary>
        /// 添加一个字段（已存在则忽略，不修改 <see cref="IsAll"/>）
        /// Add a field (ignored if already present; <see cref="IsAll"/> is not changed)
        /// </summary>
        /// <param name="field">字段名 / Field name</param>
        /// <returns>当前实例 / This instance</returns>
        public QueryFields Add(string field)
        {
            if (!Fields.Contains(field))
            {
                Fields.Add(field);
            }
            return this;
        }

        /// <summary>
        /// 创建一个指定是否查询全部字段的 <see cref="QueryFields"/> 实例
        /// Create a <see cref="QueryFields"/> instance specifying whether to query all fields
        /// </summary>
        /// <param name="all">是否查询全部字段 / Whether to query all fields</param>
        /// <returns>新的字段集合实例 / A new instance</returns>
        public static QueryFields Init(bool all = true)
        {
            return new QueryFields(all);
        }

        /// <summary>
        /// 创建一个只查询指定字段的 <see cref="QueryFields"/> 实例
        /// Create a <see cref="QueryFields"/> instance selecting the specified fields
        /// </summary>
        /// <param name="resultFields">字段名数组 / Field names</param>
        /// <returns>新的字段集合实例 / A new instance</returns>
        public static QueryFields Init(params string[] resultFields)
        {
            return new QueryFields(resultFields);
        }

    }

    /// <summary>
    /// 以 lambda 字段选择器表达的字段集合（经模型映射解析为实际列名）
    /// Field collection expressed with lambda key selectors (resolved to real column names via model mapping)
    /// </summary>
    /// <typeparam name="M">模型类型 / Model type</typeparam>
    public class QueryFields<M> : QueryFields
        where M : class
    {

        /// <summary>
        /// 模型与数据表的映射信息（构造时按 <typeparamref name="M"/> 自动获取）
        /// Model-to-table mapping (resolved from <typeparamref name="M"/> at construction)
        /// </summary>
        public TableMapping Mapping
        {
            get;
            private set;
        }

        /// <summary>
        /// 以字段选择器初始化字段集合（null 或空数组视为查询所有字段）
        /// Initialize with key selectors (null or empty means query all fields)
        /// </summary>
        /// <param name="keySelectors">字段选择器表达式数组 / Key selector expressions</param>
        public QueryFields(params Expression<Func<M, object?>>[] keySelectors)
        {
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
            Initialize(keySelectors);
        }

        /// <summary>
        /// 以字段选择器初始化字段集合：isExclude 为 true 时查询"除这些字段外"的全部列，否则仅查询这些字段
        /// Initialize with key selectors: when isExclude is true, select all columns except the specified ones; otherwise select only them
        /// </summary>
        /// <param name="isExclude">是否为排除模式 / Whether to exclude (instead of include) the specified fields</param>
        /// <param name="keySelectors">字段选择器表达式数组 / Key selector expressions</param>
        public QueryFields(bool isExclude, params Expression<Func<M, object?>>[] keySelectors)
        {
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
            if (!isExclude)
            {
                Initialize(keySelectors);
            }
            else
            {      
                Fields = new List<string>();
                var columnMappingList = Mapping.ColumnMappingList;
                if (columnMappingList != null && keySelectors != null && keySelectors.Length > 0)
                {
                    List<string> excludeFieldList = new List<string>();
                    foreach (var keySelector in keySelectors)
                    {
                        string field = LambdaExpressionParser.GetColumnName<M>(keySelector, Mapping)!;
                        if (!excludeFieldList.Contains(field))
                        {
                            excludeFieldList.Add(field);
                        }
                    }
                    foreach(var columnMapping in columnMappingList)
                    {
                        string columnName = columnMapping.ColumnName!;
                        if (!excludeFieldList.Contains(columnName) && !Fields.Contains(columnName))
                        {
                            Fields.Add(columnName);
                        }
                    }
                }
                else
                {
                    IsAll = true;
                }
            }
        }

        private void Initialize(params Expression<Func<M, object?>>[] keySelectors)
        {
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
            Fields = new List<string>();
            if (keySelectors != null && keySelectors.Length > 0)
            {
                foreach (var keySelector in keySelectors)
                {
                    string field = LambdaExpressionParser.GetColumnName<M>(keySelector, Mapping)!;
                    if (!Fields.Contains(field))
                    {
                        Fields.Add(field);
                    }
                }
            }
            else
            {
                IsAll = true;
            }
        }


        /// <summary>
        /// 创建一个空的字段集合实例（查询全部字段）
        /// Create an empty field collection instance (query all fields)
        /// </summary>
        /// <returns>新的字段集合实例 / A new instance</returns>
        public static QueryFields<M> New
        {
            get
            {
                return new QueryFields<M>();
            }
        }

        /// <summary>
        /// 创建只查询指定字段的字段集合实例
        /// Create a field collection selecting the specified fields
        /// </summary>
        /// <param name="keySelectors">字段选择器表达式数组 / Key selector expressions</param>
        /// <returns>新的字段集合实例 / A new instance</returns>
        public static QueryFields<M> Build(params Expression<Func<M, object?>>[] keySelectors)
        {
            return new QueryFields<M>(keySelectors);
        }

        /// <summary>
        /// 创建字段集合实例
        /// Create a field collection instance
        /// </summary>
        /// <param name="isExclude">
        /// 是否排除指定的字段名不进行查询
        /// Whether to exclude the specified fields from the query
        /// </param>
        /// <param name="keySelectors">字段选择器表达式数组 / Key selector expressions</param>
        /// <returns>新的字段集合实例 / A new instance</returns>
        public static QueryFields<M> Build(bool isExclude, params Expression<Func<M, object?>>[] keySelectors)
        {
            return new QueryFields<M>(isExclude, keySelectors);
        }

        /// <summary>
        /// 以字段选择器追加一个字段（经映射解析为列名，已存在则忽略）
        /// Append a field via key selector (resolved to the mapped column; ignored if already present)
        /// </summary>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <returns>当前实例 / This instance</returns>
        public QueryFields<M> Add(Expression<Func<M, object?>> keySelector)
        {
            string field = LambdaExpressionParser.GetColumnName<M>(keySelector, Mapping)!;
            if (!Fields.Contains(field))
            {
                Fields.Add(field);
            }
            return this;
        }

    }

}
