using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 必填字符串验证：值不得为 null 且不得为空字符串 / Required string validation: the value must not be null or empty
    /// </summary>
    /// <remarks>仅拒绝 null 与 ""；纯空白字符串（如 " "）可通过 / Only null and "" are rejected; whitespace-only strings (e.g. " ") still pass</remarks>
    public class RequiredStringAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 是否必须有值：本特性恒为 true（new 隐藏基类属性）/ Mandatory flag: always true for this attribute (hides the base property)
        /// </summary>
        public new bool Required
        {
            get { return true; }
        }

        /// <summary>
        /// 验证值是否为非空字符串 / Check that the value is a non-empty string
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>非 null 且非空字符串返回 true / true when the value is not null and not empty</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return false; }
            string s = v.ToString()!;

            return !string.IsNullOrEmpty(s);
        }

    }
}
