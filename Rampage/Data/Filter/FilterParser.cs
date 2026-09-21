using Rampage.ExceptionExtensions;
using Rampage.Extensions;
using Rampage.Reflection;
using Rampage.Utilities;
using System.Collections;
using System.Reflection;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 数据筛选器条件解析器：把 <see cref="DataFilter"/> 各属性值解析为 SQL WHERE 片段与参数
    /// Data filter condition parser: turns each property value of a <see cref="DataFilter"/> into a SQL WHERE fragment with parameters
    /// </summary>
    /// <remarks>
    /// 空值语义：属性值为 null 时该项整体跳过；枚举集合为空时该项条件整体丢弃（与 Lambda 路径的空集合生成 1=2 不同）；
    /// ValueScope 区间两端都为 null 时同样丢弃。各项按结构缓存的 SortOrder 排序，用属性上的 Connector 连接，
    /// OrFields 附加字段与主字段构成 OR 条件组。
    /// Empty semantics: an item whose property value is null is skipped entirely; an empty enumeration drops the whole item
    /// (unlike the lambda path, where an empty collection yields 1=2); a ValueScope with both ends null is dropped too.
    /// Items are ordered by the cached SortOrder and joined with each item's Connector; OrFields extra fields form an OR group with the main field.
    /// </remarks>
    public class FilterParser
    {
        /// <summary>
        /// 异常键：筛选条件无效（字段在模型映射中不存在等）
        /// Exception key: invalid filter condition (e.g. a field missing from the model mapping)
        /// </summary>
        protected const string EX_KEY_InvalidFilterCondition = "Invalid_FilterCondition";

        /// <summary>
        /// 目标数据库的 SQL 模板（参数与列名格式等方言设置）
        /// SQL template of the target database (dialect settings such as parameter/column formatting)
        /// </summary>
        public SqlTemplate SqlTemplate { get; protected set; }

        private SqlConditionBuilder ConditionBuilder { get; set; }

        /// <summary>
        /// 解析产出的条件参数集合（参数名 -> 参数）
        /// Condition parameters produced by parsing (parameter name -> parameter)
        /// </summary>
        public IDictionary<string, FilterConditionParameter> Parameters { get; private set; }

        /// <summary>
        /// 取得生成的 WHERE 条件 SQL 片段
        /// Get the generated WHERE condition SQL fragment
        /// </summary>
        /// <returns>条件 SQL 片段 / Condition SQL fragment</returns>
        public string GetConditionString() => ConditionBuilder.ToSql();

        /// <summary>
        /// 注册一个条件参数到 <see cref="Parameters"/>
        /// Register a condition parameter into <see cref="Parameters"/>
        /// </summary>
        /// <param name="key">参数名 / Parameter name</param>
        /// <param name="value">参数值 / Parameter value</param>
        /// <param name="column">列映射信息 / Column mapping</param>
        private void RegisterParameter(string key, object? value, FieldMapping column)
        {
            Parameters[key] = new FilterConditionParameter
            {
                Name = key,
                Value = value,
                Column = column
            };
        }

        /// <summary>
        /// 把筛选运算符映射为 SQL 运算符文本（区间类仅用于判定开闭，不在此映射）
        /// Map a filter operator to its SQL operator text (scope operators only decide open/closed bounds elsewhere)
        /// </summary>
        /// <param name="op">筛选运算符 / Filter operator</param>
        /// <returns>SQL 运算符文本 / SQL operator text</returns>
        private static string GetOperatorString(FilterOperatorType op)
        {
            return op switch
            {
                FilterOperatorType.NotEqual => " <> ",
                FilterOperatorType.GreaterThan => " > ",
                FilterOperatorType.GreaterThanOrEqual => " >= ",
                FilterOperatorType.LessThan => " < ",
                FilterOperatorType.LessThanOrEqual => " <= ",
                FilterOperatorType.Like or FilterOperatorType.LeftLike or FilterOperatorType.RightLike => " LIKE ",
                _ => " = ",
            };
        }

        /// <summary>
        /// 构建单个筛选项的条件：主字段（FieldName）与 OrFields 附加字段分别生成子条件后合并为 OR 条件组
        /// Build one filter item's condition: the main field (FieldName) plus each OrFields extra field produce sub-conditions merged into an OR group
        /// </summary>
        /// <remarks>参数只按主字段注册一次（registerParams 仅对首字段为 true），OR 组内各字段引用同名参数。/ Parameters are registered once for the main field only (registerParams is true only for the first field); fields in the OR group share the parameter names.</remarks>
        /// <param name="filterItem">筛选项特性 / Filter item attribute</param>
        /// <param name="mapping">模型映射 / Model mapping</param>
        /// <param name="filterItemValue">筛选器属性当前值 / Current value of the filter property</param>
        /// <returns>该项的条件 SQL，无可生成条件时返回 null / The item's condition SQL, or null when nothing applies</returns>
        private string? BuildItemCondition(FilterItemAttribute filterItem, TableMapping mapping, object filterItemValue)
        {
            PropertyInfo filterItemPrp = filterItem.Property!;
            Type filterItemPrpType = filterItemPrp.PropertyType;
            List<string> fieldList = filterItem.OrFieldList!.Select(f =>
            {
                var cp = mapping.ColumnMappingList!.Find(c => c.PropertyName == f || string.Compare(c.ColumnName, f, true) == 0);
                if (cp == null)
                {
                    throw new SuperException(EX_KEY_InvalidFilterCondition);
                }
                return cp.ColumnName!;
            }).ToList();

            FieldMapping? pm = mapping.ColumnMappingList!.Find(c => c.PropertyName == filterItem.FieldName! || string.Compare(c.ColumnName, filterItem.FieldName, true) == 0);
            if (pm == null)
            {
                throw new SuperException(EX_KEY_InvalidFilterCondition);
            }
            fieldList.Insert(0, pm.ColumnName!);

            List<string> fieldConditions = new List<string>();
            for (int f = 0; f < fieldList.Count; f++)
            {
                string? fieldCondition = BuildFieldCondition(filterItem, filterItemPrp, filterItemPrpType, filterItemValue, fieldList[f], pm, f == 0);
                if (!string.IsNullOrEmpty(fieldCondition))
                {
                    fieldConditions.Add(fieldCondition);
                }
            }

            return SqlConditionBuilder.WrapOrGroup(fieldConditions);
        }

        /// <summary>
        /// 按属性类型分派构建单个字段的子条件：ValueScope 区间 / 枚举集合 IN / 标量比较
        /// Dispatch by property type to build one field's sub-condition: ValueScope range / enumerable IN / scalar comparison
        /// </summary>
        /// <param name="filterItem">筛选项特性 / Filter item attribute</param>
        /// <param name="filterItemPrp">筛选器属性 / Filter property</param>
        /// <param name="filterItemPrpType">属性类型 / Property type</param>
        /// <param name="filterItemValue">属性当前值 / Current property value</param>
        /// <param name="fieldName">目标列名 / Target column name</param>
        /// <param name="pm">主字段列映射 / Main field mapping</param>
        /// <param name="registerParams">是否注册参数（仅主字段为 true，避免 OR 组重复注册）/ Whether to register parameters (true only for the main field, avoiding duplicate registrations in the OR group)</param>
        /// <returns>子条件 SQL，无则返回 null / The sub-condition SQL, or null</returns>
        private string? BuildFieldCondition(FilterItemAttribute filterItem, PropertyInfo filterItemPrp, Type filterItemPrpType, object filterItemValue, string fieldName, FieldMapping pm, bool registerParams)
        {
            if (filterItemPrpType.IsGenericType && filterItemPrpType.GetGenericTypeDefinition() == typeof(ValueScope<>))
            {
                return BuildValueScopeCondition(filterItem, filterItemPrp, filterItemValue, fieldName, pm, registerParams);
            }

            Type? genTypeDef = ReflectionTool.GetGenericTypeDefinition(filterItemPrpType);
            if (genTypeDef != null && filterItemPrpType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(genTypeDef) && filterItemPrpType.GetGenericArguments().Length > 0)
            {
                return BuildEnumerableCondition(filterItem, filterItemPrp, filterItemValue, fieldName, pm, registerParams);
            }

            return BuildScalarCondition(filterItem, filterItemPrp, filterItemPrpType, filterItemValue, fieldName, pm, registerParams);
        }

        /// <summary>
        /// 构建 ValueScope 区间条件：按 <see cref="FilterOperatorType.LeftOpenScope"/>/<see cref="FilterOperatorType.RightOpenScope"/>
        /// 决定两端开闭；起止值允许只设一端，两端皆空返回 null（该项丢弃）
        /// Build a ValueScope range condition: open/closed bounds decided by <see cref="FilterOperatorType.LeftOpenScope"/>/
        /// <see cref="FilterOperatorType.RightOpenScope"/>; either end may be omitted, and both-empty returns null (item dropped)
        /// </summary>
        /// <param name="filterItem">筛选项特性 / Filter item attribute</param>
        /// <param name="filterItemPrp">筛选器属性 / Filter property</param>
        /// <param name="filterItemValue">区间值 / Scope value</param>
        /// <param name="fieldName">目标列名 / Target column name</param>
        /// <param name="pm">主字段列映射 / Main field mapping</param>
        /// <param name="registerParams">是否注册参数 / Whether to register parameters</param>
        /// <returns>区间条件 SQL，无则返回 null / The range condition SQL, or null</returns>
        private string? BuildValueScopeCondition(FilterItemAttribute filterItem, PropertyInfo filterItemPrp, object filterItemValue, string fieldName, FieldMapping pm, bool registerParams)
        {
            string scopeStartParamKey = string.Concat("Start_", filterItemPrp.Name);
            string scopeEndParamKey = string.Concat("End_", filterItemPrp.Name);
            IValueScope tempScopeObj = (IValueScope)filterItemValue;
            object? scopeStartValue = tempScopeObj.GetStartValue();
            object? scopeEndValue = tempScopeObj.GetEndValue();

            if (scopeStartValue == null && scopeEndValue == null)
            {
                return null;
            }

            string leftScopeOperator = filterItem.Operator == FilterOperatorType.LeftOpenScope ? " > " : " >= ";
            string rightScopeOperator = filterItem.Operator == FilterOperatorType.RightOpenScope ? " < " : " <= ";
            string safeFieldName = ConditionBuilder.FormatColumn(fieldName);
            string scopeStartParamName = SqlTemplate.FormatParameter(scopeStartParamKey);
            string scopeEndParamName = SqlTemplate.FormatParameter(scopeEndParamKey);

            if (scopeStartValue != null && scopeEndValue != null)
            {
                if (registerParams)
                {
                    RegisterParameter(scopeStartParamKey, scopeStartValue, pm);
                    RegisterParameter(scopeEndParamKey, scopeEndValue, pm);
                }
                return string.Concat(safeFieldName, leftScopeOperator, scopeStartParamName, " AND ", safeFieldName, rightScopeOperator, scopeEndParamName);
            }

            if (scopeStartValue != null)
            {
                if (registerParams)
                {
                    RegisterParameter(scopeStartParamKey, scopeStartValue, pm);
                }
                return string.Concat(safeFieldName, leftScopeOperator, scopeStartParamName);
            }

            if (registerParams)
            {
                RegisterParameter(scopeEndParamKey, scopeEndValue, pm);
            }
            return string.Concat(safeFieldName, rightScopeOperator, scopeEndParamName);
        }

        /// <summary>
        /// 构建枚举集合的 IN（运算符为 NotEqual 时为 NOT IN）条件；集合为空时返回 null（该项整体丢弃，注意与 Lambda 路径生成 1=2 的差异）
        /// Build an IN (NOT IN when the operator is NotEqual) condition for an enumerable; an empty collection returns null (item dropped - note this differs from the lambda path's 1=2)
        /// </summary>
        /// <param name="filterItem">筛选项特性 / Filter item attribute</param>
        /// <param name="filterItemPrp">筛选器属性 / Filter property</param>
        /// <param name="filterItemValue">集合值 / Collection value</param>
        /// <param name="fieldName">目标列名 / Target column name</param>
        /// <param name="pm">主字段列映射 / Main field mapping</param>
        /// <param name="registerParams">是否注册参数 / Whether to register parameters</param>
        /// <returns>IN 条件 SQL，无则返回 null / The IN condition SQL, or null</returns>
        private string? BuildEnumerableCondition(FilterItemAttribute filterItem, PropertyInfo filterItemPrp, object filterItemValue, string fieldName, FieldMapping pm, bool registerParams)
        {
            IEnumerable ieb = (IEnumerable)filterItemValue;
            if (!ieb.Cast<object?>().Any())
            {
                return null;
            }

            string paramPrefix = filterItemPrp.Name + "_In";
            var paramRefs = new List<string>();
            int inIndex = 0;
            foreach (var en in ieb)
            {
                string key = $"{paramPrefix}_{inIndex}";
                paramRefs.Add(SqlTemplate.FormatParameter(key));
                if (registerParams)
                {
                    RegisterParameter(key, en, pm);
                }
                inIndex++;
            }

            string operatorName = filterItem.Operator == FilterOperatorType.NotEqual ? " NOT IN " : " IN ";
            return string.Concat(ConditionBuilder.FormatColumn(fieldName), operatorName, "(", string.Join(",", paramRefs), ")");
        }

        /// <summary>
        /// 构建标量比较条件：字符串配合模糊运算符（Like/LeftLike/RightLike）自动拼接 % 通配符；非值类型/非字符串属性返回 null
        /// Build a scalar comparison condition: string values with fuzzy operators (Like/LeftLike/RightLike) get % wildcards concatenated; non-value, non-string properties return null
        /// </summary>
        /// <param name="filterItem">筛选项特性 / Filter item attribute</param>
        /// <param name="filterItemPrp">筛选器属性 / Filter property</param>
        /// <param name="filterItemPrpType">属性类型 / Property type</param>
        /// <param name="filterItemValue">属性当前值 / Current property value</param>
        /// <param name="fieldName">目标列名 / Target column name</param>
        /// <param name="pm">主字段列映射 / Main field mapping</param>
        /// <param name="registerParams">是否注册参数 / Whether to register parameters</param>
        /// <returns>比较条件 SQL，无则返回 null / The comparison condition SQL, or null</returns>
        private string? BuildScalarCondition(FilterItemAttribute filterItem, PropertyInfo filterItemPrp, Type filterItemPrpType, object filterItemValue, string fieldName, FieldMapping pm, bool registerParams)
        {
            Type filterItemPrpBaseType = GenericTool.GetUnderlyingType(filterItemPrpType);
            if (!(filterItemPrpBaseType.IsValueType || filterItemPrpBaseType.IsEnum || filterItemPrpBaseType == typeof(Guid) || filterItemPrpBaseType == typeof(string)))
            {
                return null;
            }

            string operatorName = GetOperatorString(filterItem.Operator);
            if (filterItemPrpBaseType == typeof(string))
            {
                filterItemValue = filterItem.Operator switch
                {
                    FilterOperatorType.Like => string.Concat("%", filterItemValue, "%"),
                    FilterOperatorType.LeftLike => string.Concat("%", filterItemValue),
                    FilterOperatorType.RightLike => string.Concat(filterItemValue, "%"),
                    _ => filterItemValue
                };
            }

            if (registerParams)
            {
                RegisterParameter(filterItemPrp.Name, filterItemValue, pm);
            }

            return string.Concat(ConditionBuilder.FormatColumn(fieldName), operatorName, SqlTemplate.FormatParameter(filterItemPrp.Name));
        }

        /// <summary>
        /// 解析筛选器：遍历结构中的筛选项（按 SortOrder 排序），值为 null 或 Ignore 的项跳过，
        /// 其余逐项生成条件并以各项 Connector（AND/OR）连接为完整 WHERE 片段
        /// Parse the filter: walks schema items (ordered by SortOrder), skips null-valued or Ignored items,
        /// and joins the remaining item conditions with each item's Connector (AND/OR) into the full WHERE fragment
        /// </summary>
        /// <param name="filter">数据筛选器实例 / Data filter instance</param>
        /// <param name="mapping">模型映射 / Model mapping</param>
        /// <param name="template">目标数据库 SQL 模板 / Target SQL template</param>
        public FilterParser(DataFilter filter, TableMapping mapping, SqlTemplate template)
        {
            SqlTemplate = template;
            Parameters = new Dictionary<string, FilterConditionParameter>();
            ConditionBuilder = new SqlConditionBuilder(template, new BuildParameterSink((_, __, ___) => { }));

            Type filterType = filter.GetType();
            FilterSchema? schema = FilterSchemaStore.GetFilterSchema(filterType);
            if (schema?.ItemList == null || schema.ItemList.Count == 0)
            {
                return;
            }

            bool isFirstCondition = true;
            for (int i = 0; i < schema.ItemList.Count; i++)
            {
                FilterItemAttribute filterItem = schema.ItemList[i];
                if (filterItem.Ignore)
                {
                    continue;
                }

                PropertyInfo filterItemPrp = filterItem.Property!;
                object? filterItemValue = EmitTool.GetPropertyValue(filter, filterItemPrp.Name);
                if (filterItemValue == null)
                {
                    continue;
                }

                string connectorStr = filterItem.Connector == FilterConnectorType.Or ? "OR" : "AND";
                string? itemCondition = BuildItemCondition(filterItem, mapping, filterItemValue);
                if (string.IsNullOrEmpty(itemCondition))
                {
                    continue;
                }

                ConditionBuilder.AppendConnector(isFirstCondition, connectorStr);
                ConditionBuilder.Append(itemCondition);
                isFirstCondition = false;
            }
        }
    }
}
