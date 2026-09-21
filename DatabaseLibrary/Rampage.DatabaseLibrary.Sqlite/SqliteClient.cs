using Microsoft.Data.Sqlite;
using Rampage.Data;
using Rampage.Data.Client;
using System;
using System.Data;

namespace Rampage.DatabaseLibrary.Sqlite
{
    /// <summary>SQLite 数据库客户端（Microsoft.Data.Sqlite 驱动）/ SQLite database client (Microsoft.Data.Sqlite driver).</summary>
    /// <remarks>
    /// 集成模式：构造时设置 <see cref="SqliteTemplate"/> 方言模板；数据访问经 <see cref="DatabaseClient.DataAccessor"/> 委派，
    /// 默认使用 <see cref="SqliteDataAccessor"/>（原生 ADO.NET 实现）；当 DatabaseFactory 按客户端 Name 配置了
    /// 其他访问器类型（如 DapperDataAccessor）时优先使用配置项。
    /// Integration: the ctor installs the <see cref="SqliteTemplate"/> dialect; data access is delegated through
    /// <see cref="DatabaseClient.DataAccessor"/>, defaulting to <see cref="SqliteDataAccessor"/> (native ADO.NET);
    /// an accessor type registered per client Name in DatabaseFactory (e.g. DapperDataAccessor) takes precedence.
    /// </remarks>
    public class SqliteClient : DatabaseClient
    {

        /// <summary>设置默认数据访问器：未提供时创建 <see cref="SqliteDataAccessor"/> 并赋给 DataAccessor / Installs the default data accessor: creates a <see cref="SqliteDataAccessor"/> and assigns it to DataAccessor when none was supplied.</summary>
        /// <param name="dataAccessor">外部传入的访问器；null 表示使用默认 / an externally supplied accessor; null means use the default.</param>
        /// <returns>生效的数据访问器 / the effective data accessor.</returns>
        protected override IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor)
        {
            if (dataAccessor == null)
            {
                dataAccessor = new SqliteDataAccessor();
                DataAccessor = dataAccessor;
            }
            return dataAccessor;
        }

        /// <summary>初始化客户端并安装 SQLite 方言模板 / Initializes the client and installs the SQLite dialect template.</summary>
        public SqliteClient() : base()
        {
            SqlTemplate = new SqliteTemplate();
        }

        /// <summary>用已有连接初始化客户端并安装 SQLite 方言模板 / Initializes the client with an existing connection and installs the SQLite dialect template.</summary>
        /// <param name="conn">已打开或待打开的原生 SqliteConnection / an existing SqliteConnection, open or closed.</param>
        public SqliteClient(IDbConnection conn) : base(conn)
        {
            SqlTemplate = new SqliteTemplate();
        }

        /// <summary>用自定义连接串初始化客户端并安装 SQLite 方言模板 / Initializes the client with a custom connection string and installs the SQLite dialect template.</summary>
        /// <param name="connString">自定义数据库连接串（如 Data Source=app.db）/ a custom database connection string (e.g. Data Source=app.db).</param>
        public SqliteClient(string connString) : base(connString)
        {
            SqlTemplate = new SqliteTemplate();
        }

        /// <summary>数据库类型标识：Sqlite / The database type: Sqlite.</summary>
        public override DatabaseType DatabaseType => DatabaseType.Sqlite;


        /// <summary>连接创建器：每次访问都新建一个包装委托的 <see cref="DatabaseConnector"/>，委托用当前 ConnectionString 创建原生 SqliteConnection / The connection creator: each access returns a new <see cref="DatabaseConnector"/> whose delegate creates a native SqliteConnection from the current ConnectionString.</summary>
        public override DatabaseConnector? ConnectionCreator
        {
            get
            {
                return new DatabaseConnector(delegate ()
                {
                    return new SqliteConnection(ConnectionString);
                });
            }
        }


    }
}
