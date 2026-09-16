using Rampage.Data.Expressions;
using Rampage.ExceptionExtensions;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Query
{
    /// <summary>
    /// order rules
    /// </summary>
    public class QueryOrders
    {

        /// <summary>
        /// Initialize a empty order rule instance
        /// </summary>
        public QueryOrders()
        {
            Orders = new Dictionary<string, OrderType>();
        }

        /// <summary>
        /// Initialize a empty order rule instance
        /// </summary>
        /// <returns></returns>
        public static QueryOrders New
        {
            get
            {
                return new QueryOrders();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static QueryOrders Init(string field, OrderType order)
        {
            QueryOrders orders = new QueryOrders();
            orders = orders.Set(field, order);
            return orders;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, OrderType> Orders
        {
            get; protected set;
        }

        /// <summary>
        /// Add order
        /// </summary>
        /// <param name="field"></param>
        /// <param name="order"></param>
        /// <returns></returns>
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
        /// 排序字段是否为合法标识符（字母/下划线开头；可带一级限定名）
        /// </summary>
        public static bool IsValidOrderField(string? field)
        {
            return !string.IsNullOrWhiteSpace(field) && field.Length <= 128 && OrderFieldRegex.IsMatch(field);
        }

        /// <summary>
        /// Add Ascending order
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual QueryOrders Asc(string field)
        {
            return Set(field, OrderType.Asc);
        }

        /// <summary>
        /// Add Descending order
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual QueryOrders Desc(string field)
        {
            return Set(field, OrderType.Desc);
        }

    }

    /// <summary>
    /// order rules
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public class QueryOrders<M> : QueryOrders
        where M : class
    {

        public TableMapping Mapping
        {
            get;
            private set;
        }

        public QueryOrders() : base()
        {
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
        }

        /// <summary>
        /// Initialize a empty order rule instance
        /// </summary>
        /// <returns></returns>
        public new static QueryOrders<M> New
        {
            get
            {
                return new QueryOrders<M>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keySelector"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static QueryOrders<M> Init<TKey>(Expression<Func<M, TKey>> keySelector, OrderType order)
        {
            QueryOrders<M> orders = new QueryOrders<M>();
            orders = orders.Set(keySelector, order);
            return orders;
        }

        /// <summary>
        /// Add order
        /// </summary>
        /// <param name="keySelector"></param>
        /// <param name="order"></param>
        /// <returns></returns>
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
        /// Add order by mapped property or column name.
        /// </summary>
        public new QueryOrders<M> Set(string field, OrderType order)
        {
            string columnName = Mapping.ResolveColumnName(field);
            base.Set(columnName, order);
            return this;
        }

        /// <summary>
        /// Add ascending order by mapped property or column name.
        /// </summary>
        public new QueryOrders<M> Asc(string field)
        {
            return Set(field, OrderType.Asc);
        }

        /// <summary>
        /// Add descending order by mapped property or column name.
        /// </summary>
        public new QueryOrders<M> Desc(string field)
        {
            return Set(field, OrderType.Desc);
        }

        /// <summary>
        /// Add Ascending order
        /// </summary>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public virtual QueryOrders<M> Asc<TKey>(Expression<Func<M, TKey>> keySelector)
        {
            return Set(keySelector, OrderType.Asc);
        }

        /// <summary>
        /// Add Descending order
        /// </summary>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public virtual QueryOrders<M> Desc<TKey>(Expression<Func<M, TKey>> keySelector)
        {
            return Set(keySelector, OrderType.Desc);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// Ascending order
        /// </summary>
        Asc = 1,
        /// <summary>
        /// Descending order
        /// </summary>
        Desc = -1
    }
}
