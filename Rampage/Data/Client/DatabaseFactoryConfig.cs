using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 
    /// </summary>
    public class DatabaseFactoryConfig
    {

        /// <summary>
        /// Custom database connection string decoder. if not specified, use plaintext
        /// </summary>
        public static IConnectionStringDecoder? ConnectionStringDecoder
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, DatabaseClientConfig>? Clients
        {
            get;
            internal set;
        }

        /// <summary>
        /// Base Client Types (Key is DatabaseClientConfig.Name)
        /// </summary>
        public Dictionary<string, Type>? ClientTypes
        {
            get; 
            internal set;
        }

        /// <summary>
        /// implemental Client Types From Base Client Types (Key is DatabaseClientConfig.Name)
        /// </summary>
        public Dictionary<string, Type>? ImplClientTypes
        {
            get;
            internal set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, Type>? DataAccessTypes
        {
            get;
            internal set;
        }

        /// <summary>
        /// 
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
