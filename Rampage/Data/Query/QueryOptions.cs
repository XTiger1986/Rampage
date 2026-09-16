using System;

namespace Rampage.Data.Query
{
    /// <summary>
    /// SQL查询设置
    /// </summary>
    public class QueryOptions
    {

        /// <summary>
        /// 
        /// </summary>
        public string? Select
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? TableName
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? Condition
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? GroupBy
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long? Limit
        {
            get;
            set;
        }

        /// <summary>
        /// 数据查询偏移行数(从0开始)
        /// </summary>
        public long? Offset
        {
            get;
            set;
        }

    }

    /// <summary>
    /// SQL查询设置
    /// </summary>
    public class QueryOptions<M> where M : class
    {

        /// <summary>
        /// 
        /// </summary>
        public string? Select
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? Condition
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? GroupBy
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long? Limit
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long? Offset
        {
            get;
            set;
        }

    }
}
