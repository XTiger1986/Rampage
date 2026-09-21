using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 忽略大小写的不等于验证（仅支持字符串属性）/ Case-insensitive not-equal validation (string properties only)
    /// </summary>
    /// <remarks>
    /// 注意：字符串分支当前实现为"忽略大小写与 Data 相等时返回 true（通过）"，与类名语义相反，疑似缺陷；
    /// Note: the string branch currently returns true (pass) when the value EQUALS Data ignoring case — the opposite of the class's intent; likely a defect.
    /// 非字符串属性一律按 Required 决定通过与否。Non-string properties always resolve by Required.
    /// </remarks>
    public class NotEqualIgnoreCaseValidAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否满足忽略大小写的不等于规则 / Check the case-insensitive not-equal rule
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>通过返回 true / true when the rule passes</returns>
        public override bool Validate(object? v)
        {
            if (v == null || Data == null) { return !Required; }

            TypeCode typeCode = System.Type.GetTypeCode(Property.PropertyType);
            string s = v.ToString()!;
            switch (typeCode)
            {
                case TypeCode.String:
                    return string.Compare(Data.ToString(), s, true) == 0;
                default:
                    return !Required;
            }
        }

        /// <summary>
        /// 用于比较的基准值 / The reference value to compare against
        /// </summary>
        public object? Data
        {
            get; set;
        }

    }
}
