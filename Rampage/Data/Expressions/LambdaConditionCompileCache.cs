using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text;

namespace Rampage.Data.Expressions
{
    /// <summary>
    /// Lambda 条件的编译缓存条目：SQL 骨架 + 参数键出现顺序 + 各参数对应的列映射
    /// Compiled cache entry for a lambda condition: SQL skeleton, parameter keys in registration order, and the column of each parameter
    /// </summary>
    internal sealed class LambdaConditionCompileEntry
    {
        /// <summary>
        /// 条件 SQL 骨架（含参数占位符，不含参数值）
        /// Condition SQL skeleton (with parameter placeholders, without values)
        /// </summary>
        public string ConditionSql { get; init; } = string.Empty;

        /// <summary>
        /// 参数键按首次编译时的注册顺序排列（缓存命中时按此顺序重放取值）
        /// Parameter keys in first-compilation registration order (replayed in this order on cache hits)
        /// </summary>
        public IReadOnlyList<string> ParameterKeys { get; init; } = Array.Empty<string>();

        /// <summary>
        /// 与 <see cref="ParameterKeys"/> 一一对应的列映射
        /// Column mappings, one-to-one with <see cref="ParameterKeys"/>
        /// </summary>
        public IReadOnlyList<FieldMapping> ParameterColumns { get; init; } = Array.Empty<FieldMapping>();
    }

    /// <summary>
    /// 按表达式结构缓存已编译的 Lambda 条件 SQL 骨架。
    /// Caches compiled lambda condition SQL skeletons keyed by expression structure.
    /// </summary>
    /// <remarks>
    /// 结构键由 <see cref="ExpressionStructureKey"/> 生成（常量归一化，仅集合保留元素个数），因此结构相同、仅参数值不同的表达式共享同一份 SQL；
    /// 命中后由 ExtractParameterSink 按缓存键顺序重放取运行时参数值。
    /// The structure key is built by <see cref="ExpressionStructureKey"/> (constants normalized; only collection sizes kept), so expressions
    /// identical in structure but different in values share one SQL skeleton; on a hit, ExtractParameterSink replays runtime values in cached key order.
    /// </remarks>
    internal static class LambdaConditionCompileCache
    {
        // 结构键算法版本号：算法变更时递增，避免旧格式缓存键与新条目碰撞 / Structure-key algorithm version: bump to avoid stale entries colliding after algorithm changes
        private const int StructureCacheVersion = 2;

        private static readonly ConcurrentDictionary<string, LambdaConditionCompileEntry> Cache = new();

        /// <summary>
        /// 按缓存键尝试获取已编译条目
        /// Try to get a compiled entry by cache key
        /// </summary>
        /// <param name="cacheKey">缓存键 / Cache key</param>
        /// <param name="entry">命中的编译条目 / The compiled entry when found</param>
        /// <returns>命中返回 true / True when found</returns>
        public static bool TryGet(string cacheKey, out LambdaConditionCompileEntry entry)
        {
            return Cache.TryGetValue(cacheKey, out entry!);
        }

        /// <summary>
        /// 写入编译条目（同键覆盖）
        /// Store a compiled entry (overwrites an existing key)
        /// </summary>
        /// <param name="cacheKey">缓存键 / Cache key</param>
        /// <param name="entry">编译条目 / The compiled entry</param>
        public static void Set(string cacheKey, LambdaConditionCompileEntry entry)
        {
            Cache[cacheKey] = entry;
        }

        /// <summary>
        /// 构造缓存键：结构键算法版本 | 模型类型全名 | SQL 模板类型全名 | 表达式结构键
        /// Build the cache key: structure-key version | model type full name | SQL template type full name | expression structure key
        /// </summary>
        /// <param name="expression">已规整的表达式 / Trimmed expression</param>
        /// <param name="mapping">模型映射 / Model mapping</param>
        /// <param name="template">SQL 模板 / SQL template</param>
        /// <returns>缓存键 / The cache key</returns>
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
    /// 为 Lambda 表达式生成"仅结构"的键：常量被归一化（普通常量为 ?，集合常量为 L{元素个数}），参数化成员记为参数成员。
    /// Builds a structure-only key for lambda expressions: constants are normalized (plain constants to ?, collection constants to L{count}), closure members recorded by declaring type and name.
    /// </summary>
    internal static class ExpressionStructureKey
    {
        /// <summary>
        /// 生成表达式的结构键
        /// Build the structure key of an expression
        /// </summary>
        /// <param name="expression">表达式 / The expression</param>
        /// <returns>结构键字符串 / The structure key string</returns>
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
                // 集合常量保留元素个数（L{count}）：IN 展开的参数个数与 SQL 骨架相关，不能归一；string 虽实现 IEnumerable 但按普通常量归一为 ?
                // Collection constants keep their item count (L{count}): the IN expansion length affects the SQL skeleton; string implements IEnumerable but is normalized to ? like a plain constant
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
