using Rampage.Data.Expressions;
using Rampage.Utilities;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Query
{
    /// <summary>
    /// 查询包装器抽象基类，承载 SELECT 字段、分组、排序、限量与命令超时等查询要素
    /// Abstract base query wrapper carrying SELECT fields, grouping, ordering, limit and command timeout
    /// </summary>
    /// <typeparam name="M">模型类型 / Model type</typeparam>
    public abstract class QueryWrapper<M> where M : class
    {

        /// <summary>
        /// <see cref="NoLock"/> 的后备字段（默认 true）
        /// Backing field of <see cref="NoLock"/> (default true)
        /// </summary>
        protected bool _nolock = true;
        /// <summary>
        /// 是否强制不加锁查询（默认 true）
        /// Whether to query without locking (default true)
        /// </summary>
        public virtual bool NoLock
        {
            get { return _nolock; }
            set { _nolock = value; }
        }

        /// <summary>
        /// 查询命令超时秒数，为 null 时使用底层提供程序默认值
        /// Command timeout in seconds; null uses the underlying provider default
        /// </summary>
        public virtual int? CommandTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// 要查询的字段集合（由 <see cref="Select(QueryFields)"/> 等方法设置）
        /// Fields to query (set by <see cref="Select(QueryFields)"/> and other Select overloads)
        /// </summary>
        public QueryFields? Fields
        {
            get;
            protected set;
        }

        /// <summary>
        /// 自定义要查询的字段部分的 SQL 语句，若定义了此参数，则 <see cref="Fields"/> 参数无效
        /// Raw SQL fragment for the SELECT field list; when set, <see cref="Fields"/> is ignored
        /// </summary>
        public string? FieldsSql
        {

            get; 
            protected set;
        }

        /// <summary>
        /// 要查询的数据条数（分页查询时此项无效）
        /// Maximum number of rows to query (ignored for paged queries)
        /// </summary>
        public long? Limit
        {
            get;
            set;
        }

        /// <summary>
        /// 查询排序规则集合
        /// Ordering rules for the query
        /// </summary>
        public QueryOrders<M> Orders
        {
            get; 
            set;
        }

        /// <summary>
        /// GROUP BY 字段名列表（已解析为实际列名并去重）
        /// GROUP BY column names (resolved to real column names and deduplicated)
        /// </summary>
        public List<string> GroupByList
        {
            get;
            protected set;
        }

        /// <summary>
        /// 模型与数据表的映射信息（构造时按 <typeparamref name="M"/> 自动获取）
        /// Model-to-table mapping (resolved from <typeparamref name="M"/> at construction)
        /// </summary>
        public TableMapping Mapping
        {
            get;
            protected set;
        }

        /// <summary>
        /// 初始化一个空的查询包装器
        /// Initialize an empty query wrapper
        /// </summary>
        public QueryWrapper()
        {
            Orders = new QueryOrders<M>();
            GroupByList = new List<string>();
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
        }


        /// <summary>
        /// 直接指定 SELECT 字段部分的原始 SQL 片段，建议改用表达式或 <see cref="QueryFields"/> 重载
        /// Specify a raw SQL fragment as the SELECT field list; prefer the expression or <see cref="QueryFields"/> overloads
        /// </summary>
        /// <remarks>设置后 <see cref="Fields"/> 将被清空并不再生效。/ After this call <see cref="Fields"/> is cleared and ignored.</remarks>
        /// <param name="fieldsSql">原始字段 SQL；注意：传入不可信输入存在 SQL 注入风险 / Raw field SQL; attention: risk of SQL injection when passing untrusted input</param>
        /// <returns>当前包装器 / This wrapper</returns>
        [Obsolete("Raw SQL field lists may cause SQL injection. Use Select(QueryFields), Select(string[]), or expression overloads.")]
        public virtual QueryWrapper<M> Select(string fieldsSql)
        {
            FieldsSql = fieldsSql;
            Fields = null;
            return this;
        }

        /// <summary>
        /// 按属性名或列名指定要查询的字段（逐一经 <see cref="Mapping"/> 解析为实际列名，不会拼接原始 SQL）
        /// Specify query fields by property or column names (each resolved to the mapped column via <see cref="Mapping"/>; no raw SQL is embedded)
        /// </summary>
        /// <param name="fields">属性名或列名数组，为 null 或空数组时表示查询全部字段 / Property or column names; null or empty selects all fields</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public virtual QueryWrapper<M> Select(string[] fields)
        {
            FieldsSql = null;
            if (fields == null || fields.Length == 0)
            {
                Fields = QueryFields.Init();
                return this;
            }
            var resolvedFields = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                resolvedFields[i] = Mapping.ResolveColumnName(fields[i]);
            }
            Fields = new QueryFields(resolvedFields);
            return this;
        }

        /// <summary>
        /// 指定要查询的字段集合
        /// Specify the query field collection
        /// </summary>
        /// <param name="fields">字段集合 / Field collection</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public virtual QueryWrapper<M> Select(QueryFields fields)
        {
            Fields = fields;
            FieldsSql = null;
            return this;
        }

        /// <summary>
        /// 以 lambda 字段选择器指定要查询的字段
        /// Specify query fields with lambda key selectors
        /// </summary>
        /// <param name="keySelectors">字段选择器表达式数组 / Key selector expressions</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public virtual QueryWrapper<M> Select(params Expression<Func<M, object?>>[] keySelectors)
        {
            Fields = new QueryFields<M>(keySelectors);
            FieldsSql = null;
            return this;
        }

        /// <summary>
        /// 按属性名或列名指定 GROUP BY 字段（经映射解析为实际列名并去重）
        /// Specify GROUP BY columns by property or column names (resolved to real columns and deduplicated)
        /// </summary>
        /// <param name="columns">属性名或列名数组 / Property or column names</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public virtual QueryWrapper<M> GroupBy(params string[] columns)
        {
            if (columns.Any())
            {
                foreach (string column in columns)
                {
                    string safeColumn = Mapping.ResolveColumnName(column);
                    if (!GroupByList.Contains(safeColumn))
                    {
                        GroupByList.Add(safeColumn);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// 以 lambda 选择器指定 GROUP BY 字段（经映射解析为实际列名并去重）
        /// Specify a GROUP BY column with a lambda key selector (resolved to the mapped column)
        /// </summary>
        /// <typeparam name="TKey">字段类型 / Key type</typeparam>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public virtual QueryWrapper<M> GroupBy<TKey>(Expression<Func<M, TKey>> keySelector)
        {
            string columnName = LambdaExpressionParser.GetColumnName<M>(keySelector, Mapping)!;
            if (!GroupByList.Contains(columnName))
            {
                GroupByList.Add(columnName);
            }
            return this;
        }

        /// <summary>
        /// 整体替换当前排序规则集合
        /// Replace the whole ordering rule collection
        /// </summary>
        /// <param name="orders">排序规则集合 / Ordering rules</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public virtual QueryWrapper<M> Order(QueryOrders<M> orders)
        {
            Orders = orders;
            return this;
        }

        /// <summary>
        /// 追加一个升序排序字段
        /// Add an ascending order field
        /// </summary>
        /// <typeparam name="TKey">字段类型 / Key type</typeparam>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public virtual QueryWrapper<M> OrderBy<TKey>(Expression<Func<M, TKey>> keySelector)
        {
            Orders.Asc(keySelector);
            return this;
        }

        /// <summary>
        /// 追加一个降序排序字段
        /// Add a descending order field
        /// </summary>
        /// <typeparam name="TKey">字段类型 / Key type</typeparam>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public virtual QueryWrapper<M> OrderByDescending<TKey>(Expression<Func<M, TKey>> keySelector)
        {
            Orders.Desc(keySelector);
            return this;
        }


        /// <summary>
        /// 已构建的最终 SQL 信息（由执行器经 <see cref="SetBuiltSql(SqlInfo)"/> 写入以便复用）
        /// Built final SQL info (written by the executor via <see cref="SetBuiltSql(SqlInfo)"/> for reuse)
        /// </summary>
        public SqlInfo? BuiltSql
        {
            get;
            private set;
        }

        /// <summary>
        /// 写入已构建的 SQL 信息，供后续执行复用
        /// Store the built SQL info for reuse by subsequent executions
        /// </summary>
        /// <param name="sql">SQL 信息 / SQL info</param>
        public void SetBuiltSql(SqlInfo sql)
        {
            BuiltSql = sql;
        }

    }
}
