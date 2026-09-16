using Rampage.ExceptionExtensions;
using Rampage.Extensions;
using Rampage.Reflection;
using Rampage.Utilities;
using System.Collections;
using System.Reflection;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// Data Filter Condition Parser
    /// </summary>
    public class FilterParser
    {
        protected const string EX_KEY_InvalidFilterCondition = "Invalid_FilterCondition";

        public SqlTemplate SqlTemplate { get; protected set; }

        private SqlConditionBuilder ConditionBuilder { get; set; }

        public IDictionary<string, FilterConditionParameter> Parameters { get; private set; }

        public string GetConditionString() => ConditionBuilder.ToSql();

        private void RegisterParameter(string key, object? value, FieldMapping column)
        {
            Parameters[key] = new FilterConditionParameter
            {
                Name = key,
                Value = value,
                Column = column
            };
        }

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
