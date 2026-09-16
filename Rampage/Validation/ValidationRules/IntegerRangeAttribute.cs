using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 
    /// </summary>
    public class IntegerRangeAttribute : ValidationAttributeBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 最小值
        /// </summary>
        public long MinValue
        {
            get { return _minValue; }
            set { _minValue = value; }
        }

        private long _maxValue = long.MaxValue;
        /// <summary>
        /// 最大值
        /// </summary>
        public long MaxValue
        {
            get { return _maxValue; }
            set { _maxValue = value; }
        }

    }
}
