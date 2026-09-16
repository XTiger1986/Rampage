using System.Linq.Expressions;

namespace Rampage.DatabaseLibrary.Helpers
{
    /// <summary>
    /// Replace one expression subtree with another while walking the full tree.
    /// </summary>
    internal class ExpressionModifier : ExpressionVisitor
    {
        public ExpressionModifier(Expression newExpression, Expression oldExpression)
        {
            _newExpression = newExpression;
            _oldExpression = oldExpression;
        }

        private readonly Expression _newExpression;
        private readonly Expression _oldExpression;

        public Expression Replace(Expression node)
        {
            return Visit(node)!;
        }

        public static Expression Replace(Expression node, Expression oldExpression, Expression newExpression)
        {
            return new ExpressionModifier(newExpression, oldExpression).Replace(node);
        }

        /// <summary>
        /// Must intercept every node: ExpressionVisitor.VisitMember/Visit* call Visit on children
        /// via the virtual Visit override. Checking only the root (old Replace body) left nested
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
