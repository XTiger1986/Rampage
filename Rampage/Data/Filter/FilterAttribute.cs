using System;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FilterAttribute : Attribute
    {

        private string? _tableName;
        /// <summary>
        /// 
        /// </summary>
        public string? TableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }

    }
}
