using System;
using System.Reflection;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器字段的标识属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class FilterItemAttribute : Attribute
    {

        /// <summary>
        /// 对应字段信息
        /// </summary>
        internal PropertyInfo? Property
        {
            get; set;
        }

        private string? _fieldName;
        /// <summary>
        /// 指定这个过滤项对应的实体字段名称（如果不指定，将自动按照属性名称）
        /// </summary>
        public string? FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }

        private string? _orFields;
        /// <summary>
        /// 指定这个过滤项对应的其它实体字段名称，以英文逗号隔开，将会构建为以OR为组合的条件组
        /// </summary>
        public string? OrFields
        {
            get { return _orFields; }
            set { _orFields = value; }
        }

        private string? _formName;
        /// <summary>
        /// 指定这个过滤项对应的表单名称（如果不指定，将优先按FieldName名称，若无，则自动按照属性名称）
        /// </summary>
        public string? FormName
        {
            get { return _formName; }
            set { _formName = value; }
        }

        private bool _ignore = false;
        /// <summary>
        /// 忽略该字段，标识该字段不参与筛选器的构成
        /// </summary>
        public bool Ignore
        {
            get { return _ignore; }
            set { _ignore = value; }
        }

        private FilterConnectorType _connector = FilterConnectorType.And;
        /// <summary>
        /// 连接符
        /// </summary>
        public FilterConnectorType Connector
        {
            get { return _connector; }
            set { _connector = value; }
        }

        private FilterOperatorType _operator = FilterOperatorType.Equal;
        /// <summary>
        /// 运算符（如果筛选项是ValueScope区间类型，此设置值无效）
        /// </summary>
        public FilterOperatorType Operator
        {
            get { return _operator; }
            set { _operator = value; }
        }

        /// <summary>
        /// 排序值，值越大，组合的查询条件越靠前
        /// </summary>
        public int SortOrder
        {
            get; set;
        }

        /// <summary>
        /// 指定这个过滤项对应的其它数据表字段名称集合，将会构建为以OR为组合的条件组
        /// </summary>
        public List<string>? OrFieldList
        {
            get;
            internal set;
        }

    }
}
