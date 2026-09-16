using System;
using System.Reflection;

namespace Rampage.Validation
{
    /// <summary>
    /// 
    /// </summary>
    public class ValidationRule
    {

        /// <summary>
        /// 
        /// </summary>
        internal string PropertyName
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        internal PropertyInfo? Property
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<ValidationRuleInfo> RuleList
        {
            get;
            set;
        } = default!;

    }
}
