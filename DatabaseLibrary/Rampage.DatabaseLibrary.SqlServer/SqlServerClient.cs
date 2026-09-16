using Rampage.Data;
using Rampage.Data.Client;
using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Rampage.DatabaseLibrary.SqlServer
{
    /// <summary>
    /// SqlServer DatabaseClient
    /// </summary>
    public class SqlServerClient : DatabaseClient
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataAccessor"></param>
        protected override IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor)
        {
            if (dataAccessor == null)
            {
                dataAccessor = new SqlServerDataAccessor();
                DataAccessor = dataAccessor;
            }
            return dataAccessor;
        }

        /// <summary>
        /// 
        /// </summary>
        public SqlServerClient() : base()
        {
            SqlTemplate = new SqlServerTemplate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        public SqlServerClient(IDbConnection conn) : base(conn)
        {
            SqlTemplate = new SqlServerTemplate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connString">custom database connectionString</param>
        public SqlServerClient(string connString) : base(connString)
        {
            SqlTemplate = new SqlServerTemplate();
        }

        public override DatabaseType DatabaseType => DatabaseType.SqlServer;


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
