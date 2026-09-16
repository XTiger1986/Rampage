using System;

namespace Rampage.DatabaseDeployer.Model
{

    /// <summary>
    /// SQL脚本
    /// </summary>
    public class SqlScript
    {
        /// <summary>
        /// 目录名
        /// </summary>
        public string? DirectoryName
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? FileName
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long Version
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string? Sql
        {
            get;
            set;
        }


    }
}
