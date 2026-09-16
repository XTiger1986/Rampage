using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 数据库配置
    /// Database Client Configuration
    /// </summary>
    public class DatabaseClientConfig
    {
        /// <summary>
        /// 使用者定义的数据库名称
        /// Custom Database Name
        /// </summary>
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// 数据库类型：对应枚举Rampage.Data.Client.DatabaseType
        /// Database Type: Rampage.Data.Client.DatabaseType
        /// </summary>
        public string? Type
        {
            get;
            set;
        }

        /// <summary>
        /// 数据库客户端实现类的完整类型名称
        /// Database Client Implementation Full Type Name
        /// </summary>
        public string? TypeName
        {
            get;
            set;
        }

        /// <summary>
        /// 数据库访问器实现类的完整类型名称 (如果不指定，则使用类库自带)
        /// Customize the complete name of the database access class. If not specified, use the default database access of the database client
        /// </summary>
        public string? DataAccessTypeName
        {
            get;
            set;
        }

        /// <summary>
        /// 数据库连接字符串
        /// Database Connection String
        /// </summary>
        public string? ConnectionString
        {
            get;
            set;
        }

        /// <summary>
        /// 是否在整个 Scope 内独占一条连接（null 时使用 DatabaseFactory:KeepScopeConnection 全局设置，Sqlite 默认 true，其它默认 false）
        /// Hold one connection for the whole scope (null = use DatabaseFactory:KeepScopeConnection; Sqlite defaults to true, others to false)
        /// </summary>
        public bool? KeepScopeConnection
        {
            get;
            set;
        }

    }
}
