using System;

namespace Rampage.Utilities
{
    public class IdTool
    {

        /// <summary>
        /// Generate UUID in standard format 8-4-4-4-12 and remove horizontal lines (used without strict requirements for uniqueness and concurrency performance)
        /// </summary>
        /// <returns></returns>
        public static string UUID()
        {
            return Guid.NewGuid().ToString("N").Replace("-", string.Empty);
        }

    }
}
