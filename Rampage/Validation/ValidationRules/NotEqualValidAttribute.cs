using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 不等于验证：值不得等于 Data 指定的基准值 / Not-equal validation: the value must differ from the reference value in Data
    /// </summary>
    /// <remarks>
    /// 按属性 TypeCode 分派比较方式（数值、日期按解析后比较，不等于时通过）；
    /// The comparison is dispatched by the property's TypeCode (numeric / date values are parsed and compared; passes when not equal);
    /// 值或 Data 为 null、类型不支持、解析失败时按 Required 决定通过与否。
    /// When either side is null, the type is unsupported, or parsing fails, the outcome follows Required.
    /// </remarks>
    public class NotEqualValidAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否满足不等于规则（按属性类型分派比较）/ Check the not-equal rule (comparison dispatched by property type)
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
                    return string.Compare(Data.ToString(), s) != 0;
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
        /// 用于比较的基准值 / The reference value to compare against
        /// </summary>
        public object? Data
        {
            get; set;
        }

    }
}
