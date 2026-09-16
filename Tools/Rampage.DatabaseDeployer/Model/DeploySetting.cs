using System;

namespace Rampage.DatabaseDeployer.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class DeploySetting
    {
        /// <summary>
        /// DatabaseClient程序集
        /// </summary>
        public string? DatabaseLibrary { get; set; }

        /// <summary>
        /// DatabaseClient类名
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// 自定义SQL脚本目录(若未定义则在配置目录寻找)
        /// </summary>
        public string? ScriptDirectoryPath { get; set; }

        /// <summary>
        /// 数据库连接串
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// 版本信息表名
        /// </summary>
        public string? VersionTableName { get; set; }

        /// <summary>
        /// 获取版本信息SQL
        /// </summary>
        public string? GetVersionSql { get; set; }

        /// <summary>
        /// 更新版本信息SQL
        /// </summary>
        public string? UpdateVersionSql { get; set; }

        /// <summary>
        /// 检测版本表SQL
        /// </summary>
        public string? CountVersionTableSql { get; set; }

        /// <summary>
        /// 创建版本表SQL
        /// </summary>
        public string? CreateVersionTableSql { get; set; }

    }
}
