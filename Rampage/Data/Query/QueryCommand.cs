using System;
using System.Data;

namespace Rampage.Data.Query
{
    /// <summary>
    /// DAO 方法级 SQL 命令特性基类：在动态 DAO 接口方法上声明 SQL 及执行参数，
    /// 由动态代理生成器把方法调用转译为对应的数据库执行调用（方法参数按名转为 SQL 参数）
    /// Base attribute for method-level SQL commands on dynamic DAO interfaces: declares the SQL and
    /// execution options; the dynamic proxy generator translates the method call into the matching
    /// database execution (method parameters become named SQL parameters)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class QueryCommand : Attribute
    {

        /// <summary>
        /// 要执行的 SQL 语句
        /// SQL statement to execute
        /// </summary>
        public string Sql
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 命令类型（Text / StoredProcedure 等），为 null 时按文本命令执行
        /// Command type (Text / StoredProcedure etc.); null executes as a text command
        /// </summary>
        public CommandType? CommandType
        {
            get;
            set;
        }

        /// <summary>
        /// 命令超时秒数，为 null 时使用默认超时
        /// Command timeout in seconds; null uses the default timeout
        /// </summary>
        public int? CommandTimeout
        {
            get;
            set;
        }


        private QueryCommand() { }

        /// <summary>
        /// 以指定 SQL 语句初始化命令特性
        /// Initialize the command attribute with a SQL statement
        /// </summary>
        /// <param name="sql">要执行的 SQL 语句 / SQL statement to execute</param>
        public QueryCommand(string sql)
        {
            Sql = sql;
        }

    }
}
