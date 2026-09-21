using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 整数范围验证：值须可解析为 long 且落在 [MinValue, MaxValue] 内 / Integer range validation: the value must parse to a long within [MinValue, MaxValue]
    /// </summary>
    /// <remarks>
    /// MinValue、MaxValue 默认分别为 long.MinValue 与 long.MaxValue，仅设置一端时只校验该端；两端均为默认值时恒通过；
    /// MinValue / MaxValue default to long.MinValue / long.MaxValue; when only one bound is set only that bound is checked; when both are default the rule always passes;
    /// 值为 null 或无法解析时按 Required 决定通过与否。A null or unparseable value passes or fails according to Required.
    /// </remarks>
    public class IntegerRangeAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否可解析为整数且在范围内 / Check that the value parses to an integer within the range
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>在范围内返回 true / true when within the range</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }

            long iValue;
            if (!long.TryParse(v.ToString(), out iValue))
            {
                return !Required;
            }

            if (MinValue > long.MinValue && MaxValue < long.MaxValue)
            {
                return iValue >= MinValue && iValue <= MaxValue;
            }
            else if (MinValue > long.MinValue && MaxValue >= long.MaxValue)
            {
                return iValue >= MinValue;
            }
            else if (MinValue <= long.MinValue && MaxValue < long.MaxValue)
            {
                return iValue <= MaxValue;
            }
            return true;
        }

        /// <summary>
        /// 按边界组合返回对应的本地化键：IntegerRange / IntegerMustGreatEqualThan / IntegerMustLessEqualThan
        /// Return the localization key matching the bound combination: IntegerRange / IntegerMustGreatEqualThan / IntegerMustLessEqualThan
        /// </summary>
        /// <returns>本地化键 / The localization key</returns>
        public override string? GetLocalizerKey()
        {
            if (MinValue > long.MinValue && MaxValue < long.MaxValue)
            {
                return $"{PropertyName}.IntegerRange";
            }
            else if (MinValue > long.MinValue && MaxValue >= long.MaxValue)
            {
                return $"{PropertyName}.IntegerMustGreatEqualThan";
            }
            else if (MinValue <= long.MinValue && MaxValue < long.MaxValue)
            {
                return $"{PropertyName}.IntegerMustLessEqualThan";
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
        /// 返回参与消息格式化的边界值（两端都设置时为 {MinValue, MaxValue}）/ Return the bound values used to format the message ({MinValue, MaxValue} when both bounds are set)
        /// </summary>
        /// <returns>边界参数 / The bound arguments</returns>
        public override object?[]? GetTextArguments()
        {
            if (MinValue > long.MinValue && MaxValue < long.MaxValue)
            {
                return new object?[] { MinValue, MaxValue };
            }
            else if (MinValue > long.MinValue && MaxValue >= long.MaxValue)
            {
                return new object?[] { MinValue };
            }
            else if (MinValue <= long.MinValue && MaxValue < long.MaxValue)
            {
                return new object?[] { MaxValue };
            }
            return null;
        }

        private long _minValue = long.MinValue;
        /// <summary>
        /// 最小值（含），默认 long.MinValue 表示不限制 / Minimum value (inclusive); long.MinValue (default) means unlimited
        /// </summary>
        public long MinValue
        {
            get { return _minValue; }
            set { _minValue = value; }
        }

        private long _maxValue = long.MaxValue;
        /// <summary>
        /// 最大值（含），默认 long.MaxValue 表示不限制 / Maximum value (inclusive); long.MaxValue (default) means unlimited
        /// </summary>
        public long MaxValue
        {
            get { return _maxValue; }
            set { _maxValue = value; }
        }

    }
}
