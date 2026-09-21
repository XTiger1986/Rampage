using System;

namespace Rampage.CodeGenerator.Model
{
    /// <summary>
    /// 数据库连接设置（对应配置节数组 ConnectionSetting 中的一项）/
    /// Database connection setting (one entry of the ConnectionSetting configuration array)
    /// </summary>
    public class ConnectionSetting
    {
        /// <summary>
        /// 名称（用于在多个连接配置间选择）/ Name (used to choose between configured connections)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// DatabaseClient程序集 / DatabaseClient assembly name
        /// </summary>
        public string? DatabaseLibrary { get; set; }

        /// <summary>
        /// DatabaseClient类名 / DatabaseClient class name
        /// </summary>
        /// <remarks>
        /// 内置三种程序集（MySql/PostgreSQL/Oracle）按固定类直接创建，此项不参与选择；
        /// 仅其它程序集分支通过反射使用它，此时需为带命名空间的完整类型名。
        /// The three built-in libraries are instantiated as fixed classes, so this value is ignored there;
        /// it is only used via reflection for other assemblies and must then be a namespace-qualified type name.
        /// </remarks>
        public string? Provider { get; set; }

        /// <summary>
        /// 数据库连接串 / Database connection string
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// 要生成代码的表名(逗号分隔, *号或空则生成所有) / Table names to generate (comma-separated; "*" or empty means all tables)
        /// </summary>
        public string? GenerateTables { get; set; }
    }
}
