using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 数据库工厂配置 / Database factory configuration
    /// </summary>
    /// <remarks>
    /// 由 <see cref="DatabaseFactory.ReloadConfig"/> 从配置节 DatabaseFactory 加载；
    /// 其中 Clients/ClientTypes/DataAccessTypes 来自配置文件，而 ImplClientTypes/MappingModelTypes
    /// 由启动流程 InitializeDatabaseClients 根据数据库实际结构生成（热重载时保留）。
    /// Loaded by <see cref="DatabaseFactory.ReloadConfig"/> from the DatabaseFactory configuration section;
    /// Clients/ClientTypes/DataAccessTypes come from the config file, while ImplClientTypes/MappingModelTypes
    /// are produced at startup by InitializeDatabaseClients from the live database schema (carried over across hot reloads).
    /// </remarks>
    public class DatabaseFactoryConfig
    {

        /// <summary>
        /// 自定义连接字符串解码器；未设置时连接字符串按明文处理
        /// Custom database connection string decoder; if not specified, connection strings are treated as plaintext
        /// </summary>
        public static IConnectionStringDecoder? ConnectionStringDecoder
        {
            get;
            set;
        }

        /// <summary>
        /// 客户端配置表（键为 <see cref="DatabaseClientConfig.Name"/>，来自配置节 DatabaseFactory:Clients）
        /// Client configurations keyed by <see cref="DatabaseClientConfig.Name"/> (from the DatabaseFactory:Clients section)
        /// </summary>
        public Dictionary<string, DatabaseClientConfig>? Clients
        {
            get;
            internal set;
        }

        /// <summary>
        /// 各客户端的基类客户端类型（键为 DatabaseClientConfig.Name，由 TypeName/Type 解析得到）
        /// Base client types keyed by DatabaseClientConfig.Name (resolved from TypeName/Type)
        /// </summary>
        public Dictionary<string, Type>? ClientTypes
        {
            get; 
            internal set;
        }

        /// <summary>
        /// 由基类客户端类型在启动期动态生成的实现客户端类型（键为 DatabaseClientConfig.Name），
        /// 供 <see cref="DatabaseFactory.CreateClient"/> 实例化
        /// implemental Client Types dynamically generated from Base Client Types at startup
        /// (key is DatabaseClientConfig.Name), instantiated by <see cref="DatabaseFactory.CreateClient"/>
        /// </summary>
        public Dictionary<string, Type>? ImplClientTypes
        {
            get;
            internal set;
        }

        /// <summary>
        /// 各客户端的自定义数据访问器类型（键为 DatabaseClientConfig.Name；未配置的客户端不在表中，
        /// 此时 <see cref="DatabaseFactory.GetDataAccessType"/> 返回 null 并使用客户端默认访问器）
        /// Custom data accessor types keyed by DatabaseClientConfig.Name; clients not present here fall back to
        /// their default accessor (<see cref="DatabaseFactory.GetDataAccessType"/> returns null)
        /// </summary>
        public Dictionary<string, Type>? DataAccessTypes
        {
            get;
            internal set;
        }

        /// <summary>
        /// 各客户端实际可用的映射模型类型表（外层键为客户端名，内层键为表名，值为模型 Type）；
        /// 仅当模型映射的全部列都能在数据库表列中找到（忽略大小写）时才收录，用于多库环境下的模型路由
        /// Mapping model types per client (outer key = client name, inner key = table name, value = model Type);
        /// a model is included only when all its mapped columns exist in the table (case-insensitive), used for model routing in multi-db deployments
        /// </summary>
        public Dictionary<string, Dictionary<string, Type>>? MappingModelTypes
        {
            get;
            internal set;
        }

        /// <summary>
        /// 多库部署下，模型未能映射到任何客户端时的处理策略（配置节 DatabaseFactory:UnmappedModelFallback）：
        /// 空/"Throw"：抛出异常（默认）；"FirstClient"：回退到第一个客户端；其它值：视为客户端名称。
        /// Fallback policy when a model cannot be mapped to any client in a multi-db deployment
        /// (config key DatabaseFactory:UnmappedModelFallback):
        /// null/"Throw" = throw (default); "FirstClient" = use the first client; anything else = treat as a client name.
        /// </summary>
        public string? UnmappedModelFallback
        {
            get;
            internal set;
        }

        /// <summary>
        /// 全局默认：是否在整个 Scope 内独占一条连接（配置节 DatabaseFactory:KeepScopeConnection，默认 false）
        /// Global default for holding one connection per scope (config key DatabaseFactory:KeepScopeConnection, default false)
        /// </summary>
        public bool? KeepScopeConnection
        {
            get;
            internal set;
        }

    }
}
