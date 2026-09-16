using System;

namespace Rampage.CodeGenerator.Model
{
    /// <summary>
    /// 数据库连接设置
    /// </summary>
    public class ConnectionSetting
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// DatabaseClient程序集
        /// </summary>
        public string? DatabaseLibrary { get; set; }

        /// <summary>
        /// DatabaseClient类名
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// 数据库连接串
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// 要生成代码的表名(逗号分隔, *号或空则生成所有)
        /// </summary>
        public string? GenerateTables { get; set; }
    }
}
