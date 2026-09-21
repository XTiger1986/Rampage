using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 字符串长度验证：按 Min/Max 组合限制长度 / String length validation bounded by Min/Max
    /// </summary>
    /// <remarks>
    /// Min、Max 默认分别为 int.MinValue 与 int.MaxValue，仅设置一端时只校验该端；两端均为默认值时恒通过；
    /// Min and Max default to int.MinValue / int.MaxValue; when only one bound is set only that bound is checked; when both are default the rule always passes;
    /// 值为 null 时按 Required 决定通过与否。A null value passes or fails according to Required.
    /// </remarks>
    public class StringLengthAttribute : ValidationAttributeBase
    {
        /// <summary>
        /// 验证字符串长度是否在限定范围内 / Check that the length is within the configured bounds
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>长度合法返回 true / true when the length is within bounds</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }

            string s = v.ToString()!;
            int len = s.Length;
            if (Min > 0 && Max > 0 && Max < int.MaxValue)
            {
                return len >= Min && len <= Max;
            }
            else if (Min > 0 && (Max <= 0 || Max >= int.MaxValue))
            {
                return len >= Min;
            }
            else if ((Min <= 0 || Min <= int.MinValue) && (Max > 0 && Max < int.MaxValue))
            {
                return len <= Max;
            }
            return true;
        }

        /// <summary>
        /// 按边界组合返回对应的本地化键：StringLengthBetween / StringLengthLongerThan / StringLengthShorterThan
        /// Return the localization key matching the bound combination: StringLengthBetween / StringLengthLongerThan / StringLengthShorterThan
        /// </summary>
        /// <returns>本地化键 / The localization key</returns>
        public override string? GetLocalizerKey()
        {
            if (Min > 0 && Max > 0 && Max < int.MaxValue)
            {
                return $"{PropertyName}.StringLengthBetween";
            }
            else if (Min > 0 && (Max <= 0 || Max >= int.MaxValue))
            {
                return $"{PropertyName}.StringLengthLongerThan";
            }
            else if ((Min <= 0 || Min <= int.MinValue) && (Max > 0 && Max < int.MaxValue))
            {
                return $"{PropertyName}.StringLengthShorterThan";
            }
            else
            {
                if (Required)
                {
                    return $"{PropertyName}.Required";
                }
                else
                {
                    return base.GetLocalizerKey();
                }
            }
        }

        /// <summary>
        /// 返回参与消息格式化的边界值（两端都设置时为 {Min, Max}）/ Return the bound values used to format the message ({Min, Max} when both bounds are set)
        /// </summary>
        /// <returns>边界参数 / The bound arguments</returns>
        public override object?[]? GetTextArguments()
        {
            if (Min > 0 && Max > 0 && Max < int.MaxValue)
            {
                return new object?[] { Min, Max };
            }
            else if (Min > 0 && (Max <= 0 || Max >= int.MaxValue))
            {
                return new object?[] { Min };
            }
            else if ((Min <= 0 || Min <= int.MinValue) && (Max > 0 && Max < int.MaxValue))
            {
                return new object?[] { Max };
            }
            return null;
        }

        private int _min = int.MinValue;
        /// <summary>
        /// 最小长度（含），默认 int.MinValue 表示不限制 / Minimum length (inclusive); int.MinValue (default) means unlimited
        /// </summary>
        public int Min
        {
            get { return _min; }
            set { _min = value; }
        }

        private int _max = int.MaxValue;
        /// <summary>
        /// 最大长度（含），默认 int.MaxValue 表示不限制 / Maximum length (inclusive); int.MaxValue (default) means unlimited
        /// </summary>
        public int Max
        {
            get { return _max; }
            set { _max = value; }
        }

    }
}
