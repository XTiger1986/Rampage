using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 
    /// </summary>
    public class NotEqualIgnoreCaseValidAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        public object? Data
        {
            get; set;
        }

    }
}
