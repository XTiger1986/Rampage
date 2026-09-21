using Microsoft.Extensions.Configuration;
using Rampage.ExceptionExtensions;
using Rampage.Reflection;
using Rampage.Utilities;
using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 本框架支持的数据库类型
    /// Database types supported by the framework
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>MySQL / MySQL</summary>
        MySQL,

        /// <summary>Microsoft SQL Server / Microsoft SQL Server</summary>
        SqlServer,

        /// <summary>SQLite / SQLite</summary>
        Sqlite,

        /// <summary>PostgreSQL / PostgreSQL</summary>
        PostgreSQL,

        /// <summary>Oracle / Oracle</summary>
        Oracle,

        /// <summary>
        /// 其它/未指定（枚举默认值 0，配置解析失败时的回退值）
        /// Other/unspecified (the default enum value 0, also the fallback when config parsing fails)
        /// </summary>
        Other = 0
    }


    /// <summary>
    /// 数据库工厂：负责多库配置加载（配置节 DatabaseFactory:Clients）与客户端/访问器类型路由
    /// Database factory: loads multi-database configuration (section DatabaseFactory:Clients) and routes client/accessor types
    /// </summary>
    /// <remarks>
    /// 配置在首次访问 <see cref="Config"/> 时懒加载，可调用 <see cref="ReloadConfig"/> 热重载。
    /// 客户端类型解析：TypeName（完整类型名）优先，否则按 Type 映射到 Rampage.DatabaseLibrary.* 内置实现；
    /// 均未配置成功时抛出依赖包缺失异常（<see cref="EX_KEY_DependencyPackageNotFound"/>）。
    /// The configuration is lazily loaded on first access to <see cref="Config"/> and can be hot-reloaded via
    /// <see cref="ReloadConfig"/>. Client type resolution: TypeName (full type name) first, otherwise Type maps to
    /// the built-in Rampage.DatabaseLibrary.* implementation; a dependency-package exception
    /// (<see cref="EX_KEY_DependencyPackageNotFound"/>) is thrown when neither resolves.
    /// </remarks>
    public class DatabaseFactory
    {

        /// <summary>
        /// 方言依赖包未找到时使用的异常资源键 / Exception resource key used when a dialect dependency package is missing
        /// </summary>
        public const string EX_KEY_DependencyPackageNotFound = "DependencyPackage_NotFound";


        private static volatile DatabaseFactoryConfig? _config;
        /// <summary>
        /// 当前生效的配置；首次访问时自动从配置节 DatabaseFactory 加载
        /// The current configuration; automatically loaded from the DatabaseFactory section on first access
        /// </summary>
        public static DatabaseFactoryConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = ReloadConfig();
                }
                return _config;
            }
        }

        /// <summary>
        /// 从配置节 DatabaseFactory 重新加载配置并发布为当前 <see cref="Config"/>
        /// Reload the configuration from the DatabaseFactory section and publish it as the current <see cref="Config"/>
        /// </summary>
        /// <remarks>
        /// 处理流程：读取全局 DataAccessTypeName 与 KeepScopeConnection；解析 Clients 列表——
        /// 连接字符串经 <see cref="DatabaseFactoryConfig.ConnectionStringDecoder"/> 解码（若已设置），
        /// 每个客户端解析 ClientType 与可选 DataAccessType（客户端级配置优先于全局）。
        /// ImplClientTypes/MappingModelTypes 不来自配置文件，由启动期 InitializeDatabaseClients 生成，此处从旧配置继承，
        /// 避免热重载导致多库路由退化为第一个客户端。配置对象完整构建后才发布，避免并发读到半初始化状态。
        /// Pipeline: reads global DataAccessTypeName and KeepScopeConnection; parses the Clients list - connection
        /// strings are decoded via <see cref="DatabaseFactoryConfig.ConnectionStringDecoder"/> when set, and each
        /// client resolves its ClientType and optional DataAccessType (per-client config wins over the global one).
        /// ImplClientTypes/MappingModelTypes do not come from the config file; they are produced at startup by
        /// InitializeDatabaseClients and inherited from the previous config here, so hot reloads do not degrade
        /// multi-db routing to the first client. The config is published only after being fully built to avoid
        /// concurrent readers seeing a half-initialized state.
        /// </remarks>
        /// <returns>重新加载后的配置对象 / The freshly loaded configuration</returns>
        public static DatabaseFactoryConfig ReloadConfig()
        {
            DatabaseFactoryConfig? previous = _config;
            DatabaseFactoryConfig config = new DatabaseFactoryConfig();
            IConfigurationSection root = GlobalConfiguration.Configuration.GetSection("DatabaseFactory");
            IConfigurationSection daType = root.GetSection("DataAccessTypeName");
            config.UnmappedModelFallback = root.GetSection("UnmappedModelFallback").Value;
            string? keepScopeConnection = root.GetSection("KeepScopeConnection").Value;
            if (!string.IsNullOrEmpty(keepScopeConnection) && bool.TryParse(keepScopeConnection, out bool keep))
            {
                config.KeepScopeConnection = keep;
            }
            string? globalDataAccessTypeName = daType.Value;
            Type? globalDataAccessType = !string.IsNullOrEmpty(globalDataAccessTypeName) ? Type.GetType(globalDataAccessTypeName) : null;

            IConfigurationSection clientsNode = root.GetSection("Clients");
            List<DatabaseClientConfig>? clientList = clientsNode.Get<List<DatabaseClientConfig>>();

            if (clientList != null && clientList.Count > 0)
            {
                config.ClientTypes = new Dictionary<string, Type>();
                config.DataAccessTypes = new Dictionary<string, Type>();

                foreach (DatabaseClientConfig clientConfig in clientList)
                {
                    if (!string.IsNullOrEmpty(clientConfig.ConnectionString) && DatabaseFactoryConfig.ConnectionStringDecoder != null)
                    {
                        clientConfig.ConnectionString = DatabaseFactoryConfig.ConnectionStringDecoder.Decode(clientConfig.ConnectionString);
                    }

                    string name = clientConfig.Name!;
                    Type? type = null;
                    if (!string.IsNullOrEmpty(clientConfig.TypeName))
                    {
                        type = Type.GetType(clientConfig.TypeName);
                    }
                    else if (!string.IsNullOrEmpty(clientConfig.Type))
                    {
                        DatabaseType databaseType = StringTool.TryParse<DatabaseType>(clientConfig.Type, DatabaseType.Other);
                        switch (databaseType)
                        {
                            case DatabaseType.MySQL:
                                type = Type.GetType("Rampage.DatabaseLibrary.MySql.MySqlClient, Rampage.DatabaseLibrary.MySql");
                                break;
                            case DatabaseType.PostgreSQL:
                                type = Type.GetType("Rampage.DatabaseLibrary.PostgreSQL.PgSqlClient, Rampage.DatabaseLibrary.PostgreSQL");
                                break;
                            case DatabaseType.Oracle:
                                type = Type.GetType("Rampage.DatabaseLibrary.Oracle.OracleClient, Rampage.DatabaseLibrary.Oracle");
                                break;
                            case DatabaseType.SqlServer:
                                type = Type.GetType("Rampage.DatabaseLibrary.SqlServer.SqlServerClient, Rampage.DatabaseLibrary.SqlServer");
                                break;
                            case DatabaseType.Sqlite:
                                type = Type.GetType("Rampage.DatabaseLibrary.Sqlite.SqliteClient, Rampage.DatabaseLibrary.Sqlite");
                                break;
                        }
                    }
                    if (type == null)
                    {
                        throw new SuperException(EX_KEY_DependencyPackageNotFound, name);
                    }

                    if (!config.ClientTypes.ContainsKey(name))
                    {
                        config.ClientTypes.Add(name, type);
                    }

                    Type? dataAccessType = null;
                    if (!string.IsNullOrEmpty(clientConfig.DataAccessTypeName) && !config.DataAccessTypes.ContainsKey(name))
                    {
                        dataAccessType = Type.GetType(clientConfig.DataAccessTypeName);
                        if (dataAccessType != null)
                        {
                            config.DataAccessTypes.Add(name, dataAccessType);
                        }
                    }
                    if (!config.DataAccessTypes.ContainsKey(name) && globalDataAccessType != null)
                    {
                        config.DataAccessTypes.Add(name, globalDataAccessType);
                    }
                }

                config.Clients = clientList.ToDictionary(c => c.Name!);
            }

            // ImplClientTypes / MappingModelTypes 由 InitializeDatabaseClients 在启动期根据数据库结构生成，
            // 与 app.json 内容无关；热重载时必须保留，否则多库路由会静默退化为 databases.First()。
            // ImplClientTypes / MappingModelTypes are produced at startup from live DB schema, not from app.json;
            // carry them over across hot reloads, otherwise multi-db routing silently degrades to databases.First().
            if (previous != null)
            {
                config.ImplClientTypes = previous.ImplClientTypes;
                config.MappingModelTypes = previous.MappingModelTypes;
            }

            // 完整构建后再发布，避免并发读到半初始化配置
            _config = config;
            return config;
        }

        /// <summary>
        /// 取得指定客户端的基类客户端类型（来自配置解析，未含动态生成部分）
        /// Get the base client type of the named client (resolved from configuration, before dynamic generation)
        /// </summary>
        /// <param name="name">客户端名（DatabaseClientConfig.Name） / The client name (DatabaseClientConfig.Name)</param>
        /// <returns>基类客户端类型；名称不存在时抛出 KeyNotFoundException / The base client type; throws KeyNotFoundException for unknown names</returns>
        public static Type GetClientType(string name)
        {
            return Config!.ClientTypes![name];
        }

        /// <summary>
        /// 取得指定客户端由启动期动态生成的实现客户端类型（InitializeDatabaseClients 之后才可用）
        /// Get the dynamically generated implementation client type of the named client (available only after InitializeDatabaseClients)
        /// </summary>
        /// <param name="name">客户端名（DatabaseClientConfig.Name） / The client name (DatabaseClientConfig.Name)</param>
        /// <returns>实现客户端类型；名称不存在时抛出 KeyNotFoundException / The implementation client type; throws KeyNotFoundException for unknown names</returns>
        public static Type GetImplClientType(string name)
        {
            return Config!.ImplClientTypes![name];
        }

        /// <summary>
        /// 取得指定客户端的自定义数据访问器类型（配置 DataAccessTypeName，可用于把默认实现替换为 Dapper 等）
        /// Get the custom data accessor type configured for the named client (DataAccessTypeName, e.g. to swap in Dapper)
        /// </summary>
        /// <param name="name">客户端名（DatabaseClientConfig.Name） / The client name (DatabaseClientConfig.Name)</param>
        /// <returns>已配置的访问器类型；未配置时返回 null，表示使用客户端默认访问器
        /// The configured accessor type; null when not configured, meaning the client's default accessor</returns>
        public static Type? GetDataAccessType(string name)
        {
            if (Config!.DataAccessTypes!.ContainsKey(name))
            {
                return Config!.DataAccessTypes![name];
            }
            return null;
        }

        /// <summary>
        /// 按客户端名创建实现客户端实例（动态生成类型，经无参构造快速实例化）
        /// Create an instance of the named client's dynamically generated implementation type (fast instantiation via the parameterless constructor)
        /// </summary>
        /// <param name="name">客户端名（DatabaseClientConfig.Name） / The client name (DatabaseClientConfig.Name)</param>
        /// <returns>新创建的 <see cref="DatabaseClient"/> 实例 / A newly created <see cref="DatabaseClient"/> instance</returns>
        public static DatabaseClient CreateClient(string name)
        {
            Type dcType = GetImplClientType(name);
            return (DatabaseClient)EmitTool.CreateModel(dcType)!;
        }


        private static IDbParameterValueHandler? _dbParameterValueHandler;
        /// <summary>
        /// 全局数据库参数值处理器；未显式设置时懒加载为 <see cref="DefaultDbParameterValueHandler"/>
        /// Global database parameter value handler; lazily defaults to <see cref="DefaultDbParameterValueHandler"/>
        /// </summary>
        /// <remarks>
        /// 可整体替换以自定义参数值转换。注意 <see cref="Rampage.Data.SqlTemplate"/> 在构造时会快照当时的处理器，
        /// 替换需在创建 SqlTemplate/客户端之前完成才能对后续生成的 SQL 生效。
        /// Replace it to customize parameter value conversion. Note that <see cref="Rampage.Data.SqlTemplate"/>
        /// snapshots the handler at construction, so the swap must happen before any SqlTemplate/client is created
        /// to affect subsequently generated SQL.
        /// </remarks>
        public static IDbParameterValueHandler DbParameterValueHandler
        {
            get
            {
                if (_dbParameterValueHandler == null)
                {
                    _dbParameterValueHandler = new DefaultDbParameterValueHandler();
                }
                return _dbParameterValueHandler;
            }
            set
            {
                _dbParameterValueHandler = value;
            }
        }


    }
}
