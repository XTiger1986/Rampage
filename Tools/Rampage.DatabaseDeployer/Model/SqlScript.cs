using System;

namespace Rampage.DatabaseDeployer.Model
{

    /// <summary>
    /// SQL脚本（一个待执行的部署脚本文件）/ SQL script (one deployable script file)
    /// </summary>
    public class SqlScript
    {
        /// <summary>
        /// 目录名（始终为脚本根目录下的一级目录名，即 deploy.json 所在目录，即使脚本位于其子目录中）/
        /// Directory name (always the first-level directory under the script root where deploy.json lives, even for scripts in subdirectories)
        /// </summary>
        public string? DirectoryName
        {
            get;
            set;
        }

        /// <summary>
        /// 脚本文件名（含扩展名）/ Script file name (with extension)
        /// </summary>
        public string? FileName
        {
            get;
            set;
        }

        /// <summary>
        /// 从文件名解析出的版本号 / Version parsed from the file name
        /// </summary>
        public long Version
        {
            get;
            set;
        }

        /// <summary>
        /// 脚本文件内容（SQL 文本）/ Script file content (SQL text)
        /// </summary>
        public string? Sql
        {
            get;
            set;
        }


    }
}
