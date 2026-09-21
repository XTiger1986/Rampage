using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 日期范围验证：值须可解析为 DateTime 且落在 [MinValue, MaxValue] 内 / Date-time range validation: the value must parse to a DateTime within [MinValue, MaxValue]
    /// </summary>
    /// <remarks>
    /// MinValue、MaxValue 默认分别为 DateTime.MinValue 与 DateTime.MaxValue，仅设置一端时只校验该端；两端均为默认值时恒通过；
    /// MinValue / MaxValue default to DateTime.MinValue / DateTime.MaxValue; when only one bound is set only that bound is checked; when both are default the rule always passes;
    /// 值为 null 或无法解析时按 Required 决定通过与否。A null or unparseable value passes or fails according to Required.
    /// </remarks>
    public class DateTimeRangeAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 验证值是否可解析为日期且在范围内 / Check that the value parses to a date within the range
        /// </summary>
        /// <param name="v">被验证值 / The value to validate</param>
        /// <returns>在范围内返回 true / true when within the range</returns>
        public override bool Validate(object? v)
        {
            if (v == null) { return !Required; }

            DateTime dtValue;
            if (!DateTime.TryParse(v.ToString(), out dtValue))
            {
                return !Required;
            }

            if (MinValue > DateTime.MinValue && MaxValue < DateTime.MaxValue)
            {
                return dtValue >= MinValue && dtValue <= MaxValue;
            }
            else if (MinValue > DateTime.MinValue && MaxValue >= DateTime.MaxValue)
            {
                return dtValue >= MinValue;
            }
            else if (MinValue <= DateTime.MinValue && MaxValue < DateTime.MaxValue)
            {
                return dtValue <= MaxValue;
            }
            return true;
        }

        /// <summary>
        /// 按边界组合返回对应的本地化键：DateTimeRange / DateTimeMustGreatEqualThan / DateTimeMustLessEqualThan
        /// Return the localization key matching the bound combination: DateTimeRange / DateTimeMustGreatEqualThan / DateTimeMustLessEqualThan
        /// </summary>
        /// <returns>本地化键 / The localization key</returns>
        public override string? GetLocalizerKey()
        {
            if (MinValue > DateTime.MinValue && MaxValue < DateTime.MaxValue)
            {
                return $"{PropertyName}.DateTimeRange";
            }
            else if (MinValue > DateTime.MinValue && MaxValue >= DateTime.MaxValue)
            {
                return $"{PropertyName}.DateTimeMustGreatEqualThan";
            }
            else if (MinValue <= DateTime.MinValue && MaxValue < DateTime.MaxValue)
            {
                return $"{PropertyName}.DateTimeMustLessEqualThan";
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
            if (MinValue > DateTime.MinValue && MaxValue < DateTime.MaxValue)
            {
                return new object?[] { MinValue, MaxValue };
            }
            else if (MinValue > DateTime.MinValue && MaxValue >= DateTime.MaxValue)
            {
                return new object?[] { MinValue };
            }
            else if (MinValue <= DateTime.MinValue && MaxValue < DateTime.MaxValue)
            {
                return new object?[] { MaxValue };
            }
            return null;
        }

        private DateTime _minValue = DateTime.MinValue;
        /// <summary>
        /// 最小值（含），默认 DateTime.MinValue 表示不限制 / Minimum value (inclusive); DateTime.MinValue (default) means unlimited
        /// </summary>
        public DateTime MinValue
        {
            get { return _minValue; }
            set { _minValue = value; }
        }

        private DateTime _maxValue = DateTime.MaxValue;
        /// <summary>
        /// 最大值（含），默认 DateTime.MaxValue 表示不限制 / Maximum value (inclusive); DateTime.MaxValue (default) means unlimited
        /// </summary>
        public DateTime MaxValue
        {
            get { return _maxValue; }
            set { _maxValue = value; }
        }

    }
}
