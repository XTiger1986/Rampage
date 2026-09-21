using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 数据访问器接口
    /// Data Accessor Interface
    /// </summary>
    /// <remarks>
    /// SQL 执行的底层抽象（Execute/Select/Reader 等由 Dapper 风格实现承载），实现位于 Rampage.DatabaseLibrary 各方言包。
    /// 各客户端按 Name 静态缓存访问器实例；默认实现由客户端自带，可通过配置 DataAccessTypeName
    /// （经 <see cref="DatabaseFactory.GetDataAccessType"/>）替换为其它实现（如 Dapper 版）。
    /// The low-level abstraction for SQL execution (Execute/Select/Reader backed by a Dapper-style implementation),
    /// with concrete classes living in the Rampage.DatabaseLibrary dialect packages. Each client caches its accessor
    /// instance statically by Name; the default implementation is provided by the client and can be replaced
    /// (e.g. with a Dapper-based one) via the DataAccessTypeName config (see <see cref="DatabaseFactory.GetDataAccessType"/>).
    /// </remarks>
    public interface IDataAccessor
    {


        /// <summary>
        /// 执行非查询SQL语句
        /// Execute a non-query SQL statement
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <returns>受影响的行数 / The number of affected rows</returns>
        int Execute(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行非查询SQL语句
        /// Asynchronously execute a non-query SQL statement
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>受影响的行数 / The number of affected rows</returns>
        Task<int> ExecuteAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到数据集
        /// Execute a SQL statement and return a DataSet
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <returns>查询结果数据集 / The result DataSet</returns>
        DataSet ExecuteDataSet(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到数据集
        /// Asynchronously execute a SQL statement and return a DataSet
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>查询结果数据集 / The result DataSet</returns>
        Task<DataSet> ExecuteDataSetAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到结果首行首列的单值
        /// Execute a SQL statement and return the first column of the first row as a scalar value
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <returns>单值；无结果时为 null / The scalar value; null when no result</returns>
        object? ExecuteScalar(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到结果首行首列的单值
        /// Asynchronously execute a SQL statement and return the first column of the first row as a scalar value
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>单值；无结果时为 null / The scalar value; null when no result</returns>
        Task<object?> ExecuteScalarAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到结果首行首列的单值并转换为 T 类型
        /// Execute a SQL statement and return the first column of the first row converted to type T
        /// </summary>
        /// <typeparam name="T">目标值类型 / The target value type</typeparam>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <returns>T类型单值；无结果时为 null / The scalar value of type T; null when no result</returns>
        T? ExecuteScalar<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到结果首行首列的单值并转换为 T 类型
        /// Asynchronously execute a SQL statement and return the first column of the first row converted to type T
        /// </summary>
        /// <typeparam name="T">目标值类型 / The target value type</typeparam>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>T类型单值；无结果时为 null / The scalar value of type T; null when no result</returns>
        Task<T?> ExecuteScalarAsync<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到DataReader对象包装器
        /// Execute a SQL statement and return a DataReader wrapper
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <returns>DataReader 包装器 / The DataReader wrapper</returns>
        DataReaderWrap ExecuteReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到DataReader对象包装器
        /// Asynchronously execute a SQL statement and return a DataReader wrapper
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>DataReader 包装器 / The DataReader wrapper</returns>
        Task<DataReaderWrap> ExecuteReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到动态类型数据列表
        /// Execute a SQL statement and return a list of dynamic rows
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <returns>动态行列表 / The list of dynamic rows</returns>
        List<dynamic> SelectList(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到动态类型数据列表
        /// Asynchronously execute a SQL statement and return a list of dynamic rows
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>动态行列表 / The list of dynamic rows</returns>
        Task<List<dynamic>> SelectListAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到一条动态类型数据
        /// Execute a SQL statement and return a single dynamic row
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <returns>动态行；无结果时为 null / The dynamic row; null when no result</returns>
        dynamic? SelectOne(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到一条动态类型数据
        /// Asynchronously execute a SQL statement and return a single dynamic row
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>动态行；无结果时为 null / The dynamic row; null when no result</returns>
        Task<dynamic?> SelectOneAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到指定类型数据列表
        /// Execute a SQL statement and return a list of typed rows
        /// </summary>
        /// <typeparam name="M">目标模型类型 / The target model type</typeparam>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <returns>M 类型列表 / The list of M instances</returns>
        List<M> SelectList<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class;

        /// <summary>
        /// 异步执行SQL语句得到指定类型数据列表
        /// Asynchronously execute a SQL statement and return a list of typed rows
        /// </summary>
        /// <typeparam name="M">目标模型类型 / The target model type</typeparam>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>M 类型列表 / The list of M instances</returns>
        Task<List<M>> SelectListAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class;

        /// <summary>
        /// 执行SQL语句得到一条指定类型数据
        /// Execute a SQL statement and return a single typed row
        /// </summary>
        /// <typeparam name="M">目标模型类型 / The target model type</typeparam>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <returns>M 类型实例；无结果时为 null / The M instance; null when no result</returns>
        M? SelectOne<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class;

        /// <summary>
        /// 异步执行SQL语句得到一条指定类型数据
        /// Asynchronously execute a SQL statement and return a single typed row
        /// </summary>
        /// <typeparam name="M">目标模型类型 / The target model type</typeparam>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句 / The SQL statement to execute</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>M 类型实例；无结果时为 null / The M instance; null when no result</returns>
        Task<M?> SelectOneAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class;

        /// <summary>
        /// 执行返回多个结果集的命令，返回可依次读取各结果集的包装器
        /// Execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句（可含多条语句） / The SQL statement to execute (may contain multiple statements)</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <returns>多结果集读取包装器 / The multi-result-set reader wrapper</returns>
        GridReaderWrap ExecuteGridReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行返回多个结果集的命令，返回可依次读取各结果集的包装器
        /// Asynchronously execute a command that returns multiple result sets, and access each in turn
        /// </summary>
        /// <param name="conn">数据库连接 / The database connection</param>
        /// <param name="sql">要执行的SQL语句（可含多条语句） / The SQL statement to execute (may contain multiple statements)</param>
        /// <param name="param">SQL参数对象 / The SQL parameters (anonymous object or dictionary)</param>
        /// <param name="transaction">事务；null 表示不使用 / The transaction; null for none</param>
        /// <param name="commandType">语句类型，默认 Text / The command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数 / The command timeout in seconds</param>
        /// <param name="cancellationToken">取消令牌 / The cancellation token</param>
        /// <returns>多结果集读取包装器 / The multi-result-set reader wrapper</returns>
        Task<GridReaderWrap> ExecuteGridReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);


    }
}
