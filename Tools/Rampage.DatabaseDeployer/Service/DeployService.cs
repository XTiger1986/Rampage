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
using Rampage.Log;
using System;
using System.Reflection;
using Rampage.Hierarchy;

namespace Rampage.DatabaseDeployer.Service
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDeployService : IBaseService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        DeploySetting? GetDeploySetting(string directoryName);

        /// <summary>
        /// 获取当前数据库版本号
        /// </summary>
        /// <returns></returns>
        long GetVersion(string directoryName);

        /// <summary>
        /// 检查是否有版本信息表(没有则自动创建)
        /// </summary>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        bool CheckOrCreateVersionTable(string directoryName);

        /// <summary>
        /// 执行指定的部署脚本
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        int ExecuteVersionScript(SqlScript script);
    }

    /// <summary>
    /// 
    /// </summary>
    public class DeployService : BaseService, IDeployService
    {

        private readonly ScriptSetting _scriptSetting;

        /// <summary>
        /// 
        /// </summary>
        public DeployService() : base()
        {
            _scriptSetting = GlobalConfiguration.GetSectionConfig<ScriptSetting>("ScriptSetting")!;
            DeploySettingContainer = new Dictionary<string, DeploySetting>();
            DatabaseClientContainer = new Dictionary<string, DatabaseClient>();
        }

        #region Privates

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, DeploySetting> DeploySettingContainer
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, DatabaseClient> DatabaseClientContainer
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="directoryName"></param>
        /// <returns></returns>
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
        /// 获取当前数据库版本号
        /// </summary>
        /// <returns></returns>
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
        /// 检查是否有版本信息表(没有则自动创建)
        /// </summary>
        /// <param name="directoryName"></param>
        /// <returns></returns>
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
        /// 执行指定的部署脚本
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
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
                effects = -500;
            }

            return effects;
        }

        /// <summary>
        /// 
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
