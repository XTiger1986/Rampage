using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 验证结果 / The validation result
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否全部通过（任一非警告规则失败即为 false）/ Whether all rules passed (false when any non-warning rule fails)
        /// </summary>
        public bool Success
        {
            get;
            internal set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasWarnings
        {
            get;
            internal set;
        }

        /// <summary>
        /// 失败明细（键为属性名，每属性仅收录首条失败）/ Failure details keyed by property name (first failure only per property)
        /// </summary>
        public Dictionary<string, ValidationResultItem>? Details
        {
            get;
            internal set;
        }

    }
}
