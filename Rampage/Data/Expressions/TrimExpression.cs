using Rampage.Data.Extensions;
using Rampage.Utilities;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Expressions
{
    /// <summary>
    /// 表达式树规整访问器：把条件表达式改写成 SQL 解析器可处理的标准形态
    /// Expression-tree trimming visitor: rewrites a condition expression into the canonical shape the SQL parser understands
    /// </summary>
    /// <remarks>
    /// 规整规则：闭包成员（根为常量的成员链）急切编译求值并内联为 Constant；裸布尔成员改写为 ==true；
    /// Not 改写为 ==false；剥除两侧类型一致的冗余 Convert；AndAlso/OrElse 两侧节点类型不一致时改写布尔成员一侧。
    /// 注意急切求值不短路：lambda 里写 !x.HasValue || y == x.Value 这类条件，x 为 null 时会在规整阶段直接抛异常
    /// （求值闭包成员 x.Value 即抛 InvalidOperationException，任何一侧的判空保护都无效）。请改用不触发求值的写法
    /// （如直接比较 y == x），或在 lambda 外先求值再以局部常量引用。
    /// Rules: closure members (member chains rooted at a constant) are eagerly compiled and inlined as Constants; bare boolean
    /// members are rewritten to ==true; Not to ==false; redundant converts with identical operand/output types are stripped;
    /// when the two sides of AndAlso/OrElse differ in node type the boolean-member side is rewritten. Note the eager evaluation does
    /// NOT short-circuit: a condition like !x.HasValue || y == x.Value throws during trimming when x is null (evaluating the closure
    /// member x.Value throws InvalidOperationException regardless of any null-guard on the other side). Use a form that does not
    /// evaluate x.Value (e.g. compare y == x directly), or evaluate outside the lambda and reference a local constant.
    /// </remarks>
    internal class TrimExpression : ExpressionVisitor
    {

        private bool _isDeep;

        /// <summary>
        /// 规整表达式树并返回规整后的新树
        /// Trim the expression tree and return the rewritten one
        /// </summary>
        /// <param name="expression">要规整的表达式 / Expression to trim</param>
        /// <returns>规整后的表达式 / The trimmed expression</returns>
        public static Expression Trim(Expression expression)
        {
            return new TrimExpression().Visit(expression)!;
        }

        /// <summary>
        /// 按节点类型规整单个表达式节点（常量类型校正、闭包成员求值内联、布尔成员与 Not 改写、冗余 Convert 剥除）
        /// Trim a single node by its type (constant type fixup, closure member inlining, boolean-member and Not rewriting, redundant-convert stripping)
        /// </summary>
        /// <param name="expression">待规整的节点 / Node to trim</param>
        /// <returns>规整后的节点 / The trimmed node</returns>
        private Expression Fix(Expression expression)
        {
            var type = expression.Type;
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    if (GenericTool.GetUnderlyingType(expression.Type).Equals(GenericTool.GetUnderlyingType(type)))
                    {
                        return Expression.Constant(((ConstantExpression)expression).Value, type);
                    }
                    break;

                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)expression;
                    var root = memberExpression.GetRootMember();
                    if (root != null)
                    {
                        // 闭包成员链（根为常量）：编译委托急切求值并内联为常量——求值不短路，
                        // 引用捕获变量且变量可能为 null 时在此就可能抛异常 / Closure member chain (constant-rooted):
                        // eagerly compiled and inlined as a constant - evaluation does not short-circuit, so a null captured value can throw here
                        var value = memberExpression.MemberToValue(root);
                        return Expression.Constant(value, type);
                    }
                    else
                    {
                        // 参数上的裸布尔成员（x.IsDeleted）：改写为 x.IsDeleted == true 才能生成二元比较；
                        // 已处于深层（_isDeep）时不再包一层，避免无限改写 / Bare boolean member on the parameter
                        // (x.IsDeleted): rewritten to x.IsDeleted == true to form a binary comparison; when already deep (_isDeep) do not wrap again
                        if (_isDeep)
                            return expression;

                        _isDeep = true;
                        return Expression.Equal(expression, Expression.Constant(true));
                    }

                case ExpressionType.Convert:
                    var u = (UnaryExpression)expression;
                    // 仅当两侧类型完全一致时才是冗余转换可安全剥除；
                    // 值类型与 Nullable<T> 之间的提升/还原转换必须保留，
                    // 否则上层重建二元节点（如 DateTime? > DateTime 的 GreaterThan）
                    // 会因操作数类型不一致抛 InvalidOperationException
                    if (u.Operand.Type == type)
                    {
                        expression = u.Operand;
                        return expression;
                    }

                    if (u.Operand.Type.IsEnum && u.Operand.NodeType == ExpressionType.MemberAccess)
                    {
                        var mem = (MemberExpression)u.Operand;
                        if (mem.Expression!.NodeType == ExpressionType.Parameter)
                        {
                            return expression;
                        }
                        else
                        {
                            var value = Convert.ChangeType(mem.MemberToValue(), type);
                            return Expression.Constant(value, type);
                        }
                    }
                    break;

                case ExpressionType.Not:
                    // 裸 Not（!x.Flag）改写为 x.Flag == false，与布尔成员改写保持同一二元形态 / Bare Not (!x.Flag) rewritten to x.Flag == false, mirroring the boolean-member rewrite
                    var n = (UnaryExpression)expression;
                    return Expression.Equal(n.Operand, Expression.Constant(false));
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    var b = (BinaryExpression)expression;
                    _isDeep = true;
                    // 两侧节点类型不一致时改写布尔成员一侧为 ==true，或直接折叠常量一侧 / When sides differ in node type, rewrite the boolean-member side to ==true, or fold the constant side
                    if (b.Left.NodeType != b.Right.NodeType)
                    {
                        if (b.Left.NodeType == ExpressionType.MemberAccess && b.Left.Type.Name == "Boolean")
                        {
                            if (expression.NodeType == ExpressionType.AndAlso)
                                return Expression.AndAlso(Expression.Equal(b.Left, Expression.Constant(true)), b.Right);
                            if (expression.NodeType == ExpressionType.OrElse)
                                return Expression.OrElse(Expression.Equal(b.Left, Expression.Constant(true)), b.Right);
                        }
                        if (b.Right.NodeType == ExpressionType.MemberAccess && b.Right.Type.Name == "Boolean")
                        {
                            if (expression.NodeType == ExpressionType.AndAlso)
                                return Expression.AndAlso(b.Left, Expression.Equal(b.Right, Expression.Constant(true)));
                            if (expression.NodeType == ExpressionType.OrElse)
                                return Expression.OrElse(b.Left, Expression.Equal(b.Right, Expression.Constant(true)));
                        }
                        if (b.Left.NodeType == ExpressionType.Constant)
                            return b.Right;
                        if (b.Right.NodeType == ExpressionType.Constant)
                            return b.Left;
                    }
                    break;
                default:
                    _isDeep = true;
                    return expression;
            }

            return expression;
        }

        /// <summary>
        /// 访问并规整节点后再继续访问子节点
        /// Trim the node first, then continue visiting its children
        /// </summary>
        /// <param name="exp">待访问节点 / Node to visit</param>
        /// <returns>规整后的节点 / The trimmed node</returns>
        public override Expression? Visit(Expression? exp)
        {
            if (exp == null)
                return null;

            exp = Fix(exp);
            return base.Visit(exp);
        }

    }
}
