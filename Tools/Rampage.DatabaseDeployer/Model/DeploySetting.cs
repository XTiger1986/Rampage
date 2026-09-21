using System;

namespace Rampage.DatabaseDeployer.Model
{
    /// <summary>
    /// 部署配置（对应脚本目录下的 deploy.json）/ Deploy setting (mapped from deploy.json in a script directory)
    /// </summary>
    public class DeploySetting
    {
        /// <summary>
        /// DatabaseClient程序集 / DatabaseClient assembly name
        /// </summary>
        public string? DatabaseLibrary { get; set; }

        /// <summary>
        /// DatabaseClient类名 / DatabaseClient class name
        /// </summary>
        /// <remarks>
        /// 内置四种程序集（MySql/PostgreSQL/Oracle/Sqlite）按固定类直接创建，此项不参与选择；
        /// 仅第三方程序集分支通过反射使用它，此时需为带命名空间的完整类型名。
        /// The four built-in libraries are instantiated as fixed classes, so this value is ignored there;
        /// it is only used via reflection for third-party assemblies and must then be a namespace-qualified type name.
        /// </remarks>
        public string? Provider { get; set; }

        /// <summary>
        /// 自定义SQL脚本目录(若未定义则在配置目录寻找) / Custom script directory (falls back to the configured directory when unset)
        /// </summary>
        public string? ScriptDirectoryPath { get; set; }

        /// <summary>
        /// 数据库连接串 / Database connection string
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// 版本信息表名 / Version table name
        /// </summary>
        public string? VersionTableName { get; set; }

        /// <summary>
        /// 获取版本信息SQL / SQL that reads the current version
        /// </summary>
        public string? GetVersionSql { get; set; }

        /// <summary>
        /// 更新版本信息SQL（以 {0} 占位符接收新版本号）/ SQL that updates the version (receives the new version as the {0} format argument)
        /// </summary>
        public string? UpdateVersionSql { get; set; }

        /// <summary>
        /// 检测版本表SQL（返回记录数，&lt;=0 视为表不存在）/ SQL that counts the version table (a result &lt;= 0 means the table is missing)
        /// </summary>
        public string? CountVersionTableSql { get; set; }

        /// <summary>
        /// 创建版本表SQL / SQL that creates the version table
        /// </summary>
        public string? CreateVersionTableSql { get; set; }

    }
}
