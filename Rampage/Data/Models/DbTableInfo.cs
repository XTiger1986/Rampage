using System;

namespace Rampage.Data
{
    /// <summary>
    /// data table info
    /// </summary>
    public class DbTableInfo
    {
        /// <summary>
        /// data table name
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// table comment
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// table column info list
        /// </summary>
        public List<DbColumnInfo>? Columns { get; set; }
    }
}
