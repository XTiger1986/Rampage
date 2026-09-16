using Rampage.Data.Expressions;
using System;
using System.Linq.Expressions;

namespace Rampage.Data.Query
{
    /// <summary>
    /// Specify update fields for data table
    /// </summary>
    public class UpdateFields
    {

        /// <summary>
        /// Initialize a empty update fields instance
        /// </summary>
        public UpdateFields()
        {
            Fields = new Dictionary<string, object?>();
        }

        /// <summary>
        /// Initialize a empty update fields instance
        /// </summary>
        /// <returns></returns>
        public static UpdateFields New
        {
            get
            {
                return new UpdateFields();
            }
        }

        public static UpdateFields Init(string field, object? value)
        {
            UpdateFields fields = new UpdateFields();
            fields = fields.Set(field, value);
            return fields;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, object?> Fields
        {
            get; protected set;
        }

        /// <summary>
        /// Add update field
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual UpdateFields Set(string field, object? value)
        {
            if (!Fields.ContainsKey(field))
                Fields.Add(field, value);
            else
                Fields[field] = value;
            return this;
        }

    }

    public class UpdateFields<M> : UpdateFields
        where M : class
    {

        public TableMapping Mapping
        {
            get;
            private set;
        }

        public UpdateFields() : base()
        {
            Mapping = ModelSchemaStore.GetModelMapping<M>()!;
        }

        /// <summary>
        /// Initialize a empty update fields instance
        /// </summary>
        /// <returns></returns>
        public new static UpdateFields<M> New
        {
            get
            {
                return new UpdateFields<M>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keySelector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static UpdateFields<M> Build<TKey>(Expression<Func<M, TKey>> keySelector, TKey? value)
        {
            UpdateFields<M> fields = new UpdateFields<M>();
            fields = fields.Set(keySelector, value);
            return fields;
        }

        /// <summary>
        /// Add update field
        /// </summary>
        /// <param name="keySelector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual UpdateFields<M> Set<TKey>(Expression<Func<M, TKey>> keySelector, TKey? value)
        {
            string field = LambdaExpressionParser.GetColumnName<M>(keySelector, Mapping)!;
            if (!Fields.ContainsKey(field))
                Fields.Add(field, value);
            else
                Fields[field] = value;
            return this;
        }

    }

}
