using Rampage.ExceptionExtensions;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Rampage.Data.Expressions
{
    /// <summary>
    /// 字段选择器 Lambda 表达式解析器：解析出成员对应的模型映射与列名
    /// Key selector lambda expression parser: resolves the model mapping and column name of the selected member
    /// </summary>
    public class LambdaSelector : ExpressionVisitor
    {
        /// <summary>
        /// 解析到的模型映射
        /// Resolved model mapping
        /// </summary>
        public TableMapping? Model
        {
            get;
            protected set;
        }

        /// <summary>
        /// 解析到的字段映射信息
        /// Resolved field mapping
        /// </summary>
        public FieldMapping? Property
        {
            get;
            protected set;
        }

        private LambdaSelector() { }

        /// <summary>
        /// 以一个字段选择器表达式初始化解析器并立即访问解析
        /// Initialize the parser with a key selector expression and parse it immediately
        /// </summary>
        /// <param name="expression">要解析的字段选择器表达式 / The key selector expression to parse</param>
        /// <param name="mapping">模型映射，为 null 时按成员声明类型推断 / Model mapping; inferred from the member's declaring type when null</param>
        public LambdaSelector(LambdaExpression expression, TableMapping? mapping = null)
        {
            Model = mapping;
            Visit(expression);
        }

        /// <summary>
        /// 访问成员节点：解析其模型映射与字段映射，无法解析时抛出参数无效异常
        /// Visit a member node: resolves its model and field mappings, throwing an invalid-argument exception when unmapped
        /// </summary>
        /// <param name="node">成员表达式节点 / Member expression node</param>
        /// <returns>原节点 / The original node</returns>
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
        /// 取解析到的模型映射
        /// Get the resolved model mapping
        /// </summary>
        /// <returns>模型映射 / Model mapping</returns>
        public TableMapping GetMapping()
        {
            return Model!;
        }

        /// <summary>
        /// 取解析到的字段映射信息
        /// Get the resolved field mapping
        /// </summary>
        /// <returns>字段映射 / Field mapping</returns>
        public FieldMapping GetMappingProperty()
        {
            return Property!;
        }

        /// <summary>
        /// 取字段选择器对应的数据表列名
        /// Get the datatable column name of the key selector
        /// </summary>
        /// <returns>实际列名 / The mapped column name</returns>
        public string GetMappingColumnName()
        {
            return Property!.ColumnName!;
        }

    }
}
