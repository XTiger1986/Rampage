using System;
using System.Reflection;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器字段的标识属性：声明一个筛选器属性如何映射到实体字段并参与条件构建
    /// Filter item attribute: declares how a filter property maps to entity fields and builds conditions
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class FilterItemAttribute : Attribute
    {

        /// <summary>
        /// 对应的筛选器属性信息（构建结构时回填）
        /// The filter property this item is bound to (back-filled when the schema is built)
        /// </summary>
        internal PropertyInfo? Property
        {
            get; set;
        }

        private string? _fieldName;
        /// <summary>
        /// 指定这个过滤项对应的实体字段名称（如果不指定，将自动按照属性名称）
        /// Entity field name of this filter item (defaults to the property name when unspecified)
        /// </summary>
        public string? FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }

        private string? _orFields;
        /// <summary>
        /// 指定这个过滤项对应的其它实体字段名称，以英文逗号隔开，将会构建为以OR为组合的条件组
        /// Other entity field names of this item (comma-separated); together with the main field they form an OR-joined group
        /// </summary>
        public string? OrFields
        {
            get { return _orFields; }
            set { _orFields = value; }
        }

        private string? _formName;
        /// <summary>
        /// 指定这个过滤项对应的表单名称（如果不指定，将优先按FieldName名称，若无，则自动按照属性名称）
        /// Form field name of this item (falls back to FieldName, then to the property name)
        /// </summary>
        public string? FormName
        {
            get { return _formName; }
            set { _formName = value; }
        }

        private bool _ignore = false;
        /// <summary>
        /// 忽略该字段，标识该字段不参与筛选器的构成
        /// Ignore this field: it takes no part in filter condition building
        /// </summary>
        public bool Ignore
        {
            get { return _ignore; }
            set { _ignore = value; }
        }

        private FilterConnectorType _connector = FilterConnectorType.And;
        /// <summary>
        /// 连接符（该项与前一条件之间的 AND/OR 连接方式）
        /// Connector (how this item joins the previous condition: AND / OR)
        /// </summary>
        public FilterConnectorType Connector
        {
            get { return _connector; }
            set { _connector = value; }
        }

        private FilterOperatorType _operator = FilterOperatorType.Equal;
        /// <summary>
        /// 运算符（如果筛选项是ValueScope区间类型，此设置值无效；区间类型请改用 LeftOpenScope/RightOpenScope）
        /// Operator (ignored when the item is a ValueScope range type; range items use LeftOpenScope/RightOpenScope instead)
        /// </summary>
        public FilterOperatorType Operator
        {
            get { return _operator; }
            set { _operator = value; }
        }

        /// <summary>
        /// 排序值，值越大，组合的查询条件越靠前
        /// Sort order: the larger the value, the earlier the condition is placed
        /// </summary>
        public int SortOrder
        {
            get; set;
        }

        /// <summary>
        /// 指定这个过滤项对应的其它数据表字段名称集合，将会构建为以OR为组合的条件组（由 <see cref="OrFields"/> 按逗号拆分回填）
        /// Other table field names of this item forming an OR-joined group (split from <see cref="OrFields"/> by comma)
        /// </summary>
        public List<string>? OrFieldList
        {
            get;
            internal set;
        }

    }
}
