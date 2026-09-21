using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Rampage.Reflection
{
    /// <summary>
    /// 取代 FieldInfo 反射的字段访问器：构造时用表达式树一次性编译出 Get/Set 委托，之后访问不再走反射
    /// Field accessor replacing FieldInfo reflection: compiles Get/Set delegates once via expression trees at construction time
    /// </summary>
    public class FieldAccessor
    {
        private static readonly ParameterExpression fieldParameter = Expression.Parameter(typeof(object));
        private static readonly ParameterExpression ownerParameter = Expression.Parameter(typeof(object));

        /// <summary>
        /// 为指定类型的字段编译访问委托 / Compile access delegates for the field of the given type
        /// </summary>
        /// <param name="type">字段所属类型 / The declaring type</param>
        /// <param name="fieldName">字段名 / The field name</param>
        /// <exception cref="ArgumentException">字段不存在 / The field does not exist</exception>
        public FieldAccessor(Type type, string fieldName)
        {
            var field = ReflectionTool.GetField(type, fieldName);
            if (field == null) throw new ArgumentException();

            var fieldExpression = Expression.Field(Expression.Convert(ownerParameter, type), field);

            Get = Expression.Lambda<Func<object, object?>>(
                Expression.Convert(fieldExpression, typeof(object)),
                ownerParameter).Compile();

            Set = Expression.Lambda<Action<object, object?>>(
                Expression.Assign(fieldExpression,
                    Expression.Convert(fieldParameter, field.FieldType)),
                ownerParameter, fieldParameter).Compile();
        }

        /// <summary>
        /// 字段读取委托：入参为实例，返回装箱后的字段值 / Field get delegate: takes the instance, returns the boxed field value
        /// </summary>
        public Func<object, object?> Get { get; }

        /// <summary>
        /// 字段写入委托：入参为(实例, 装箱值) / Field set delegate: takes (instance, boxed value)
        /// </summary>
        public Action<object, object?> Set { get; }
    }



}
