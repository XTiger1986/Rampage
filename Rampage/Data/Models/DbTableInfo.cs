using System;

namespace Rampage.Data
{
    /// <summary>
    /// 数据表结构信息
    /// Data table info
    /// </summary>
    public class DbTableInfo
    {
        /// <summary>
        /// 数据表名
        /// Data table name
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// 表注释
        /// Table comment
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// 表字段信息列表
        /// Table column info list
        /// </summary>
        public List<DbColumnInfo>? Columns { get; set; }
    }
}
