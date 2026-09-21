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
    /// <remarks>
    /// 按数据库方言生成 CRUD SQL：自增主键回填、分页、字符串包含函数、标识符引号风格等方言差异全部在此处理。
    /// 防注入三道闸：
    /// 1) 值一律参数化——Oracle 使用 ":" 前缀，其余数据库使用 "@" 前缀（见 <see cref="FormatParameter"/>）；
    /// 2) 标识符（表名、列名、排序/分组字段）一律经 <see cref="QuoteIdentifier"/> 强制引号包裹并转义闭合引号，
    ///    未知数据库类型退化为 <see cref="IsSafeIdentifier"/> 白名单校验；
    /// 3) 排序字段在 QueryOrders.Set 处还有一层正则校验（仅允许合法标识符或一级限定名）。
    /// 逃生口：QueryWrapper.Select(fieldsSql)（已标记 Obsolete）与 QueryWrapper.FieldsSql 会把原始 SQL 片段
    /// 原样拼入 SELECT 子句，不经任何转义——传入不可信输入存在注入风险，务必使用受控白名单。
    /// Generates CRUD SQL per database dialect: identity backfill, paging, string-contains functions,
    /// identifier quoting styles and other dialect differences are all handled here. Anti-injection gates:
    /// (1) all values are parameterized - ":" prefix on Oracle, "@" elsewhere (see <see cref="FormatParameter"/>);
    /// (2) identifiers (tables, columns, order/group fields) are always quoted with the closing quote escaped via
    /// <see cref="QuoteIdentifier"/>; unknown database types fall back to the <see cref="IsSafeIdentifier"/> whitelist;
    /// (3) order fields are additionally regex-validated in QueryOrders.Set (bare identifiers or one-level qualified names only).
    /// Escape hatch: QueryWrapper.Select(fieldsSql) (Obsolete) and QueryWrapper.FieldsSql splice raw SQL fragments into
    /// the SELECT clause without any escaping - feeding them untrusted input is an injection risk; use controlled whitelists.
    /// </remarks>
    public abstract class SqlTemplate
    {

        /// <summary>
        /// 生成 SQL 失败时使用的异常资源键 / Exception resource key used when SQL generation fails
        /// </summary>
        public const string EX_KEY_FailedToGenerateSQL = "FailedTo_Generate_SQL";

        /// <summary>
        /// 以指定数据库类型构造实例，并快照当时的 <see cref="DatabaseFactory.DbParameterValueHandler"/>
        /// Construct with the specified database type, snapshotting the current <see cref="DatabaseFactory.DbParameterValueHandler"/>
        /// </summary>
        /// <param name="databaseType">目标数据库类型 / The target database type</param>
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

        /// <summary>
        /// 构造时快照的参数值处理器 / The parameter value handler snapshotted at construction time
        /// </summary>
        private IDbParameterValueHandler DbParameterValueHandler
        {
            get;
            set;
        }

        /// <summary>
        /// 查询当前数据库时间的SQL语句
        /// SQL statement to query the current database time
        /// </summary>
        /// <returns>按方言选择 NOW()/CURRENT_TIMESTAMP/SYSDATE/GETDATE()/datetime('now')，结果列别名 CurrentTime
        /// NOW()/CURRENT_TIMESTAMP/SYSDATE/GETDATE()/datetime('now') per dialect, aliased as CurrentTime</returns>
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
        /// 把字段名格式化为参数占位符：Oracle 加 ":" 前缀，其余数据库加 "@" 前缀；已带前缀则原样返回
        /// Format a field name as a parameter placeholder: ":" prefix on Oracle, "@" elsewhere; returned as-is when already prefixed
        /// </summary>
        /// <param name="fieldName">字段（参数）名，如 ProductId / The field (parameter) name, e.g. ProductId</param>
        /// <returns>参数占位符，如 @ProductId 或 :ProductId / The parameter placeholder, e.g. @ProductId or :ProductId</returns>
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
        /// Legal bare identifier: starts with a letter/underscore followed by letters, digits, underscore, $, # (Unicode letters allowed for Chinese column names)
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex SafeIdentifierRegex =
            new System.Text.RegularExpressions.Regex(@"^[\p{L}_][\p{L}\p{N}_$#]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// 是否为无需引号包裹即可安全拼接的标识符（白名单校验：非空、长度不超过 128 且匹配 <see cref="SafeIdentifierRegex"/>）
        /// Whether the identifier can be safely spliced without quoting (whitelist: non-empty, length up to 128, and matching <see cref="SafeIdentifierRegex"/>)
        /// </summary>
        /// <param name="identifier">待校验标识符 / The identifier to check</param>
        /// <returns>安全返回 true；null、空串或含特殊字符返回 false / true when safe; false for null, empty, or special characters</returns>
        public static bool IsSafeIdentifier(string? identifier)
        {
            return !string.IsNullOrEmpty(identifier) && identifier.Length <= 128 && SafeIdentifierRegex.IsMatch(identifier);
        }

        /// <summary>
        /// 取得当前数据库的标识符引号（open/close）；不支持引号的数据库返回 null
        /// Get the identifier quote characters (open/close) of the current database; null when the database has none
        /// </summary>
        /// <returns>MySQL 为 ``；PostgreSQL/Oracle/Sqlite 为 ""；SqlServer 为 []；其它类型返回 null
        /// Backticks for MySQL; double quotes for PostgreSQL/Oracle/Sqlite; brackets for SqlServer; null otherwise</returns>
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
        /// Safely format a single identifier: always quoted with the closing quote escaped, so no input can escape the identifier position.
        /// </summary>
        /// <remarks>
        /// 支持 "schema.table" / "alias.column" 形式（各段分别处理）；已带引号的输入会被剥离后重新转义，
        /// 因此形如 `a`, (SELECT ...) ` 的“伪引号”注入不再能原样透传。含 \0、\r、\n 等控制字符时直接拒绝；
        /// 未知数据库类型（无引号）只放行 <see cref="IsSafeIdentifier"/> 白名单内的裸标识符。
        /// Supports "schema.table" / "alias.column" forms (each segment handled separately); already-quoted input is
        /// unquoted then re-escaped, so pseudo-quote injection such as `a`, (SELECT ...) ` can no longer pass through
        /// verbatim. Control characters (\0, \r, \n) are rejected; unknown database types (no quotes) only allow
        /// bare identifiers whitelisted by <see cref="IsSafeIdentifier"/>.
        /// </remarks>
        /// <param name="identifier">原始标识符 / The raw identifier</param>
        /// <returns>引号包裹并转义后的标识符 / The quoted and escaped identifier</returns>
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
        /// <param name="columnName">列名，如 data / The column name, e.g. data</param>
        /// <returns>引号包裹的列名，如 `data` / The quoted column name, e.g. `data`</returns>
        public virtual string FormatColumn(string columnName)
        {
            return QuoteIdentifier(columnName);
        }

        /// <summary>
        /// 格式化表名（始终引号包裹并转义）
        /// Format table name (always quoted and escaped)
        /// </summary>
        /// <param name="tableName">表名，如 Product / The table name, e.g. Product</param>
        /// <returns>引号包裹的表名，如 `Product` / The quoted table name, e.g. `Product`</returns>
        public virtual string FormatTable(string tableName)
        {
            return QuoteIdentifier(tableName);
        }

        /// <summary>
        /// 构造 SELECT 字段列表：列名引号包裹并以属性名作为别名（便于结果集按属性名映射）；
        /// 未指定字段或映射无列时返回 "*"
        /// Build the SELECT field list: columns are quoted and aliased by property name (for result mapping);
        /// returns "*" when no fields are specified or the mapping has no columns
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="fields">要查询的字段；null 或空表示全部字段 / Fields to select; null or empty means all columns</param>
        /// <param name="mapping">表映射；null 时从 ModelSchemaStore 解析 / Table mapping; resolved from ModelSchemaStore when null</param>
        /// <returns>例如：`id` `Id`, `name` `Name`, `product_id` ProductId / e.g. `id` `Id`, `name` `Name`, `product_id` ProductId</returns>
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
        /// 构造 GROUP BY 字段列表（列名经 FormatColumn 引号包裹防注入）
        /// Build the GROUP BY column list (columns quoted via FormatColumn against injection)
        /// </summary>
        /// <param name="groupByList">分组列名列表 / The list of group-by column names</param>
        /// <returns>例如：`category_id`, `status`；空列表返回空字符串 / e.g. `category_id`, `status`; empty string for an empty list</returns>
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
        /// 构造 ORDER BY 子句内容（排序字段已在 QueryOrders.Set 处经正则校验，此处再经 FormatColumn 引号包裹双重防护）
        /// Build the ORDER BY clause (order fields are regex-validated in QueryOrders.Set and additionally quoted by FormatColumn)
        /// </summary>
        /// <param name="orders">排序规则 / The order rules</param>
        /// <returns>例如：`count` desc, `create_time` desc；无排序返回空字符串 / e.g. `count` desc, `create_time` desc; empty string when no orders</returns>
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
        /// 把 Lambda 条件包装器解析为参数化 WHERE 条件，并把参数写入 <paramref name="sqlInfo"/>
        /// Parse a lambda condition wrapper into a parameterized WHERE condition and add parameters to <paramref name="sqlInfo"/>
        /// </summary>
        /// <remarks>
        /// 条件值全部经 <see cref="DatabaseFactory.DbParameterValueHandler"/> 处理后以参数形式加入；
        /// 与 SET 子句同列重名时会自动改用唯一参数名（见 AddConditionParameterAvoidCollision）。
        /// All condition values go through <see cref="DatabaseFactory.DbParameterValueHandler"/> before being added
        /// as parameters; names colliding with SET parameters are automatically renamed (see AddConditionParameterAvoidCollision).
        /// </remarks>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="wrapper">Lambda 条件包装器 / The lambda condition wrapper</param>
        /// <param name="sqlInfo">接收参数的 SQL 信息对象 / The SQL info object receiving parameters</param>
        /// <param name="mapping">表映射；null 时从模型解析 / Table mapping; resolved from the model when null</param>
        /// <returns>条件 SQL 片段（不含 WHERE 关键字）；无条件时返回 null / The condition fragment (without the WHERE keyword); null when the wrapper has no condition</returns>
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
        /// 把 Filter 条件包装器解析为参数化 WHERE 条件，并把参数写入 <paramref name="sqlInfo"/>
        /// Parse a filter condition wrapper into a parameterized WHERE condition and add parameters to <paramref name="sqlInfo"/>
        /// </summary>
        /// <remarks>
        /// 条件值全部经 <see cref="DatabaseFactory.DbParameterValueHandler"/> 处理后以参数形式加入；
        /// 与 SET 子句同列重名时会自动改用唯一参数名（见 AddConditionParameterAvoidCollision）。
        /// All condition values go through <see cref="DatabaseFactory.DbParameterValueHandler"/> before being added
        /// as parameters; names colliding with SET parameters are automatically renamed (see AddConditionParameterAvoidCollision).
        /// </remarks>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="wrapper">Filter 条件包装器 / The filter condition wrapper</param>
        /// <param name="sqlInfo">接收参数的 SQL 信息对象 / The SQL info object receiving parameters</param>
        /// <param name="mapping">表映射；null 时从模型解析 / Table mapping; resolved from the model when null</param>
        /// <returns>条件 SQL 片段（不含 WHERE 关键字）；无条件时返回 null / The condition fragment (without the WHERE keyword); null when the wrapper has no condition</returns>
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
        /// When UPDATE SET and WHERE reference the same column (e.g. CAS: SET power=@power WHERE power=@power),
        /// rename the condition parameter to a unique name and rewrite the SQL to avoid duplicate dictionary keys.
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

        /// <summary>
        /// 替换参数占位符，词边界匹配，避免替换 @power 时误伤 @power_score。
        /// Replace a parameter placeholder with word-boundary matching so replacing @power does not hit @power_score.
        /// </summary>
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
        /// 构造主键等值条件（主键值经属性读取后作为参数加入 sqlInfo）
        /// Build the primary-key equality condition (the key value read from the model is added to sqlInfo as a parameter)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="model">携带主键值的模型实例 / The model instance carrying the primary key value</param>
        /// <param name="sqlInfo">接收主键参数的 SQL 信息对象 / The SQL info object receiving the key parameter</param>
        /// <param name="mapping">表映射；null 时从模型解析 / Table mapping; resolved from the model when null</param>
        /// <returns>例如 `id` = @id / e.g. `id` = @id</returns>
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
        /// 构造“列包含指定字符串”的参数化条件：把字符串作为参数加入 sqlInfo 后委托
        /// <see cref="BuildStringInColumnConditionWithParameter"/> 生成（字面值不内嵌）
        /// Build a parameterized "column contains string" condition: adds the string as a parameter to sqlInfo,
        /// then delegates to <see cref="BuildStringInColumnConditionWithParameter"/> (no literal embedding)
        /// </summary>
        /// <param name="str">要查找的字符串 / The string to search for</param>
        /// <param name="columnName">目标列名 / The target column name</param>
        /// <param name="sqlInfo">接收参数的 SQL 信息对象 / The SQL info object receiving the parameter</param>
        /// <param name="paramName">参数名，默认 SearchStr / The parameter name, SearchStr by default</param>
        /// <param name="ignoreCase">是否忽略大小写（LOWER 包裹两侧） / Whether to ignore case (wraps both sides with LOWER)</param>
        /// <returns>条件 SQL 片段 / The condition SQL fragment</returns>
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
        /// 使用已格式化的参数占位符构造“列包含字符串”条件（不内嵌字面值）
        /// Build the "column contains string" condition from an already-formatted parameter reference (no literal embedding)
        /// </summary>
        /// <remarks>
        /// 方言差异：Oracle/MySQL/Sqlite 用 INSTR(列, 参数)，SqlServer 用 CHARINDEX(参数, 列)（注意参数顺序相反），
        /// PostgreSQL 用 POSITION(参数 IN 列)。
        /// Dialects: INSTR(column, param) on Oracle/MySQL/Sqlite, CHARINDEX(param, column) on SqlServer
        /// (note the reversed argument order), POSITION(param IN column) on PostgreSQL.
        /// </remarks>
        /// <param name="formattedParam">已格式化的参数占位符，如 @SearchStr / The formatted parameter placeholder, e.g. @SearchStr</param>
        /// <param name="columnName">目标列名 / The target column name</param>
        /// <param name="ignoreCase">是否忽略大小写 / Whether to ignore case</param>
        /// <returns>条件 SQL 片段 / The condition SQL fragment</returns>
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
        /// 构造列 IN (值列表) 的参数化条件（每个值一个独立参数，绝不内嵌字面值）
        /// Build a parameterized column IN (value list) condition (one parameter per value, never inlined literals)
        /// </summary>
        /// <remarks>列表为 null 或空时返回恒假条件 "1 = 2"，避免生成非法的空 IN 子句
        /// Returns the always-false "1 = 2" for a null/empty list to avoid an invalid empty IN clause</remarks>
        /// <param name="list">值列表 / The value list</param>
        /// <param name="columnName">目标列名 / The target column name</param>
        /// <param name="sqlInfo">接收 IN 参数的 SQL 信息对象 / The SQL info object receiving the IN parameters</param>
        /// <param name="paramPrefix">参数名前缀，默认 InList / The parameter name prefix, InList by default</param>
        /// <returns>例如 `id` IN (@InList_0, @InList_1)；空列表为 1 = 2 / e.g. `id` IN (@InList_0, @InList_1); "1 = 2" for an empty list</returns>
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
        /// 按 QueryOptions 组装完整 SELECT 语句（表名经引号包裹）
        /// Assemble a complete SELECT statement from QueryOptions (table name quoted)
        /// </summary>
        /// <remarks>
        /// 分页方言：SqlServer——带 Offset 时用 ROW_NUMBER() OVER (ORDER BY ...) 子查询（无 OrderBy 时以
        /// (SELECT NULL) 占位），仅 Limit 时用 TOP；Oracle 12c+ 用 OFFSET/FETCH；PostgreSQL 用 LIMIT/OFFSET；
        /// MySQL/Sqlite 用 LIMIT offset,size。
        /// 注意：options.Select/Condition/GroupBy/OrderBy 均为原始 SQL 片段，原样拼接、不做任何转义，
        /// 属于原始 SQL 逃生口——严禁拼接不可信输入（框架内部生成的 Condition 已参数化）。
        /// Paging dialects: SqlServer - ROW_NUMBER() OVER (ORDER BY ...) subquery when Offset is set
        /// ((SELECT NULL) placeholder without OrderBy), TOP with Limit only; Oracle 12c+ - OFFSET/FETCH;
        /// PostgreSQL - LIMIT/OFFSET; MySQL/Sqlite - LIMIT offset,size.
        /// Warning: options.Select/Condition/GroupBy/OrderBy are raw SQL fragments spliced verbatim without escaping -
        /// a raw SQL escape hatch; never splice untrusted input (framework-generated Condition is parameterized).
        /// </remarks>
        /// <param name="options">查询选项 / The query options</param>
        /// <returns>完整 SELECT 语句（含结尾分号） / The complete SELECT statement (with trailing semicolon)</returns>
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
        /// 把泛型查询选项绑定到模型表名后组装 SELECT 语句
        /// Bind generic query options to the model's table name and assemble the SELECT statement
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="options">查询选项（TableName 将被映射表名覆盖） / The query options (TableName is overridden by the mapped table)</param>
        /// <param name="mapping">表映射；null 时从模型解析 / Table mapping; resolved from the model when null</param>
        /// <returns>完整 SELECT 语句 / The complete SELECT statement</returns>
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
        /// 按 Filter 条件构造 SELECT 语句（可选分页），等价于 <see cref="BuildSelectSql{M}(QueryWrapper{M}, PagerInfo)"/>
        /// Build a SELECT statement from a filter wrapper (optionally paged); equivalent to <see cref="BuildSelectSql{M}(QueryWrapper{M}, PagerInfo)"/>
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="wrapper">Filter 条件包装器；null 表示无条件 / The filter wrapper; null means no condition</param>
        /// <param name="pagerInfo">分页信息，null 表示不分页 / The pager info if selecting a page list</param>
        /// <returns>含 SQL 与参数的 SqlInfo / The SqlInfo carrying SQL and parameters</returns>
        public virtual SqlInfo BuildSqlOfSelect<M>(FilterWrapper<M>? wrapper = null, PagerInfo? pagerInfo = null) 
            where M : class
        {
            return BuildSelectSql<M>(wrapper, pagerInfo);
        }

        /// <summary>
        /// 按 Lambda 条件构造 SELECT 语句（可选分页），等价于 <see cref="BuildSelectSql{M}(QueryWrapper{M}, PagerInfo)"/>
        /// Build a SELECT statement from a lambda wrapper (optionally paged); equivalent to <see cref="BuildSelectSql{M}(QueryWrapper{M}, PagerInfo)"/>
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="wrapper">Lambda 条件包装器 / The lambda wrapper</param>
        /// <param name="pagerInfo">分页信息，null 表示不分页 / The pager info if selecting a page list</param>
        /// <returns>含 SQL 与参数的 SqlInfo / The SqlInfo carrying SQL and parameters</returns>
        public virtual SqlInfo BuildSqlOfSelect<M>(LambdaWrapper<M> wrapper, PagerInfo? pagerInfo = null)
            where M : class
        {
            return BuildSelectSql<M>(wrapper, pagerInfo);
        }

        /// <summary>
        /// 通用 SELECT 构造入口：处理字段选择、分页、条件、分组与排序
        /// Central SELECT builder handling field selection, paging, conditions, grouping and ordering
        /// </summary>
        /// <remarks>
        /// 字段优先级：wrapper.FieldsSql（原始 SQL 片段，未经转义——注入逃生口）优先；未设置时按 Fields 生成引号包裹的字段列表。
        /// 分页：Offset = (PageIndex - 1) * PageSize，Limit = PageSize；未分页时取 wrapper.Limit。
        /// 追加统计：分页且 pagerInfo.RecordCount 为 null 时，在同一 SqlInfo 中追加一条 COUNT(1) 查询
        /// （结果列别名 <see cref="DatabaseClient.RECORD_COUNT_RESULT_COLUMN"/>），供执行层一次往返回填总数。
        /// Fields priority: wrapper.FieldsSql (a raw, unescaped SQL fragment - the injection escape hatch) wins;
        /// otherwise a quoted field list is generated from Fields. Paging: Offset = (PageIndex - 1) * PageSize,
        /// Limit = PageSize; without paging, wrapper.Limit applies. When paging with a null RecordCount, a COUNT(1)
        /// query (aliased <see cref="DatabaseClient.RECORD_COUNT_RESULT_COLUMN"/>) is appended to the same SqlInfo
        /// so the executor can fill the total in one round trip.
        /// </remarks>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="wrapper">查询包装器；null 表示查询全表全部列 / The query wrapper; null selects all columns of the whole table</param>
        /// <param name="pagerInfo">分页信息，null 表示不分页 / The pager info; null means no paging</param>
        /// <returns>含 SQL（可能含追加的 COUNT 语句）与参数的 SqlInfo / The SqlInfo carrying the SQL (possibly with an appended COUNT statement) and parameters</returns>
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
        /// 构造按主键查询单条的 SELECT 语句（主键值参数化，Limit=1）
        /// Build the SELECT statement for one row by primary key (key parameterized, Limit = 1)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="key">主键值 / The primary key value</param>
        /// <param name="resultFields">返回字段；null 表示全部列 / Result fields; null means all columns</param>
        /// <returns>含 SQL 与主键参数的 SqlInfo / The SqlInfo carrying SQL and the key parameter</returns>
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
        /// 构造按主键值列表批量查询的 SELECT 语句（IN 条件全参数化）
        /// Build the SELECT statement for a batch query by primary key list (fully parameterized IN condition)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="keyList">主键值列表 / The primary key values</param>
        /// <param name="resultFields">返回字段；null 表示全部列 / Result fields; null means all columns</param>
        /// <returns>含 SQL 与 IN 参数的 SqlInfo / The SqlInfo carrying SQL and the IN parameters</returns>
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
        /// 构造单条 INSERT 语句（全部列值参数化，并按方言处理自增主键回填）
        /// Build a single INSERT statement (all column values parameterized, with dialect-specific identity backfill)
        /// </summary>
        /// <remarks>
        /// 自增主键回填方言：SqlServer 用 SELECT @@IDENTITY（insertId=true 时包裹 SET IDENTITY_INSERT ON/OFF）；
        /// MySQL 用 SELECT LAST_INSERT_ID()；Sqlite 用 SELECT last_insert_rowid()（强制插自增列时值置 NULL）；
        /// PostgreSQL 用 RETURNING 主键列（insertId=true 时改为 SELECT setval 校正序列）；
        /// Oracle 用 RETURNING 主键列 INTO :RETURNING_ID。
        /// 需要回填时置 sqlInfo.IsAutoIncrement = true 并记录 AutoIncrementInfo，由执行层读取回填值。
        /// Identity backfill per dialect: SqlServer appends SELECT @@IDENTITY (wrapped in SET IDENTITY_INSERT ON/OFF
        /// when insertId=true); MySQL appends SELECT LAST_INSERT_ID(); Sqlite appends SELECT last_insert_rowid()
        /// (forcing the auto-increment column to NULL when inserted); PostgreSQL appends RETURNING pk (switched to a
        /// SELECT setval sequence fix-up when insertId=true); Oracle appends RETURNING pk INTO :RETURNING_ID.
        /// When backfill is needed, sqlInfo.IsAutoIncrement is set with AutoIncrementInfo recorded for the executor.
        /// </remarks>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="model">要插入的模型实例 / The model instance to insert</param>
        /// <param name="insertId">
        /// 指定是否插入自增长主键
        /// Specify whether to force the insertion of an auto-increment primary key
        /// </param>
        /// <returns>含 SQL 与列参数的 SqlInfo；无有效列时抛出 EX_KEY_FailedToGenerateSQL
        /// The SqlInfo carrying SQL and column parameters; throws EX_KEY_FailedToGenerateSQL when no column applies</returns>
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
        /// 构造批量 INSERT 语句：每行生成一条独立 INSERT，参数名加 "_行号" 后缀避免跨行冲突；不回填自增值
        /// Build batch INSERT statements: one INSERT per row, parameter names suffixed with "_rowIndex" to avoid
        /// cross-row collisions; auto-increment values are not backfilled
        /// </summary>
        /// <remarks>
        /// PostgreSQL 无论是否强制插入自增主键，只要存在自增列就在末尾追加 SELECT setval 校正序列；
        /// Sqlite 强制插自增列时值置 NULL；其余方言行为与单条插入一致但均不追加取回自增值的语句。
        /// On PostgreSQL, a SELECT setval sequence fix-up is appended whenever an auto-increment column exists
        /// (regardless of insertId); on Sqlite the auto-increment column value is forced to NULL; other dialects
        /// behave like the single-row insert but never append identity-retrieval statements.
        /// </remarks>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="list">要插入的模型列表 / The list of model instances to insert</param>
        /// <param name="insertId">
        /// 指定是否插入自增长主键
        /// Specify whether to force the insertion of an auto-increment primary key
        /// </param>
        /// <returns>含逐行 INSERT 语句与参数的 SqlInfo；空列表抛出 EX_KEY_FailedToGenerateSQL
        /// The SqlInfo carrying per-row INSERT statements and parameters; throws EX_KEY_FailedToGenerateSQL for an empty list</returns>
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
        /// 构造按主键更新单个模型的 UPDATE 语句（以模型当前属性值作为 SET 值，全部参数化）
        /// Build an UPDATE by primary key from a model instance (SET values taken from current property values, all parameterized)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="model">携带新值与主键值的模型实例 / The model instance carrying new values and the primary key</param>
        /// <param name="updateFields">Specify the fields to be update / 要更新的字段；null 表示除主键外全部列</param>
        /// <returns>含 SQL 与参数的 SqlInfo / The SqlInfo carrying SQL and parameters</returns>
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
        /// 构造按主键更新的 UPDATE 语句（字段名与值来自 UpdateFields 字典，全部参数化）
        /// Build an UPDATE by primary key from an UpdateFields dictionary (fields and values fully parameterized)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="updateFields">Specify the fields to be update / 键为列名、值为新值的更新字段字典</param>
        /// <param name="id">主键值 / The primary key value</param>
        /// <returns>含 SQL 与参数的 SqlInfo / The SqlInfo carrying SQL and parameters</returns>
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
        /// 构造批量 UPDATE：每个模型一条按主键更新的语句，参数名加 "_行号" 后缀避免跨行冲突
        /// Build batch UPDATE statements: one UPDATE-by-pk per model, parameter names suffixed with "_rowIndex"
        /// </summary>
        /// <remarks>列表为空或无有效更新列（仅主键）时返回空 SqlInfo（Sql 为空字符串），不产生任何语句；
        /// 主键字段本身只出现在 WHERE 中，不会被更新。
        /// Returns an empty SqlInfo (blank Sql) for an empty list or when no updatable column applies
        /// (primary key only); the primary key itself appears only in WHERE and is never updated.
        /// </remarks>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="list">要更新的模型列表 / The list of model instances to update</param>
        /// <param name="updateFields">Specify the fields to be update / 要更新的字段；null 表示除主键外全部列</param>
        /// <returns>含逐行 UPDATE 语句与参数的 SqlInfo / The SqlInfo carrying per-row UPDATE statements and parameters</returns>
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
        /// 构造按 Lambda 条件更新的 UPDATE 语句（SET 值取自模型属性，全部参数化）
        /// Build an UPDATE with a lambda condition (SET values taken from model properties, all parameterized)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="model">携带新值的模型实例（主键不参与 SET） / The model instance carrying new values (pk excluded from SET)</param>
        /// <param name="wrapper">Lambda 条件包装器 / The lambda condition wrapper</param>
        /// <param name="updateFields">Specify the fields to be update / 要更新的字段；null 表示除主键外全部列</param>
        /// <returns>含 SQL 与参数的 SqlInfo / The SqlInfo carrying SQL and parameters</returns>
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
        /// 构造按 Lambda 条件更新的 UPDATE 语句（字段与值来自 UpdateFields 字典）
        /// Build an UPDATE with a lambda condition from an UpdateFields dictionary
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="updateFields">Specify the fields to be update / 键为列名、值为新值的更新字段字典（不能为空）</param>
        /// <param name="wrapper">Lambda 条件包装器 / The lambda condition wrapper</param>
        /// <returns>含 SQL 与参数的 SqlInfo；字段字典为空时抛出无效参数异常
        /// The SqlInfo carrying SQL and parameters; throws for an empty field dictionary</returns>
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
        /// 构造按 Filter 条件更新的 UPDATE 语句（SET 值取自模型属性，全部参数化）
        /// Build an UPDATE with a filter condition (SET values taken from model properties, all parameterized)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="model">携带新值的模型实例（主键不参与 SET） / The model instance carrying new values (pk excluded from SET)</param>
        /// <param name="wrapper">Filter 条件包装器 / The filter condition wrapper</param>
        /// <param name="updateFields">Specify the fields to be update / 要更新的字段；null 表示除主键外全部列</param>
        /// <returns>含 SQL 与参数的 SqlInfo / The SqlInfo carrying SQL and parameters</returns>
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
        /// 构造按 Filter 条件更新的 UPDATE 语句（字段与值来自 UpdateFields 字典）
        /// Build an UPDATE with a filter condition from an UpdateFields dictionary
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="updateFields">Specify the fields to be update / 键为列名、值为新值的更新字段字典（不能为空）</param>
        /// <param name="wrapper">Filter 条件包装器 / The filter condition wrapper</param>
        /// <returns>含 SQL 与参数的 SqlInfo；字段字典为空时抛出无效参数异常
        /// The SqlInfo carrying SQL and parameters; throws for an empty field dictionary</returns>
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
        /// 构造按 Lambda 条件删除的 DELETE 语句（条件参数化）
        /// Build a DELETE with a lambda condition (condition parameterized)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="wrapper">Lambda 条件包装器；无条件时将删除全表 / The lambda wrapper; no condition deletes the whole table</param>
        /// <returns>含 SQL 与参数的 SqlInfo / The SqlInfo carrying SQL and parameters</returns>
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
        /// 构造按 Filter 条件删除的 DELETE 语句（条件参数化）
        /// Build a DELETE with a filter condition (condition parameterized)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="wrapper">Filter 条件包装器；无条件时将删除全表 / The filter wrapper; no condition deletes the whole table</param>
        /// <returns>含 SQL 与参数的 SqlInfo / The SqlInfo carrying SQL and parameters</returns>
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
        /// 构造按主键删除单条的 DELETE 语句（主键值参数化）
        /// Build a DELETE for one row by primary key (key parameterized)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="key">主键值 / The primary key value</param>
        /// <returns>含 SQL 与主键参数的 SqlInfo / The SqlInfo carrying SQL and the key parameter</returns>
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
        /// 构造按主键值列表批量删除的 DELETE 语句（IN 条件全参数化）
        /// Build a batch DELETE by primary key list (fully parameterized IN condition)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="keyList">主键值列表 / The primary key values</param>
        /// <returns>含 SQL 与 IN 参数的 SqlInfo / The SqlInfo carrying SQL and the IN parameters</returns>
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
        /// 构造按 Lambda 条件统计行数的 SELECT COUNT(1) 语句
        /// Build a SELECT COUNT(1) statement with a lambda condition
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="wrapper">Lambda 条件包装器 / The lambda wrapper</param>
        /// <returns>含 SQL 与参数的 SqlInfo（COUNT 结果列别名为 _record_count_）/ The SqlInfo carrying SQL and parameters (COUNT aliased _record_count_)</returns>
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
        /// 构造按 Filter 条件统计行数的 SELECT COUNT(1) 语句
        /// Build a SELECT COUNT(1) statement with a filter condition
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="wrapper">Filter 条件包装器；null 统计全表 / The filter wrapper; null counts the whole table</param>
        /// <returns>含 SQL 与参数的 SqlInfo（COUNT 结果列别名为 _record_count_）/ The SqlInfo carrying SQL and parameters (COUNT aliased _record_count_)</returns>
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
        /// 解析要查询/更新的字段映射列表；fields 为 null 或空时返回全部列映射
        /// Resolve the field mappings to select/update; returns all column mappings when fields is null or empty
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="mapping">表映射；null 时从模型解析 / Table mapping; resolved from the model when null</param>
        /// <param name="fields">字段名列表（属性名或列名，经 ResolveColumnName 解析）/ Field names (property or column names, resolved via ResolveColumnName)</param>
        /// <returns>字段映射列表；字段在映射中不存在时抛出无效参数异常 / The field mapping list; throws for fields missing from the mapping</returns>
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
        /// 从模型实例构造 UPDATE SET 子句（跳过主键；值经 DbParameterValueHandler 处理后参数化）
        /// Build the UPDATE SET clause from a model instance (primary key skipped; values parameterized via DbParameterValueHandler)
        /// </summary>
        /// <typeparam name="M">模型类型 / The model type</typeparam>
        /// <param name="model">携带新值的模型实例 / The model instance carrying new values</param>
        /// <param name="mapping">表映射；null 时从模型解析 / Table mapping; resolved from the model when null</param>
        /// <param name="sqlInfo">接收参数的 SQL 信息对象 / The SQL info object receiving parameters</param>
        /// <param name="updateFields">要更新的字段；null 表示除主键外全部列 / Fields to update; null means all non-pk columns</param>
        /// <returns>For example: `Name` = @Name, `Price` = @Price, ... / 例如：`Name` = @Name, `Price` = @Price, ...</returns>
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
        /// 从 UpdateFields 字典构造 UPDATE SET 子句（键为列名，值经 DbParameterValueHandler 处理后参数化）
        /// Build the UPDATE SET clause from an UpdateFields dictionary (keys are column names; values parameterized via DbParameterValueHandler)
        /// </summary>
        /// <param name="updateFields">键为列名、值为新值的更新字段字典（不能为空）/ The field dictionary keyed by column name (must not be empty)</param>
        /// <param name="sqlInfo">接收参数的 SQL 信息对象 / The SQL info object receiving parameters</param>
        /// <param name="mapping">表映射 / Table mapping</param>
        /// <returns>For example: `Name` = @Name, `Price` = @Price, ... / 例如：`Name` = @Name, `Price` = @Price, ...</returns>
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
        /// 格式化写入（SET/VALUES）用的参数表达式：PostgreSQL 的 Jsonb 列在参数后追加 "::jsonb" 强转，其余原样返回
        /// Format a write (SET/VALUES) parameter expression: appends "::jsonb" for Jsonb columns on PostgreSQL; passthrough elsewhere
        /// </summary>
        /// <remarks>
        /// PostgreSQL 不会把 text 参数隐式写入 jsonb 字段；映射明确标记后统一在 SQL 侧转换。
        /// 其他数据库保持原参数表达式，避免引入方言语法。
        /// PostgreSQL does not implicitly cast text parameters into jsonb columns; the cast is applied on the SQL side
        /// once the mapping marks the column. Other databases keep the raw parameter expression to avoid dialect syntax.
        /// </remarks>
        /// <param name="fieldMapping">目标字段映射（含 Jsonb 标记）/ The target field mapping (carrying the Jsonb flag)</param>
        /// <param name="formattedParameter">已格式化的参数占位符 / The formatted parameter placeholder</param>
        /// <returns>写入用参数表达式，如 @Name 或 @Data::jsonb / The parameter expression for writes, e.g. @Name or @Data::jsonb</returns>
        protected virtual string FormatWriteParameter(FieldMapping fieldMapping, string formattedParameter)
        {
            return DatabaseType == DatabaseType.PostgreSQL && fieldMapping.Jsonb
                ? $"{formattedParameter}::jsonb"
                : formattedParameter;
        }

        #endregion

    }
}
