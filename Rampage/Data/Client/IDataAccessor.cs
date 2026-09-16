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
    public interface IDataAccessor
    {


        /// <summary>
        /// 执行SQL语句
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="param">参数</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">超时秒数</param>
        /// <param name="commandType">语句类型</param>
        /// <returns>影响行数</returns>
        int Execute(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句
        /// </summary>
        Task<int> ExecuteAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到数据集
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="param">参数</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandType">语句类型</param>
        /// <param name="commandTimeout">超时秒数</param>
        /// <returns>数据集</returns>
        DataSet ExecuteDataSet(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到数据集
        /// </summary>
        Task<DataSet> ExecuteDataSetAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到单值
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="transaction">事务</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时秒数</param>
        /// <param name="commandType">语句类型</param>
        /// <returns>单值</returns>
        object? ExecuteScalar(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到单值
        /// </summary>
        Task<object?> ExecuteScalarAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到单值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">数据库连接</param>
        /// <param name="transaction">事务</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时秒数</param>
        /// <param name="commandType">语句类型</param>
        /// <returns>T类型单值</returns>
        T? ExecuteScalar<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到单值
        /// </summary>
        Task<T?> ExecuteScalarAsync<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到DataReader对象包装器
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="transaction">事务</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时秒数</param>
        /// <param name="commandType">语句类型</param>
        /// <returns></returns>
        DataReaderWrap ExecuteReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到DataReader对象包装器
        /// </summary>
        Task<DataReaderWrap> ExecuteReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到动态类型数据列表
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="param">参数</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandType">语句类型</param>
        /// <param name="commandTimeout">超时秒数</param>
        /// <returns></returns>
        List<dynamic> SelectList(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到动态类型数据列表
        /// </summary>
        Task<List<dynamic>> SelectListAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到一条动态类型数据
        /// </summary>
        /// <param name="conn">数据库连接</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="param">参数</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandType">语句类型</param>
        /// <param name="commandTimeout">超时秒数</param>
        /// <returns></returns>
        dynamic? SelectOne(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// 异步执行SQL语句得到一条动态类型数据
        /// </summary>
        Task<dynamic?> SelectOneAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行SQL语句得到指定类型数据列表
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="commandType"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        List<M> SelectList<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class;

        /// <summary>
        /// 异步执行SQL语句得到指定类型数据列表
        /// </summary>
        Task<List<M>> SelectListAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class;

        /// <summary>
        /// 执行SQL语句得到一条指定类型数据
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="commandType"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        M? SelectOne<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class;

        /// <summary>
        /// 异步执行SQL语句得到一条指定类型数据
        /// </summary>
        Task<M?> SelectOneAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class;

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <param name="commandType"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        GridReaderWrap ExecuteGridReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null);

        /// <summary>
        /// Asynchronously execute a command that returns multiple result sets.
        /// </summary>
        Task<GridReaderWrap> ExecuteGridReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default);


    }
}
