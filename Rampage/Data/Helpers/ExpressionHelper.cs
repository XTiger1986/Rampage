using Rampage.ExceptionExtensions;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Rampage.DatabaseLibrary.Helpers
{
    /// <summary>
    /// Utilities of Expression
    /// </summary>
    public class ExpressionHelper
    {

        public const string EX_KEY_InvalidMemberExpression = "Invalid_MemberExpression";


        /// <summary>
        /// Expression Type and operator mapping dictionary
        /// </summary>
        private static readonly Dictionary<ExpressionType, string> NodeTypeOperatorDic = new Dictionary<ExpressionType, string>
        {
            { ExpressionType.And, " AND " },
            { ExpressionType.AndAlso, " AND " },
            { ExpressionType.Or, " OR " },
            { ExpressionType.OrElse, " OR " },
            { ExpressionType.Equal, " = " },
            { ExpressionType.NotEqual, " != " },
            { ExpressionType.LessThan, " < " },
            { ExpressionType.LessThanOrEqual, " <= " },
            { ExpressionType.GreaterThan, " > " },
            { ExpressionType.GreaterThanOrEqual, " >= " }
        };

        /// <summary>
        /// get the operator of expression type
        /// </summary>
        /// <param name="node">binary expression</param>
        /// <returns></returns>
        public static string GetExpressionTypeOperator(BinaryExpression node)
        {
            var nodeTypeDic = NodeTypeOperatorDic[node.NodeType];

            string? nodeType = null;
            if (node.Right.NodeType == ExpressionType.Constant && ((ConstantExpression)node.Right).Value == null)
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                        nodeType = " IS ";
                        break;
                    case ExpressionType.NotEqual:
                        nodeType = " IS NOT ";
                        break;
                }
            }

            return !string.IsNullOrEmpty(nodeType) ? nodeType : nodeTypeDic;
        }

        /// <summary>
        /// Get root member expression
        /// </summary>
        /// <param name="memberExpression"></param>
        /// <returns></returns>
        public static MemberExpression? GetRootMember(MemberExpression memberExpression)
        {
            if (memberExpression.Expression == null || memberExpression.Expression.NodeType == ExpressionType.Constant)
            {
                return memberExpression;
            }

            if (memberExpression.Expression.NodeType == ExpressionType.MemberAccess)
            {
                return GetRootMember((MemberExpression)memberExpression.Expression);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 转换成一元表达式并取值（编译委托按表达式实例缓存，随表达式回收自动释放）
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object ToConvertAndGetValue(Expression expression)
        {
            Func<object> invoker = ValueInvokerCache.GetValue(expression, expr =>
            {
                Expression body = expr.Type != typeof(object) ? Expression.Convert(expr, typeof(object)) : expr;
                return Expression.Lambda<Func<object>>(body).Compile();
            });
            return invoker();
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Expression, Func<object>> ValueInvokerCache = new System.Runtime.CompilerServices.ConditionalWeakTable<Expression, Func<object>>();

        private static readonly ConcurrentDictionary<string, Func<object?, object[]?, object>> MemberValueDelegateCache = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberExpression"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static object MemberToValue(MemberExpression memberExpression)
        {
            var topMember = GetRootMember(memberExpression);
            if (topMember == null)
            {
                throw new SuperException(EX_KEY_InvalidMemberExpression);
            }
            return MemberToValue(memberExpression, topMember);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberExpression"></param>
        /// <param name="topMember"></param>
        /// <returns></returns>
        public static object MemberToValue(MemberExpression memberExpression, MemberExpression topMember)
        {
            if (topMember.Expression == null)
            {
                string cacheKey = memberExpression.ToString()!;
                var aquire = MemberValueDelegateCache.GetOrAdd(cacheKey, _ => GetStaticPropertyDelegate(memberExpression));
                return aquire(null, null);
            }
            else
            {
                string cacheKey = memberExpression.ToString() + "|" + topMember.ToString();
                var aquire = MemberValueDelegateCache.GetOrAdd(cacheKey, _ => GetInstancePropertyDelegate(memberExpression, topMember));
                return aquire((topMember.Expression as ConstantExpression)!.Value, null);
            }
        }

        /// <summary>
        /// Get 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="topMember"></param>
        /// <returns></returns>
        private static Func<object?, object[]?, object> GetInstancePropertyDelegate(Expression e, MemberExpression topMember)
        {
            var parameter = Expression.Parameter(typeof(object), "local");
            var parameters = Expression.Parameter(typeof(object[]), "args");
            var castExpression = Expression.Convert(parameter, topMember.Member!.DeclaringType!);
            var localExpression = topMember.Update(castExpression);
            var replaceExpression = ExpressionModifier.Replace(e, topMember, localExpression);
            replaceExpression = Expression.Convert(replaceExpression, typeof(object));
            var compileExpression = Expression.Lambda<Func<object?, object[]?, object>>(replaceExpression, parameter, parameters);
            return compileExpression.Compile();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static Func<object?, object[]?, object> GetStaticPropertyDelegate(Expression e)
        {
            var parameter = Expression.Parameter(typeof(object), "local");
            var parameters = Expression.Parameter(typeof(object[]), "args");
            var convertExpression = Expression.Convert(e, typeof(object));
            var compileExpression = Expression.Lambda<Func<object?, object[]?, object>>(convertExpression, parameter, parameters);
            return compileExpression.Compile();
        }


    }
}
