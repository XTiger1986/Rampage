using Oracle.ManagedDataAccess.Client;
using Rampage.Data;
using Rampage.Data.Client;
using System.Data;

namespace Rampage.DatabaseLibrary.Oracle
{

    /// <summary>
    /// Oracle DatabaseClient
    /// </summary>
    public class OracleClient : DatabaseClient
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataAccessor"></param>
        protected override IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor)
        {
            if (dataAccessor == null)
            {
                dataAccessor = new OracleDataAccessor();
                DataAccessor = dataAccessor;
            }
            return dataAccessor;
        }

        /// <summary>
        /// 
        /// </summary>
        public OracleClient() : base()
        {
            SqlTemplate = new OracleTemplate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        public OracleClient(IDbConnection conn) : base(conn)
        {
            SqlTemplate = new OracleTemplate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connString">custom database connectionString</param>
        public OracleClient(string connString) : base(connString)
        {
            SqlTemplate = new OracleTemplate();
        }

        public override DatabaseType DatabaseType => DatabaseType.Oracle;


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
