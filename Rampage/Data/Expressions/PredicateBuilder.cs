using System;
using System.Linq.Expressions;

namespace Rampage.Data.Expressions
{
    /// <summary>
    /// 谓词表达式组合构建器：以 AND/OR 合并两个谓词表达式（统一参数并合并表达式体）
    /// Predicate expression composition builder: merges two predicates with AND/OR (unifying parameters and combining bodies)
    /// </summary>
    public static class PredicateBuilder
    {

        /// <summary>
        /// 创建恒真谓词（f => true）
        /// Create an always-true predicate (f => true)
        /// </summary>
        /// <typeparam name="T">谓词参数类型 / Predicate parameter type</typeparam>
        /// <returns>恒真表达式 / The always-true expression</returns>
        public static Expression<Func<T, bool>> True<T>() { return f => true; }
        /// <summary>
        /// 创建恒假谓词（f => false）
        /// Create an always-false predicate (f => false)
        /// </summary>
        /// <typeparam name="T">谓词参数类型 / Predicate parameter type</typeparam>
        /// <returns>恒假表达式 / The always-false expression</returns>
        public static Expression<Func<T, bool>> False<T>() { return f => false; }

        /// <summary>
        /// 以指定合并委托组合两个同形表达式：复用第一个表达式的参数，重绑第二个表达式的参数后合并表达式体
        /// Compose two same-shaped expressions with a merge delegate: reuses the first expression's parameters, rebinds the second's, then merges the bodies
        /// </summary>
        /// <typeparam name="F">表达式委托类型 / Expression delegate type</typeparam>
        /// <param name="first">第一个表达式（参数被保留）/ First expression (its parameters are kept)</param>
        /// <param name="second">第二个表达式（参数被重绑到第一个）/ Second expression (parameters rebound to the first)</param>
        /// <param name="merge">表达式体合并委托，如 Expression.And / Expression.OrElse / Body merge delegate, e.g. Expression.And / Expression.OrElse</param>
        /// <returns>合并后的新表达式 / The merged expression</returns>
        public static Expression<F> Compose<F>(this Expression<F> first, Expression<F> second, Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first) / 构建参数映射（第二个表达式的参数 -> 第一个表达式的参数）
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with parameters from the first / 把第二个表达式的参数替换为第一个表达式的参数
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression / 以第一个表达式的参数组合两个表达式体
            return Expression.Lambda<F>(merge(first.Body, secondBody), first.Parameters);
        }

        /// <summary>
        /// 以 AND 合并两个谓词（无嵌套分组，多次调用为左折叠）
        /// Merge two predicates with AND (no nested grouping; repeated calls left-fold the tree)
        /// </summary>
        /// <remarks>
        /// 使用 <see cref="Expression.And"/>（非短路）而非 AndAlso，仅风格不一致：运算符映射把 And 与 AndAlso 同等译为 SQL 的 AND
        /// （SQL 本身无短路语义），两者最终 SQL 相同。对应地 <see cref="Or{T}"/> 使用 OrElse。
        /// Uses non-short-circuiting <see cref="Expression.And"/> rather than AndAlso - a style inconsistency only: the operator
        /// mapping translates And and AndAlso alike to SQL AND (SQL has no short-circuit semantics), so both yield the same SQL.
        /// <see cref="Or{T}"/> correspondingly uses OrElse.
        /// </remarks>
        /// <typeparam name="T">谓词参数类型 / Predicate parameter type</typeparam>
        /// <param name="first">左谓词 / Left predicate</param>
        /// <param name="second">右谓词 / Right predicate</param>
        /// <returns>合并后的谓词 / The merged predicate</returns>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.And);
        }

        /// <summary>
        /// 以 OR 合并两个谓词（无嵌套分组，多次调用为左折叠）
        /// Merge two predicates with OR (no nested grouping; repeated calls left-fold the tree)
        /// </summary>
        /// <remarks>使用 <see cref="Expression.OrElse"/>；详见 <see cref="And{T}"/> 备注中关于风格不一致的说明。/ Uses <see cref="Expression.OrElse"/>; see the remarks on <see cref="And{T}"/> about the style inconsistency.</remarks>
        /// <typeparam name="T">谓词参数类型 / Predicate parameter type</typeparam>
        /// <param name="first">左谓词 / Left predicate</param>
        /// <param name="second">右谓词 / Right predicate</param>
        /// <returns>合并后的谓词 / The merged predicate</returns>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

    }
}
