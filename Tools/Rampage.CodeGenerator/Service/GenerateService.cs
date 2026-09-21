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
    /// 代码生成服务接口：读取库表元数据，并将数据库列映射为语言类型与 DbType 参数类型 /
    /// Generate service interface: loads table metadata and maps database columns to language types and DbType parameter types
    /// </summary>
    public interface IGenerateService : IDisposable
    {

        /// <summary>
        /// 获取指定连接配置对应的数据库表信息列表（含列信息）/
        /// Gets the table metadata list (including columns) for the given connection setting
        /// </summary>
        /// <param name="settingName">连接配置名称 / Connection setting name</param>
        /// <returns>表信息列表，表范围由配置的 GenerateTables 决定 / The table info list, scoped by the configured GenerateTables</returns>
        List<DbTableInfo> GetTableInfoList(string settingName);

        /// <summary>
        /// 将数据库列映射为 C# 语言类型名（如 int、string、Guid、byte[]）/
        /// Maps a database column to its C# language type name (e.g. int, string, Guid, byte[])
        /// </summary>
        /// <param name="settingName">连接配置名称 / Connection setting name</param>
        /// <param name="columnInfo">列信息 / Column info</param>
        /// <returns>C# 类型名字符串 / The C# type name</returns>
        string ParseLanguageType(string settingName, DbColumnInfo columnInfo);

        /// <summary>
        /// 将数据库列映射为参数类型字符串（形如 "DbType.Int32"）/
        /// Maps a database column to a parameter type string (e.g. "DbType.Int32")
        /// </summary>
        /// <param name="settingName">连接配置名称 / Connection setting name</param>
        /// <param name="columnInfo">列信息 / Column info</param>
        /// <returns>形如 "DbType.Xxx" 的类型字符串 / A string of the form "DbType.Xxx"</returns>
        string ParseDbParameterType(string settingName, DbColumnInfo columnInfo);

    }

    /// <summary>
    /// 代码生成服务实现：按 DatabaseLibrary/Provider 创建并缓存 DatabaseClient（内置 MySql/PostgreSQL/Oracle，
    /// 其余程序集走反射加载），基于库表元数据完成列→C# 类型与列→DbType 的映射 /
    /// Generate service implementation: creates and caches DatabaseClients by DatabaseLibrary/Provider
    /// (built-in MySql/PostgreSQL/Oracle, other assemblies via reflection) and maps columns to C# types and DbTypes
    /// </summary>
    public class GenerateService : IGenerateService
    {

        /// <summary>
        /// 连接配置缓存（键：配置名称）/ Connection setting cache (keyed by setting name)
        /// </summary>
        private readonly Dictionary<string, ConnectionSetting> _connectionSettings;

        /// <summary>
        /// 初始化服务：读取 ConnectionSetting 配置节建立名称索引，并创建 DatabaseClient 缓存容器 /
        /// Initializes the service: indexes the ConnectionSetting section by name and creates the client cache
        /// </summary>
        public GenerateService()
        {
            List<ConnectionSetting> connectionSettingList = GlobalConfiguration.GetSectionConfig<List<ConnectionSetting>>("ConnectionSetting")!;
            _connectionSettings = connectionSettingList.ToDictionary<ConnectionSetting, string>(x => x.Name!);
            DatabaseClientContainer = new Dictionary<string, DatabaseClient>();
        }

        #region Privates

        /// <summary>
        /// DatabaseClient 缓存（键：DatabaseLibrary.Provider 组合键）/ DatabaseClient cache (keyed by the "DatabaseLibrary.Provider" composite key)
        /// </summary>
        private Dictionary<string, DatabaseClient> DatabaseClientContainer
        {
            get;
            set;
        }

        /// <summary>
        /// 获取（或创建并缓存）连接配置对应的 DatabaseClient /
        /// Gets (or creates and caches) the DatabaseClient for the given connection setting
        /// </summary>
        /// <param name="setting">连接配置 / Connection setting</param>
        /// <returns>对应的 DatabaseClient 实例（已装配 DapperDataAccessor）/ The matching DatabaseClient (equipped with a DapperDataAccessor)</returns>
        /// <remarks>
        /// 缓存键为 "DatabaseLibrary.Provider"。内置三种程序集（MySql/PostgreSQL/Oracle）在 switch 中按固定类直接创建，
        /// 此时配置中的 Provider 并不参与选择；其余程序集通过 Assembly.Load 加载后以 Provider 为完整类型名反射实例化（忽略大小写）。
        /// The cache key is "DatabaseLibrary.Provider". The three built-in libraries are instantiated as fixed classes in the switch
        /// (the configured Provider is ignored there); any other assembly is loaded via Assembly.Load and instantiated by reflection,
        /// with Provider as the namespace-qualified type name (case-insensitive).
        /// </remarks>
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
        /// 获取指定连接配置对应的数据库表信息列表（含列信息）/
        /// Gets the table metadata list (including columns) for the given connection setting
        /// </summary>
        /// <param name="settingName">连接配置名称 / Connection setting name</param>
        /// <returns>表信息列表 / The table info list</returns>
        /// <exception cref="InvalidOperationException">连接配置名不存在 / The connection setting name is unknown</exception>
        /// <remarks>
        /// GenerateTables 为空或 "*" 时表名列表传 null（获取全部表）；否则按逗号拆分为表名列表。
        /// When GenerateTables is empty or "*", null is passed (all tables); otherwise the value is split by comma into a name list.
        /// </remarks>
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
        /// 将 CLR 类型映射为 C# 关键字类型名（如 Int32 → int、String → string）/
        /// Maps a CLR type to its C# keyword name (e.g. Int32 → int, String → string)
        /// </summary>
        /// <param name="type">CLR 类型 / The CLR type</param>
        /// <returns>C# 类型名；非基元类型返回 type.FullName / The C# type name; Type.FullName for non-primitive types</returns>
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
        /// 将数据库列映射为 C# 语言类型名（如 int、string、Guid、byte[]）/
        /// Maps a database column to its C# language type name (e.g. int, string, Guid, byte[])
        /// </summary>
        /// <param name="settingName">连接配置名称 / Connection setting name</param>
        /// <param name="columnInfo">列信息 / Column info</param>
        /// <returns>C# 类型名字符串 / The C# type name</returns>
        /// <exception cref="InvalidOperationException">连接配置名不存在 / The connection setting name is unknown</exception>
        /// <remarks>
        /// 先经 DatabaseClient.GetColumnType 取得 CLR 类型：Guid 特判为 "Guid"，数组类型递归映射为 "元素类型[]"，
        /// 其余按 TypeCode 走 C# 关键字映射。
        /// Obtains the CLR type via DatabaseClient.GetColumnType first: Guid is special-cased to "Guid",
        /// array types are recursively mapped to "elementType[]", and the rest go through the TypeCode keyword mapping.
        /// </remarks>
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
        /// 将数据库列映射为参数类型字符串（形如 "DbType.Int32"）/
        /// Maps a database column to a parameter type string (e.g. "DbType.Int32")
        /// </summary>
        /// <param name="settingName">连接配置名称 / Connection setting name</param>
        /// <param name="columnInfo">列信息 / Column info</param>
        /// <returns>形如 "DbType.Xxx" 的类型字符串 / A string of the form "DbType.Xxx"</returns>
        /// <exception cref="InvalidOperationException">连接配置名不存在 / The connection setting name is unknown</exception>
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
        /// 释放缓存的全部 DatabaseClient / Disposes all cached DatabaseClients
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
