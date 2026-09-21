using System;
using System.Collections;

namespace Rampage.Extensions
{
    /// <summary>
    /// 非泛型 IEnumerable 扩展方法 / Extensions for the non-generic IEnumerable
    /// </summary>
    public static class IEnumerableExtensions
    {

        /// <summary>
        /// 通过枚举器遍历统计序列元素数量 / Counts the elements of a sequence by walking its enumerator
        /// </summary>
        /// <param name="list">待统计的序列 / Sequence to count</param>
        /// <returns>元素数量 / The number of elements</returns>
        public static int Count(this IEnumerable list)
        {
            IEnumerator enumerator = list.GetEnumerator();
            int c = 0;
            while (enumerator.MoveNext())
            {
                c++;
            }
            enumerator.Reset();
            return c;
        }

        /// <summary>
        /// 判断序列是否包含至少一个元素 / Determines whether the sequence contains at least one element
        /// </summary>
        /// <param name="list">待检查的序列 / Sequence to check</param>
        /// <returns>包含元素时返回 true / true when the sequence has any element</returns>
        public static bool Any(this IEnumerable list)
        {
            bool result = false;
            IEnumerator enumerator = list.GetEnumerator();
            if (enumerator.MoveNext())
            {
                result = true;
            }
            enumerator.Reset();
            return result;
        }


    }
}
