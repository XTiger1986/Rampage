using Oracle.ManagedDataAccess.Client;
using Rampage.Data;
using Rampage.Data.Client;
using System.Data;

namespace Rampage.DatabaseLibrary.Oracle
{

    /// <summary>Oracle 数据库客户端（Oracle.ManagedDataAccess.Core 托管驱动）/ Oracle database client (Oracle.ManagedDataAccess.Core managed driver).</summary>
    /// <remarks>
    /// 集成模式：构造时设置 <see cref="OracleTemplate"/> 方言模板；数据访问经 <see cref="DatabaseClient.DataAccessor"/> 委派，
    /// 默认使用 <see cref="OracleDataAccessor"/>（原生 ADO.NET 实现）；当 DatabaseFactory 按客户端 Name 配置了
    /// 其他访问器类型（如 DapperDataAccessor）时优先使用配置项。
    /// Integration: the ctor installs the <see cref="OracleTemplate"/> dialect; data access is delegated through
    /// <see cref="DatabaseClient.DataAccessor"/>, defaulting to <see cref="OracleDataAccessor"/> (native ADO.NET);
    /// an accessor type registered per client Name in DatabaseFactory (e.g. DapperDataAccessor) takes precedence.
    /// </remarks>
    public class OracleClient : DatabaseClient
    {

        /// <summary>设置默认数据访问器：未提供时创建 <see cref="OracleDataAccessor"/> 并赋给 DataAccessor / Installs the default data accessor: creates an <see cref="OracleDataAccessor"/> and assigns it to DataAccessor when none was supplied.</summary>
        /// <param name="dataAccessor">外部传入的访问器；null 表示使用默认 / an externally supplied accessor; null means use the default.</param>
        /// <returns>生效的数据访问器 / the effective data accessor.</returns>
        protected override IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor)
        {
            if (dataAccessor == null)
            {
                dataAccessor = new OracleDataAccessor();
                DataAccessor = dataAccessor;
            }
            return dataAccessor;
        }

        /// <summary>初始化客户端并安装 Oracle 方言模板 / Initializes the client and installs the Oracle dialect template.</summary>
        public OracleClient() : base()
        {
            SqlTemplate = new OracleTemplate();
        }

        /// <summary>用已有连接初始化客户端并安装 Oracle 方言模板 / Initializes the client with an existing connection and installs the Oracle dialect template.</summary>
        /// <param name="conn">已打开或待打开的原生 OracleConnection / an existing OracleConnection, open or closed.</param>
        public OracleClient(IDbConnection conn) : base(conn)
        {
            SqlTemplate = new OracleTemplate();
        }

        /// <summary>用自定义连接串初始化客户端并安装 Oracle 方言模板 / Initializes the client with a custom connection string and installs the Oracle dialect template.</summary>
        /// <param name="connString">自定义数据库连接串 / a custom database connection string.</param>
        public OracleClient(string connString) : base(connString)
        {
            SqlTemplate = new OracleTemplate();
        }

        /// <summary>数据库类型标识：Oracle / The database type: Oracle.</summary>
        public override DatabaseType DatabaseType => DatabaseType.Oracle;


        /// <summary>连接创建器：每次访问都新建一个包装委托的 <see cref="DatabaseConnector"/>，委托用当前 ConnectionString 创建原生 OracleConnection / The connection creator: each access returns a new <see cref="DatabaseConnector"/> whose delegate creates a native OracleConnection from the current ConnectionString.</summary>
        public override DatabaseConnector? ConnectionCreator
        {
            get
            {
                return new DatabaseConnector(delegate ()
                {
                    return new OracleConnection(ConnectionString);
                });
            }
        }

    }
}
