using System;

namespace Rampage.Data.Query
{
    /// <summary>
    /// DAO 方法级非查询命令：标记的方法由动态代理生成为返回受影响行数（Execute）的非查询执行
    /// Method-level non-query command: the marked method is proxied to a non-query execution returning the affected row count (Execute)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ExecuteCommand : QueryCommand
    {

        /// <summary>
        /// 以指定 SQL 语句初始化非查询命令
        /// Initialize the non-query command with a SQL statement
        /// </summary>
        /// <param name="sql">要执行的 SQL 语句 / SQL statement to execute</param>
        public ExecuteCommand(string sql) : base(sql) { }

    }
}
