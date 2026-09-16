using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 
        /// </summary>
        public bool Success
        {
            get;
            internal set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasWarnings
        {
            get;
            internal set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, ValidationResultItem>? Details
        {
            get;
            internal set;
        }

    }
}
