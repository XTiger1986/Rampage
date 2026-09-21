using System.Linq.Expressions;

namespace Rampage.DatabaseLibrary.Helpers
{
    /// <summary>
    /// 表达式子树替换访问器：遍历整棵树时把指定子树替换为另一个表达式。
    /// Replace one expression subtree with another while walking the full tree.
    /// </summary>
    internal class ExpressionModifier : ExpressionVisitor
    {
        /// <summary>
        /// 以替换目标与被替换子树初始化访问器
        /// Initialize the visitor with the replacement and the subtree to replace
        /// </summary>
        /// <param name="newExpression">替换目标表达式 / Replacement expression</param>
        /// <param name="oldExpression">被替换的子树 / Subtree to replace</param>
        public ExpressionModifier(Expression newExpression, Expression oldExpression)
        {
            _newExpression = newExpression;
            _oldExpression = oldExpression;
        }

        private readonly Expression _newExpression;
        private readonly Expression _oldExpression;

        /// <summary>
        /// 访问并替换整棵表达式树
        /// Visit and rewrite the whole expression tree
        /// </summary>
        /// <param name="node">目标表达式 / Target expression</param>
        /// <returns>替换后的表达式 / The rewritten expression</returns>
        public Expression Replace(Expression node)
        {
            return Visit(node)!;
        }

        /// <summary>
        /// 静态便捷方法：在目标表达式中把旧子树替换为新子树
        /// Static convenience: replace an old subtree with a new one inside the target expression
        /// </summary>
        /// <param name="node">目标表达式 / Target expression</param>
        /// <param name="oldExpression">被替换的子树 / Subtree to replace</param>
        /// <param name="newExpression">替换目标表达式 / Replacement expression</param>
        /// <returns>替换后的表达式 / The rewritten expression</returns>
        public static Expression Replace(Expression node, Expression oldExpression, Expression newExpression)
        {
            return new ExpressionModifier(newExpression, oldExpression).Replace(node);
        }

        /// <summary>
        /// 必须拦截每个节点：ExpressionVisitor 的 VisitMember/Visit* 各重载经由虚方法 Visit 递归访问子节点。
        /// 若只检查根节点（旧版 Replace 的实现），嵌套闭包成员（如 <c>closure.market.ItemId</c>）不会被替换，
        /// 导致 MemberValueDelegateCache 编译出的取值委托仍闭包引用首个 Constant，返回过期值。
        /// Must intercept every node: ExpressionVisitor.VisitMember/Visit* call Visit on children
        /// via the virtual Visit override. Checking only the root (the old Replace body) left nested
        /// closure members like <c>closure.market.ItemId</c> unbound, so MemberValueDelegateCache
        /// compiled delegates closed over the first Constant and returned stale values.
        /// </summary>
        public override Expression? Visit(Expression? node)
        {
            if (node == null)
            {
                return null;
            }

            if (ReferenceEquals(node, _oldExpression))
            {
                return _newExpression;
            }

            return base.Visit(node);
        }
    }
}
