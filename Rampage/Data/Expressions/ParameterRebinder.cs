using System;
using System.Linq.Expressions;

namespace Rampage.Data.Expressions
{
    /// <summary>
    /// 参数重绑访问器：把表达式树中的参数节点替换为映射表中的目标参数（用于多表达式合并时统一参数）
    /// Parameter rebinding visitor: replaces parameter nodes with targets from a map (used to unify parameters when merging expressions)
    /// </summary>
    public class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> map;

        /// <summary>
        /// 以参数映射表（源参数 -> 目标参数）初始化访问器
        /// Initialize the visitor with a parameter map (source -> target)
        /// </summary>
        /// <param name="map">参数映射表，为 null 时视为空表 / Parameter map; null is treated as an empty map</param>
        public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
        {
            this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        /// <summary>
        /// 把表达式中的参数按映射表批量替换后返回新表达式
        /// Replace parameters in an expression according to the map and return the new expression
        /// </summary>
        /// <param name="map">参数映射表（源参数 -> 目标参数）/ Parameter map (source -> target)</param>
        /// <param name="exp">要替换的表达式 / Expression to rewrite</param>
        /// <returns>替换后的表达式 / The rewritten expression</returns>
        public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
        {
            return new ParameterRebinder(map).Visit(exp);
        }

        /// <summary>
        /// 访问参数节点：在映射表中则替换为目标参数，否则保持原样
        /// Visit a parameter node: replaced by the mapped target when present, otherwise unchanged
        /// </summary>
        /// <param name="p">参数表达式节点 / Parameter expression node</param>
        /// <returns>替换后的参数节点 / The (possibly replaced) parameter node</returns>
        protected override Expression VisitParameter(ParameterExpression p)
        {
            ParameterExpression? replacement;
            if (map.TryGetValue(p, out replacement))
            {
                p = replacement;
            }
            return base.VisitParameter(p);
        }
    }  
}
