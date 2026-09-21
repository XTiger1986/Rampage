using Rampage.Data.Extensions;
using Rampage.ExceptionExtensions;
using Rampage.Extensions;
using Rampage.Reflection;
using Rampage.Utilities;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Rampage.Data.Expressions
{
    /// <summary>
    /// Lambda 条件表达式到 SQL WHERE 片段的解析器（表达式访问器）
    /// Expression-visitor parser turning a lambda condition into a SQL WHERE fragment
    /// </summary>
    /// <remarks>
    /// 解析管线：表达式先经 <see cref="TrimExpression.Trim"/> 规整（闭包成员急切求值为常量、布尔成员改写为 ==true、剥除冗余 Convert），
    /// 再以结构键（<see cref="ExpressionStructureKey"/>，常量归一为 ?/L{count}）查询 <see cref="LambdaConditionCompileCache"/>：
    /// 命中缓存时不重新生成 SQL，仅按缓存顺序重放取参数（ExtractParameterSink 复用原参数名）；
    /// 未命中才由本类完整访问表达式树生成 SQL 片段与参数。
    /// Pipeline: the expression is first normalized by <see cref="TrimExpression.Trim"/> (closure members eagerly evaluated to
    /// constants, boolean members rewritten to ==true, redundant converts stripped), then looked up in
    /// <see cref="LambdaConditionCompileCache"/> by structure key (<see cref="ExpressionStructureKey"/>, constants normalized to ?/L{count});
    /// on a hit, SQL is not regenerated and parameters are re-extracted in cached order (ExtractParameterSink reuses the original
    /// parameter names); on a miss this class walks the tree to produce the SQL fragment and parameters.
    /// </remarks>
    public class LambdaCondition : ExpressionVisitor
    {
        /// <summary>
        /// 异常键：Lambda 表达式参数无效（如方法调用的实参不是预期的节点类型）
        /// Exception key: invalid lambda expression arguments (e.g. a method call argument is not the expected node type)
        /// </summary>
        public const string EX_KEY_InvalidLambdaExpressionArguments = "Invalid_LambdaExpressionArguments";
        /// <summary>
        /// 异常键：Lambda 条件中使用了不支持的构造（方法/运算符）
        /// Exception key: unsupported construct (method/operator) used inside the lambda condition
        /// </summary>
        public const string EX_KEY_LambdaConditionNotSupported = "LambdaCondition_NotSupported";
        /// <summary>
        /// 异常键：Lambda 条件中引用了模型映射里不存在的列
        /// Exception key: the lambda condition references a column missing from the model mapping
        /// </summary>
        public const string EX_KEY_UnknownColumnInLambdaExpression = "UnknownColumn_InLambdaExpression";

        private readonly SqlConditionBuilder _builder;
        private readonly bool _generateSql;
        private readonly string? _cachedSql;
        private readonly List<string> _parameterKeyOrder = new();

        /// <summary>
        /// 目标数据库的 SQL 模板（参数格式、列名格式等方言设置）
        /// SQL template of the target database (dialect settings such as parameter/column formatting)
        /// </summary>
        public SqlTemplate SqlTemplate { get; protected set; }
        /// <summary>
        /// 模型与数据表的映射（为 null 时按成员的声明类型自动推断）
        /// Model-to-table mapping (inferred from the member's declaring type when null)
        /// </summary>
        public TableMapping? Mapping { get; set; }
        /// <summary>
        /// 解析产出的条件参数集合（参数名 -> 参数）
        /// Condition parameters produced by parsing (parameter name -> parameter)
        /// </summary>
        public IDictionary<string, LambdaConditionParameter> Parameters { get; private set; }
        private string? CurrentColumnName { get; set; }
        private FieldMapping? CurrentColumn { get; set; }

        /// <summary>
        /// 取得生成的 WHERE 条件 SQL 片段（缓存命中时直接返回缓存的 SQL）
        /// Get the generated WHERE condition SQL fragment (returns the cached SQL on a cache hit)
        /// </summary>
        /// <returns>条件 SQL 片段 / Condition SQL fragment</returns>
        public string GetConditionString() => _cachedSql ?? _builder.ToSql();

        /// <summary>
        /// 导出为可缓存的编译条目（SQL 骨架 + 参数键顺序 + 参数列映射），供结构键相同的后续调用重放参数
        /// Export as a cacheable compile entry (SQL skeleton + ordered parameter keys + parameter columns) for later value replay
        /// </summary>
        /// <returns>编译条目 / The compile entry</returns>
        internal LambdaConditionCompileEntry ToCompileEntry()
        {
            return new LambdaConditionCompileEntry
            {
                ConditionSql = GetConditionString(),
                ParameterKeys = _parameterKeyOrder.ToArray(),
                ParameterColumns = _parameterKeyOrder.Select(k => Parameters[k].Column).ToArray()
            };
        }

        /// <summary>
        /// 缓存命中路径：不重新生成 SQL，重访已规整的表达式树，仅按缓存键顺序重放取运行时参数值
        /// Cache-hit path: reuse the cached SQL, re-walk the trimmed tree and only re-extract runtime parameter values in cached key order
        /// </summary>
        /// <param name="trimmedExpression">已规整的表达式树 / Trimmed expression tree</param>
        /// <param name="template">SQL 模板 / SQL template</param>
        /// <param name="mapping">模型映射 / Model mapping</param>
        /// <param name="cached">缓存命中的编译条目 / Cached compile entry</param>
        /// <returns>复用缓存 SQL 的条件实例 / A condition instance reusing the cached SQL</returns>
        internal static LambdaCondition FromCached(Expression trimmedExpression, SqlTemplate template, TableMapping mapping, LambdaConditionCompileEntry cached)
        {
            var parameters = new Dictionary<string, LambdaConditionParameter>();
            var sink = new ExtractParameterSink(cached.ParameterKeys, (key, value, column) =>
            {
                parameters[key] = new LambdaConditionParameter
                {
                    Name = key,
                    Value = value,
                    Column = column
                };
            });

            var extractor = new LambdaCondition(trimmedExpression, template, mapping, sink, generateSql: false, cachedSql: cached.ConditionSql);
            extractor.Visit(trimmedExpression);
            return new LambdaCondition(cached.ConditionSql, parameters, template, mapping);
        }

        /// <summary>
        /// 解析 Lambda 条件表达式生成 SQL 片段与参数
        /// Parse a lambda condition expression into a SQL fragment with parameters
        /// </summary>
        /// <param name="expression">条件表达式 / Condition expression</param>
        /// <param name="template">目标数据库 SQL 模板 / Target SQL template</param>
        /// <param name="mapping">模型映射，为 null 时按成员声明类型推断 / Model mapping; inferred from member declaring types when null</param>
        /// <param name="alreadyTrimmed">表达式是否已经过 <see cref="TrimExpression.Trim"/> 规整 / Whether the expression is already trimmed by <see cref="TrimExpression.Trim"/></param>
        public LambdaCondition(LambdaExpression expression, SqlTemplate template, TableMapping? mapping = null, bool alreadyTrimmed = false)
            : this((Expression)expression, template, mapping, alreadyTrimmed)
        {
        }

        /// <summary>
        /// 编译缓存未命中路径：完整访问已规整的表达式树，生成 SQL 片段并记录参数键顺序
        /// Cache-miss path: fully walk the trimmed tree, generating the SQL fragment and recording parameter key order
        /// </summary>
        /// <param name="trimmedExpression">已规整的表达式树 / Trimmed expression tree</param>
        /// <param name="template">SQL 模板 / SQL template</param>
        /// <param name="mapping">模型映射 / Model mapping</param>
        /// <returns>新生成的条件实例 / The newly compiled condition</returns>
        internal static LambdaCondition Compile(Expression trimmedExpression, SqlTemplate template, TableMapping mapping)
        {
            return new LambdaCondition(trimmedExpression, template, mapping, alreadyTrimmed: true);
        }

        private LambdaCondition(Expression expression, SqlTemplate template, TableMapping? mapping, bool alreadyTrimmed)
        {
            SqlTemplate = template;
            Mapping = mapping;
            _generateSql = true;
            _cachedSql = null;
            Parameters = new Dictionary<string, LambdaConditionParameter>();
            _builder = new SqlConditionBuilder(template, new BuildParameterSink(RegisterParameterInternal));

            if (expression != null)
            {
                Expression exp = alreadyTrimmed ? expression : TrimExpression.Trim(expression);
                Visit(exp);
            }
        }

        private LambdaCondition(Expression trimmedExpression, SqlTemplate template, TableMapping? mapping, IConditionParameterSink sink, bool generateSql, string? cachedSql)
        {
            SqlTemplate = template;
            Mapping = mapping;
            _generateSql = generateSql;
            _cachedSql = cachedSql;
            Parameters = new Dictionary<string, LambdaConditionParameter>();
            _builder = new SqlConditionBuilder(template, sink);
        }

        private LambdaCondition(string cachedSql, IDictionary<string, LambdaConditionParameter> parameters, SqlTemplate template, TableMapping? mapping)
        {
            SqlTemplate = template;
            Mapping = mapping;
            Parameters = parameters;
            _cachedSql = cachedSql;
            _generateSql = false;
            _builder = new SqlConditionBuilder(template, new BuildParameterSink((_, __, ___) => { }));
        }

        private void RegisterParameterInternal(string key, object? value, FieldMapping column)
        {
            Parameters[key] = new LambdaConditionParameter
            {
                Name = key,
                Value = value,
                Column = column
            };
            _parameterKeyOrder.Add(key);
        }

        private string RegisterParameter(string baseName, object? value, FieldMapping column)
        {
            return _builder.RegisterParameter(baseName, value, column);
        }

        private FieldMapping? GetMemberProperty(MemberExpression node)
        {
            MemberInfo memberInfo = node.Member;
            Mapping = Mapping ?? ModelSchemaStore.GetModelMapping(memberInfo.DeclaringType!);
            if (Mapping == null)
            {
                return null;
            }
            return Mapping.GetPropertyMapping(memberInfo.Name);
        }

        /// <summary>
        /// 访问二元节点：为每个二元节点生成一对括号，因此 AND/OR 混用不存在优先级歧义
        /// Visit a binary node: emits a parenthesis pair for every binary node, so mixed AND/OR has no precedence ambiguity
        /// </summary>
        /// <param name="node">二元表达式节点 / Binary expression node</param>
        /// <returns>原节点 / The original node</returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (_generateSql)
            {
                _builder.AppendOpenGroup();
            }

            // 左操作数为常量 false 时不访问左子树，直接用恒假条件（1=2）占位，避免输出无意义的常量 / When the left operand is the constant false, emit the always-false condition (1=2) instead of visiting it
            if (node.Left.Type == typeof(bool) && node.Left.NodeType == ExpressionType.Constant
                && node.Left is ConstantExpression boolConstant && boolConstant.Value is bool boolValue && !boolValue)
            {
                if (_generateSql)
                {
                    _builder.AppendFalseCondition();
                    _builder.AppendOperator(node.GetExpressionTypeOperator());
                }
            }
            else
            {
                Visit(node.Left);
                if (_generateSql)
                {
                    _builder.AppendOperator(node.GetExpressionTypeOperator());
                }
            }

            Visit(node.Right);

            if (_generateSql)
            {
                _builder.AppendCloseGroup();
            }

            return node;
        }

        /// <summary>
        /// 访问成员节点：映射到数据表列则输出列名并记录当前列上下文（供后续常量注册参数使用）
        /// Visit a member node: emits the mapped column name and records the current column context for later constant-to-parameter registration
        /// </summary>
        /// <param name="node">成员表达式节点 / Member expression node</param>
        /// <returns>原节点 / The original node</returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            FieldMapping? propertyMapping = GetMemberProperty(node);
            if (propertyMapping == null)
            {
                return node;
            }

            string columnName = propertyMapping.ColumnName!;
            if (_generateSql)
            {
                _builder.AppendColumn(columnName);
            }

            CurrentColumnName = columnName;
            CurrentColumn = propertyMapping;
            return node;
        }

        /// <summary>
        /// 访问常量节点：位于列上下文中时注册为命名参数，值为 null 时直接输出 NULL 字面量（配合 =/!= 改写为 IS NULL / IS NOT NULL）
        /// Visit a constant node: registers a named parameter when in a column context; a null value emits the NULL literal (= / != become IS NULL / IS NOT NULL)
        /// </summary>
        /// <param name="node">常量表达式节点 / Constant expression node</param>
        /// <returns>原节点 / The original node</returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value != null)
            {
                if (!string.IsNullOrEmpty(CurrentColumnName) && CurrentColumn != null)
                {
                    string formatted = RegisterParameter(CurrentColumnName, node.Value, CurrentColumn);
                    if (_generateSql)
                    {
                        _builder.Append(formatted);
                    }
                }
            }
            else if (_generateSql)
            {
                _builder.AppendNullLiteral();
            }

            return node;
        }

        /// <summary>
        /// 尝试把一个成员表达式识别为数据表列并设为当前列上下文
        /// Try to recognize a member expression as a table column and set it as the current column context
        /// </summary>
        /// <param name="expression">候选表达式 / Candidate expression</param>
        /// <returns>识别成功返回 true / True when recognized as a mapped column</returns>
        private bool TrySetColumnContext(Expression expression)
        {
            if (expression is not MemberExpression memberExpr)
            {
                return false;
            }

            FieldMapping? propertyMapping = GetMemberProperty(memberExpr);
            if (propertyMapping == null)
            {
                return false;
            }

            CurrentColumnName = propertyMapping.ColumnName;
            CurrentColumn = propertyMapping;
            return true;
        }

        /// <summary>
        /// 访问方法调用节点，支持三类写法：实例字符串方法（列.Contains/StartsWith/EndsWith/Equals）、
        /// 集合 Contains（list.Contains(列) 生成 IN，空集合生成 1=2）、静态工具方法（StringTool/StringExtensions 的模糊与等值匹配）
        /// Visit a method call node, supporting three families: instance string methods (column.Contains/StartsWith/EndsWith/Equals),
        /// collection Contains (list.Contains(column) produces IN; an empty collection produces 1=2), and static helper methods
        /// (StringTool/StringExtensions fuzzy and equality matching)
        /// </summary>
        /// <param name="node">方法调用表达式节点 / Method call expression node</param>
        /// <returns>原节点 / The original node</returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            ConstantExpression? paramExpression = null;
            string? currentColumnName = null;

            if (node.Object != null)
            {
                var nodeObjectType = node.Object.NodeType;
                if (nodeObjectType == ExpressionType.MemberAccess)
                {
                    if (!TrySetColumnContext(node.Object))
                    {
                        throw new SuperException(EX_KEY_InvalidLambdaExpressionArguments, node);
                    }

                    currentColumnName = CurrentColumnName;

                    switch (node.Method.Name)
                    {
                        case "Contains":
                        case "StartsWith":
                        case "EndsWith":
                            paramExpression = (ConstantExpression)node.Arguments[0];
                            string leftLikeOp = node.Method.Name == "Contains" || node.Method.Name == "StartsWith" ? "%" : string.Empty;
                            string rightLikeOp = node.Method.Name == "Contains" || node.Method.Name == "EndsWith" ? "%" : string.Empty;
                            string likeParam = RegisterParameter(currentColumnName!, leftLikeOp + paramExpression.Value + rightLikeOp, CurrentColumn!);
                            if (_generateSql)
                            {
                                _builder.AppendLike(currentColumnName!, likeParam);
                            }
                            break;
                        case "Equals":
                            object equalsValue = node.Arguments[0].ToConvertAndGetValue();
                            string equalsParam = RegisterParameter(currentColumnName!, equalsValue, CurrentColumn!);
                            if (_generateSql)
                            {
                                _builder.AppendComparison(currentColumnName!, " = ", equalsParam);
                            }
                            break;
                        default:
                            throw new SuperException(EX_KEY_LambdaConditionNotSupported, node);
                    }
                }
                else if (nodeObjectType == ExpressionType.Constant)
                {
                    switch (node.Method.Name)
                    {
                        case "Contains":
                            if (node.Arguments != null && node.Arguments.Count > 0)
                            {
                                if ((typeof(IEnumerable).IsAssignableFrom(node.Method.DeclaringType)
                                    || typeof(IEnumerable<>).IsAssignableFrom(node.Method.DeclaringType))
                                    && node.Method.DeclaringType != typeof(string))
                                {
                                    BuildListContains(node);
                                }
                                else if (node.Method.DeclaringType == typeof(string) && node.Arguments[0].NodeType == ExpressionType.MemberAccess)
                                {
                                    string strValue = (string)((ConstantExpression)node.Object!).Value!;
                                    FieldMapping? propertyMapping = GetMemberProperty((MemberExpression)node.Arguments[0]);
                                    if (propertyMapping != null)
                                    {
                                        string paramName = "SearchStr_" + propertyMapping.PropertyName;
                                        if (_generateSql)
                                        {
                                            _builder.Append(SqlTemplate.BuildStringInColumnConditionWithParameter(
                                                SqlTemplate.FormatParameter(paramName),
                                                propertyMapping.ColumnName!));
                                        }
                                        RegisterParameter(paramName, strValue, propertyMapping);
                                    }
                                    else
                                    {
                                        throw new SuperException(EX_KEY_UnknownColumnInLambdaExpression, node);
                                    }
                                }
                                else if (node.Method.DeclaringType == typeof(System.Linq.Enumerable))
                                {
                                    BuildEnumableContains(node);
                                }
                                else
                                {
                                    throw new SuperException(EX_KEY_LambdaConditionNotSupported, node);
                                }
                            }
                            else
                            {
                                throw new SuperException(EX_KEY_LambdaConditionNotSupported, node);
                            }
                            break;
                    }
                }
            }
            else
            {
                if ((node.Method.DeclaringType == typeof(StringTool) || node.Method.DeclaringType == typeof(StringExtensions)) && node.Arguments != null && node.Arguments.Count > 0)
                {
                    foreach (var arg in node.Arguments)
                    {
                        if (arg.NodeType == ExpressionType.MemberAccess)
                        {
                            TrySetColumnContext(arg);
                        }
                        else if (arg.NodeType == ExpressionType.Constant)
                        {
                            paramExpression = (ConstantExpression)arg;
                        }
                    }

                    if (!string.IsNullOrEmpty(CurrentColumnName) && CurrentColumn != null)
                    {
                        currentColumnName = CurrentColumnName;
                    }

                    switch (node.Method.Name)
                    {
                        case "Equals":
                        case "EqualsIgnoreCase":
                            if (paramExpression != null && !string.IsNullOrEmpty(currentColumnName) && CurrentColumn != null)
                            {
                                string equalsParam = RegisterParameter(currentColumnName, paramExpression.Value, CurrentColumn);
                                if (_generateSql)
                                {
                                    _builder.AppendComparison(currentColumnName, " = ", equalsParam);
                                }
                            }
                            else
                            {
                                throw new SuperException(EX_KEY_InvalidLambdaExpressionArguments, node);
                            }
                            break;
                        case "StartsWith":
                        case "StartsWithIgnoreCase":
                        case "EndsWith":
                        case "EndsWithIgnoreCase":
                            if (paramExpression != null && !string.IsNullOrEmpty(currentColumnName) && CurrentColumn != null)
                            {
                                string leftLikeOp = node.Method.Name == "StartsWithIgnoreCase" || node.Method.Name == "StartsWith" ? "%" : string.Empty;
                                string rightLikeOp = node.Method.Name == "EndsWithIgnoreCase" || node.Method.Name == "EndsWith" ? "%" : string.Empty;
                                string likeParam = RegisterParameter(currentColumnName, leftLikeOp + paramExpression.Value + rightLikeOp, CurrentColumn);
                                if (_generateSql)
                                {
                                    _builder.AppendLike(currentColumnName, likeParam);
                                }
                            }
                            else
                            {
                                throw new SuperException(EX_KEY_InvalidLambdaExpressionArguments, node);
                            }
                            break;
                        case "Contains":
                            if (paramExpression != null && !string.IsNullOrEmpty(currentColumnName) && CurrentColumn != null)
                            {
                                string likeParam = RegisterParameter(currentColumnName, "%" + paramExpression.Value + "%", CurrentColumn);
                                if (_generateSql)
                                {
                                    _builder.AppendLike(currentColumnName, likeParam);
                                }
                            }
                            else
                            {
                                throw new SuperException(EX_KEY_InvalidLambdaExpressionArguments, node);
                            }
                            break;
                        default:
                            throw new SuperException(EX_KEY_LambdaConditionNotSupported, node);
                    }
                }
                else if (node.Method.DeclaringType == typeof(System.Linq.Enumerable) && node.Arguments != null && node.Arguments.Count > 0)
                {
                    BuildEnumableContains(node);
                }
                else
                {
                    throw new SuperException(EX_KEY_LambdaConditionNotSupported, node);
                }
            }

            return node;
        }

        /// <summary>
        /// 构建集合常量.Contains(列) 的 IN 条件；集合为 null 或空集合时生成恒假条件 1=2（而非丢弃条件）
        /// Build the IN condition for collectionConstant.Contains(column); a null or empty collection yields the always-false condition 1=2 (the condition is not dropped)
        /// </summary>
        /// <param name="node">方法调用表达式节点 / Method call expression node</param>
        private void BuildListContains(MethodCallExpression node)
        {
            var listValue = ((ConstantExpression)node.Object!).Value as IEnumerable;
            if (listValue == null || !listValue.Cast<object?>().Any())
            {
                if (_generateSql)
                {
                    _builder.AppendFalseCondition();
                }
                return;
            }

            if (node.Arguments[0].NodeType == ExpressionType.MemberAccess)
            {
                if (!TrySetColumnContext(node.Arguments[0]))
                {
                    throw new SuperException(EX_KEY_LambdaConditionNotSupported, node);
                }

                string paramPrefix = "InList_" + (CurrentColumnName ?? "Col");
                AppendInList(CurrentColumnName!, listValue, paramPrefix, CurrentColumn!);
            }
            else
            {
                throw new SuperException(EX_KEY_LambdaConditionNotSupported, node);
            }
        }

        /// <summary>
        /// 构建 Enumerable.Contains(列, 集合) 的 IN 条件；集合为空或 null 时生成恒假条件 1=2
        /// Build the IN condition for Enumerable.Contains(column, collection); an empty or null collection yields the always-false condition 1=2
        /// </summary>
        /// <param name="node">方法调用表达式节点 / Method call expression node</param>
        private void BuildEnumableContains(MethodCallExpression node)
        {
            ConstantExpression? paramExpression = null;
            Expression? columnArg = null;

            foreach (var arg in node.Arguments)
            {
                if (arg.NodeType == ExpressionType.MemberAccess)
                {
                    columnArg = arg;
                }
                else if (arg.NodeType == ExpressionType.Constant)
                {
                    paramExpression = (ConstantExpression)arg;
                }
            }

            if (columnArg == null)
            {
                throw new SuperException(EX_KEY_InvalidLambdaExpressionArguments, node);
            }

            if (paramExpression != null && paramExpression.Value != null)
            {
                IEnumerable? valueList = paramExpression.Value as IEnumerable;
                if (valueList == null || !valueList.Cast<object?>().Any())
                {
                    if (_generateSql)
                    {
                        _builder.AppendFalseCondition();
                    }
                    return;
                }

                if (!TrySetColumnContext(columnArg))
                {
                    throw new SuperException(EX_KEY_InvalidLambdaExpressionArguments, node);
                }

                AppendInList(CurrentColumnName!, valueList, "InList_" + CurrentColumnName, CurrentColumn!);
            }
            else if (_generateSql)
            {
                _builder.AppendFalseCondition();
            }
        }

        /// <summary>
        /// 追加 IN 列表：生成 SQL 时直接经构建器展开为逐元素参数；缓存重放时仅按顺序注册每个元素参数（不产 SQL）
        /// Append an IN list: when generating SQL it is expanded element-wise via the builder; on cache replay only the element parameters are registered in order (no SQL)
        /// </summary>
        /// <param name="columnName">列名 / Column name</param>
        /// <param name="listValue">集合值 / Collection value</param>
        /// <param name="paramPrefix">参数名前缀 / Parameter name prefix</param>
        /// <param name="column">列映射信息 / Column mapping</param>
        private void AppendInList(string columnName, IEnumerable listValue, string paramPrefix, FieldMapping column)
        {
            if (_generateSql)
            {
                _builder.AppendIn(columnName, listValue, paramPrefix, column);
                return;
            }

            int index = 0;
            foreach (object? item in listValue)
            {
                RegisterParameter($"{paramPrefix}_{index}", item, column);
                index++;
            }
        }
    }
}
