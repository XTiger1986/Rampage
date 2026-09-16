using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 
    /// </summary>
    public class DateTimeRangeAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 最小值
        /// </summary>
        public DateTime MinValue
        {
            get { return _minValue; }
            set { _minValue = value; }
        }

        private DateTime _maxValue = DateTime.MaxValue;
        /// <summary>
        /// 最大值
        /// </summary>
        public DateTime MaxValue
        {
            get { return _maxValue; }
            set { _maxValue = value; }
        }

    }
}
