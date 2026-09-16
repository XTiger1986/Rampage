using Rampage.Caching;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Rampage.Data.Expressions
{
    public class LambdaExpressionParser
    {

        static LambdaExpressionParser()
        {
            CacheManager = new MemoryCacheManager();
        }

        public static IStandAloneCache CacheManager
        {
            get;
            private set;
        }

        private static readonly ConcurrentDictionary<string, string?> ColumnNameContainer = new ConcurrentDictionary<string, string?>();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="expression"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="expression"></param>
        /// <param name="template"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
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
