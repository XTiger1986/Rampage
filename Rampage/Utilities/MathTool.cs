using System;
using System.Security.Cryptography;

namespace Rampage.Utilities
{
    /// <summary>
    /// 数学工具类（数字列表构建、加密安全随机数） /
    /// Math utility class (number list building, cryptographically secure random numbers)
    /// </summary>
    public class MathTool
    {

        /// <summary>
        /// 构建步进整数列表（升序含 start 不含 end，降序含 start 不含 end） /
        /// Builds a stepped integer list (contains start, excludes end, in either direction)
        /// </summary>
        /// <param name="start">起始数（包含） / The start number (inclusive)</param>
        /// <param name="end">结束数（不包含） / The end number (exclusive)</param>
        /// <param name="step">步长，小于等于 0 时按 1 处理 / The step; values at or below 0 are treated as 1</param>
        /// <returns>步进整数列表；start 与 end 相等时为空列表 / The stepped list; empty when start equals end</returns>
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
                for (int i = start; i > end; i -= step)
                {
                    list.Add(i);
                }
            }
            else
            {
                for (int i = start; i < end; i += step)
                {
                    list.Add(i);
                }
            }
            return list;
        }

        /// <summary>
        /// 生成 [min, max) 区间内的加密安全随机整数 /
        /// Generate a cryptographically secure random integer in [min, max)
        /// </summary>
        /// <remarks>
        /// min 与 max 相等时直接返回该值（避免空区间）；大小顺序颠倒时自动交换后再生效。
        /// When min equals max the value is returned directly (avoiding an empty range); reversed bounds are swapped automatically.
        /// </remarks>
        /// <param name="min">下界（包含） / The inclusive lower bound of the random range</param>
        /// <param name="max">上界（不包含） / The exclusive upper bound of the random range</param>
        /// <returns>区间内的随机整数 / A random integer within the range</returns>
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
        /// 按百分比概率随机判定是否成功 /
        /// Randomly determine success by a percentage probability
        /// </summary>
        /// <remarks>
        /// successRate 以百分比计（0~100）：小于等于 0 恒为 false，大于等于 100 恒为 true；
        /// 其余情况用加密随机 64 位值均匀映射到 [0,100) 后与 successRate 比较。
        /// successRate is a percentage (0~100): values at or below 0 always return false and values at or
        /// above 100 always return true; otherwise a cryptographically random 64-bit value is mapped
        /// uniformly onto [0,100) and compared against successRate.
        /// </remarks>
        /// <param name="successRate">成功概率百分比（0~100） / The success rate percentage (0~100)</param>
        /// <returns>是否命中成功 / true when the trial succeeds</returns>
        public static bool RandomSuccess(double successRate)
        {
            if (successRate <= 0) return false;
            if (successRate >= 100) return true;

            byte[] buf = new byte[8];
            RandomNumberGenerator.Fill(buf);
            ulong randRaw = BitConverter.ToUInt64(buf);
            // 均匀映射 [0,2^64) → [0,100) / map [0,2^64) uniformly onto [0,100)
            double randVal = randRaw / (ulong.MaxValue + 1.0) * 100;

            return randVal < successRate;
        }

    }
}
