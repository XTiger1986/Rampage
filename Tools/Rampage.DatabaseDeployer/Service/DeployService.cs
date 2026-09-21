using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Npgsql;
using Rampage.Dapper;
using Rampage.Data.Client;
using Rampage.DatabaseDeployer.Model;
using Rampage.DatabaseLibrary.MySql;
using Rampage.DatabaseLibrary.PostgreSQL;
using Oracle.ManagedDataAccess.Client;
using Rampage.DatabaseLibrary.Oracle;
using Rampage.DatabaseLibrary.Sqlite;
using System;
using System.Reflection;
using Rampage.Hierarchy;
using Rampage.AppLog;

namespace Rampage.DatabaseDeployer.Service
{
    /// <summary>
    /// 部署服务接口：提供部署配置读取、数据库版本查询、版本表维护与版本脚本事务执行 /
    /// Deploy service interface: deploy-setting loading, database version query, version table maintenance and transactional script execution
    /// </summary>
    public interface IDeployService : IBaseService
    {
        /// <summary>
        /// 获取指定脚本目录的部署配置（deploy.json），结果按目录名缓存 /
        /// Loads the deploy setting (deploy.json) of the given script directory, cached by directory name
        /// </summary>
        /// <param name="directoryName">脚本目录名（脚本根目录下的一级目录）/ Script directory name (a first-level directory under the script root)</param>
        /// <returns>部署配置；目录不存在或缺少 deploy.json 时返回 null / The deploy setting; null if the directory or deploy.json does not exist</returns>
        DeploySetting? GetDeploySetting(string directoryName);

        /// <summary>
        /// 获取当前数据库版本号 / Gets the current database version number
        /// </summary>
        /// <param name="directoryName">脚本目录名 / Script directory name</param>
        /// <returns>版本信息表中记录的当前版本号 / The current version recorded in the version table</returns>
        long GetVersion(string directoryName);

        /// <summary>
        /// 检查是否有版本信息表(没有则自动创建) / Checks whether the version table exists, creating it automatically when missing
        /// </summary>
        /// <param name="directoryName">脚本目录名 / Script directory name</param>
        /// <returns>表已存在或创建成功返回 true；发生异常（已记录日志）返回 false / True if the table exists or was created; false when an exception occurred (already logged)</returns>
        bool CheckOrCreateVersionTable(string directoryName);

        /// <summary>
        /// 执行指定的部署脚本 / Executes the given deploy script in a transaction
        /// </summary>
        /// <param name="script">要执行的脚本（含目录名、版本号与 SQL 内容）/ The script to execute (directory name, version and SQL included)</param>
        /// <returns>脚本 SQL 与版本更新 SQL 受影响行数之和；失败回滚后返回 -500 / Sum of affected rows of the script and the version update; -500 after rollback on failure</returns>
        int ExecuteVersionScript(SqlScript script);
    }

    /// <summary>
    /// 部署服务实现：按 DatabaseLibrary/Provider 创建并缓存 DatabaseClient（内置 MySql/PostgreSQL/Oracle/Sqlite，
    /// 其余程序集走反射加载），并在事务中执行版本脚本 /
    /// Deploy service implementation: creates and caches DatabaseClients by DatabaseLibrary/Provider
    /// (built-in MySql/PostgreSQL/Oracle/Sqlite, other assemblies loaded via reflection) and executes versioned scripts in transactions
    /// </summary>
    public class DeployService : BaseService, IDeployService
    {

        private readonly ScriptSetting _scriptSetting;

        /// <summary>
        /// 初始化服务：读取 ScriptSetting 配置节，并创建部署配置与 DatabaseClient 缓存容器 /
        /// Initializes the service: loads the ScriptSetting section and creates the setting and client caches
        /// </summary>
        public DeployService() : base()
        {
            _scriptSetting = GlobalConfiguration.GetSectionConfig<ScriptSetting>("ScriptSetting")!;
            DeploySettingContainer = new Dictionary<string, DeploySetting>();
            DatabaseClientContainer = new Dictionary<string, DatabaseClient>();
        }

        #region Privates

        /// <summary>
        /// 部署配置缓存（键：脚本目录名）/ Deploy setting cache (keyed by script directory name)
        /// </summary>
        private Dictionary<string, DeploySetting> DeploySettingContainer
        {
            get;
            set;
        }

        /// <summary>
        /// DatabaseClient 缓存（键：DatabaseLibrary.Provider 组合键）/ DatabaseClient cache (keyed by the "DatabaseLibrary.Provider" composite key)
        /// </summary>
        private Dictionary<string, DatabaseClient> DatabaseClientContainer
        {
            get;
            set;
        }

        /// <summary>
        /// 获取（或创建并缓存）部署配置对应的 DatabaseClient /
        /// Gets (or creates and caches) the DatabaseClient for the given deploy setting
        /// </summary>
        /// <param name="setting">部署配置 / Deploy setting</param>
        /// <returns>对应的 DatabaseClient 实例（已装配 DapperDataAccessor）/ The matching DatabaseClient (equipped with a DapperDataAccessor)</returns>
        /// <remarks>
        /// 缓存键为 "DatabaseLibrary.Provider"。内置四种程序集（MySql/PostgreSQL/Oracle/Sqlite）在 switch 中按固定类直接创建，
        /// 此时配置中的 Provider 并不参与选择；其余程序集通过 Assembly.Load 加载后以 Provider 为完整类型名反射实例化（忽略大小写）。
        /// The cache key is "DatabaseLibrary.Provider". The four built-in libraries are instantiated as fixed classes in the switch
        /// (the configured Provider is ignored there); any other assembly is loaded via Assembly.Load and instantiated by reflection,
        /// with Provider as the namespace-qualified type name (case-insensitive).
        /// </remarks>
        private DatabaseClient GetDatabaseClient(DeploySetting setting)
        {
            string key = string.Concat(setting.DatabaseLibrary, ".", setting.Provider);
            if (DatabaseClientContainer.ContainsKey(key))
            {
                return DatabaseClientContainer[key];
            }

            DatabaseClient? provider = null;
            switch (setting.DatabaseLibrary)
            {
                case "Rampage.DatabaseLibrary.MySql":
                    provider = new MySqlClient(new MySqlConnection(setting.ConnectionString));
                    break;
                case "Rampage.DatabaseLibrary.PostgreSQL":
                    provider = new PgSqlClient(new NpgsqlConnection(setting.ConnectionString));
                    break;
                case "Rampage.DatabaseLibrary.Oracle":
                    provider = new OracleClient(new OracleConnection(setting.ConnectionString));
                    break;
                case "Rampage.DatabaseLibrary.Sqlite":
                    provider = new SqliteClient(new SqliteConnection(setting.ConnectionString));
                    break;
                default:
                    Assembly lib = Assembly.Load(setting.DatabaseLibrary!);
                    provider = (DatabaseClient)lib.CreateInstance(setting.Provider!, true)!;
                    break;
            }
            provider.DataAccessor = new DapperDataAccessor();
            if (!DatabaseClientContainer.ContainsKey(key))
            {
                DatabaseClientContainer.Add(key, provider);
            }

            return provider!;
        }

        #endregion

        /// <summary>
        /// 获取指定脚本目录的部署配置（deploy.json），结果按目录名缓存 /
        /// Loads the deploy setting (deploy.json) of the given script directory, cached by directory name
        /// </summary>
        /// <param name="directoryName">脚本目录名（脚本根目录下的一级目录）/ Script directory name (a first-level directory under the script root)</param>
        /// <returns>部署配置；目录不存在或缺少 deploy.json 时返回 null / The deploy setting; null if the directory or deploy.json does not exist</returns>
        public DeploySetting? GetDeploySetting(string directoryName)
        {
            string deploySettingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _scriptSetting.Root!, directoryName, "deploy.json");
            if (DeploySettingContainer.ContainsKey(directoryName))
            {
                return DeploySettingContainer[directoryName];
            }

            if (!File.Exists(deploySettingPath))
            {
                return null;
            }
            ConfigurationManager configuration = new ConfigurationManager();
            configuration.AddJsonFile(deploySettingPath);

            DeploySetting deploySetting = configuration.Get<DeploySetting>()!;
            if (!DeploySettingContainer.ContainsKey(directoryName))
            {
                DeploySettingContainer.Add(directoryName, deploySetting);
            }
            return deploySetting;
        }

        /// <summary>
        /// 获取当前数据库版本号 / Gets the current database version number
        /// </summary>
        /// <param name="directoryName">脚本目录名 / Script directory name</param>
        /// <returns>执行 GetVersionSql 得到的当前版本号 / The current version returned by executing GetVersionSql</returns>
        /// <exception cref="InvalidOperationException">目录不存在或缺少 deploy.json / The script directory is invalid or deploy.json is missing</exception>
        public long GetVersion(string directoryName)
        {
            DeploySetting? setting = GetDeploySetting(directoryName);
            if (setting == null)
            {
                throw new InvalidOperationException("Invalid Script Directory: " + directoryName);
            }

            DatabaseClient databaseClient = GetDatabaseClient(setting);
            long version = databaseClient.ExecuteScalar<long>(setting.GetVersionSql!);
            return version;
        }

        /// <summary>
        /// 检查是否有版本信息表(没有则自动创建) / Checks whether the version table exists, creating it automatically when missing
        /// </summary>
        /// <param name="directoryName">脚本目录名 / Script directory name</param>
        /// <returns>表已存在或创建成功返回 true；发生异常（已记录日志）返回 false / True if the table exists or was created; false when an exception occurred (already logged)</returns>
        /// <exception cref="InvalidOperationException">目录不存在或缺少 deploy.json / The script directory is invalid or deploy.json is missing</exception>
        public bool CheckOrCreateVersionTable(string directoryName)
        {
            DeploySetting? setting = GetDeploySetting(directoryName);
            if (setting == null)
            {
                throw new InvalidOperationException("Invalid Script Directory: " + directoryName);
            }

            bool result = false;

            try
            {
                DatabaseClient databaseClient = GetDatabaseClient(setting);

                int checkCount = databaseClient.ExecuteScalar<int>(setting.CountVersionTableSql!);
                if (checkCount <= 0)
                {
                    databaseClient.Execute(setting.CreateVersionTableSql!);
                }
                result = true;
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// 执行指定的部署脚本 / Executes the given deploy script
        /// </summary>
        /// <param name="script">要执行的脚本 / The script to execute</param>
        /// <returns>成功时为脚本 SQL 与版本更新 SQL 受影响行数之和；失败回滚后返回 -500 / On success the sum of affected rows of both statements; -500 after rollback on failure</returns>
        /// <remarks>
        /// 在同一事务内先执行脚本 SQL，再以脚本版本号（string.Format 的 {0} 实参）执行 UpdateVersionSql 更新版本表；
        /// 任一步失败即回滚整个事务、记录日志并返回哨兵值 -500，调用方以此判定部署失败并中断。
        /// Runs the script SQL and then the version update (script version as the {0} format argument) inside one transaction;
        /// any failure rolls the transaction back, logs the exception and returns the -500 sentinel so the caller aborts deployment.
        /// </remarks>
        public int ExecuteVersionScript(SqlScript script)
        {
            string directoryName = script.DirectoryName!;
            DeploySetting? setting = GetDeploySetting(directoryName);
            if (setting == null)
            {
                throw new InvalidOperationException("Invalid Script Directory: " + directoryName);
            }

            int effects = 0;
            DatabaseClient databaseClient = GetDatabaseClient(setting);

            try
            {
                databaseClient.BeginTransaction();

                effects += databaseClient.Execute(script.Sql!);
                effects += databaseClient.Execute(string.Format(setting.UpdateVersionSql!, script.Version));

                databaseClient.CommitTransaction();
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                databaseClient.RollbackTransaction();
                effects = -500; // 失败哨兵值，调用方据此中断部署 / failure sentinel the caller uses to abort deployment
            }

            return effects;
        }

        /// <summary>
        /// 释放服务并清理缓存的全部 DatabaseClient / Disposes the service and all cached DatabaseClients
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            if (DatabaseClientContainer != null && DatabaseClientContainer.Count > 0)
            {
                foreach(var item in DatabaseClientContainer)
                {
                    if (item.Value != null)
                    {
                        item.Value.Dispose();
                    }
                }
                DatabaseClientContainer.Clear();
            }
        }
    }
}
