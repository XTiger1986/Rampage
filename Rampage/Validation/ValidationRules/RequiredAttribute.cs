using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 
    /// </summary>
    public class RequiredAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 是否必须有值
        /// </summary>
        public new bool Required
        {
            get { return true; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return false; }

            return true;
        }

    }
}
