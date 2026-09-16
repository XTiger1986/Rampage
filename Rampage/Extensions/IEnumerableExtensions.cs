using System;
using System.Collections;

namespace Rampage.Extensions
{
    /// <summary>
    /// IEnumerable Extensions
    /// </summary>
    public static class IEnumerableExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
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
