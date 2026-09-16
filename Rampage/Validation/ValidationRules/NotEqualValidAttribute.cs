using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 
    /// </summary>
    public class NotEqualValidAttribute : ValidationAttributeBase
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
                    return string.Compare(Data.ToString(), s) == 0;
                case TypeCode.Int64:
                case TypeCode.Int32:
                case TypeCode.Int16:
                case TypeCode.SByte:
                    long longValue;
                    if (!long.TryParse(s, out longValue))
                    {
                        return !Required;
                    }
                    long validLong;
                    if (!long.TryParse(Data.ToString(), out validLong))
                    {
                        return !Required;
                    }
                    return longValue != validLong;
                case TypeCode.UInt64:
                case TypeCode.UInt32:
                case TypeCode.UInt16:
                case TypeCode.Byte:
                    ulong ulongValue;
                    if (!ulong.TryParse(s, out ulongValue))
                    {
                        return !Required;
                    }
                    ulong validULong;
                    if (!ulong.TryParse(Data.ToString(), out validULong))
                    {
                        return !Required;
                    }
                    return ulongValue != validULong;
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    decimal decimalValue;
                    if (!decimal.TryParse(s, out decimalValue))
                    {
                        return !Required;
                    }
                    decimal validDecimal;
                    if (!decimal.TryParse(Data.ToString(), out validDecimal))
                    {
                        return !Required;
                    }
                    return decimalValue != validDecimal;
                case TypeCode.DateTime:
                    DateTime dateTimeValue;
                    if (!DateTime.TryParse(s, out dateTimeValue))
                    {
                        return !Required;
                    }
                    DateTime validDateTime;
                    if (!DateTime.TryParse(Data.ToString(), out validDateTime))
                    {
                        return !Required;
                    }
                    return dateTimeValue != validDateTime;
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
