using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Rampage.Reflection
{
    /// <summary>
    /// 取代FieldInfo反射的字段访问器
    /// </summary>
    public class FieldAccessor
    {
        private static readonly ParameterExpression fieldParameter = Expression.Parameter(typeof(object));
        private static readonly ParameterExpression ownerParameter = Expression.Parameter(typeof(object));

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

        public Func<object, object?> Get { get; }

        public Action<object, object?> Set { get; }
    }



}
