using Rampage.Data.Client;
using Rampage.Data.Expressions;
using Rampage.Data.Filter;
using Rampage.Data.Query;
using Rampage.ExceptionExtensions;
using Rampage.Extensions;
using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Collections;
using System.Text;

namespace Rampage.Data
{
    /// <summary>
    /// Sql语句生成类
    /// Sql statements Generator
    /// </summary>
    public abstract class SqlTemplate
    {

        public const string EX_KEY_FailedToGenerateSQL = "FailedTo_Generate_SQL";

        public SqlTemplate(DatabaseType databaseType)
        {
            DatabaseType = databaseType;
            DbParameterValueHandler = DatabaseFactory.DbParameterValueHandler;
        }

        /// <summary>
        /// 数据库类型
        /// Database Type
        /// </summary>
        public virtual DatabaseType DatabaseType { get; protected set; }

        private IDbParameterValueHandler DbParameterValueHandler
        {
            get;
            set;
        }

        /// <summary>
        /// 查询当前数据库时间的SQL语句
        /// SQL statement to query the current database time
        /// </summary>
        /// <returns></returns>
        public virtual string GetNowSql()
        {
            switch (DatabaseType)
            {
                case DatabaseType.MySQL:
                    return "SELECT NOW() CurrentTime";
                case DatabaseType.PostgreSQL:
                    return "SELECT CURRENT_TIMESTAMP AS CurrentTime";
                case DatabaseType.Oracle:
                    return "SELECT SYSDATE AS CurrentTime FROM DUAL";
                case DatabaseType.SqlServer:
                    return "SELECT GETDATE() CurrentTime";
                case DatabaseType.Sqlite:
                    return "SELECT datetime('now') CurrentTime";
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Format the specified field name as a parameter name
        /// </summary>
        /// <param name="fieldName">ProductId</param>
        /// <returns>For example: @ProductId</returns>
        public virtual string FormatParameter(string fieldName)
        {
            switch (DatabaseType)
            {
                case DatabaseType.Oracle:
                    if (!StringTool.StartsWith(fieldName, ":"))
                    {
                        return string.Concat(":", fieldName);
                    }
                    break;
                default:
                    if (!StringTool.StartsWith(fieldName, "@"))
                    {
                        return string.Concat("@", fieldName);
                    }
                    break;
            }         
            return fieldName;
        }

        /// <summary>
        /// 合法的裸标识符：字母/下划线开头，后接字母、数字、下划线、$、#（允许 Unicode 字母以兼容中文列名）
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex SafeIdentifierRegex =
            new System.Text.RegularExpressions.Regex(@"^[\p{L}_][\p{L}\p{N}_$#]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// 是否为无需引号包裹即可安全拼接的标识符
        /// </summary>
        public static bool IsSafeIdentifier(string? identifier)
        {
            return !string.IsNullOrEmpty(identifier) && identifier.Length <= 128 && SafeIdentifierRegex.IsMatch(identifier);
        }

        /// <summary>
        /// 取得当前数据库的标识符引号（open/close）；不支持引号的数据库返回 null
        /// </summary>
        protected virtual (char Open, char Close)? GetIdentifierQuotes()
        {
            switch (DatabaseType)
            {
                case DatabaseType.MySQL:
                    return ('`', '`');
                case DatabaseType.PostgreSQL:
                case DatabaseType.Oracle:
                case DatabaseType.Sqlite:
                    return ('"', '"');
                case DatabaseType.SqlServer:
                    return ('[', ']');
                default:
                    return null;
            }
        }

        /// <summary>
        /// 安全地格式化单个标识符：始终引号包裹并转义内部的闭合引号，使任何输入都无法逃逸出标识符位置。
        /// 支持 "schema.table" / "alias.column" 形式（各段分别处理）；已带引号的输入会被剥离后重新转义，
        /// 因此形如 `a`, (SELECT ...) ` 的“伪引号”注入不再能原样透传。
        /// </summary>
        protected virtual string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException("SQL identifier is required.", nameof(identifier));
            }
            identifier = identifier.Trim();

            (char Open, char Close)? quotes = GetIdentifierQuotes();
            if (quotes == null)
            {
                // 未知数据库：只放行裸标识符
                if (!IsSafeIdentifier(identifier))
                {
                    throw new ArgumentException($"Unsafe SQL identifier '{identifier}'.", nameof(identifier));
                }
                return identifier;
            }

            char open = quotes.Value.Open;
            char close = quotes.Value.Close;

            // 多段限定名（未带引号时）：alias.column / schema.table
            if (identifier.IndexOf(open) < 0 && identifier.IndexOf(close) < 0 && identifier.IndexOf('.') > 0)
            {
                string[] parts = identifier.Split('.');
                if (parts.Length <= 3 && parts.All(IsSafeIdentifier))
                {
                    return string.Join(".", parts.Select(p => string.Concat(open, p, close)));
                }
            }

            // 已带引号：剥离最外层后重新处理
            if (identifier.Length >= 2 && identifier[0] == open && identifier[identifier.Length - 1] == close)
            {
                identifier = identifier.Substring(1, identifier.Length - 2);
                // 还原内部已转义的闭合引号（`` / "" / ]]）
                identifier = identifier.Replace(new string(close, 2), close.ToString());
            }

            if (identifier.IndexOfAny(new[] { '\0', '\r', '\n' }) >= 0)
            {
                throw new ArgumentException("SQL identifier contains control characters.", nameof(identifier));
            }

            string escaped = identifier.Replace(close.ToString(), new string(close, 2));
            return string.Concat(open, escaped, close);
        }

        /// <summary>
        /// 格式化字段名（始终引号包裹并转义，防止排序字段/列名注入）
        /// Format field name (always quoted and escaped to prevent identifier injection)
        /// </summary>
        /// <param name="columnName">For example: data</param>
        /// <returns>For example: `data`</returns>
        public virtual string FormatColumn(string columnName)
        {
            return QuoteIdentifier(columnName);
        }

        /// <summary>
        /// 格式化表名（始终引号包裹并转义）
        /// Format table name (always quoted and escaped)
        /// </summary>
        /// <param name="tableName">For example: Product</param>
        /// <returns>For example: `Product`</returns>
        public virtual string FormatTable(string tableName)
        {
            return QuoteIdentifier(tableName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="mapping"></param>
        /// <returns>For example: `id` `Id`, `name` `Name`, `product_id` ProductId</returns>
        public virtual string BuildQueryFieldsString<M>(QueryFields? fields, TableMapping? mapping = null)
            where M : class
        {
            mapping = mapping ?? ModelSchemaStore.GetModelMapping<M>()!;

            List<FieldMapping>? columnMappingList = GetQueryFieldsMappingList<M>(fields, mapping);
            if (columnMappingList != null && columnMappingList.Count > 0)
            {
                // use alias field name
                StringBuilder s = new StringBuilder();
                foreach (FieldMapping property in columnMappingList)
                {
                    string formattedColumn = FormatColumn(property.ColumnName!);
                    string formattedAlias = FormatColumn(property.PropertyName!);
                    if (fields != null && !string.IsNullOrEmpty(fields.TableAlias))
                    {
                        s.Append($"{fields.TableAlias}.{formattedColumn} {formattedAlias},");
                    }
                    else
                    {
                        s.Append($"{formattedColumn} {formattedAlias},");
                    }
                }
                s = s.Remove(s.Length - 1, 1);
                return s.ToString();
            }
            else
            {
                return "*";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupByList"></param>
        /// <returns>For example: `category_id`, `status`</returns>
        public virtual string BuildGroupByString(List<string> groupByList)
        {
            if (groupByList.Any())
            {
                string text = string.Empty;
                foreach (string columnName in groupByList)
                {
                    string safeColumn = FormatColumn(columnName);
                    text += $"{safeColumn},";
                }
                text = text.TrimEnd(',');

                return text;
            }
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orders"></param>
        /// <returns>For example: `count` desc, `create_time` desc</returns>
        public virtual string BuildOrderString(QueryOrders? orders)
        {
            if (orders == null || orders.Orders == null || orders.Orders.Count == 0)
                return string.Empty;

            string orderString = string.Empty;

            foreach (var order in orders.Orders)
            {
                string columnName = order.Key;
                OrderType orderType = order.Value;
                string safeColumn = FormatColumn(columnName);

                orderString += $" {safeColumn} {orderType},";
            }
            orderString = orderString.TrimEnd(',');
            return orderString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="sqlInfo"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public virtual string? BuildConditionString<M>(LambdaWrapper<M> wrapper, SqlInfo sqlInfo, TableMapping? mapping = null)
            where M : class
        {
            mapping = mapping ?? ModelSchemaStore.GetModelMapping<M>()!;

            if (!sqlInfo.ParametersInitialized)
            {
                sqlInfo.InitializeParameters();
            }
            string? condition = null;
            if (wrapper.Condition != null)
            {
                var conditionParser = LambdaExpressionParser.GetCondition<M>(wrapper.Condition, this, mapping);
                condition = conditionParser.GetConditionString();
                foreach (var kv in conditionParser.Parameters)
                {
                    LambdaConditionParameter conditionParameter = kv.Value;
                    condition = AddConditionParameterAvoidCollision(
                        sqlInfo,
                        condition,
                        kv.Key,
                        DbParameterValueHandler.Handle(conditionParameter.Value, mapping, conditionParameter.Column));
                }
            }

            return condition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="sqlInfo"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public virtual string? BuildConditionString<M>(FilterWrapper<M> wrapper, SqlInfo sqlInfo, TableMapping? mapping = null) where M : class
        {
            mapping = mapping ?? ModelSchemaStore.GetModelMapping<M>()!;

            if (!sqlInfo.ParametersInitialized)
            {
                sqlInfo.InitializeParameters();
            }
            string? condition = null;
            if (wrapper.Filter != null)
            {
                var conditionParser = new FilterParser(wrapper.Filter, mapping, this);
                condition = conditionParser.GetConditionString();
                foreach (var pkv in conditionParser.Parameters)
                {
                    FilterConditionParameter conditionParameter = pkv.Value;
                    condition = AddConditionParameterAvoidCollision(
                        sqlInfo,
                        condition,
                        pkv.Key,
                        DbParameterValueHandler.Handle(conditionParameter.Value, mapping, conditionParameter.Column));
                }
            }
            return condition;
        }

        /// <summary>
        /// UPDATE SET 与 WHERE 同时引用同列时（如 CAS：SET power=@power WHERE power=@power），
        /// 条件参数改用唯一名并回写 SQL，避免 Dictionary 重复键。
        /// </summary>
        private string? AddConditionParameterAvoidCollision(SqlInfo sqlInfo, string? conditionSql, string key, object? value)
        {
            IDictionary<string, object?>? existing = sqlInfo.GetParameters();
            string uniqueKey = key;
            int suffix = 0;
            while (existing != null && existing.ContainsKey(uniqueKey))
            {
                suffix++;
                uniqueKey = $"{key}_c{suffix}";
            }

            if (uniqueKey != key && !string.IsNullOrEmpty(conditionSql))
            {
                conditionSql = ReplaceSqlParameterRef(conditionSql!, FormatParameter(key), FormatParameter(uniqueKey));
            }

            sqlInfo.AddParameter(uniqueKey, value);
            return conditionSql;
        }

        /// <summary>替换参数占位符，避免 @power 误伤 @power_score。</summary>
        private static string ReplaceSqlParameterRef(string sql, string oldRef, string newRef)
        {
            if (string.IsNullOrEmpty(sql) || string.Equals(oldRef, newRef, StringComparison.Ordinal))
            {
                return sql;
            }

            System.Text.RegularExpressions.Regex regex = new(
                System.Text.RegularExpressions.Regex.Escape(oldRef) + @"(?![A-Za-z0-9_])",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            return regex.Replace(sql, newRef);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="model"></param>
        /// <param name="sqlInfo"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public virtual string BuildIdCondition<M>(M model, SqlInfo sqlInfo, TableMapping? mapping = null) where M : class
        {
            mapping = mapping ?? ModelSchemaStore.GetModelMapping<M>()!;
            string primaryKey = mapping.PrimaryKey!;
            string safePrimaryKey = FormatColumn(primaryKey);
            string primaryKeyParam = FormatParameter(primaryKey);

            FieldMapping pkMapping = mapping.GetColumnMapping(primaryKey)!;
            if (!sqlInfo.ParametersInitialized)
            {
                sqlInfo.InitializeParameters();
            }
            object pkValue = EmitTool.GetPropertyValue(model, pkMapping.PropertyName!)!;
            sqlInfo.AddParameter(primaryKey, pkValue);

            return $"{safePrimaryKey} = {primaryKeyParam}";
        }

        /// <summary>
        /// Build a parameterized "search string contained in column" condition.
        /// </summary>
        public virtual string BuildStringInColumnCondition(string str, string columnName, SqlInfo sqlInfo, string paramName = "SearchStr", bool ignoreCase = false)
        {
            if (!sqlInfo.ParametersInitialized)
            {
                sqlInfo.InitializeParameters();
            }
            sqlInfo.AddParameter(paramName, str);
            return BuildStringInColumnConditionWithParameter(FormatParameter(paramName), columnName, ignoreCase);
        }

        /// <summary>
        /// Build condition using an already-formatted parameter reference (no literal embedding).
        /// </summary>
        public virtual string BuildStringInColumnConditionWithParameter(string formattedParam, string columnName, bool ignoreCase = false)
        {
            string safeColumn = FormatColumn(columnName);
            switch (DatabaseType)
            {
                case DatabaseType.MySQL:
                case DatabaseType.Sqlite:
                    if (ignoreCase)
                    {
                        return string.Format("INSTR(LOWER({0}), LOWER({1})) > 0", safeColumn, formattedParam);
                    }
                    return string.Format("INSTR({0}, {1}) > 0", safeColumn, formattedParam);
                case DatabaseType.SqlServer:
                    if (ignoreCase)
                    {
                        return string.Format("CHARINDEX(LOWER({0}), LOWER({1})) > 0", formattedParam, safeColumn);
                    }
                    return string.Format("CHARINDEX({0}, {1}) > 0", formattedParam, safeColumn);
                case DatabaseType.Oracle:
                    if (ignoreCase)
                    {
                        return string.Format("INSTR(LOWER({0}), LOWER({1})) > 0", safeColumn, formattedParam);
                    }
                    return string.Format("INSTR({0}, {1}) > 0", safeColumn, formattedParam);
                case DatabaseType.PostgreSQL:
                default:
                    if (ignoreCase)
                    {
                        return string.Format("POSITION(LOWER({0}) IN LOWER({1})) > 0", formattedParam, safeColumn);
                    }
                    return string.Format("POSITION({0} IN {1}) > 0", formattedParam, safeColumn);
            }
        }

        /// <summary>
        /// Build a parameterized IN condition for the given value list.
        /// </summary>
        public virtual string BuildColumnInListCondition(IEnumerable list, string columnName, SqlInfo sqlInfo, string paramPrefix = "InList")
        {
            string safeColumnName = FormatColumn(columnName);
            if (list == null || !list.Cast<object?>().Any())
            {
                return "1 = 2";
            }
            string inSql = sqlInfo.AddInParameters(list, FormatParameter, paramPrefix);
            return $"{safeColumnName} IN ({inSql})";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual string BuildSqlOfSelect(QueryOptions options)
        {
            string tableName = options.TableName!;
            string safeTableName = FormatTable(tableName);
            string selectPart = string.IsNullOrWhiteSpace(options.Select) ? "*" : options.Select;
            string where = string.IsNullOrWhiteSpace(options.Condition) ? string.Empty : $" WHERE {options.Condition}";
            string groupBy = string.IsNullOrWhiteSpace(options.GroupBy) ? string.Empty : $" GROUP BY {options.GroupBy}";
            string orderBy = string.IsNullOrWhiteSpace(options.OrderBy) ? string.Empty : $" ORDER BY {options.OrderBy}";

            string sql;
            string limitString;
            switch (DatabaseType)
            {
                case DatabaseType.SqlServer:
                    if (options.Limit != null)
                    {
                        if (options.Offset != null)
                        {
                            long endOffset = options.Offset.Value + options.Limit.Value;
                            string rowNumberOrder = string.IsNullOrWhiteSpace(options.OrderBy) ? "(SELECT NULL)" : options.OrderBy;
                            sql = $"SELECT {selectPart} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {rowNumberOrder}) _RowNum_, {selectPart} FROM {safeTableName}{where}{groupBy}) _T_ WHERE _RowNum_ > {options.Offset.Value} AND _RowNum_ <= {endOffset};";
                        }
                        else
                        {
                            sql = $"SELECT TOP {options.Limit} {selectPart} FROM {safeTableName}{where}{groupBy}{orderBy};";
                        }
                    }
                    else
                    {
                        sql = $"SELECT {selectPart} FROM {safeTableName}{where}{groupBy}{orderBy};";
                    }
                    break;
                case DatabaseType.PostgreSQL:
                    if (options.Limit != null && options.Offset != null)
                    {
                        limitString = $" LIMIT {options.Limit} OFFSET {options.Offset}";
                    }
                    else if (options.Limit != null)
                    {
                        limitString = $" LIMIT {options.Limit}";
                    }
                    else
                    {
                        limitString = string.Empty;
                    }
                    sql = $"SELECT {selectPart} FROM {safeTableName}{where}{groupBy}{orderBy}{limitString};";
                    break;
                case DatabaseType.Oracle:
                    if (options.Limit != null && options.Offset != null)
                    {
                        limitString = $" OFFSET {options.Offset} ROWS FETCH NEXT {options.Limit} ROWS ONLY";
                    }
                    else if (options.Limit != null)
                    {
                        limitString = $" FETCH FIRST {options.Limit} ROWS ONLY";
                    }
                    else
                    {
                        limitString = string.Empty;
                    }
                    sql = $"SELECT {selectPart} FROM {safeTableName}{where}{groupBy}{orderBy}{limitString};";
                    break;
                case DatabaseType.MySQL:
                case DatabaseType.Sqlite:
                default:
                    if (options.Limit != null && options.Offset != null)
                    {
                        limitString = $" LIMIT {options.Offset},{options.Limit}";
                    }
                    else if (options.Limit != null)
                    {
                        limitString = $" LIMIT {options.Limit}";
                    }
                    else
                    {
                        limitString = string.Empty;
                    }
                    sql = $"SELECT {selectPart} FROM {safeTableName}{where}{groupBy}{orderBy}{limitString};";
                    break;
            }

            return sql;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="options"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public virtual string BuildSqlOfSelect<M>(QueryOptions<M> options, TableMapping? mapping = null) where M : class
        {
            mapping = mapping ?? ModelSchemaStore.GetModelMapping<M>()!;

            QueryOptions selectOptions = new QueryOptions
            {
                TableName = mapping.TableName,
                Select = options.Select,
                Condition = options.Condition,
                GroupBy = options.GroupBy,
                OrderBy = options.OrderBy,
                Limit = options.Limit,
                Offset = options.Offset
            };

            return BuildSqlOfSelect(selectOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="pagerInfo">pager info if select page list</param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfSelect<M>(FilterWrapper<M>? wrapper = null, PagerInfo? pagerInfo = null) 
            where M : class
        {
            return BuildSelectSql<M>(wrapper, pagerInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="pagerInfo">pager info if select page list</param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfSelect<M>(LambdaWrapper<M> wrapper, PagerInfo? pagerInfo = null)
            where M : class
        {
            return BuildSelectSql<M>(wrapper, pagerInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="pagerInfo"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSelectSql<M>(QueryWrapper<M>? wrapper = null, PagerInfo? pagerInfo = null)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;

            QueryOptions options = new QueryOptions
            {
                TableName = mapping.TableName
            };
            string safeTableName = FormatTable(mapping.TableName!);

            if (wrapper == null)
            {
                options.Select = BuildQueryFieldsString<M>(null, mapping);
                sqlInfo.Sql = BuildSqlOfSelect(options);
                return sqlInfo;
            }

            if (!string.IsNullOrWhiteSpace(wrapper.FieldsSql))
            {
                options.Select = wrapper.FieldsSql;
            }
            else
            {
                options.Select = BuildQueryFieldsString<M>(wrapper.Fields, mapping);
            }

            if (pagerInfo != null)
            {
                options.Offset = (pagerInfo.PageIndex - 1) * pagerInfo.PageSize;
                options.Limit = pagerInfo.PageSize;
            }
            else
            {
                if (wrapper.Limit != null)
                {
                    options.Limit = wrapper.Limit;
                }
            }

            if (!sqlInfo.ParametersInitialized)
            {
                sqlInfo.InitializeParameters();
            }
            if (wrapper.GetType() == typeof(FilterWrapper<M>))
            {
                FilterWrapper<M> filterWrapper = (FilterWrapper<M>)wrapper;
                options.Condition = BuildConditionString(filterWrapper, sqlInfo, mapping);
            }
            else if (wrapper.GetType() == typeof(LambdaWrapper<M>))
            {
                LambdaWrapper<M> lambdaWrapper = (LambdaWrapper<M>)wrapper;
                options.Condition = BuildConditionString(lambdaWrapper, sqlInfo, mapping);
            }

            if (wrapper.GroupByList != null && wrapper.GroupByList.Count > 0)
            {
                options.GroupBy = BuildGroupByString(wrapper.GroupByList);
            }

            if (wrapper.Orders != null)
            {
                options.OrderBy = BuildOrderString(wrapper.Orders);
            }

            string sql = BuildSqlOfSelect(options);
            if (pagerInfo != null && pagerInfo.RecordCount == null)
            {
                QueryOptions pagerOptions = new QueryOptions
                {
                    TableName = mapping.TableName!
                };
                pagerOptions.Select = $"COUNT(1) {DatabaseClient.RECORD_COUNT_RESULT_COLUMN}";
                pagerOptions.Condition = options.Condition;
                sql += Environment.NewLine;
                sql += BuildSqlOfSelect(pagerOptions);
            }
            sqlInfo.Sql = sql;
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="key"></param>
        /// <param name="resultFields"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfSelectById<M>(object key, QueryFields? resultFields = null)
            where M : class
        {
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string primaryKey = mapping.PrimaryKey!;
            string safePrimaryKey = FormatColumn(primaryKey);
            string primaryKeyParam = FormatParameter(primaryKey);

            QueryOptions parts = new QueryOptions
            {
                TableName = mapping.TableName,
                Select = BuildQueryFieldsString<M>(resultFields, mapping),
                Condition = $"{safePrimaryKey} = {primaryKeyParam}",
                Limit = 1
            };
            SqlInfo sqlInfo = new SqlInfo
            {
                Sql = BuildSqlOfSelect(parts)
            };
            sqlInfo.InitializeParameters();
            sqlInfo.AddParameter(primaryKey, key);
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="keyList"></param>
        /// <param name="resultFields"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfSelectByIds<M>(IEnumerable keyList, QueryFields? resultFields = null)
            where M : class
        {
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string primaryKey = mapping.PrimaryKey!;
            SqlInfo sqlInfo = new SqlInfo();
            sqlInfo.InitializeParameters();
            string condition = BuildColumnInListCondition(keyList, primaryKey, sqlInfo, "SelectByIds");

            QueryOptions options = new QueryOptions
            {
                TableName = mapping.TableName!,
                Select = BuildQueryFieldsString<M>(resultFields, mapping),
                Condition = condition
            };
            sqlInfo.Sql = BuildSqlOfSelect(options);
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="model"></param>
        /// <param name="insertId">
        /// 指定是否插入自增长主键 
        /// Specify whether to force the insertion of an auto-increment primary key
        /// </param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfInsert<M>(M model, bool insertId)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            sqlInfo.Sql = string.Empty;
            sqlInfo.InitializeParameters();

            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string safeTableName = FormatTable(mapping.TableName!);

            StringBuilder fieldsBuilder = new StringBuilder();
            StringBuilder paramsBuilder = new StringBuilder();
            bool hasAutoIncrement = false;

            FieldMapping? autoPkField = null;
            if (mapping.ColumnMappingList != null && mapping.ColumnMappingList.Count > 0)
            {
                foreach (FieldMapping columnMapping in mapping.ColumnMappingList)
                {
                    object? columnValue = EmitTool.GetPropertyValue(model, columnMapping.PropertyName);

                    if (columnMapping.AutoIncrement)
                    {
                        sqlInfo.AutoIncrementInfo = columnMapping;
                        hasAutoIncrement = true;
                        autoPkField = columnMapping;

                        if (!insertId && DatabaseType != DatabaseType.Sqlite)
                        {
                            continue;
                        }
                        if (DatabaseType == DatabaseType.Sqlite)
                        {
                            /* Sqlite自定义插入自增长主键需要将插入值设置为NULL
                             * When custom inserting into an auto-increment primary key in Sqlite, the inserted value needs to be set to NULL
                             */
                            columnValue = null;
                        }
                    }

                    string formattedFieldName = FormatColumn(columnMapping.ColumnName!);
                    fieldsBuilder.Append($"{formattedFieldName},");

                    string paramName = columnMapping.PropertyName!;
                    string formattedParamName = FormatParameter(paramName);
                    paramsBuilder.Append($"{FormatWriteParameter(columnMapping, formattedParamName)},");

                    sqlInfo.AddParameter(paramName, DbParameterValueHandler.Handle(columnValue, mapping, columnMapping));
                }
            }

            if (fieldsBuilder.Length > 0)
            {
                fieldsBuilder.Remove(fieldsBuilder.Length - 1, 1);
            }
            if (paramsBuilder.Length > 0)
            {
                paramsBuilder.Remove(paramsBuilder.Length - 1, 1);
            }
            string fieldsString = fieldsBuilder.ToString();
            string paramsString = paramsBuilder.ToString();

            if (string.IsNullOrWhiteSpace(fieldsString) || string.IsNullOrWhiteSpace(paramsString))
            {
                throw new SuperException(EX_KEY_FailedToGenerateSQL);
            }

            switch (DatabaseType)
            {
                case DatabaseType.SqlServer:
                    if (insertId)
                    {
                        sqlInfo.Sql += $"SET IDENTITY_INSERT {safeTableName} ON;";
                    }
                    sqlInfo.Sql += $"INSERT INTO {safeTableName} ({fieldsString}) VALUES ({paramsString});";

                    if (hasAutoIncrement && !insertId)
                    {
                        sqlInfo.Sql += "\nSELECT @@IDENTITY;";
                        sqlInfo.IsAutoIncrement = true;
                    }
                    if (insertId)
                    {
                        sqlInfo.Sql += $"SET IDENTITY_INSERT {safeTableName} OFF;";
                    }
                    break;
                case DatabaseType.Sqlite:
                    sqlInfo.Sql += $"INSERT INTO {safeTableName} ({fieldsString}) VALUES ({paramsString});";

                    if (hasAutoIncrement && !insertId)
                    {
                        sqlInfo.Sql += "\nSELECT last_insert_rowid();";
                        sqlInfo.IsAutoIncrement = true;
                    }
                    break;
                case DatabaseType.PostgreSQL:
                    sqlInfo.Sql += $"INSERT INTO {safeTableName} ({fieldsString}) VALUES ({paramsString}) ";

                    if (hasAutoIncrement && !insertId && autoPkField != null)
                    {
                        string safeAutoPkFieldName = FormatColumn(autoPkField.ColumnName!);
                        sqlInfo.Sql += $"\nRETURNING {safeAutoPkFieldName};";
                        sqlInfo.IsAutoIncrement = true;
                    }
                    else if (hasAutoIncrement && insertId)
                    {
                        string autoIncrementColumn = sqlInfo.AutoIncrementInfo!.ColumnName!;
                        string safeAutoIncrementColumn = FormatColumn(autoIncrementColumn);
                        sqlInfo.Sql += $"\nSELECT setval(pg_get_serial_sequence('{mapping.TableName}', '{autoIncrementColumn}'),COALESCE(MAX({safeAutoIncrementColumn}), 1), false) FROM {safeTableName};";
                    }
                    break;
                case DatabaseType.Oracle:
                    sqlInfo.Sql += $"INSERT INTO {safeTableName} ({fieldsString}) VALUES ({paramsString})";

                    if (hasAutoIncrement && !insertId && autoPkField != null)
                    {
                        string safeAutoPkFieldName = FormatColumn(autoPkField.ColumnName!);
                        sqlInfo.Sql += $"\nRETURNING {safeAutoPkFieldName} INTO :RETURNING_ID;";
                        sqlInfo.IsAutoIncrement = true;
                    }
                    break;
                case DatabaseType.MySQL:
                default:
                    sqlInfo.Sql += $"INSERT INTO {safeTableName} ({fieldsString}) VALUES ({paramsString});";

                    if (hasAutoIncrement && !insertId)
                    {
                        sqlInfo.Sql += "\nSELECT LAST_INSERT_ID();";
                        sqlInfo.IsAutoIncrement = true;
                    }
                    break;
            }
            
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="list"></param>
        /// <param name="insertId">
        /// 指定是否插入自增长主键 
        /// Specify whether to force the insertion of an auto-increment primary key
        /// </param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfInsert<M>(IEnumerable<M> list, bool insertId)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            if (!list.Any())
            {
                throw new SuperException(EX_KEY_FailedToGenerateSQL);
            }

            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string safeTableName = FormatTable(mapping.TableName!);
            bool hasAutoIncrement = false;
            sqlInfo.InitializeParameters();

            StringBuilder sqlBuilder = new StringBuilder();
            int rowIndex = 0;

            foreach (var item in list)
            {
                if (mapping.ColumnMappingList == null || mapping.ColumnMappingList.Count == 0)
                {
                    continue;
                }

                StringBuilder fieldsBuilder = new StringBuilder();
                StringBuilder paramsBuilder = new StringBuilder();

                foreach (FieldMapping columnMapping in mapping.ColumnMappingList)
                {
                    object? columnValue = EmitTool.GetPropertyValue(item, columnMapping.PropertyName!);

                    if (columnMapping.AutoIncrement)
                    {
                        sqlInfo.AutoIncrementInfo = columnMapping;
                        hasAutoIncrement = true;

                        if (!insertId && DatabaseType != DatabaseType.Sqlite)
                        {
                            continue;
                        }
                        if (DatabaseType == DatabaseType.Sqlite)
                        {
                            columnValue = null;
                        }
                    }

                    string formattedFieldName = FormatColumn(columnMapping.ColumnName!);
                    fieldsBuilder.Append($"{formattedFieldName},");

                    string paramName = $"{columnMapping.PropertyName}_{rowIndex}";
                    string formattedParamName = FormatParameter(paramName);
                    paramsBuilder.Append($"{FormatWriteParameter(columnMapping, formattedParamName)},");

                    sqlInfo.AddParameter(paramName, DbParameterValueHandler.Handle(columnValue, mapping, columnMapping));
                }

                if (fieldsBuilder.Length == 0)
                {
                    rowIndex++;
                    continue;
                }

                fieldsBuilder.Remove(fieldsBuilder.Length - 1, 1);
                paramsBuilder.Remove(paramsBuilder.Length - 1, 1);
                sqlBuilder.AppendLine($"INSERT INTO {safeTableName} ({fieldsBuilder}) VALUES ({paramsBuilder});");
                rowIndex++;
            }

            if (hasAutoIncrement && DatabaseType == DatabaseType.PostgreSQL)
            {
                string autoIncrementColumn = sqlInfo.AutoIncrementInfo!.ColumnName!;
                string safeAutoIncrementColumn = FormatColumn(autoIncrementColumn);
                sqlBuilder.Append($"\nSELECT setval(pg_get_serial_sequence('{mapping.TableName}', '{autoIncrementColumn}'),COALESCE(MAX({safeAutoIncrementColumn}), 1), false) FROM {safeTableName};");
            }

            sqlInfo.Sql = sqlBuilder.ToString();
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="model"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfUpdateById<M>(M model, QueryFields? updateFields = null)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);

            string updateString = BuildUpdateFieldsString(model, sqlInfo, updateFields, mapping);
            string idCondition = BuildIdCondition<M>(model, sqlInfo, mapping);
            sqlInfo.Sql = $"UPDATE {formattedTableName} SET {updateString} WHERE {idCondition};";
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfUpdateById<M>(UpdateFields updateFields, object id)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);
            FieldMapping pkMapping = mapping.GetColumnMapping(mapping.PrimaryKey!)!;
            string formattedPkName = FormatColumn(mapping.PrimaryKey!);
            string formattedPkParam = FormatParameter(mapping.PrimaryKey!);
            sqlInfo.InitializeParameters();

            string updateString = BuildUpdateFieldsString(updateFields, sqlInfo, mapping);
            sqlInfo.Sql = $"UPDATE {formattedTableName} SET {updateString} WHERE {formattedPkName} = {formattedPkParam};";
            sqlInfo.AddParameter(mapping.PrimaryKey!, id);
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="list"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfUpdate<M>(IEnumerable<M> list, QueryFields? updateFields = null)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            if (!list.Any())
            {
                return sqlInfo;
            }

            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string safeTableName = FormatTable(mapping.TableName!);
            string safePrimaryKey = FormatColumn(mapping.PrimaryKey!);

            List<FieldMapping>? columMappingList = GetQueryFieldsMappingList<M>(updateFields, mapping);
            if (columMappingList == null || columMappingList.Count == 0)
            {
                return sqlInfo;
            }

            sqlInfo.InitializeParameters();
            StringBuilder sqlBuilder = new StringBuilder();
            int rowIndex = 0;

            foreach (var item in list)
            {
                StringBuilder updateBuilder = new StringBuilder();
                string? pkParamName = null;

                foreach (var columnMapping in columMappingList)
                {
                    object? columnValue = EmitTool.GetPropertyValue(item, columnMapping.PropertyName!);
                    string paramName = $"{columnMapping.PropertyName}_{rowIndex}";
                    string formattedParamName = FormatParameter(paramName);
                    sqlInfo.AddParameter(paramName, DbParameterValueHandler.Handle(columnValue, mapping, columnMapping));

                    if (columnMapping.PrimaryKey)
                    {
                        pkParamName = formattedParamName;
                    }
                    else
                    {
                        string formattedFieldName = FormatColumn(columnMapping.ColumnName!);
                        updateBuilder.Append($"{formattedFieldName} = {FormatWriteParameter(columnMapping, formattedParamName)},");
                    }
                }

                if (updateBuilder.Length == 0 || pkParamName == null)
                {
                    rowIndex++;
                    continue;
                }

                updateBuilder.Remove(updateBuilder.Length - 1, 1);
                sqlBuilder.AppendLine($"UPDATE {safeTableName} SET {updateBuilder} WHERE {safePrimaryKey} = {pkParamName};");
                rowIndex++;
            }

            sqlInfo.Sql = sqlBuilder.ToString();
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="model"></param>
        /// <param name="wrapper"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfUpdate<M>(M model, LambdaWrapper<M> wrapper, QueryFields? updateFields = null)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);

            string updateString = BuildUpdateFieldsString(model, sqlInfo, updateFields, mapping);
            string? condition = BuildConditionString<M>(wrapper, sqlInfo, mapping);
            string where = string.IsNullOrEmpty(condition) ? string.Empty : $" WHERE {condition}";
            sqlInfo.Sql = $"UPDATE {formattedTableName} SET {updateString} {where};";
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfUpdate<M>(UpdateFields updateFields, LambdaWrapper<M> wrapper)
            where M : class
        {
            if (updateFields.Fields == null || updateFields.Fields.Count == 0)
            {
                throw new SuperException(Globals.EX_KEY_INVALID_ARGUMENT, nameof(updateFields));
            }

            SqlInfo sqlInfo = new SqlInfo();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);

            string updateString = BuildUpdateFieldsString(updateFields, sqlInfo, mapping);
            string? condition = BuildConditionString<M>(wrapper, sqlInfo, mapping);
            string where = string.IsNullOrEmpty(condition) ? string.Empty : $" WHERE {condition}";
            sqlInfo.Sql = $"UPDATE {formattedTableName} SET {updateString} {where};";
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="model"></param>
        /// <param name="wrapper"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfUpdate<M>(M model, FilterWrapper<M> wrapper, QueryFields? updateFields = null)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);

            string updateString = BuildUpdateFieldsString(model, sqlInfo, updateFields, mapping);
            string? condition = BuildConditionString<M>(wrapper, sqlInfo, mapping);
            string where = string.IsNullOrEmpty(condition) ? string.Empty : $" WHERE {condition}";
            sqlInfo.Sql = $"UPDATE {formattedTableName} SET {updateString} {where};";
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfUpdate<M>(UpdateFields updateFields, FilterWrapper<M> wrapper)
            where M : class
        {
            if (updateFields.Fields == null || updateFields.Fields.Count == 0)
            {
                throw new SuperException(Globals.EX_KEY_INVALID_ARGUMENT, nameof(updateFields));
            }

            SqlInfo sqlInfo = new SqlInfo();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);

            string updateString = BuildUpdateFieldsString(updateFields, sqlInfo, mapping);
            string? condition = BuildConditionString<M>(wrapper, sqlInfo, mapping);
            string where = string.IsNullOrEmpty(condition) ? string.Empty : $" WHERE {condition}";
            sqlInfo.Sql = $"UPDATE {formattedTableName} SET {updateString} {where};";
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfDelete<M>(LambdaWrapper<M> wrapper)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);

            string? condition = BuildConditionString(wrapper, sqlInfo, mapping);
            string where = string.IsNullOrEmpty(condition) ? "" : $" WHERE {condition}";
            sqlInfo.Sql = $"DELETE FROM {formattedTableName}{where}";

            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfDelete<M>(FilterWrapper<M> wrapper)
            where M : class
        {
            SqlInfo sqlInfo = new SqlInfo();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);

            string? condition = BuildConditionString<M>(wrapper, sqlInfo, mapping);
            string where = string.IsNullOrEmpty(condition) ? "" : $" WHERE {condition}";
            sqlInfo.Sql = $"DELETE FROM {formattedTableName}{where}";

            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfDelete<M>(object key)
            where M : class
        {
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);
            string primaryKey = mapping.PrimaryKey!;
            string safePrimaryKey = FormatColumn(primaryKey);
            string primaryKeyParam = FormatParameter(primaryKey);

            SqlInfo sqlInfo = new SqlInfo
            {
                Sql = $"DELETE FROM {formattedTableName} WHERE {safePrimaryKey} = {primaryKeyParam}",
            };
            sqlInfo.InitializeParameters();
            sqlInfo.AddParameter(primaryKey, key);

            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="keyList"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfDelete<M>(IEnumerable keyList)
            where M : class
        {
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string formattedTableName = FormatTable(mapping.TableName!);
            string primaryKey = mapping.PrimaryKey!;
            SqlInfo sqlInfo = new SqlInfo();
            sqlInfo.InitializeParameters();
            string condition = BuildColumnInListCondition(keyList, primaryKey, sqlInfo, "DeleteByIds");
            sqlInfo.Sql = $"DELETE FROM {formattedTableName} WHERE {condition}";
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfCount<M>(LambdaWrapper<M> wrapper)
            where M : class
        {
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string tableName = mapping.TableName!;

            SqlInfo sqlInfo = new SqlInfo();

            string? condition = BuildConditionString<M>(wrapper, sqlInfo, mapping);
            QueryOptions selectSqlParts = new QueryOptions
            {
                TableName = tableName,
                Select = $"COUNT(1) {DatabaseClient.RECORD_COUNT_RESULT_COLUMN}",
                Condition = condition
            };

            sqlInfo.Sql = BuildSqlOfSelect(selectSqlParts);
            return sqlInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual SqlInfo BuildSqlOfCount<M>(FilterWrapper<M>? wrapper = null)
            where M : class
        {
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string tableName = mapping.TableName!;

            SqlInfo sqlInfo = new SqlInfo();

            string? condition = wrapper != null ? BuildConditionString<M>(wrapper, sqlInfo, mapping) : string.Empty;
            QueryOptions selectSqlParts = new QueryOptions
            {
                TableName = tableName,
                Select = $"COUNT(1) {DatabaseClient.RECORD_COUNT_RESULT_COLUMN}",
                Condition = condition
            };

            sqlInfo.Sql = BuildSqlOfSelect(selectSqlParts);
            return sqlInfo;
        }


        #region Utilities

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        protected virtual List<FieldMapping>? GetQueryFieldsMappingList<M>(QueryFields? fields, TableMapping? mapping = null) where M : class
        {
            mapping = mapping ?? ModelSchemaStore.GetModelMapping<M>()!;
            List<FieldMapping>? columMappingList = null;
            if (fields != null && fields.Count > 0)
            {
                columMappingList = new List<FieldMapping>();
                foreach (string field in fields.Fields)
                {
                    string columnName = mapping.ResolveColumnName(field);
                    FieldMapping? columnMapping = mapping.GetColumnMapping(columnName);
                    if (columnMapping == null)
                    {
                        throw new SuperException(Globals.EX_KEY_INVALID_ARGUMENT, field);
                    }
                    columMappingList.Add(columnMapping);
                }
            }
            else
            {
                columMappingList = mapping.ColumnMappingList;
            }

            return columMappingList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="model"></param>
        /// <param name="mapping"></param>
        /// <param name="sqlInfo"></param>
        /// <param name="updateFields"></param>
        /// <returns>For example: `Name` = @Name, `Price` = @Price, ...</returns>
        protected virtual string BuildUpdateFieldsString<M>(M model, SqlInfo sqlInfo, QueryFields? updateFields, TableMapping? mapping = null)
            where M : class
        {
            mapping = mapping ?? ModelSchemaStore.GetModelMapping<M>()!;

            StringBuilder updateBuilder = new StringBuilder();
            List<FieldMapping>? columMappingList = GetQueryFieldsMappingList<M>(updateFields, mapping);
            if (!sqlInfo.ParametersInitialized)
            {
                sqlInfo.InitializeParameters();
            }

            if (columMappingList != null && columMappingList.Count > 0)
            {
                foreach (var columnMapping in columMappingList)
                {
                    if (columnMapping.PrimaryKey)
                    {
                        continue;
                    }
                    string formattedFieldName = FormatColumn(columnMapping.ColumnName!);
                    string paramName = columnMapping.PropertyName!;
                    string formattedParamName = FormatParameter(paramName);
                    updateBuilder.Append($"{formattedFieldName} = {FormatWriteParameter(columnMapping, formattedParamName)},");

                    object? columnValue = EmitTool.GetPropertyValue(model, columnMapping.PropertyName!);

                    sqlInfo.AddParameter(paramName, DbParameterValueHandler.Handle(columnValue, mapping, columnMapping));
                }
                if (updateBuilder.Length > 0)
                {
                    updateBuilder.Remove(updateBuilder.Length - 1, 1);
                }
            }
            else
            {
                throw new SuperException(EX_KEY_FailedToGenerateSQL);
            }

            string updateString = updateBuilder.ToString();
            return updateString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="updateFields"></param>
        /// <param name="sqlInfo"></param>
        /// <param name="mapping"></param>
        /// <returns>For example: `Name` = @Name, `Price` = @Price, ...</returns>
        protected virtual string BuildUpdateFieldsString(UpdateFields updateFields, SqlInfo sqlInfo, TableMapping mapping)
        {
            if (updateFields.Fields == null || updateFields.Fields.Count == 0)
            {
                throw new SuperException(EX_KEY_FailedToGenerateSQL);
            }
            if (!sqlInfo.ParametersInitialized)
            {
                sqlInfo.InitializeParameters();
            }

            StringBuilder updateBuilder = new StringBuilder();
            foreach (var updateField in updateFields.Fields)
            {
                string field = updateField.Key;
                object? fieldValue = updateField.Value;
                FieldMapping fieldMapping = mapping.GetColumnMapping(field)!;

                string formattedFieldName = FormatColumn(field);
                string formattedParamName = FormatParameter(field);
                updateBuilder.Append($"{formattedFieldName} = {FormatWriteParameter(fieldMapping, formattedParamName)},");

                sqlInfo.AddParameter(field, DbParameterValueHandler.Handle(fieldValue, mapping, fieldMapping));
            }
            if (updateBuilder.Length > 0)
            {
                updateBuilder.Remove(updateBuilder.Length - 1, 1);
            }

            string updateString = updateBuilder.ToString();
            return updateString;
        }

        /// <summary>
        /// PostgreSQL 不会把 text 参数隐式写入 jsonb 字段；映射明确标记后统一在 SQL 侧转换。
        /// 其他数据库保持原参数表达式，避免引入方言语法。
        /// </summary>
        protected virtual string FormatWriteParameter(FieldMapping fieldMapping, string formattedParameter)
        {
            return DatabaseType == DatabaseType.PostgreSQL && fieldMapping.Jsonb
                ? $"{formattedParameter}::jsonb"
                : formattedParameter;
        }

        #endregion

    }
}
