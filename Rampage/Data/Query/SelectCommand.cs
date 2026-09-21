using System;

namespace Rampage.Data.Query
{
    /// <summary>
    /// DAO 方法级单行查询命令：标记的方法由动态代理生成为返回单个模型（SelectOne）的查询
    /// Method-level single-row select command: the marked method is proxied to a query returning one model (SelectOne)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SelectCommand : QueryCommand
    {

        /// <summary>
        /// 以指定 SQL 语句初始化单行查询命令
        /// Initialize the single-row select command with a SQL statement
        /// </summary>
        /// <param name="sql">要执行的 SQL 语句 / SQL statement to execute</param>
        public SelectCommand(string sql) : base(sql) { }

    }
}
