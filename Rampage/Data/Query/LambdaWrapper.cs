using Rampage.Data.Expressions;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Query
{
    /// <summary>
    /// Database Lambda wrapper
    /// </summary>
    public class LambdaWrapper<M> : QueryWrapper<M> where M : class
    {

        /// <summary>
        /// 
        /// </summary>
        public Expression<Func<M, bool>>? Condition
        {
            get;
            protected set;
        }

        /// <summary>
        /// Initialize a empty Database Lambda wrapper
        /// </summary>
        public LambdaWrapper() : base()
        {
        }

        /// <summary>
        /// Initialize a Database Lambda wrapper by a Lambda Expression
        /// </summary>
        /// <param name="expression"></param>
        public LambdaWrapper(Expression<Func<M, bool>> expression) : base()
        {
            Condition = expression;
        }

        /// <summary>
        /// Initialize a empty Database Lambda wrapper
        /// </summary>
        /// <returns></returns>
        public static LambdaWrapper<M> Empty()
        {
            return new LambdaWrapper<M>();
        }

        /// <summary>
        /// Initialize a Database Lambda wrapper by a Lambda Expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static LambdaWrapper<M> Create(Expression<Func<M, bool>> expression)
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(expression);

            return wrapper;
        }

        /// <summary>
        /// Add a conditional expression for the "AND" operator
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public LambdaWrapper<M> And(Expression<Func<M, bool>> condition)
        {
            if (Condition == null)
            {
                Condition = condition;
            }
            else
            {
                Condition = Condition.And(condition);
            }
            return this;
        }

        /// <summary>
        /// Add a conditional expression for the "OR" operator
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public LambdaWrapper<M> Or(Expression<Func<M, bool>> condition)
        {
            if (Condition == null)
            {
                Condition = condition;
            }
            else
            {
                Condition = Condition.Or(condition);
            }
            return this;
        }

    }
}
