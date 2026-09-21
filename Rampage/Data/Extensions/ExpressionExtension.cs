using Rampage.DatabaseLibrary.Helpers;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Extensions
{
    /// <summary>
    /// 表达式扩展方法（转发到 <see cref="ExpressionHelper"/>）
    /// Expression extensions (forwarding to <see cref="ExpressionHelper"/>)
    /// </summary>
    internal static class ExpressionExtension
    {

        /// <summary>
        /// 取二元表达式的 SQL 运算符（右侧为 null 常量时等于/不等于改写为 IS / IS NOT）
        /// Get the SQL operator of a binary expression (equal/not-equal become IS / IS NOT when the right side is a null constant)
        /// </summary>
        /// <param name="node">二元表达式 / Binary expression</param>
        /// <returns>SQL 运算符文本 / SQL operator text</returns>
        public static string GetExpressionTypeOperator(this BinaryExpression node)
        {
            return ExpressionHelper.GetExpressionTypeOperator(node);
        }

        /// <summary>
        /// 取成员链的根成员表达式（根为闭包常量或静态成员）
        /// Get the root member expression of a member chain (closure-constant or static root)
        /// </summary>
        /// <param name="memberExpression">成员表达式 / Member expression</param>
        /// <returns>根成员表达式，不可求值时为 null / The root member expression, or null when not evaluable</returns>
        public static MemberExpression? GetRootMember(this MemberExpression memberExpression)
        {
            return ExpressionHelper.GetRootMember(memberExpression);
        }

        /// <summary>
        /// 转换为一元表达式并编译求值（委托按表达式实例缓存）
        /// Convert to a unary expression and evaluate it (delegate cached per expression instance)
        /// </summary>
        /// <param name="expression">要求值的表达式 / Expression to evaluate</param>
        /// <returns>求值结果 / The evaluated value</returns>
        public static object ToConvertAndGetValue(this Expression expression)
        {
            return ExpressionHelper.ToConvertAndGetValue(expression);
        }

        /// <summary>
        /// 对成员表达式求值（自动定位根成员）
        /// Evaluate a member expression (root member located automatically)
        /// </summary>
        /// <param name="memberExpression">成员表达式 / Member expression</param>
        /// <returns>求值结果 / The evaluated value</returns>
        public static object MemberToValue(this MemberExpression memberExpression)
        {
            return ExpressionHelper.MemberToValue(memberExpression);
        }

        /// <summary>
        /// 对成员表达式按指定根成员求值（委托以表达式字符串为键缓存）
        /// Evaluate a member expression against the given root member (delegates cached by expression string)
        /// </summary>
        /// <param name="memberExpression">成员表达式 / Member expression</param>
        /// <param name="topMember">根成员表达式 / Root member expression</param>
        /// <returns>求值结果 / The evaluated value</returns>
        public static object MemberToValue(this MemberExpression memberExpression, MemberExpression topMember)
        {
            return ExpressionHelper.MemberToValue(memberExpression, topMember);
        }


    }
}
