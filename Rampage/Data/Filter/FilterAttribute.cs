using System;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器类标识特性：声明筛选器默认关联的数据表名
    /// Filter class attribute: declares the default table name associated with the filter
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FilterAttribute : Attribute
    {

        private string? _tableName;
        /// <summary>
        /// 关联的数据表名；未指定时默认取筛选器类名并去除末尾的 "Filter" 后缀
        /// Associated table name; defaults to the filter class name with a trailing "Filter" suffix stripped
        /// </summary>
        public string? TableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }

    }
}
