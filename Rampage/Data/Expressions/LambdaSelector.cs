using Rampage.ExceptionExtensions;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Rampage.Data.Expressions
{
    /// <summary>
    /// Key Selector Lambda Expression Parser
    /// </summary>
    public class LambdaSelector : ExpressionVisitor
    {
        /// <summary>
        /// Mapping Model
        /// </summary>
        public TableMapping? Model
        {
            get;
            protected set;
        }

        /// <summary>
        /// Mapping Property Info
        /// </summary>
        public FieldMapping? Property
        {
            get;
            protected set;
        }

        private LambdaSelector() { }

        /// <summary>
        /// Initialize a Key Selector Expression Parser by a Key Selector Expression
        /// </summary>
        /// <param name="expression">the Key Selector Expression to parse</param>
        /// <param name="mapping"></param>
        public LambdaSelector(LambdaExpression expression, TableMapping? mapping = null)
        {
            Model = mapping;
            Visit(expression);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            MemberInfo memberInfo = node.Member;
            Model = Model ?? ModelSchemaStore.GetModelMapping(memberInfo.DeclaringType!);
            if (Model == null)
            {
                throw new SuperException(Globals.EX_KEY_INVALID_ARGUMENT, nameof(node));
            }
            Property = Model.GetPropertyMapping(memberInfo.Name);
            if (Property == null)
            {
                throw new SuperException(Globals.EX_KEY_INVALID_ARGUMENT, nameof(node));
            }

            return node;
        }

        /// <summary>
        /// Get Model Mapping
        /// </summary>
        /// <returns></returns>
        public TableMapping GetMapping()
        {
            return Model!;
        }

        /// <summary>
        /// Get Property Mapping
        /// </summary>
        /// <returns></returns>
        public FieldMapping GetMappingProperty()
        {
            return Property!;
        }

        /// <summary>
        /// Get datatable column name of the Key Selector
        /// </summary>
        /// <returns></returns>
        public string GetMappingColumnName()
        {
            return Property!.ColumnName!;
        }

    }
}
