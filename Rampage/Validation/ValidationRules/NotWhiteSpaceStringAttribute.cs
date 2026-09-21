using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 非空白字符串验证：值不得为 null 且不得全为空白字符 / Non-whitespace validation: the value must not be null or whitespace-only
    /// </summary>
    public class NotWhiteSpaceStringAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 是否必须有值：本特性恒为 true（new 隐藏基类属性）/ Mandatory flag: always true for this attribute (hides the base property)
        /// </summary>
        public new bool Required
        {
            get { return true; }
        }

        /// <summary>
        /// 验证值是否包含非空白字符 / Check that the value contains non-whitespace characters
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>非 null 且非纯空白返回 true / true when the value is not null and not whitespace-only</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return false; }
            string s = v.ToString()!;

            return !string.IsNullOrWhiteSpace(s);
        }

    }
}
