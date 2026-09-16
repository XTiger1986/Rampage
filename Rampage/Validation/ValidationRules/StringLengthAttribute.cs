using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 
    /// </summary>
    public class StringLengthAttribute : ValidationAttributeBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 最小长度
        /// </summary>
        public int Min
        {
            get { return _min; }
            set { _min = value; }
        }

        private int _max = int.MaxValue;
        /// <summary>
        /// 最大长度
        /// </summary>
        public int Max
        {
            get { return _max; }
            set { _max = value; }
        }

    }
}
