using Rampage.Data;
using Rampage.Data.Client;
using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Rampage.DatabaseLibrary.SqlServer
{
    /// <summary>SQL Server 数据库客户端（Microsoft.Data.SqlClient 驱动）/ SQL Server database client (Microsoft.Data.SqlClient driver).</summary>
    /// <remarks>
    /// 集成模式：构造时设置 <see cref="SqlServerTemplate"/> 方言模板；数据访问经 <see cref="DatabaseClient.DataAccessor"/> 委派，
    /// 默认使用 <see cref="SqlServerDataAccessor"/>（原生 ADO.NET 实现）；当 DatabaseFactory 按客户端 Name 配置了
    /// 其他访问器类型（如 DapperDataAccessor）时优先使用配置项。
    /// Integration: the ctor installs the <see cref="SqlServerTemplate"/> dialect; data access is delegated through
    /// <see cref="DatabaseClient.DataAccessor"/>, defaulting to <see cref="SqlServerDataAccessor"/> (native ADO.NET);
    /// an accessor type registered per client Name in DatabaseFactory (e.g. DapperDataAccessor) takes precedence.
    /// </remarks>
    public class SqlServerClient : DatabaseClient
    {

        /// <summary>设置默认数据访问器：未提供时创建 <see cref="SqlServerDataAccessor"/> 并赋给 DataAccessor / Installs the default data accessor: creates a <see cref="SqlServerDataAccessor"/> and assigns it to DataAccessor when none was supplied.</summary>
        /// <param name="dataAccessor">外部传入的访问器；null 表示使用默认 / an externally supplied accessor; null means use the default.</param>
        /// <returns>生效的数据访问器 / the effective data accessor.</returns>
        protected override IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor)
        {
            if (dataAccessor == null)
            {
                dataAccessor = new SqlServerDataAccessor();
                DataAccessor = dataAccessor;
            }
            return dataAccessor;
        }

        /// <summary>初始化客户端并安装 SQL Server 方言模板 / Initializes the client and installs the SQL Server dialect template.</summary>
        public SqlServerClient() : base()
        {
            SqlTemplate = new SqlServerTemplate();
        }

        /// <summary>用已有连接初始化客户端并安装 SQL Server 方言模板 / Initializes the client with an existing connection and installs the SQL Server dialect template.</summary>
        /// <param name="conn">已打开或待打开的原生 SqlConnection / an existing SqlConnection, open or closed.</param>
        public SqlServerClient(IDbConnection conn) : base(conn)
        {
            SqlTemplate = new SqlServerTemplate();
        }

        /// <summary>用自定义连接串初始化客户端并安装 SQL Server 方言模板 / Initializes the client with a custom connection string and installs the SQL Server dialect template.</summary>
        /// <param name="connString">自定义数据库连接串 / a custom database connection string.</param>
        public SqlServerClient(string connString) : base(connString)
        {
            SqlTemplate = new SqlServerTemplate();
        }

        /// <summary>数据库类型标识：SqlServer / The database type: SqlServer.</summary>
        public override DatabaseType DatabaseType => DatabaseType.SqlServer;


        /// <summary>连接创建器：每次访问都新建一个包装委托的 <see cref="DatabaseConnector"/>，委托用当前 ConnectionString 创建原生 SqlConnection / The connection creator: each access returns a new <see cref="DatabaseConnector"/> whose delegate creates a native SqlConnection from the current ConnectionString.</summary>
        public override DatabaseConnector? ConnectionCreator
        {
            get
            {
                return new DatabaseConnector(delegate ()
                {
                    return new SqlConnection(ConnectionString);
                });
            }
        }



    }
}
