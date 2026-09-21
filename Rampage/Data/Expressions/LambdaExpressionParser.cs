using Rampage.Caching;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Rampage.Data.Expressions
{
    /// <summary>
    /// Lambda 表达式解析入口：字段选择器解析为列名、条件表达式解析为 SQL 条件（含编译缓存）
    /// Entry point for lambda expression parsing: resolves key selectors to column names and condition expressions to SQL conditions (with compile cache)
    /// </summary>
    public class LambdaExpressionParser
    {

        static LambdaExpressionParser()
        {
            CacheManager = new MemoryCacheManager();
        }

        /// <summary>
        /// 解析器使用的独立内存缓存管理器
        /// Stand-alone memory cache manager used by the parser
        /// </summary>
        public static IStandAloneCache CacheManager
        {
            get;
            private set;
        }

        private static readonly ConcurrentDictionary<string, string?> ColumnNameContainer = new ConcurrentDictionary<string, string?>();

        /// <summary>
        /// 解析字段选择器表达式对应的数据表列名
        /// Resolve the mapped table column name of a key selector expression
        /// </summary>
        /// <remarks>以表达式字符串为键缓存解析结果；同一表达式字符串的重复调用不会重新解析。/ Results are cached by the expression string; repeated calls with the same string are not re-parsed.</remarks>
        /// <typeparam name="M">模型类型 / Model type</typeparam>
        /// <param name="expression">字段选择器表达式 / Key selector expression</param>
        /// <param name="mapping">模型映射，为 null 时按 <typeparamref name="M"/> 自动获取 / Model mapping; resolved from <typeparamref name="M"/> when null</param>
        /// <returns>实际列名，解析失败返回 null / The column name, or null when resolution fails</returns>
        public static string? GetColumnName<M>(LambdaExpression expression, TableMapping? mapping = null) where M : class 
        {
            mapping = mapping ?? ModelSchemaStore.GetModelMapping<M>();

            string expressionStr = expression.ToString();
            string? columnName = ColumnNameContainer.GetOrAdd(expressionStr, _ =>
            {
                LambdaSelector selectorParser = new LambdaSelector(expression, mapping);
                return selectorParser.GetMappingColumnName();
            });

            return columnName;
        }

        /// <summary>
        /// 解析 Lambda 条件表达式为 SQL 条件（先经 <see cref="TrimExpression.Trim"/> 规整，再走编译缓存）
        /// Parse a lambda condition expression into a SQL condition (trimmed by <see cref="TrimExpression.Trim"/> first, then via the compile cache)
        /// </summary>
        /// <remarks>
        /// 以表达式结构键查询 <see cref="LambdaConditionCompileCache"/>：命中时只按缓存顺序重放取参数值（复用 SQL 骨架），
        /// 未命中才完整编译并回填缓存。注意规整阶段会急切求值闭包成员且不短路，lambda 里写 !x.HasValue || y == x.Value 这类条件
        /// 会在 x 为 null 时直接抛异常。
        /// The structure key is looked up in <see cref="LambdaConditionCompileCache"/>: on a hit only parameter values are replayed in cached order
        /// (the SQL skeleton is reused); on a miss the condition is fully compiled and stored. Note the trim stage eagerly evaluates closure members
        /// without short-circuiting: a condition like !x.HasValue || y == x.Value throws when x is null.
        /// </remarks>
        /// <typeparam name="M">模型类型 / Model type</typeparam>
        /// <param name="expression">条件表达式 / Condition expression</param>
        /// <param name="template">目标数据库 SQL 模板 / Target SQL template</param>
        /// <param name="mapping">模型映射，为 null 时按 <typeparamref name="M"/> 自动获取 / Model mapping; resolved from <typeparamref name="M"/> when null</param>
        /// <returns>解析得到的条件（SQL 片段 + 参数）/ The parsed condition (SQL fragment plus parameters)</returns>
        public static LambdaCondition GetCondition<M>(LambdaExpression expression, SqlTemplate template, TableMapping? mapping = null) where M : class
        {
            mapping = mapping ?? ModelSchemaStore.GetModelMapping<M>()!;

            Expression trimmed = TrimExpression.Trim(expression);
            string cacheKey = LambdaConditionCompileCache.BuildCacheKey(trimmed, mapping, template);

            if (LambdaConditionCompileCache.TryGet(cacheKey, out LambdaConditionCompileEntry? cached))
            {
                return LambdaCondition.FromCached(trimmed, template, mapping, cached);
            }

            var condition = LambdaCondition.Compile(trimmed, template, mapping);
            LambdaConditionCompileCache.Set(cacheKey, condition.ToCompileEntry());
            return condition;
        }

    }
}
