using Npgsql;
using Rampage.Data;
using Rampage.Data.Client;
using System;
using System.Data;

namespace Rampage.DatabaseLibrary.PostgreSQL
{

    /// <summary>
    /// PostgreSQL DatabaseClient
    /// </summary>
    public class PgSqlClient : DatabaseClient
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataAccessor"></param>
        protected override IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor)
        {
            if (dataAccessor == null)
            {
                dataAccessor = new PgSqlDataAccessor();
                DataAccessor = dataAccessor;
            }
            return dataAccessor;
        }

        /// <summary>
        /// 
        /// </summary>
        public PgSqlClient() : base()
        {
            SqlTemplate = new PgSqlTemplate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        public PgSqlClient(IDbConnection conn) : base(conn)
        {
            SqlTemplate = new PgSqlTemplate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connString">custom database connectionString</param>
        public PgSqlClient(string connString) : base(connString)
        {
            SqlTemplate = new PgSqlTemplate();
        }

        public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;


        public override DatabaseConnector? ConnectionCreator
        {
            get
            {
                return new DatabaseConnector(delegate ()
                {
                    return new NpgsqlConnection(ConnectionString);
                } );
            }
        }




    }
}
