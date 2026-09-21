using Rampage.Hierarchy;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 静态 CURD 执行器：由 DynamicDaoCodeBuilder 生成的 DAO 代理方法在 IL 中直接 call，
    /// 与 [SelectCommand] / [SelectListCommand] / [ExecuteCommand] 特性配合，把特性声明的 SQL
    /// 与方法参数字典转发给底层 DatabaseClient 的对应 API。
    /// Static CURD executor invoked directly (via Reflection.Emit call) by the dynamically generated DAO proxy methods;
    /// works with the [SelectCommand] / [SelectListCommand] / [ExecuteCommand] attributes to forward the attribute SQL
    /// and the method-argument dictionary to the underlying DatabaseClient API.
    /// </summary>
    /// <remarks>
    /// IL 生成端对未设置的 CommandType 压入 0、未设置的 CommandTimeout 压入 -1；
    /// 本执行器把 0/负数一律还原为 null（即"未指定"），交由驱动使用默认值。
    /// The IL generator pushes 0 for an unset CommandType and -1 for an unset CommandTimeout;
    /// both are normalized here to null (= unspecified) so the provider default applies.
    /// </remarks>
    public class CurdExecutor
    {

        /// <summary>
        /// 执行特性声明的单行查询（对应 [SelectCommand]）
        /// Execute the attribute-declared single-row query (for [SelectCommand])
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dao">目标 DAO 实例（提供 DatabaseClient）/ target DAO instance providing the DatabaseClient</param>
        /// <param name="sql">特性中声明的 SQL 语句 / SQL statement declared on the attribute</param>
        /// <param name="args">方法参数名到参数值的字典 / dictionary of method argument name to argument value</param>
        /// <param name="commandType">命令类型；0 表示未指定（转 null）/ command type; 0 means unspecified (mapped to null)</param>
        /// <param name="commandTimeout">命令超时秒数；小于等于 0 表示未指定（转 null）/ command timeout in seconds; &lt;= 0 means unspecified (mapped to null)</param>
        /// <returns>查到的单条数据，无匹配返回 null / the matched row, or null when none</returns>
        public static M? Select<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout) where M : class
        {
            return dao.DatabaseClient.SelectOne<M>(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null);
        }

        /// <summary>
        /// 执行特性声明的多行查询（对应 [SelectListCommand]）
        /// Execute the attribute-declared multi-row query (for [SelectListCommand])
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dao">目标 DAO 实例（提供 DatabaseClient）/ target DAO instance providing the DatabaseClient</param>
        /// <param name="sql">特性中声明的 SQL 语句 / SQL statement declared on the attribute</param>
        /// <param name="args">方法参数名到参数值的字典 / dictionary of method argument name to argument value</param>
        /// <param name="commandType">命令类型；0 表示未指定（转 null）/ command type; 0 means unspecified (mapped to null)</param>
        /// <param name="commandTimeout">命令超时秒数；小于等于 0 表示未指定（转 null）/ command timeout in seconds; &lt;= 0 means unspecified (mapped to null)</param>
        /// <returns>查询到的数据列表 / the matched rows</returns>
        public static List<M> SelectList<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout) where M : class
        {
            return dao.DatabaseClient.SelectList<M>(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null);
        }

        /// <summary>
        /// 执行特性声明的增/删/改语句（对应 [ExecuteCommand]）
        /// Execute the attribute-declared insert/delete/update statement (for [ExecuteCommand])
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dao">目标 DAO 实例（提供 DatabaseClient）/ target DAO instance providing the DatabaseClient</param>
        /// <param name="sql">特性中声明的 SQL 语句 / SQL statement declared on the attribute</param>
        /// <param name="args">方法参数名到参数值的字典 / dictionary of method argument name to argument value</param>
        /// <param name="commandType">命令类型；0 表示未指定（转 null）/ command type; 0 means unspecified (mapped to null)</param>
        /// <param name="commandTimeout">命令超时秒数；小于等于 0 表示未指定（转 null）/ command timeout in seconds; &lt;= 0 means unspecified (mapped to null)</param>
        /// <returns>受影响行数 / the number of rows affected</returns>
        public static int Execute<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout) where M : class
        {
            return dao.DatabaseClient.Execute(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null);
        }

        /// <summary>
        /// 执行特性声明的单行查询（异步版本）
        /// Execute the attribute-declared single-row query (asynchronous)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dao">目标 DAO 实例（提供 DatabaseClient）/ target DAO instance providing the DatabaseClient</param>
        /// <param name="sql">特性中声明的 SQL 语句 / SQL statement declared on the attribute</param>
        /// <param name="args">方法参数名到参数值的字典 / dictionary of method argument name to argument value</param>
        /// <param name="commandType">命令类型；0 表示未指定（转 null）/ command type; 0 means unspecified (mapped to null)</param>
        /// <param name="commandTimeout">命令超时秒数；小于等于 0 表示未指定（转 null）/ command timeout in seconds; &lt;= 0 means unspecified (mapped to null)</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>查到的单条数据（无匹配为 null）的任务 / task delivering the matched row, or null when none</returns>
        public static Task<M?> SelectAsync<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout, CancellationToken cancellationToken = default) where M : class
        {
            return dao.DatabaseClient.SelectOneAsync<M>(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null, cancellationToken);
        }

        /// <summary>
        /// 执行特性声明的多行查询（异步版本）
        /// Execute the attribute-declared multi-row query (asynchronous)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dao">目标 DAO 实例（提供 DatabaseClient）/ target DAO instance providing the DatabaseClient</param>
        /// <param name="sql">特性中声明的 SQL 语句 / SQL statement declared on the attribute</param>
        /// <param name="args">方法参数名到参数值的字典 / dictionary of method argument name to argument value</param>
        /// <param name="commandType">命令类型；0 表示未指定（转 null）/ command type; 0 means unspecified (mapped to null)</param>
        /// <param name="commandTimeout">命令超时秒数；小于等于 0 表示未指定（转 null）/ command timeout in seconds; &lt;= 0 means unspecified (mapped to null)</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>查询到的数据列表的任务 / task delivering the matched rows</returns>
        public static Task<List<M>> SelectListAsync<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout, CancellationToken cancellationToken = default) where M : class
        {
            return dao.DatabaseClient.SelectListAsync<M>(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null, cancellationToken);
        }

        /// <summary>
        /// 执行特性声明的增/删/改语句（异步版本）
        /// Execute the attribute-declared insert/delete/update statement (asynchronous)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dao">目标 DAO 实例（提供 DatabaseClient）/ target DAO instance providing the DatabaseClient</param>
        /// <param name="sql">特性中声明的 SQL 语句 / SQL statement declared on the attribute</param>
        /// <param name="args">方法参数名到参数值的字典 / dictionary of method argument name to argument value</param>
        /// <param name="commandType">命令类型；0 表示未指定（转 null）/ command type; 0 means unspecified (mapped to null)</param>
        /// <param name="commandTimeout">命令超时秒数；小于等于 0 表示未指定（转 null）/ command timeout in seconds; &lt;= 0 means unspecified (mapped to null)</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数的任务 / task delivering the number of rows affected</returns>
        public static Task<int> ExecuteAsync<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout, CancellationToken cancellationToken = default) where M : class
        {
            return dao.DatabaseClient.ExecuteAsync(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null, cancellationToken);
        }

    }
}
