using System.Collections;
using System.Text;

namespace Rampage.Data
{
    /// <summary>
    /// 供筛选器与 Lambda 条件解析器共用的 SQL WHERE 片段构建器。
    /// Shared SQL WHERE fragment builder for the Filter and Lambda condition parsers.
    /// </summary>
    /// <remarks>参数注册策略由 <see cref="IConditionParameterSink"/> 注入：首次编译用 <see cref="BuildParameterSink"/> 分配新键，缓存命中重放用 <see cref="ExtractParameterSink"/> 复用键序。/ The parameter strategy is injected via <see cref="IConditionParameterSink"/>: <see cref="BuildParameterSink"/> allocates fresh keys on first compilation, <see cref="ExtractParameterSink"/> replays cached keys on cache hits.</remarks>
    public class SqlConditionBuilder
    {
        private readonly SqlTemplate _template;
        private readonly StringBuilder _sql;
        private readonly HashSet<string> _usedParamKeys;
        private readonly IConditionParameterSink _parameterSink;

        /// <summary>
        /// 以 SQL 模板与参数注册策略初始化构建器
        /// Initialize the builder with a SQL template and a parameter sink
        /// </summary>
        /// <param name="template">SQL 模板 / SQL template</param>
        /// <param name="parameterSink">参数注册策略 / Parameter registration strategy</param>
        public SqlConditionBuilder(SqlTemplate template, IConditionParameterSink parameterSink)
        {
            _template = template;
            _parameterSink = parameterSink;
            _sql = new StringBuilder();
            _usedParamKeys = new HashSet<string>();
        }

        /// <summary>
        /// 构建使用的 SQL 模板
        /// The SQL template in use
        /// </summary>
        public SqlTemplate Template => _template;

        /// <summary>
        /// 取得已构建的 SQL 文本
        /// Get the built SQL text
        /// </summary>
        /// <returns>SQL 文本 / The SQL text</returns>
        public string ToSql() => _sql.ToString();

        /// <summary>
        /// 直接访问内部 SQL 缓冲区
        /// Direct access to the internal SQL buffer
        /// </summary>
        public StringBuilder Buffer => _sql;

        /// <summary>
        /// 追加任意 SQL 文本
        /// Append arbitrary SQL text
        /// </summary>
        /// <param name="text">SQL 文本 / SQL text</param>
        public void Append(string text) => _sql.Append(text);

        /// <summary>
        /// 追加左括号（开启一个条件组）
        /// Append an opening parenthesis (start a condition group)
        /// </summary>
        public void AppendOpenGroup() => _sql.Append("(");

        /// <summary>
        /// 追加右括号（结束一个条件组）
        /// Append a closing parenthesis (end a condition group)
        /// </summary>
        public void AppendCloseGroup() => _sql.Append(")");

        /// <summary>
        /// 追加 NULL 字面量
        /// Append the NULL literal
        /// </summary>
        public void AppendNullLiteral() => _sql.Append("NULL");

        /// <summary>
        /// 追加恒假条件（1 = 2），用于空集合 IN 等需要恒假的场景
        /// Append the always-false condition (1 = 2), e.g. for an IN over an empty collection
        /// </summary>
        public void AppendFalseCondition() => _sql.Append(" 1 = 2 ");

        /// <summary>
        /// 按模板格式化列名（如加引号/限定符）
        /// Format a column name per the template (quoting/qualifiers)
        /// </summary>
        /// <param name="columnName">列名 / Column name</param>
        /// <returns>格式化后的列名 / The formatted column name</returns>
        public string FormatColumn(string columnName) => _template.FormatColumn(columnName);

        /// <summary>
        /// 追加格式化后的列名
        /// Append a formatted column name
        /// </summary>
        /// <param name="columnName">列名 / Column name</param>
        public void AppendColumn(string columnName) => _sql.Append(FormatColumn(columnName));

        /// <summary>
        /// 追加运算符 SQL 文本
        /// Append an operator SQL text
        /// </summary>
        /// <param name="operatorSql">运算符文本 / Operator text</param>
        public void AppendOperator(string operatorSql) => _sql.Append(operatorSql);

        /// <summary>
        /// 在顶层条件项之间追加连接符（AND / OR），首个条件项不追加。
        /// Append a connector between top-level condition items (AND / OR); nothing is appended before the first item.
        /// </summary>
        /// <param name="isFirstItem">当前是否首个条件项 / Whether the current item is the first one</param>
        /// <param name="connector">连接符（AND 或 OR）/ Connector (AND or OR)</param>
        public void AppendConnector(bool isFirstItem, string connector)
        {
            if (isFirstItem)
            {
                return;
            }

            _sql.Append(' ').Append(connector).Append(' ');
        }

        /// <summary>
        /// 注册一个参数并返回格式化后的参数占位符（键由注入的策略分配）
        /// Register a parameter and return the formatted placeholder (the key is allocated by the injected strategy)
        /// </summary>
        /// <param name="baseName">参数基础名 / Parameter base name</param>
        /// <param name="value">参数值 / Parameter value</param>
        /// <param name="column">列映射信息 / Column mapping</param>
        /// <returns>格式化后的参数占位符 / The formatted parameter placeholder</returns>
        public string RegisterParameter(string baseName, object? value, FieldMapping column)
        {
            string paramKey = _parameterSink.AllocateKey(baseName, _usedParamKeys);
            _usedParamKeys.Add(paramKey);
            _parameterSink.Register(paramKey, value, column);
            return _template.FormatParameter(paramKey);
        }

        /// <summary>
        /// 追加"列 运算符 参数"形式的比较条件
        /// Append a comparison of the form "column operator parameter"
        /// </summary>
        /// <param name="columnName">列名 / Column name</param>
        /// <param name="operatorSql">运算符文本 / Operator text</param>
        /// <param name="formattedParameter">已格式化的参数占位符 / Formatted parameter placeholder</param>
        public void AppendComparison(string columnName, string operatorSql, string formattedParameter)
        {
            _sql.Append(FormatColumn(columnName));
            _sql.Append(operatorSql);
            _sql.Append(formattedParameter);
        }

        /// <summary>
        /// 追加"列 LIKE 参数"形式的模糊匹配条件
        /// Append a "column LIKE parameter" fuzzy-match condition
        /// </summary>
        /// <param name="columnName">列名 / Column name</param>
        /// <param name="formattedParameter">已格式化的参数占位符（通常已带 % 通配符）/ Formatted parameter placeholder (usually already carrying % wildcards)</param>
        public void AppendLike(string columnName, string formattedParameter)
        {
            _sql.Append(FormatColumn(columnName));
            _sql.Append(" LIKE ");
            _sql.Append(formattedParameter);
        }

        /// <summary>
        /// 追加 IN / NOT IN 条件：集合每个元素注册为独立参数后逐个展开
        /// Append an IN / NOT IN condition: every element of the collection is registered as its own parameter and expanded
        /// </summary>
        /// <param name="columnName">列名 / Column name</param>
        /// <param name="values">集合值（空集合仍会生成空括号，调用方需自行改用恒假条件）/ Collection values (an empty collection still yields empty parentheses; callers must emit an always-false condition themselves)</param>
        /// <param name="paramPrefix">参数名前缀 / Parameter name prefix</param>
        /// <param name="column">列映射信息 / Column mapping</param>
        /// <param name="notIn">true 时生成 NOT IN / Generate NOT IN when true</param>
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

        /// <summary>
        /// 追加区间条件：列 左运算符 起始参数 AND 列 右运算符 结束参数
        /// Append a range condition: column leftOp startParameter AND column rightOp endParameter
        /// </summary>
        /// <param name="columnName">列名 / Column name</param>
        /// <param name="leftOperator">起始端运算符（如 &gt;=）/ Left (start) operator (e.g. &gt;=)</param>
        /// <param name="startParameter">起始参数占位符 / Start parameter placeholder</param>
        /// <param name="rightOperator">结束端运算符（如 &lt;）/ Right (end) operator (e.g. &lt;)</param>
        /// <param name="endParameter">结束参数占位符 / End parameter placeholder</param>
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
        /// 把非空的各部分包装为 "( part1 OR part2 ... )" 条件组；全部为空时返回 null。
        /// Wrap non-empty parts as "( part1 OR part2 ... )"; returns null when all parts are empty.
        /// </summary>
        /// <param name="parts">各部分条件（可为 null 或空串）/ Condition parts (null or empty allowed)</param>
        /// <returns>OR 条件组 SQL，无有效部分时为 null / The OR group SQL, or null when nothing is valid</returns>
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
    /// 条件构建器的参数注册策略。
    /// Parameter registration strategy for condition builders.
    /// </summary>
    public interface IConditionParameterSink
    {
        /// <summary>
        /// 为参数基础名分配一个本次构建内唯一的键
        /// Allocate a key unique within this build for a parameter base name
        /// </summary>
        /// <param name="baseName">参数基础名 / Parameter base name</param>
        /// <param name="usedKeys">已占用的键集合 / Keys already used</param>
        /// <returns>分配出的参数键 / The allocated parameter key</returns>
        string AllocateKey(string baseName, ISet<string> usedKeys);

        /// <summary>
        /// 登记一个参数的键、值与列映射
        /// Record a parameter's key, value and column mapping
        /// </summary>
        /// <param name="key">参数键 / Parameter key</param>
        /// <param name="value">参数值 / Parameter value</param>
        /// <param name="column">列映射信息 / Column mapping</param>
        void Register(string key, object? value, FieldMapping column);
    }

    /// <summary>
    /// 首次编译时的参数键分配策略：重名时追加 _1/_2… 数字后缀分配新键。
    /// Allocates unique parameter keys during initial SQL compilation (numeric suffixes on collision).
    /// </summary>
    public class BuildParameterSink : IConditionParameterSink
    {
        private readonly Action<string, object?, FieldMapping> _register;

        /// <summary>
        /// 以参数登记回调初始化
        /// Initialize with the parameter registration callback
        /// </summary>
        /// <param name="register">登记回调 / Registration callback</param>
        public BuildParameterSink(Action<string, object?, FieldMapping> register)
        {
            _register = register;
        }

        /// <summary>
        /// 分配唯一参数键：与已用键重复时追加数字后缀
        /// Allocate a unique parameter key, appending a numeric suffix on collision
        /// </summary>
        /// <param name="baseName">参数基础名 / Parameter base name</param>
        /// <param name="usedKeys">已占用的键集合 / Keys already used</param>
        /// <returns>分配出的参数键 / The allocated parameter key</returns>
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

        /// <summary>
        /// 经回调登记参数
        /// Register the parameter via the callback
        /// </summary>
        /// <param name="key">参数键 / Parameter key</param>
        /// <param name="value">参数值 / Parameter value</param>
        /// <param name="column">列映射信息 / Column mapping</param>
        public void Register(string key, object? value, FieldMapping column)
        {
            _register(key, value, column);
        }
    }

    /// <summary>
    /// 缓存命中重放时的参数键复用策略：忽略基础名，按缓存键队列依序出队复用原参数名。
    /// Reuses cached parameter keys when rebinding runtime values: ignores base names and dequeues the cached key queue in order.
    /// </summary>
    public class ExtractParameterSink : IConditionParameterSink
    {
        private readonly Queue<string> _orderedKeys;
        private readonly Action<string, object?, FieldMapping> _register;

        /// <summary>
        /// 以缓存键序列与登记回调初始化
        /// Initialize with the cached key sequence and the registration callback
        /// </summary>
        /// <param name="orderedKeys">缓存命中的参数键（按首次编译顺序）/ Cached parameter keys in first-compilation order</param>
        /// <param name="register">登记回调 / Registration callback</param>
        public ExtractParameterSink(IEnumerable<string> orderedKeys, Action<string, object?, FieldMapping> register)
        {
            _orderedKeys = new Queue<string>(orderedKeys);
            _register = register;
        }

        /// <summary>
        /// 按序出队复用缓存键；队列耗尽说明实际表达式与缓存骨架参数数不一致，抛出异常
        /// Dequeue the next cached key in order; an exhausted queue means the expression's parameter count no longer matches the cached skeleton and throws
        /// </summary>
        /// <param name="baseName">参数基础名（被忽略）/ Parameter base name (ignored)</param>
        /// <param name="usedKeys">已占用的键集合 / Keys already used</param>
        /// <returns>复用的缓存参数键 / The reused cached parameter key</returns>
        /// <exception cref="InvalidOperationException">缓存键队列耗尽时抛出 / Thrown when the cached key queue is exhausted</exception>
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

        /// <summary>
        /// 经回调登记参数
        /// Register the parameter via the callback
        /// </summary>
        /// <param name="key">参数键 / Parameter key</param>
        /// <param name="value">参数值 / Parameter value</param>
        /// <param name="column">列映射信息 / Column mapping</param>
        public void Register(string key, object? value, FieldMapping column)
        {
            _register(key, value, column);
        }
    }

}
