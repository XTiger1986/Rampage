using System.Collections;
using System.Text;

namespace Rampage.Data
{
    /// <summary>
    /// Shared SQL WHERE fragment builder for Filter and Lambda condition parsers.
    /// </summary>
    public class SqlConditionBuilder
    {
        private readonly SqlTemplate _template;
        private readonly StringBuilder _sql;
        private readonly HashSet<string> _usedParamKeys;
        private readonly IConditionParameterSink _parameterSink;

        public SqlConditionBuilder(SqlTemplate template, IConditionParameterSink parameterSink)
        {
            _template = template;
            _parameterSink = parameterSink;
            _sql = new StringBuilder();
            _usedParamKeys = new HashSet<string>();
        }

        public SqlTemplate Template => _template;

        public string ToSql() => _sql.ToString();

        public StringBuilder Buffer => _sql;

        public void Append(string text) => _sql.Append(text);

        public void AppendOpenGroup() => _sql.Append("(");

        public void AppendCloseGroup() => _sql.Append(")");

        public void AppendNullLiteral() => _sql.Append("NULL");

        public void AppendFalseCondition() => _sql.Append(" 1 = 2 ");

        public string FormatColumn(string columnName) => _template.FormatColumn(columnName);

        public void AppendColumn(string columnName) => _sql.Append(FormatColumn(columnName));

        public void AppendOperator(string operatorSql) => _sql.Append(operatorSql);

        /// <summary>
        /// Append a connector between top-level condition items (AND / OR).
        /// </summary>
        public void AppendConnector(bool isFirstItem, string connector)
        {
            if (isFirstItem)
            {
                return;
            }

            _sql.Append(' ').Append(connector).Append(' ');
        }

        public string RegisterParameter(string baseName, object? value, FieldMapping column)
        {
            string paramKey = _parameterSink.AllocateKey(baseName, _usedParamKeys);
            _usedParamKeys.Add(paramKey);
            _parameterSink.Register(paramKey, value, column);
            return _template.FormatParameter(paramKey);
        }

        public void AppendComparison(string columnName, string operatorSql, string formattedParameter)
        {
            _sql.Append(FormatColumn(columnName));
            _sql.Append(operatorSql);
            _sql.Append(formattedParameter);
        }

        public void AppendLike(string columnName, string formattedParameter)
        {
            _sql.Append(FormatColumn(columnName));
            _sql.Append(" LIKE ");
            _sql.Append(formattedParameter);
        }

        public void AppendIn(string columnName, IEnumerable values, string paramPrefix, FieldMapping column, bool notIn = false)
        {
            List<string> paramRefs = new List<string>();
            int index = 0;
            foreach (object? item in values)
            {
                paramRefs.Add(RegisterParameter($"{paramPrefix}_{index}", item, column));
                index++;
            }

            string operatorSql = notIn ? " NOT IN " : " IN ";
            _sql.Append(FormatColumn(columnName));
            _sql.Append(operatorSql);
            _sql.Append('(');
            _sql.Append(string.Join(",", paramRefs));
            _sql.Append(')');
        }

        public void AppendRange(string columnName, string leftOperator, string startParameter,
            string rightOperator, string endParameter)
        {
            _sql.Append(FormatColumn(columnName));
            _sql.Append(leftOperator);
            _sql.Append(startParameter);
            _sql.Append(" AND ");
            _sql.Append(FormatColumn(columnName));
            _sql.Append(rightOperator);
            _sql.Append(endParameter);
        }

        /// <summary>
        /// Wrap non-empty parts as "( part1 OR part2 ... )".
        /// </summary>
        public static string? WrapOrGroup(IEnumerable<string?> parts)
        {
            List<string> validParts = parts.Where(p => !string.IsNullOrEmpty(p)).ToList()!;
            if (validParts.Count == 0)
            {
                return null;
            }

            return string.Concat("( ", string.Join(" OR ", validParts), " )");
        }
    }

    /// <summary>
    /// Parameter registration strategy for condition builders.
    /// </summary>
    public interface IConditionParameterSink
    {
        string AllocateKey(string baseName, ISet<string> usedKeys);

        void Register(string key, object? value, FieldMapping column);
    }

    /// <summary>
    /// Allocates unique parameter keys during initial SQL compilation.
    /// </summary>
    public class BuildParameterSink : IConditionParameterSink
    {
        private readonly Action<string, object?, FieldMapping> _register;

        public BuildParameterSink(Action<string, object?, FieldMapping> register)
        {
            _register = register;
        }

        public string AllocateKey(string baseName, ISet<string> usedKeys)
        {
            string paramKey = baseName;
            int suffix = 0;
            while (usedKeys.Contains(paramKey))
            {
                suffix++;
                paramKey = $"{baseName}_{suffix}";
            }

            return paramKey;
        }

        public void Register(string key, object? value, FieldMapping column)
        {
            _register(key, value, column);
        }
    }

    /// <summary>
    /// Reuses cached parameter keys when rebinding runtime values.
    /// </summary>
    public class ExtractParameterSink : IConditionParameterSink
    {
        private readonly Queue<string> _orderedKeys;
        private readonly Action<string, object?, FieldMapping> _register;

        public ExtractParameterSink(IEnumerable<string> orderedKeys, Action<string, object?, FieldMapping> register)
        {
            _orderedKeys = new Queue<string>(orderedKeys);
            _register = register;
        }

        public string AllocateKey(string baseName, ISet<string> usedKeys)
        {
            if (_orderedKeys.Count == 0)
            {
                throw new InvalidOperationException("Parameter key count mismatch during lambda condition value extraction.");
            }

            string key = _orderedKeys.Dequeue();
            usedKeys.Add(key);
            return key;
        }

        public void Register(string key, object? value, FieldMapping column)
        {
            _register(key, value, column);
        }
    }

}
