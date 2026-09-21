using Rampage.Data.Expressions;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Query
{
    /// <summary>
    /// 基于 Lambda 表达式条件的查询包装器
    /// Query wrapper whose condition is a lambda expression
    /// </summary>
    /// <typeparam name="M">模型类型 / Model type</typeparam>
    public class LambdaWrapper<M> : QueryWrapper<M> where M : class
    {

        /// <summary>
        /// 当前累积的查询条件表达式，为 null 表示无条件
        /// Accumulated condition expression; null means no condition
        /// </summary>
        public Expression<Func<M, bool>>? Condition
        {
            get;
            protected set;
        }

        /// <summary>
        /// 初始化一个不带条件的 Lambda 查询包装器
        /// Initialize a lambda wrapper without a condition
        /// </summary>
        public LambdaWrapper() : base()
        {
        }

        /// <summary>
        /// 以一个 Lambda 表达式初始化 Lambda 查询包装器
        /// Initialize a lambda wrapper with a condition expression
        /// </summary>
        /// <param name="expression">查询条件表达式 / Condition expression</param>
        public LambdaWrapper(Expression<Func<M, bool>> expression) : base()
        {
            Condition = expression;
        }

        /// <summary>
        /// 创建一个不带条件的 Lambda 查询包装器
        /// Create an empty lambda wrapper without a condition
        /// </summary>
        /// <returns>新的包装器实例 / A new wrapper instance</returns>
        public static LambdaWrapper<M> Empty()
        {
            return new LambdaWrapper<M>();
        }

        /// <summary>
        /// 以一个 Lambda 表达式创建 Lambda 查询包装器
        /// Create a lambda wrapper with a condition expression
        /// </summary>
        /// <param name="expression">查询条件表达式 / Condition expression</param>
        /// <returns>新的包装器实例 / A new wrapper instance</returns>
        public static LambdaWrapper<M> Create(Expression<Func<M, bool>> expression)
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(expression);

            return wrapper;
        }

        /// <summary>
        /// 以 AND 连接追加条件（当前无条件时直接作为首个条件）
        /// Append a condition joined with AND (becomes the first condition when none exists yet)
        /// </summary>
        /// <remarks>
        /// 无嵌套分组 API：多次 And/Or 调用会把条件树整体左折叠合并（复用首个表达式的参数并重绑后续表达式的参数），
        /// 因此 "A.And(B).Or(C)" 语义是 ((A AND B) OR C)。最终生成 SQL 时每个二元节点都会加括号，不存在优先级歧义。
        /// There is no nested-group API: repeated And/Or calls left-fold the whole condition tree
        /// (parameters of the first expression are reused, later ones rebound), so A.And(B).Or(C) means ((A AND B) OR C).
        /// Every binary node is parenthesized when SQL is generated, so precedence is never ambiguous.
        /// </remarks>
        /// <param name="condition">要追加的条件表达式 / Condition to append</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public LambdaWrapper<M> And(Expression<Func<M, bool>> condition)
        {
            if (Condition == null)
            {
                Condition = condition;
            }
            else
            {
                Condition = Condition.And(condition);
            }
            return this;
        }

        /// <summary>
        /// 以 OR 连接追加条件（当前无条件时直接作为首个条件）
        /// Append a condition joined with OR (becomes the first condition when none exists yet)
        /// </summary>
        /// <remarks>见 <see cref="And"/>：And/Or 为左折叠合并，无嵌套分组语义。/ See <see cref="And"/>: And/Or left-fold the tree; there is no nested grouping.</remarks>
        /// <param name="condition">要追加的条件表达式 / Condition to append</param>
        /// <returns>当前包装器 / This wrapper</returns>
        public LambdaWrapper<M> Or(Expression<Func<M, bool>> condition)
        {
            if (Condition == null)
            {
                Condition = condition;
            }
            else
            {
                Condition = Condition.Or(condition);
            }
            return this;
        }

    }
}
