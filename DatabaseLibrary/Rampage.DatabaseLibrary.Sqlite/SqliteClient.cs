using Microsoft.Data.Sqlite;
using Rampage.Data;
using Rampage.Data.Client;
using System;
using System.Data;

namespace Rampage.DatabaseLibrary.Sqlite
{
    public class SqliteClient : DatabaseClient
    {

        /// <summary>
        /// Sqlite DatabaseClient
        /// </summary>
        /// <param name="dataAccessor"></param>
        protected override IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor)
        {
            if (dataAccessor == null)
            {
                dataAccessor = new SqliteDataAccessor();
                DataAccessor = dataAccessor;
            }
            return dataAccessor;
        }

        /// <summary>
        /// 
        /// </summary>
        public SqliteClient() : base()
        {
            SqlTemplate = new SqliteTemplate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        public SqliteClient(IDbConnection conn) : base(conn)
        {
            SqlTemplate = new SqliteTemplate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connString">custom database connectionString</param>
        public SqliteClient(string connString) : base(connString)
        {
            SqlTemplate = new SqliteTemplate();
        }

        public override DatabaseType DatabaseType => DatabaseType.Sqlite;


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
