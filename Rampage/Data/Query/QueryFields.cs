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
        /// 
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
        /// 
        /// </summary>
        public string? TableAlias
        {
            get;
            set;
        }

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        /// <param name="resultFields"></param>
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
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public QueryFields Add(string field)
        {
            if (!Fields.Contains(field))
            {
                Fields.Add(field);
            }
            return this;
        }

        /// <summary>
        /// Initialize a "QueryFields" instance of Specify query all fields
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        public static QueryFields Init(bool all = true)
        {
            return new QueryFields(all);
        }

        /// <summary>
        /// Initialize a "QueryFields" instance of Specify query some fields
        /// </summary>
        /// <param name="resultFields"></param>
        /// <returns></returns>
        public static QueryFields Init(params string[] resultFields)
        {
            return new QueryFields(resultFields);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public class QueryFields<M> : QueryFields
        where M : class
    {

        public TableMapping Mapping
        {
            get;
            private set;
        }

        public QueryFields(params Expression<Func<M, object?>>[] keySelectors)
        {
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
            Initialize(keySelectors);
        }

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
        /// Initialize a empty query fields instance
        /// </summary>
        /// <returns></returns>
        public static QueryFields<M> New
        {
            get
            {
                return new QueryFields<M>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keySelectors"></param>
        /// <returns></returns>
        public static QueryFields<M> Build(params Expression<Func<M, object?>>[] keySelectors)
        {
            return new QueryFields<M>(keySelectors);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isExclude">
        /// 是否排除指定的字段名不进行查询
        /// Whether to exclude specified field names from the query
        /// </param>
        /// <param name="keySelectors"></param>
        /// <returns></returns>
        public static QueryFields<M> Build(bool isExclude, params Expression<Func<M, object?>>[] keySelectors)
        {
            return new QueryFields<M>(isExclude, keySelectors);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keySelector"></param>
        /// <returns></returns>
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
