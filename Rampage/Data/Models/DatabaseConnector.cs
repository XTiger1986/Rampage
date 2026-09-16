using System;
using System.Data;

namespace Rampage.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class DatabaseConnector
    {

        /// <summary>
        /// delegate to create database connection
        /// </summary>
        public Func<IDbConnection>? ConnectionCreator
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        private DatabaseConnector() { }

        /// <summary>
        /// initialize delegate of creating database connection 
        /// </summary>
        /// <param name="creator"></param>
        public DatabaseConnector(Func<IDbConnection> creator)
        {
            this.ConnectionCreator = creator;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal IDbConnection CreateConnection()
        {
            return this.ConnectionCreator!.Invoke();
        }

    }
}
