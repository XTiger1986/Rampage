using System;
using System.Data;

namespace Rampage.Data
{
    /// <summary>
    /// 数据库连接器：封装创建数据库连接的委托
    /// Database connector: wraps the delegate that creates database connections
    /// </summary>
    public class DatabaseConnector
    {

        /// <summary>
        /// 创建数据库连接的委托
        /// Delegate that creates database connections
        /// </summary>
        public Func<IDbConnection>? ConnectionCreator
        {
            get;
            private set;
        }

        /// <summary>
        /// 私有构造：禁止外部无参构造
        /// Private ctor: prevents parameterless construction
        /// </summary>
        private DatabaseConnector() { }

        /// <summary>
        /// 用创建连接的委托初始化连接器
        /// Initialize the connector with a connection-creator delegate
        /// </summary>
        /// <param name="creator">创建数据库连接的委托 / delegate that creates a connection</param>
        public DatabaseConnector(Func<IDbConnection> creator)
        {
            this.ConnectionCreator = creator;
        }

        /// <summary>
        /// 调用委托创建一个新的数据库连接
        /// Invoke the delegate to create a new database connection
        /// </summary>
        /// <returns>新的数据库连接 / a new connection</returns>
        internal IDbConnection CreateConnection()
        {
            return this.ConnectionCreator!.Invoke();
        }

    }
}
