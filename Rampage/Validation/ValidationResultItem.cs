using Rampage.Enums;
using System;

namespace Rampage.Validation
{

    /// <summary>
    /// 
    /// </summary>
    public class ValidationResultItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        public ValidationResultCode Code
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? Description
        {
            get;
            set;
        }


    }
}
