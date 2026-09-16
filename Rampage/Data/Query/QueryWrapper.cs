using Rampage.Data.Expressions;
using Rampage.Utilities;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Query
{
    /// <summary>
    /// 查询包装器
    /// </summary>
    public abstract class QueryWrapper<M> where M : class
    {

        protected bool _nolock = true;
        /// <summary>
        /// 是否强制不加锁查询
        /// </summary>
        public virtual bool NoLock
        {
            get { return _nolock; }
            set { _nolock = value; }
        }

        /// <summary>
        /// 查询超时秒数
        /// </summary>
        public virtual int? CommandTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// 要查询的字段
        /// </summary>
        public QueryFields? Fields
        {
            get;
            protected set;
        }

        /// <summary>
        /// 自定义要查询的字段部分的SQL语句，若定义了此参数，则Fields参数无效
        /// </summary>
        public string? FieldsSql
        {

            get; 
            protected set;
        }

        /// <summary>
        /// 要查询的数据条数 (当进行分页查询时此项无效)
        /// </summary>
        public long? Limit
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public QueryOrders<M> Orders
        {
            get; 
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<string> GroupByList
        {
            get;
            protected set;
        }

        public TableMapping Mapping
        {
            get;
            protected set;
        }

        /// <summary>
        /// Initialize a empty Database query wrapper
        /// </summary>
        public QueryWrapper()
        {
            Orders = new QueryOrders<M>();
            GroupByList = new List<string>();
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
        }


        /// <summary>
        /// Specify raw SELECT field SQL. Prefer expression or <see cref="QueryFields"/> overloads.
        /// </summary>
        /// <param name="fieldsSql">Attention: There is a risk of SQL injection when passing untrusted input.</param>
        /// <returns></returns>
        [Obsolete("Raw SQL field lists may cause SQL injection. Use Select(QueryFields), Select(string[]), or expression overloads.")]
        public virtual QueryWrapper<M> Select(string fieldsSql)
        {
            FieldsSql = fieldsSql;
            Fields = null;
            return this;
        }

        /// <summary>
        /// Specify query fields sql
        /// </summary>
        /// <param name="fieldsSql">Attention: There is a risk of SQL injection</param>
        /// <returns></returns>
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
        /// Specify query fields
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public virtual QueryWrapper<M> Select(QueryFields fields)
        {
            Fields = fields;
            FieldsSql = null;
            return this;
        }

        /// <summary>
        /// Specify query fields
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public virtual QueryWrapper<M> Select(params Expression<Func<M, object?>>[] keySelectors)
        {
            Fields = new QueryFields<M>(keySelectors);
            FieldsSql = null;
            return this;
        }

        /// <summary>
        /// Specify "group by" columns
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
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
        /// Specify "group by" columns
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        /// <returns></returns>
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
        /// set orders
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public virtual QueryWrapper<M> Order(QueryOrders<M> orders)
        {
            Orders = orders;
            return this;
        }

        /// <summary>
        /// Add Ascending order
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public virtual QueryWrapper<M> OrderBy<TKey>(Expression<Func<M, TKey>> keySelector)
        {
            Orders.Asc(keySelector);
            return this;
        }

        /// <summary>
        /// Add Descending order
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public virtual QueryWrapper<M> OrderByDescending<TKey>(Expression<Func<M, TKey>> keySelector)
        {
            Orders.Desc(keySelector);
            return this;
        }


        /// <summary>
        /// 
        /// </summary>
        public SqlInfo? BuiltSql
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        public void SetBuiltSql(SqlInfo sql)
        {
            BuiltSql = sql;
        }

    }
}
