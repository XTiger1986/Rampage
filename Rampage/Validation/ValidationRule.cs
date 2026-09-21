using System;
using System.Reflection;

namespace Rampage.Validation
{
    /// <summary>
    /// 单个属性绑定的验证规则集合 / The set of validation rules bound to a single property
    /// </summary>
    public class ValidationRule
    {

        /// <summary>
        /// 属性名 / The property name
        /// </summary>
        internal string PropertyName
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 属性元数据 / The property metadata
        /// </summary>
        internal PropertyInfo? Property
        {
            get;
            set;
        }

        /// <summary>
        /// 该属性上的全部规则（按特性声明顺序） / All rules on the property (in attribute declaration order)
        /// </summary>
        public List<ValidationRuleInfo> RuleList
        {
            get;
            set;
        } = default!;

    }
}
