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
        MySQL,

        SqlServer,

        Sqlite,

        PostgreSQL,

        Oracle,

        Other = 0
    }


    /// <summary>
    /// 
    /// </summary>
    public class DatabaseFactory
    {

        public const string EX_KEY_DependencyPackageNotFound = "DependencyPackage_NotFound";


        private static volatile DatabaseFactoryConfig? _config;
        /// <summary>
        ///
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
        ///
        /// </summary>
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
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type GetClientType(string name)
        {
            return Config!.ClientTypes![name];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type GetImplClientType(string name)
        {
            return Config!.ImplClientTypes![name];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type? GetDataAccessType(string name)
        {
            if (Config!.DataAccessTypes!.ContainsKey(name))
            {
                return Config!.DataAccessTypes![name];
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DatabaseClient CreateClient(string name)
        {
            Type dcType = GetImplClientType(name);
            return (DatabaseClient)EmitTool.CreateModel(dcType)!;
        }


        private static IDbParameterValueHandler? _dbParameterValueHandler;
        /// <summary>
        /// 
        /// </summary>
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
