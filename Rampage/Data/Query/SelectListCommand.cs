using System;

namespace Rampage.Data.Query
{
    /// <summary>
    /// DAO 方法级列表查询命令：标记的方法由动态代理生成为返回模型列表（SelectList）的查询
    /// Method-level list select command: the marked method is proxied to a query returning a list of models (SelectList)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SelectListCommand : QueryCommand
    {

        /// <summary>
        /// 以指定 SQL 语句初始化列表查询命令
        /// Initialize the list select command with a SQL statement
        /// </summary>
        /// <param name="sql">要执行的 SQL 语句 / SQL statement to execute</param>
        public SelectListCommand(string sql) : base(sql) { }

    }
}
