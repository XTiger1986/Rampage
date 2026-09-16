using System;

namespace Rampage.Data
{
    /// <summary>
    /// data table column info
    /// </summary>
    public class DbColumnInfo
    {
        /// <summary>
        /// column name
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// data table name
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// column data type in database
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// is a AutoIncrement column
        /// </summary>
        public bool AutoIncrement { get; set; }

        /// <summary>
        /// is a primary key column
        /// </summary>
        public bool PrimaryKey { get; set; }

        /// <summary>
        /// is a nullable column
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// is a Unsigned number (if it is a numeric column)
        /// </summary>
        public bool IsUnsigned { get; set; }

        /// <summary>
        /// column character length (if it is a string column)
        /// </summary>
        public long CharacterLength { get; set; }

        /// <summary>
        /// numeric precision (if it is a numeric column)
        /// </summary>
        public int NumericPrecision { get; set; }

        /// <summary>
        /// numeric scale (if it is a numeric column)
        /// </summary>
        public int NumericScale { get; set; }

        /// <summary>
        /// column comment
        /// </summary>
        public string? Comment { get; set; }

    }
}
