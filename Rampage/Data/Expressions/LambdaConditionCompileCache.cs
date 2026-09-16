using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text;

namespace Rampage.Data.Expressions
{
    internal sealed class LambdaConditionCompileEntry
    {
        public string ConditionSql { get; init; } = string.Empty;

        public IReadOnlyList<string> ParameterKeys { get; init; } = Array.Empty<string>();

        public IReadOnlyList<FieldMapping> ParameterColumns { get; init; } = Array.Empty<FieldMapping>();
    }

    /// <summary>
    /// Caches compiled lambda condition SQL skeletons keyed by expression structure.
    /// </summary>
    internal static class LambdaConditionCompileCache
    {
        private const int StructureCacheVersion = 2;

        private static readonly ConcurrentDictionary<string, LambdaConditionCompileEntry> Cache = new();

        public static bool TryGet(string cacheKey, out LambdaConditionCompileEntry entry)
        {
            return Cache.TryGetValue(cacheKey, out entry!);
        }

        public static void Set(string cacheKey, LambdaConditionCompileEntry entry)
        {
            Cache[cacheKey] = entry;
        }

        public static string BuildCacheKey(Expression expression, TableMapping mapping, SqlTemplate template)
        {
            return string.Concat(
                StructureCacheVersion,
                "|",
                mapping.ModelType?.FullName ?? mapping.TableName,
                "|",
                template.GetType().FullName,
                "|",
                ExpressionStructureKey.Build(expression));
        }
    }

    /// <summary>
    /// Builds a structure-only key for lambda expressions (constants and list sizes normalized).
    /// </summary>
    internal static class ExpressionStructureKey
    {
        public static string Build(Expression expression)
        {
            var builder = new StringBuilder(256);
            Visit(expression, builder);
            return builder.ToString();
        }

        private static void Visit(Expression? expression, StringBuilder builder)
        {
            if (expression == null)
            {
                return;
            }

            if (expression is LambdaExpression lambda)
            {
                Visit(lambda.Body, builder);
                return;
            }

            if (expression is BinaryExpression binary)
            {
                builder.Append('(');
                Visit(binary.Left, builder);
                builder.Append('|').Append((int)binary.NodeType).Append('|');
                Visit(binary.Right, builder);
                builder.Append(')');
                return;
            }

            if (expression is MemberExpression member)
            {
                if (member.Expression?.NodeType == ExpressionType.Parameter)
                {
                    builder.Append('M').Append(member.Member.Name);
                }
                else
                {
                    builder.Append('C').Append(member.Member.DeclaringType?.Name).Append('.').Append(member.Member.Name);
                }
                return;
            }

            if (expression is ConstantExpression constant)
            {
                if (expression.Type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(expression.Type))
                {
                    int count = 0;
                    if (constant.Value is IEnumerable enumerable)
                    {
                        foreach (object? _ in enumerable)
                        {
                            count++;
                        }
                    }
                    builder.Append('L').Append(count);
                }
                else
                {
                    builder.Append('?');
                }
                return;
            }

            if (expression is MethodCallExpression call)
            {
                builder.Append('F').Append(call.Method.DeclaringType?.Name).Append('.').Append(call.Method.Name).Append('(');
                if (call.Object != null)
                {
                    Visit(call.Object, builder);
                    builder.Append(';');
                }
                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }
                    Visit(call.Arguments[i], builder);
                }
                builder.Append(')');
                return;
            }

            if (expression is UnaryExpression unary)
            {
                builder.Append('U').Append((int)unary.NodeType).Append('(');
                Visit(unary.Operand, builder);
                builder.Append(')');
                return;
            }

            if (expression.NodeType == ExpressionType.Parameter)
            {
                builder.Append('P');
                return;
            }

            builder.Append('X').Append((int)expression.NodeType);
        }
    }
}
