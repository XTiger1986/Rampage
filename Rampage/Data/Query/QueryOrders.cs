using Rampage.Data.Expressions;
using Rampage.ExceptionExtensions;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Query
{
    /// <summary>
    /// 查询排序规则集合（字段名 -> 排序方向）
    /// Query ordering rules (field name -> order direction)
    /// </summary>
    public class QueryOrders
    {

        /// <summary>
        /// 初始化一个空的排序规则集合
        /// Initialize an empty ordering rule collection
        /// </summary>
        public QueryOrders()
        {
            Orders = new Dictionary<string, OrderType>();
        }

        /// <summary>
        /// 创建一个空的排序规则集合
        /// Create an empty ordering rule collection
        /// </summary>
        /// <returns>新的排序规则实例 / A new instance</returns>
        public static QueryOrders New
        {
            get
            {
                return new QueryOrders();
            }
        }

        /// <summary>
        /// 创建包含单个排序字段的排序规则集合
        /// Create an ordering rule collection with a single field
        /// </summary>
        /// <param name="field">排序字段名（须为合法标识符）/ Order field name (must be a valid identifier)</param>
        /// <param name="order">排序方向 / Order direction</param>
        /// <returns>新的排序规则实例 / A new instance</returns>
        public static QueryOrders Init(string field, OrderType order)
        {
            QueryOrders orders = new QueryOrders();
            orders = orders.Set(field, order);
            return orders;
        }

        /// <summary>
        /// 排序规则字典（字段名 -> 排序方向），保留插入顺序
        /// Ordering dictionary (field name -> direction); insertion order preserved
        /// </summary>
        public Dictionary<string, OrderType> Orders
        {
            get; protected set;
        }

        /// <summary>
        /// 设置一个排序字段（同名字段覆盖原方向）
        /// Set an order field (an existing field name overwrites its direction)
        /// </summary>
        /// <remarks>非泛型版本没有模型映射可做白名单校验，字段名必须通过 <see cref="IsValidOrderField"/> 的标识符校验以防 ORDER BY 注入。/ The non-generic version has no model mapping for whitelisting, so the field must pass the <see cref="IsValidOrderField"/> identifier check to prevent ORDER BY injection.</remarks>
        /// <param name="field">排序字段名 / Order field name</param>
        /// <param name="order">排序方向 / Order direction</param>
        /// <returns>当前实例 / This instance</returns>
        public virtual QueryOrders Set(string field, OrderType order)
        {
            // 非泛型版本没有模型映射可做白名单，至少要求是合法标识符（可带一级限定名 alias.column），
            // 避免上层把客户端传入的排序字段直接拼进 ORDER BY。需要按模型白名单校验请使用 QueryOrders&lt;M&gt;。
            if (!IsValidOrderField(field))
            {
                throw new SuperException(Globals.EX_KEY_INVALID_ARGUMENT, field ?? string.Empty);
            }
            if (!Orders.ContainsKey(field))
                Orders.Add(field, order);
            else
                Orders[field] = order;
            return this;
        }

        private static readonly System.Text.RegularExpressions.Regex OrderFieldRegex =
            new System.Text.RegularExpressions.Regex(@"^[\p{L}_][\p{L}\p{N}_$#]*(\.[\p{L}_][\p{L}\p{N}_$#]*)?$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// 排序字段是否为合法标识符（字母/下划线开头；可带一级限定名 alias.column；长度不超过 128）
        /// Whether the order field is a valid identifier (letter/underscore leading; one-level qualifier alias.column allowed; max length 128)
        /// </summary>
        /// <param name="field">待校验的排序字段 / Field to validate</param>
        /// <returns>合法返回 true / True when valid</returns>
        public static bool IsValidOrderField(string? field)
        {
            return !string.IsNullOrWhiteSpace(field) && field.Length <= 128 && OrderFieldRegex.IsMatch(field);
        }

        /// <summary>
        /// 添加升序排序
        /// Add an ascending order
        /// </summary>
        /// <param name="field">排序字段名 / Order field name</param>
        /// <returns>当前实例 / This instance</returns>
        public virtual QueryOrders Asc(string field)
        {
            return Set(field, OrderType.Asc);
        }

        /// <summary>
        /// 添加降序排序
        /// Add a descending order
        /// </summary>
        /// <param name="field">排序字段名 / Order field name</param>
        /// <returns>当前实例 / This instance</returns>
        public virtual QueryOrders Desc(string field)
        {
            return Set(field, OrderType.Desc);
        }

    }

    /// <summary>
    /// 强类型排序规则集合（经模型映射把选择器解析为实际列名）
    /// Strongly-typed ordering rules (selectors resolved to real column names via model mapping)
    /// </summary>
    /// <typeparam name="M">模型类型 / Model type</typeparam>
    public class QueryOrders<M> : QueryOrders
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
        /// 初始化一个空的强类型排序规则集合
        /// Initialize an empty strongly-typed ordering collection
        /// </summary>
        public QueryOrders() : base()
        {
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
        }

        /// <summary>
        /// 创建一个空的强类型排序规则集合
        /// Create an empty strongly-typed ordering collection
        /// </summary>
        /// <returns>新的排序规则实例 / A new instance</returns>
        public new static QueryOrders<M> New
        {
            get
            {
                return new QueryOrders<M>();
            }
        }

        /// <summary>
        /// 创建包含单个排序字段的强类型排序规则集合
        /// Create a strongly-typed ordering collection with a single field
        /// </summary>
        /// <typeparam name="TKey">字段类型 / Key type</typeparam>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <param name="order">排序方向 / Order direction</param>
        /// <returns>新的排序规则实例 / A new instance</returns>
        public static QueryOrders<M> Init<TKey>(Expression<Func<M, TKey>> keySelector, OrderType order)
        {
            QueryOrders<M> orders = new QueryOrders<M>();
            orders = orders.Set(keySelector, order);
            return orders;
        }

        /// <summary>
        /// 以字段选择器设置排序字段（解析失败时静默忽略，同名列覆盖原方向）
        /// Set an order field via key selector (silently ignored when resolution fails; an existing column overwrites its direction)
        /// </summary>
        /// <typeparam name="TKey">字段类型 / Key type</typeparam>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <param name="order">排序方向 / Order direction</param>
        /// <returns>当前实例 / This instance</returns>
        public virtual QueryOrders<M> Set<TKey>(Expression<Func<M, TKey>> keySelector, OrderType order)
        {
            string? columnName = LambdaExpressionParser.GetColumnName<M>(keySelector, Mapping);
            if (string.IsNullOrEmpty(columnName))
            {
                return this;
            }
            if (!Orders.ContainsKey(columnName))
                Orders.Add(columnName, order);
            else
                Orders[columnName] = order;
            return this;
        }

        /// <summary>
        /// 按映射的属性名或列名设置排序字段（经 <see cref="Mapping"/> 解析为实际列名后再校验合法性）
        /// Set an order field by mapped property or column name (resolved via <see cref="Mapping"/> then validated)
        /// </summary>
        /// <param name="field">属性名或列名 / Property or column name</param>
        /// <param name="order">排序方向 / Order direction</param>
        /// <returns>当前实例 / This instance</returns>
        public new QueryOrders<M> Set(string field, OrderType order)
        {
            string columnName = Mapping.ResolveColumnName(field);
            base.Set(columnName, order);
            return this;
        }

        /// <summary>
        /// 按属性名或列名添加升序排序
        /// Add an ascending order by mapped property or column name
        /// </summary>
        /// <param name="field">属性名或列名 / Property or column name</param>
        /// <returns>当前实例 / This instance</returns>
        public new QueryOrders<M> Asc(string field)
        {
            return Set(field, OrderType.Asc);
        }

        /// <summary>
        /// 按属性名或列名添加降序排序
        /// Add a descending order by mapped property or column name
        /// </summary>
        /// <param name="field">属性名或列名 / Property or column name</param>
        /// <returns>当前实例 / This instance</returns>
        public new QueryOrders<M> Desc(string field)
        {
            return Set(field, OrderType.Desc);
        }

        /// <summary>
        /// 以字段选择器添加升序排序
        /// Add an ascending order via key selector
        /// </summary>
        /// <typeparam name="TKey">字段类型 / Key type</typeparam>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <returns>当前实例 / This instance</returns>
        public virtual QueryOrders<M> Asc<TKey>(Expression<Func<M, TKey>> keySelector)
        {
            return Set(keySelector, OrderType.Asc);
        }

        /// <summary>
        /// 以字段选择器添加降序排序
        /// Add a descending order via key selector
        /// </summary>
        /// <typeparam name="TKey">字段类型 / Key type</typeparam>
        /// <param name="keySelector">字段选择器 / Key selector</param>
        /// <returns>当前实例 / This instance</returns>
        public virtual QueryOrders<M> Desc<TKey>(Expression<Func<M, TKey>> keySelector)
        {
            return Set(keySelector, OrderType.Desc);
        }

    }

    /// <summary>
    /// 排序方向
    /// Order direction
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// 升序
        /// Ascending order
        /// </summary>
        Asc = 1,
        /// <summary>
        /// 降序
        /// Descending order
        /// </summary>
        Desc = -1
    }
}
