using System;

namespace Rampage.Data
{
    /// <summary>
    /// 数据表字段结构信息
    /// Data table column info
    /// </summary>
    public class DbColumnInfo
    {
        /// <summary>
        /// 字段名
        /// Column name
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// 所属数据表名
        /// Owning data table name
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// 数据库中的字段数据类型
        /// Column data type in the database
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// 是否为自增列
        /// Whether it is an auto-increment column
        /// </summary>
        public bool AutoIncrement { get; set; }

        /// <summary>
        /// 是否为主键列
        /// Whether it is a primary key column
        /// </summary>
        public bool PrimaryKey { get; set; }

        /// <summary>
        /// 是否允许为空
        /// Whether the column is nullable
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 是否为无符号数（数值列时有意义）
        /// Whether it is an unsigned number (for numeric columns)
        /// </summary>
        public bool IsUnsigned { get; set; }

        /// <summary>
        /// 字符长度（字符串列时有意义）
        /// Character length (for string columns)
        /// </summary>
        public long CharacterLength { get; set; }

        /// <summary>
        /// 数值精度（数值列时有意义）
        /// Numeric precision (for numeric columns)
        /// </summary>
        public int NumericPrecision { get; set; }

        /// <summary>
        /// 数值小数位数（数值列时有意义）
        /// Numeric scale (for numeric columns)
        /// </summary>
        public int NumericScale { get; set; }

        /// <summary>
        /// 字段注释
        /// Column comment
        /// </summary>
        public string? Comment { get; set; }

    }
}
