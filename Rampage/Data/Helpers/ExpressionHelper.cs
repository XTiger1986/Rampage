using Rampage.ExceptionExtensions;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Rampage.DatabaseLibrary.Helpers
{
    /// <summary>
    /// 表达式工具类：表达式运算符映射、根成员定位与成员表达式求值（带委托缓存）
    /// Expression utilities: operator mapping, root-member locating and member-expression evaluation (with delegate caching)
    /// </summary>
    public class ExpressionHelper
    {

        /// <summary>
        /// 异常键：无效的成员表达式（根成员不是常量或静态成员）
        /// Exception key: invalid member expression (root is neither a constant nor a static member)
        /// </summary>
        public const string EX_KEY_InvalidMemberExpression = "Invalid_MemberExpression";


        /// <summary>
        /// 表达式节点类型到 SQL 运算符的映射字典
        /// Expression type to SQL operator mapping dictionary
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
        /// 取二元表达式的 SQL 运算符；右侧为 null 常量时，等于/不等于分别改写为 IS / IS NOT
        /// Get the SQL operator of a binary expression; when the right side is a null constant, equal/not-equal become IS / IS NOT
        /// </summary>
        /// <param name="node">二元表达式 / Binary expression</param>
        /// <returns>SQL 运算符文本 / SQL operator text</returns>
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
        /// 取成员链的根成员表达式：链根为常量（闭包）或静态成员时返回根，否则返回 null
        /// Get the root member of a member chain: returns the root when rooted at a constant (closure) or static member, otherwise null
        /// </summary>
        /// <param name="memberExpression">成员表达式 / Member expression</param>
        /// <returns>根成员表达式，不可求值时为 null / The root member expression, or null when not evaluable</returns>
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
        /// Convert to a unary expression and evaluate it (compiled delegate cached per expression instance, released with the expression)
        /// </summary>
        /// <param name="expression">要求值的表达式 / Expression to evaluate</param>
        /// <returns>求值结果 / The evaluated value</returns>
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
        /// 对成员表达式求值：定位根成员后编译取值（根为闭包常量或静态成员均可）
        /// Evaluate a member expression: locates the root member then compiles and evaluates (closure-constant or static roots both work)
        /// </summary>
        /// <param name="memberExpression">成员表达式 / Member expression</param>
        /// <returns>求值结果 / The evaluated value</returns>
        /// <exception cref="SuperException">根成员不可求值时抛出 / Thrown when the root member is not evaluable</exception>
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
        /// 对成员表达式按指定根成员求值：编译后的取值委托以"成员表达式字符串"为键缓存复用（实例根的键附加根表达式字符串）
        /// Evaluate a member expression against the given root member: compiled delegates are cached by the member expression string (the instance-root key appends the root expression string)
        /// </summary>
        /// <remarks>注意缓存键基于表达式文本，文本相同但实际捕获对象不同的表达式会命中同一委托——仅当根常量对象也一致时才安全。/ Note the cache key is expression text: distinct captures with identical text share one delegate - safe only when the root constant object is also identical.</remarks>
        /// <param name="memberExpression">成员表达式 / Member expression</param>
        /// <param name="topMember">根成员表达式 / Root member expression</param>
        /// <returns>求值结果 / The evaluated value</returns>
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
        /// 编译实例根成员的取值委托：把根成员替换为可传入实例的参数后整体编译
        /// Compile the value delegate for an instance-rooted member: replaces the root member with an instance parameter, then compiles the whole chain
        /// </summary>
        /// <param name="e">成员表达式 / Member expression</param>
        /// <param name="topMember">根成员表达式 / Root member expression</param>
        /// <returns>取值委托（参数 1 为根实例对象）/ The value delegate (arg 1 is the root instance)</returns>
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
        /// 编译静态根成员的取值委托
        /// Compile the value delegate for a static-rooted member
        /// </summary>
        /// <param name="e">成员表达式 / Member expression</param>
        /// <returns>取值委托（根为静态成员，不依赖实例）/ The value delegate (static root, instance-independent)</returns>
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
