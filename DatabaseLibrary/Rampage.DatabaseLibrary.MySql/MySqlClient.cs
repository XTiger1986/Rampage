using MySqlConnector;
using Rampage.Data;
using Rampage.Data.Client;
using System;
using System.Data;

namespace Rampage.DatabaseLibrary.MySql
{

    /// <summary>MySQL 数据库客户端（MySqlConnector 驱动）/ MySQL database client (MySqlConnector driver).</summary>
    /// <remarks>
    /// 集成模式：构造时设置 <see cref="MySqlTemplate"/> 方言模板；数据访问经 <see cref="DatabaseClient.DataAccessor"/> 委派，
    /// 默认使用 <see cref="MySqlDataAccessor"/>（原生 ADO.NET 实现）；当 DatabaseFactory 按客户端 Name 配置了
    /// 其他访问器类型（如 DapperDataAccessor）时优先使用配置项。
    /// Integration: the ctor installs the <see cref="MySqlTemplate"/> dialect; data access is delegated through
    /// <see cref="DatabaseClient.DataAccessor"/>, defaulting to <see cref="MySqlDataAccessor"/> (native ADO.NET);
    /// an accessor type registered per client Name in DatabaseFactory (e.g. DapperDataAccessor) takes precedence.
    /// </remarks>
    public class MySqlClient : DatabaseClient
    {

        /// <summary>设置默认数据访问器：未提供时创建 <see cref="MySqlDataAccessor"/> 并赋给 DataAccessor / Installs the default data accessor: creates an <see cref="MySqlDataAccessor"/> and assigns it to DataAccessor when none was supplied.</summary>
        /// <param name="dataAccessor">外部传入的访问器；null 表示使用默认 / an externally supplied accessor; null means use the default.</param>
        /// <returns>生效的数据访问器 / the effective data accessor.</returns>
        protected override IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor)
        {
            if (dataAccessor == null)
            {
                dataAccessor = new MySqlDataAccessor();
                DataAccessor = dataAccessor;
            }
            return dataAccessor;
        }

        /// <summary>初始化客户端并安装 MySQL 方言模板 / Initializes the client and installs the MySQL dialect template.</summary>
        public MySqlClient() : base()
        {
            SqlTemplate = new MySqlTemplate();
        }

        /// <summary>用已有连接初始化客户端并安装 MySQL 方言模板 / Initializes the client with an existing connection and installs the MySQL dialect template.</summary>
        /// <param name="conn">已打开或待打开的原生 MySqlConnection / an existing MySqlConnection, open or closed.</param>
        public MySqlClient(IDbConnection conn) : base(conn)
        {
            SqlTemplate = new MySqlTemplate();
        }

        /// <summary>用自定义连接串初始化客户端并安装 MySQL 方言模板 / Initializes the client with a custom connection string and installs the MySQL dialect template.</summary>
        /// <param name="connString">自定义数据库连接串 / a custom database connection string.</param>
        public MySqlClient(string connString) : base(connString)
        {
            SqlTemplate = new MySqlTemplate();
        }

        /// <summary>数据库类型标识：MySQL / The database type: MySQL.</summary>
        public override DatabaseType DatabaseType => DatabaseType.MySQL;


        /// <summary>连接创建器：每次访问都新建一个包装委托的 <see cref="DatabaseConnector"/>，委托用当前 ConnectionString 创建原生 MySqlConnection / The connection creator: each access returns a new <see cref="DatabaseConnector"/> whose delegate creates a native MySqlConnection from the current ConnectionString.</summary>
        public override DatabaseConnector? ConnectionCreator
        {
            get
            {
                return new DatabaseConnector(delegate ()
                {
                    return new MySqlConnection(ConnectionString);
                } );
            }
        }




        /* ******************************** MySQL 备忘 / MySQL cheat sheet ********************************
         * 本客户端实际使用 MySqlConnector 驱动（MySqlConnection）。
         * This client actually uses the MySqlConnector driver (MySqlConnection).
         *
         * 创建数据库 / Create database:
         *   CREATE DATABASE IF NOT EXISTS yourdbname DEFAULT CHARSET utf8mb4 COLLATE utf8mb4_unicode_ci;
         *
         * 连接串示例（MySqlConnector）/ Connection string samples (MySqlConnector):
         *   标准连接 / standard     - Server=myServerAddress;Port=3306;Database=myDataBase;Uid=myUsername;Pwd=myPassword;
         *   连接/命令超时 / timeouts - Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;Connection Timeout=5;Default Command Timeout=20;
         *   加密传输 / TLS          - Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;SSL Mode=Required;
         * 字符集说明：MySqlConnector 始终以 utf8mb4 通信，连接串中的 CharSet/Character Set 选项会被忽略，
         * 建议建库时统一 utf8mb4；旧驱动（MyODBC / OLE DB / Connector/Net 等）的连接串样本已删除。
         * Charset note: MySqlConnector always speaks utf8mb4 and ignores CharSet/Character Set in the connection string,
         * so create the database as utf8mb4; samples for legacy drivers (MyODBC / OLE DB / Connector/Net etc.) were removed.
         * ************************************************************************************************ */
    }
}
