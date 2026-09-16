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
    /// Data Lambda Condition Expression Parser
    /// </summary>
    public class LambdaCondition : ExpressionVisitor
    {
        public const string EX_KEY_InvalidLambdaExpressionArguments = "Invalid_LambdaExpressionArguments";
        public const string EX_KEY_LambdaConditionNotSupported = "LambdaCondition_NotSupported";
        public const string EX_KEY_UnknownColumnInLambdaExpression = "UnknownColumn_InLambdaExpression";

        private readonly SqlConditionBuilder _builder;
        private readonly bool _generateSql;
        private readonly string? _cachedSql;
        private readonly List<string> _parameterKeyOrder = new();

        public SqlTemplate SqlTemplate { get; protected set; }
        public TableMapping? Mapping { get; set; }
        public IDictionary<string, LambdaConditionParameter> Parameters { get; private set; }
        private string? CurrentColumnName { get; set; }
        private FieldMapping? CurrentColumn { get; set; }

        public string GetConditionString() => _cachedSql ?? _builder.ToSql();

        internal LambdaConditionCompileEntry ToCompileEntry()
        {
            return new LambdaConditionCompileEntry
            {
                ConditionSql = GetConditionString(),
                ParameterKeys = _parameterKeyOrder.ToArray(),
                ParameterColumns = _parameterKeyOrder.Select(k => Parameters[k].Column).ToArray()
            };
        }

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

        public LambdaCondition(LambdaExpression expression, SqlTemplate template, TableMapping? mapping = null, bool alreadyTrimmed = false)
            : this((Expression)expression, template, mapping, alreadyTrimmed)
        {
        }

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

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (_generateSql)
            {
                _builder.AppendOpenGroup();
            }

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
