using Rampage.Enums;
using System;

namespace Rampage.Validation
{

    /// <summary>
    /// 单个属性的验证结果明细 / The validation detail of a single property
    /// </summary>
    public class ValidationResultItem
    {
        /// <summary>
        /// 属性名 / The property name
        /// </summary>
        public string Name
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 结果代码 / The result code
        /// </summary>
        public ValidationResultCode Code
        {
            get;
            set;
        }

        /// <summary>
        /// 格式化后的失败描述 / The formatted failure description
        /// </summary>
        public string? Description
        {
            get;
            set;
        }


    }
}
