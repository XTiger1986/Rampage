using MySqlConnector;
using Npgsql;
using Rampage.CodeGenerator.Model;
using Rampage.Dapper;
using Rampage.Data;
using Rampage.Data.Client;
using Rampage.DatabaseLibrary.MySql;
using Rampage.DatabaseLibrary.PostgreSQL;
using Oracle.ManagedDataAccess.Client;
using Rampage.DatabaseLibrary.Oracle;
using Rampage.Utilities;
using System;
using System.Reflection;

namespace Rampage.CodeGenerator.Service
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGenerateService : IDisposable
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingName"></param>
        /// <returns></returns>
        List<DbTableInfo> GetTableInfoList(string settingName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        string ParseLanguageType(string settingName, DbColumnInfo columnInfo);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        string ParseDbParameterType(string settingName, DbColumnInfo columnInfo);

    }

    /// <summary>
    /// 
    /// </summary>
    public class GenerateService : IGenerateService
    {

        /// <summary>
        /// 
        /// </summary>
        private readonly Dictionary<string, ConnectionSetting> _connectionSettings;

        /// <summary>
        /// 
        /// </summary>
        public GenerateService()
        {
            List<ConnectionSetting> connectionSettingList = GlobalConfiguration.GetSectionConfig<List<ConnectionSetting>>("ConnectionSetting")!;
            _connectionSettings = connectionSettingList.ToDictionary<ConnectionSetting, string>(x => x.Name!);
            DatabaseClientContainer = new Dictionary<string, DatabaseClient>();
        }

        #region Privates

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
        private DatabaseClient GetDatabaseClient(ConnectionSetting setting)
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
        /// <param name="settingName"></param>
        /// <returns></returns>
        public List<DbTableInfo> GetTableInfoList(string settingName)
        {
            if (!_connectionSettings.ContainsKey(settingName))
            {
                throw new InvalidOperationException("Invalid Connection Setting: " + settingName);
            }
            ConnectionSetting setting = _connectionSettings[settingName];
            List<string>? tableNames;
            if (string.IsNullOrEmpty(setting.GenerateTables) || setting.GenerateTables.Equals("*"))
            {
                tableNames = null;
            }
            else
            {
                tableNames = StringTool.Explode<string>(setting.GenerateTables);
            }

            DatabaseClient databaseClient = GetDatabaseClient(setting);
            List<DbTableInfo> tableList = databaseClient.GetTableInfos(tableNames);

            return tableList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string ParseLanguageType(Type type)
        {
            string typeName;
            switch (Type.GetTypeCode(type))
            {
                default:
                    typeName = type.FullName!;
                    break;
                case TypeCode.Boolean:
                    typeName = "bool";
                    break;
                case TypeCode.Byte:
                    typeName = "byte";
                    break;
                case TypeCode.SByte:
                    typeName = "sbyte";
                    break;
                case TypeCode.Int16:
                    typeName = "short";
                    break;
                case TypeCode.UInt16:
                    typeName = "ushort";
                    break;
                case TypeCode.Int32:
                    typeName = "int";
                    break;
                case TypeCode.UInt32:
                    typeName = "uint";
                    break;
                case TypeCode.Int64:
                    typeName = "long";
                    break;
                case TypeCode.UInt64:
                    typeName = "ulong";
                    break;
                case TypeCode.Single:
                    typeName = "float";
                    break;
                case TypeCode.Double:
                    typeName = "double";
                    break;
                case TypeCode.Decimal:
                    typeName = "decimal";
                    break;
                case TypeCode.DateTime:
                    typeName = "DateTime";
                    break;
                case TypeCode.Char:
                    typeName = "char";
                    break;
                case TypeCode.String:
                    typeName = "string";
                    break;
            }
            return typeName;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        public string ParseLanguageType(string settingName, DbColumnInfo columnInfo)
        {
            if (!_connectionSettings.ContainsKey(settingName))
            {
                throw new InvalidOperationException("Invalid Connection Setting: " + settingName);
            }
            ConnectionSetting setting = _connectionSettings[settingName];
            DatabaseClient databaseClient = GetDatabaseClient(setting);
            Type type = databaseClient.GetColumnType(columnInfo);

            string typeName;
            if (type == typeof(Guid))
            {
                typeName = "Guid";
            }
            else if (type.IsArray)
            {
                Type elementType = type.GetElementType()!;
                typeName = string.Concat(ParseLanguageType(elementType), "[]");
            }
            else
            {
                typeName = ParseLanguageType(type);
            }
            return typeName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        public string ParseDbParameterType(string settingName, DbColumnInfo columnInfo)
        {
            if (!_connectionSettings.ContainsKey(settingName))
            {
                throw new InvalidOperationException("Invalid Connection Setting: " + settingName);
            }
            ConnectionSetting setting = _connectionSettings[settingName];
            DatabaseClient databaseClient = GetDatabaseClient(setting);
            System.Data.DbType type = databaseClient.GetColumnDbType(columnInfo);

            string typeName = string.Concat("DbType.", type.ToString());
            return typeName;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (DatabaseClientContainer != null && DatabaseClientContainer.Count > 0)
            {
                foreach (var item in DatabaseClientContainer)
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
