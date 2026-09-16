using System;
using System.Security.Cryptography;

namespace Rampage.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class MathTool
    {

        /// <summary>
        /// build integer list from start to end
        /// </summary>
        /// <param name="start">start number (inclusive)</param>
        /// <param name="end">end number (exclusive)</param>
        /// <param name="step">min/default is 1</param>
        /// <returns></returns>
        public static List<int> BuildNumberList(int start, int end, int step = 1)
        {
            if (start == end)
            {
                return new List<int>();
            }
            if (step <= 0)
            {
                step = 1;
            }
            int size = Math.Abs(end - start);
            List<int> list = new List<int>(size);
            if (end < start)
            {
                for (int i = start; i > end; i-= step)
                {
                    list.Add(i);
                }
            }
            else
            {
                for (int i = start; i < end; i++)
                {
                    list.Add(i);
                }
            }
            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="min">The inclusive lower bound of the random range.</param>
        /// <param name="max">The exclusive upper bound of the random range.</param>
        /// <returns></returns>
        public static int GenerateInt(int min, int max)
        {
            if (min == max)
            {
                return max;
            }
            int from, to;
            if (min > max)
            {
                from = max;
                to = min;
            }
            else
            {
                from = min;
                to = max;
            }
            return RandomNumberGenerator.GetInt32(from, to);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="successRate"></param>
        /// <returns></returns>
        public static bool RandomSuccess(double successRate)
        {
            if (successRate <= 0) return false;
            if (successRate >= 100) return true;

            byte[] buf = new byte[8];
            RandomNumberGenerator.Fill(buf);
            ulong randRaw = BitConverter.ToUInt64(buf);
            double randVal = randRaw / (ulong.MaxValue + 1.0) * 100;

            return randVal < successRate;
        }

    }
}
