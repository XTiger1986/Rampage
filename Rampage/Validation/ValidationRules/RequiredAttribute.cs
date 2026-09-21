using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 必填验证：值不得为 null / Required validation: the value must not be null
    /// </summary>
    /// <remarks>仅拒绝 null，空字符串视为"有值"可通过 / Only null is rejected; empty strings count as present and pass</remarks>
    public class RequiredAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 是否必须有值：本特性恒为 true（new 隐藏基类属性）/ Mandatory flag: always true for this attribute (hides the base property)
        /// </summary>
        public new bool Required
        {
            get { return true; }
        }

        /// <summary>
        /// 验证值是否非 null / Check that the value is not null
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>非 null 返回 true / true when the value is not null</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return false; }

            return true;
        }

    }
}
