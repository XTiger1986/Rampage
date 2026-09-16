using Rampage.DatabaseLibrary.Helpers;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Extensions
{
    /// <summary>
    /// extensions of Expression
    /// </summary>
    internal static class ExpressionExtension
    {

        /// <summary>
        /// get the operator of expression type
        /// </summary>
        /// <param name="node">binary expression</param>
        /// <returns></returns>
        public static string GetExpressionTypeOperator(this BinaryExpression node)
        {
            return ExpressionHelper.GetExpressionTypeOperator(node);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberExpression"></param>
        /// <returns></returns>
        public static MemberExpression? GetRootMember(this MemberExpression memberExpression)
        {
            return ExpressionHelper.GetRootMember(memberExpression);
        }

        /// <summary>
        /// Convert to a unary expression and take a value
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object ToConvertAndGetValue(this Expression expression)
        {
            return ExpressionHelper.ToConvertAndGetValue(expression);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberExpression"></param>
        /// <returns></returns>
        public static object MemberToValue(this MemberExpression memberExpression)
        {
            return ExpressionHelper.MemberToValue(memberExpression);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberExpression"></param>
        /// <param name="topMember"></param>
        /// <returns></returns>
        public static object MemberToValue(this MemberExpression memberExpression, MemberExpression topMember)
        {
            return ExpressionHelper.MemberToValue(memberExpression, topMember);
        }


    }
}
