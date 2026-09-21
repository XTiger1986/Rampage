using Rampage.Data.Filter;
using Rampage.Data.Query;
using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 数据库客户端的异步 API（partial 实现）
    /// Asynchronous API for <see cref="DatabaseClient"/>
    /// </summary>
    /// <remarks>
    /// 事务/连接语义与同步版本一致（计数式嵌套、连接门串行化、默认借还连接）。
    /// 连接与事务为 DbConnection/DbTransaction 时走 OpenAsync/BeginTransactionAsync/CommitAsync 等真异步 API，并全程 ConfigureAwait(false)；否则同步降级。
    /// 返回 DataReaderWrap/GridReaderWrap 的方法：包装对象的 Dispose 回调负责归还连接门与临时连接，调用方必须 using。
    /// Transaction and connection semantics match the synchronous part (count-based nesting, gate serialization, borrow/return connections by default).
    /// DbConnection/DbTransaction instances use the true async APIs (OpenAsync/BeginTransactionAsync/CommitAsync) with ConfigureAwait(false) throughout; other implementations degrade to synchronous calls.
    /// For methods returning DataReaderWrap/GridReaderWrap the wrapper's dispose callback releases the connection gate and any owned connection — the caller must use <c>using</c>.
    /// </remarks>
    public abstract partial class DatabaseClient
    {
        #region Transaction Async

        /// <summary>
        /// 异步开启数据库事务，使用 DefaultIsolationLevel（ReadCommitted）
        /// Asynchronously start a database transaction, using DefaultIsolationLevel (ReadCommitted)
        /// </summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        public virtual Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return BeginTransactionAsync(DefaultIsolationLevel, cancellationToken);
        }

        /// <summary>
        /// 异步开启数据库事务。已有事务时视为加入现有事务（嵌套计数）；仅最外层开启时使用本隔离级别
        /// Asynchronously start a database transaction. When one is active the call joins it (nesting counter); the isolation level applies only to the outermost start
        /// </summary>
        /// <param name="iso">事务隔离级别 / isolation level</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        public virtual async Task BeginTransactionAsync(IsolationLevel iso, CancellationToken cancellationToken = default)
        {
            await _connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Transaction != null)
                {
                    _transactionDepth++;
                    return;
                }

                IDbConnection conn = _externalConnection ?? await EnsureScopeConnectionAsync(cancellationToken).ConfigureAwait(false);
                await EnsureConnectionOpenAsync(conn, cancellationToken).ConfigureAwait(false);

                _transactionConnection = conn;
                if (conn is DbConnection dbConnection)
                {
                    Transaction = await dbConnection.BeginTransactionAsync(iso, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Transaction = conn.BeginTransaction(iso);
                }
                _transactionDepth = 1;
                _transactionRolledBackByInner = false;
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        /// <summary>
        /// 异步提交数据库事务。嵌套语义见 <see cref="CommitTransaction"/>：仅最外层真正提交；内层已回滚时抛出 InvalidOperationException；
        /// 提交失败时尽力回滚（DbTransaction 走 CommitAsync，其余同步降级），事务对象最终都会被释放。
        /// Asynchronously commit the database transaction. See <see cref="CommitTransaction"/> for nesting semantics: only the outermost call commits;
        /// throws InvalidOperationException when an inner scope already rolled back; a failed commit triggers a best-effort rollback (CommitAsync for DbTransaction, synchronous otherwise) and the transaction object is always disposed.
        /// </summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            await _connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Transaction == null)
                {
                    if (_transactionRolledBackByInner)
                    {
                        _transactionRolledBackByInner = false;
                        throw new InvalidOperationException(
                            $"DatabaseClient '{Name}': the transaction was already rolled back by an inner scope; commit is not allowed.");
                    }
                    return;
                }

                if (_transactionDepth > 1)
                {
                    _transactionDepth--;
                    return;
                }

                IDbTransaction transaction = Transaction;
                try
                {
                    if (transaction is DbTransaction dbTransaction)
                    {
                        await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        transaction.Commit();
                    }
                }
                catch
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception rollbackEx)
                    {
                        Log.RampageLogger.Error($"DatabaseClient '{Name}': rollback after failed commit also failed.", rollbackEx);
                    }
                    throw;
                }
                finally
                {
                    transaction.Dispose();
                    ReleaseTransactionConnection();
                }
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        /// <summary>
        /// 异步回滚数据库事务。任意层级调用都会立即回滚整个事务并置回滚标记（外层再提交将抛异常）
        /// Asynchronously roll back the database transaction. A rollback at any nesting level aborts the whole transaction immediately and sets the poisoned flag (a later outer commit throws)
        /// </summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            await _connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Transaction == null)
                {
                    _transactionRolledBackByInner = false;
                    return;
                }

                IDbTransaction transaction = Transaction;
                bool nested = _transactionDepth > 1;
                try
                {
                    if (transaction is DbTransaction dbTransaction)
                    {
                        await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
                finally
                {
                    transaction.Dispose();
                    ReleaseTransactionConnection();
                    _transactionRolledBackByInner = nested;
                }
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        #endregion

        #region Data Access Async

        /// <summary>
        /// 异步获取数据库当前时间
        /// Asynchronously get the current time from the database
        /// </summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据库服务器当前时间 / the current time on the database server</returns>
        public virtual Task<DateTime> GetNowAsync(CancellationToken cancellationToken = default)
        {
            string sql = SqlTemplate.GetNowSql();
            return ExecuteScalarAsync<DateTime>(sql, cancellationToken: cancellationToken)!;
        }

        /// <summary>
        /// 异步执行参数化 SQL（增/删/改或存储过程）
        /// Asynchronously execute parameterized SQL (insert/delete/update or a stored procedure)
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 / the number of rows affected</returns>
        public virtual async Task<int> ExecuteAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步执行返回单值的参数化 SQL
        /// Asynchronously execute parameterized SQL that selects a single value
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>首行首列的值，无结果为 null / the first column of the first row, or null when there is no result</returns>
        public virtual async Task<object?> ExecuteScalarAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步执行返回单值的参数化 SQL 并转换为 T
        /// Asynchronously execute parameterized SQL that selects a single value, converted to T
        /// </summary>
        /// <typeparam name="T">目标值类型 / target value type</typeparam>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>转换后的单值 / the single value converted to T</returns>
        public virtual async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync<T>(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步执行查询并返回读取器包装。调用方必须 using 释放返回的 <see cref="DataReaderWrap"/>，
        /// 其 Dispose 回调负责释放底层读取器、归还连接门与临时连接
        /// Asynchronously execute a query and return a reader wrapper. The caller must dispose the returned
        /// <see cref="DataReaderWrap"/> (e.g. <c>using</c>); its dispose callback releases the underlying reader and returns the connection gate and any owned connection
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>读取器包装 / the reader wrapper</returns>
        public virtual async Task<DataReaderWrap> ExecuteReaderAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithReaderConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteReaderAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                (reader, onDisposed) => reader.AttachDisposeCallback(onDisposed),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步执行 SQL 并以 DataSet 返回结果
        /// Asynchronously execute SQL and return the result as a DataSet
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>填充后的 DataSet / the filled DataSet</returns>
        public virtual async Task<DataSet> ExecuteDataSetAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteDataSetAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步查询并返回动态对象列表（属性与结果列对应）
        /// Asynchronously return a sequence of dynamic objects with properties matching the columns
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>动态对象列表 / the list of dynamic rows</returns>
        public virtual async Task<List<dynamic>> SelectListAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步查询单个动态对象（属性与结果列对应），无结果为 null
        /// Asynchronously return a dynamic object with properties matching the columns, or null when no row matches
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单个动态对象，无匹配为 null / the dynamic row, or null when none</returns>
        public virtual async Task<dynamic?> SelectOneAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步查询并返回类型化为 M 的数据列表
        /// Asynchronously execute a query, returning the data typed as M
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual async Task<List<M>> SelectListAsync<M>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步执行单行查询，返回类型化为 M 的数据，无结果为 null
        /// Asynchronously execute a single-row query, returning the data typed as M, or null when no row matches
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单个数据对象，无匹配为 null / the matched row, or null when none</returns>
        public virtual async Task<M?> SelectOneAsync<M>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步执行返回多个结果集的命令。调用方必须 using 释放返回的 <see cref="GridReaderWrap"/>，
        /// 其 Dispose 回调负责释放底层读取器、归还连接门与临时连接
        /// Asynchronously execute a command that returns multiple result sets. The caller must dispose the returned
        /// <see cref="GridReaderWrap"/>; its dispose callback releases the underlying reader and returns the connection gate and any owned connection
        /// </summary>
        /// <param name="sql">SQL 语句（可含多个结果集）/ SQL statement (may produce multiple result sets)</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>多结果集读取器包装 / the grid reader wrapper</returns>
        public virtual async Task<GridReaderWrap> ExecuteGridReaderAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithReaderConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteGridReaderAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                (grid, onDisposed) => grid.AttachDisposeCallback(onDisposed),
                cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Data Access Plus Async

        /// <summary>
        /// 异步按 LambdaWrapper 查询数据列表
        /// Asynchronously query a data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual async Task<List<M>> SelectListAsync<M>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 LambdaWrapper 查询数据列表，并映射为结果类型 R
        /// Asynchronously query a data list by a LambdaWrapper, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual async Task<List<R>> SelectListAsync<M, R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件查询数据列表
        /// Asynchronously query a data list by a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual async Task<List<M>> SelectListAsync<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件查询数据列表，并映射为结果类型 R
        /// Asynchronously query a data list by a Lambda expression, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual async Task<List<R>> SelectListAsync<M, R>(Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 FilterWrapper 查询数据列表；wrapper 为 null 时查询全部
        /// Asynchronously query a data list by a FilterWrapper; a null wrapper queries all rows
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">过滤包装器，null 表示无条件 / the filter wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual async Task<List<M>> SelectListAsync<M>(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 FilterWrapper 查询数据列表，并映射为结果类型 R；wrapper 为 null 时查询全部
        /// Asynchronously query a data list by a FilterWrapper, mapped to the result type R; a null wrapper queries all rows
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">过滤包装器，null 表示无条件 / the filter wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual async Task<List<R>> SelectListAsync<M, R>(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按数据过滤器查询数据列表
        /// Asynchronously query a data list by a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual async Task<List<M>> SelectListAsync<M>(DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按数据过滤器查询数据列表，并映射为结果类型 R
        /// Asynchronously query a data list by a DataFilter, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual async Task<List<R>> SelectListAsync<M, R>(DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 LambdaWrapper 分页查询数据列表
        /// Asynchronously query a paged data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；RecordCount 未预置时从同批 GridReader 追加的 COUNT 结果集回填（当前实现按列名 "RecordCount" 读取）/ pager info: the offset is (PageIndex-1)*PageSize; when RecordCount is not preset it is filled from the appended COUNT result set of the same grid reader (the current implementation reads the column named "RecordCount")</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表；总行数写入 pagerInfo.RecordCount / the rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>

        public virtual async Task<List<M>> SelectPageAsync<M>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
            {
                using (GridReaderWrap grid = await DataAccessor!.ExecuteGridReaderAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout, ct).ConfigureAwait(false))
                {
                    List<M> dataList = grid.SelectList<M>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>("RecordCount");
                    }
                    return dataList;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 LambdaWrapper 分页查询数据列表，并映射为结果类型 R
        /// Asynchronously query a paged data list by a LambdaWrapper, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；RecordCount 未预置时从同批 GridReader 追加的 COUNT 结果集回填（当前实现按列名 "RecordCount" 读取）/ pager info: the offset is (PageIndex-1)*PageSize; when RecordCount is not preset it is filled from the appended COUNT result set of the same grid reader (the current implementation reads the column named "RecordCount")</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页结果列表；总行数写入 pagerInfo.RecordCount / the mapped rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>

        public virtual async Task<List<R>> SelectPageAsync<M, R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
            {
                using (GridReaderWrap grid = await DataAccessor!.ExecuteGridReaderAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout, ct).ConfigureAwait(false))
                {
                    List<R> dataList = grid.SelectList<R>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>("RecordCount");
                    }
                    return dataList;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件分页查询数据列表
        /// Asynchronously query a paged data list by a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；RecordCount 未预置时从同批 GridReader 追加的 COUNT 结果集回填（当前实现按列名 "RecordCount" 读取）/ pager info: the offset is (PageIndex-1)*PageSize; when RecordCount is not preset it is filled from the appended COUNT result set of the same grid reader (the current implementation reads the column named "RecordCount")</param>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表；总行数写入 pagerInfo.RecordCount / the rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>

        public virtual async Task<List<M>> SelectPageAsync<M>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
            {
                using (GridReaderWrap grid = await DataAccessor!.ExecuteGridReaderAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false))
                {
                    List<M> dataList = grid.SelectList<M>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>("RecordCount");
                    }
                    return dataList;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件分页查询数据列表，并映射为结果类型 R
        /// Asynchronously query a paged data list by a Lambda expression, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；RecordCount 未预置时从同批 GridReader 追加的 COUNT 结果集回填（当前实现按列名 "RecordCount" 读取）/ pager info: the offset is (PageIndex-1)*PageSize; when RecordCount is not preset it is filled from the appended COUNT result set of the same grid reader (the current implementation reads the column named "RecordCount")</param>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页结果列表；总行数写入 pagerInfo.RecordCount / the mapped rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>

        public virtual async Task<List<R>> SelectPageAsync<M, R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
            {
                using (GridReaderWrap grid = await DataAccessor!.ExecuteGridReaderAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false))
                {
                    List<R> dataList = grid.SelectList<R>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>("RecordCount");
                    }
                    return dataList;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 FilterWrapper 分页查询数据列表；wrapper 为 null 时查询全部
        /// Asynchronously query a paged data list by a FilterWrapper; a null wrapper pages all rows
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；RecordCount 未预置时从同批 GridReader 追加的 COUNT 结果集回填（当前实现按列名 "RecordCount" 读取）/ pager info: the offset is (PageIndex-1)*PageSize; when RecordCount is not preset it is filled from the appended COUNT result set of the same grid reader (the current implementation reads the column named "RecordCount")</param>
        /// <param name="wrapper">过滤包装器，null 表示无条件 / the filter wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表；总行数写入 pagerInfo.RecordCount / the rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>

        public virtual async Task<List<M>> SelectPageAsync<M>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
            {
                using (GridReaderWrap grid = await DataAccessor!.ExecuteGridReaderAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout, ct).ConfigureAwait(false))
                {
                    List<M> dataList = grid.SelectList<M>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>("RecordCount");
                    }
                    return dataList;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 FilterWrapper 分页查询数据列表，并映射为结果类型 R；wrapper 为 null 时查询全部
        /// Asynchronously query a paged data list by a FilterWrapper, mapped to the result type R; a null wrapper pages all rows
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；RecordCount 未预置时从同批 GridReader 追加的 COUNT 结果集回填（当前实现按列名 "RecordCount" 读取）/ pager info: the offset is (PageIndex-1)*PageSize; when RecordCount is not preset it is filled from the appended COUNT result set of the same grid reader (the current implementation reads the column named "RecordCount")</param>
        /// <param name="wrapper">过滤包装器，null 表示无条件 / the filter wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页结果列表；总行数写入 pagerInfo.RecordCount / the mapped rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>

        public virtual async Task<List<R>> SelectPageAsync<M, R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
            {
                using (GridReaderWrap grid = await DataAccessor!.ExecuteGridReaderAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout, ct).ConfigureAwait(false))
                {
                    List<R> dataList = grid.SelectList<R>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>("RecordCount");
                    }
                    return dataList;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按数据过滤器分页查询数据列表
        /// Asynchronously query a paged data list by a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；RecordCount 未预置时从同批 GridReader 追加的 COUNT 结果集回填（当前实现按列名 "RecordCount" 读取）/ pager info: the offset is (PageIndex-1)*PageSize; when RecordCount is not preset it is filled from the appended COUNT result set of the same grid reader (the current implementation reads the column named "RecordCount")</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表；总行数写入 pagerInfo.RecordCount / the rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>

        public virtual async Task<List<M>> SelectPageAsync<M>(PagerInfo pagerInfo, DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
            {
                using (GridReaderWrap grid = await DataAccessor!.ExecuteGridReaderAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false))
                {
                    List<M> dataList = grid.SelectList<M>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>("RecordCount");
                    }
                    return dataList;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按数据过滤器分页查询数据列表，并映射为结果类型 R
        /// Asynchronously query a paged data list by a DataFilter, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；RecordCount 未预置时从同批 GridReader 追加的 COUNT 结果集回填（当前实现按列名 "RecordCount" 读取）/ pager info: the offset is (PageIndex-1)*PageSize; when RecordCount is not preset it is filled from the appended COUNT result set of the same grid reader (the current implementation reads the column named "RecordCount")</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页结果列表；总行数写入 pagerInfo.RecordCount / the mapped rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>

        public virtual async Task<List<R>> SelectPageAsync<M, R>(PagerInfo pagerInfo, DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
            {
                using (GridReaderWrap grid = await DataAccessor!.ExecuteGridReaderAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false))
                {
                    List<R> dataList = grid.SelectList<R>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>("RecordCount");
                    }
                    return dataList;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按主键集合查询数据列表
        /// Asynchronously query a data list by a primary key list
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="keyList">主键值集合 / the list of primary key values</param>
        /// <param name="resultFields">返回字段集，null 表示全部字段 / result fields; null selects all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表 / the list of matched rows</returns>
        public virtual async Task<List<M>> SelectByIdsAsync<M>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectByIds<M>(keyList, resultFields);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按主键集合查询数据列表，并映射为结果类型 R
        /// Asynchronously query a data list by a primary key list, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="keyList">主键值集合 / the list of primary key values</param>
        /// <param name="resultFields">返回字段集，null 表示全部字段 / result fields; null selects all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual async Task<List<R>> SelectByIdsAsync<M, R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectByIds<M>(keyList, resultFields);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 LambdaWrapper 查询单条数据（内部会把 wrapper.Limit 强制置为 1）
        /// Asynchronously query one row by a LambdaWrapper (the wrapper's Limit is forced to 1 internally)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">查询包装器（会被修改 Limit）/ the query wrapper (its Limit is mutated)</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual async Task<M?> SelectOneAsync<M>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 LambdaWrapper 查询单条数据，并映射为结果类型 R（内部会把 wrapper.Limit 强制置为 1）
        /// Asynchronously query one row by a LambdaWrapper, mapped to the result type R (the wrapper's Limit is forced to 1 internally)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器（会被修改 Limit）/ the query wrapper (its Limit is mutated)</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
        public virtual async Task<R?> SelectOneAsync<M, R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件查询单条数据（LIMIT 1）
        /// Asynchronously query one row by a Lambda expression (LIMIT 1)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual async Task<M?> SelectOneAsync<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件查询单条数据，并映射为结果类型 R（LIMIT 1）
        /// Asynchronously query one row by a Lambda expression, mapped to the result type R (LIMIT 1)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
        public virtual async Task<R?> SelectOneAsync<M, R>(Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 FilterWrapper 查询单条数据（内部会把 wrapper.Limit 强制置为 1）
        /// Asynchronously query one row by a FilterWrapper (the wrapper's Limit is forced to 1 internally)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">过滤包装器（会被修改 Limit）/ the filter wrapper (its Limit is mutated)</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual async Task<M?> SelectOneAsync<M>(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 FilterWrapper 查询单条数据，并映射为结果类型 R（内部会把 wrapper.Limit 强制置为 1）
        /// Asynchronously query one row by a FilterWrapper, mapped to the result type R (the wrapper's Limit is forced to 1 internally)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">过滤包装器（会被修改 Limit）/ the filter wrapper (its Limit is mutated)</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
        public virtual async Task<R?> SelectOneAsync<M, R>(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按数据过滤器查询单条数据（LIMIT 1）
        /// Asynchronously query one row by a DataFilter (LIMIT 1)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual async Task<M?> SelectOneAsync<M>(DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按数据过滤器查询单条数据，并映射为结果类型 R（LIMIT 1）
        /// Asynchronously query one row by a DataFilter, mapped to the result type R (LIMIT 1)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
        public virtual async Task<R?> SelectOneAsync<M, R>(DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按主键查询单条数据
        /// Asynchronously query one row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段集，null 表示全部字段 / result fields; null selects all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual async Task<M?> SelectByIdAsync<M>(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectById<M>(key, resultFields);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按主键查询单条数据，并映射为结果类型 R
        /// Asynchronously query one row by primary key, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段集，null 表示全部字段 / result fields; null selects all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
        public virtual async Task<R?> SelectByIdAsync<M, R>(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectById<M>(key, resultFields);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步插入单条数据；有自增列时自增主键值会自动回写到实体属性
        /// Asynchronously insert one row; the auto-increment key is written back to the entity's mapping property automatically
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据 / the entity data</param>
        /// <param name="insertId">是否显式插入自增主键（默认 false，由数据库生成）/ whether to insert the auto-increment primary key explicitly (default false, generated by the database)</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>无自增列时受影响行数 &gt; 0 返回 true；有自增列时成功回填主键即返回 true / effects &gt; 0 for tables without an auto-increment column; true once the generated key has been written back for auto-increment tables</returns>
        /// <remarks>
        /// 无自增列且 SQL 未预置参数时，直接把实体对象本身作为 Dapper 参数传入；
        /// 有自增列时改用 ExecuteScalar 取回生成的主键并回写（类型不一致时自动转换）。
        /// When the table has no auto-increment column and parameters were not pre-built, the entity object itself is passed as the Dapper parameter;
        /// with an auto-increment column, ExecuteScalar retrieves the generated key and writes it back (converting automatically when types differ).
        /// </remarks>
        public virtual async Task<bool> InsertAsync<M>(M data, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfInsert<M>(data, insertId);
            if (!sqlInfo.IsAutoIncrement)
            {
                // 未预置参数时直接把实体对象作为 Dapper 参数传入 / pass the entity itself as the Dapper parameter when parameters were not pre-built
                int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                    await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                return effects > 0;
            }
            else
            {
                object? dbIdValue = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                    await DataAccessor!.ExecuteScalarAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                object? id;
                Type idType = dbIdValue!.GetType();
                Type idMappingType = sqlInfo.AutoIncrementInfo!.PropertyType;
                if (!idType.Equals(idMappingType))
                {
                    StringTool.TryParse(dbIdValue.ToString()!, idMappingType, out id);
                }
                else
                {
                    id = dbIdValue;
                }
                EmitTool.SetPropertyValue(data, sqlInfo.AutoIncrementInfo!.PropertyName!, id);
                return true;
            }
        }

        /// <summary>
        /// 异步按主键更新整条数据（未指定字段集时更新全部字段）
        /// Asynchronously update one row by primary key (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据（主键取自该实体）/ the entity data (its primary key is used)</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual async Task<bool> UpdateByIdAsync<M>(M data, QueryFields? updateFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(data, updateFields);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按主键更新部分字段
        /// Asynchronously update some fields of a row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateByIdAsync<M>(object id, UpdateFields updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按主键更新部分字段（强类型字段表达式版本）
        /// Asynchronously update some fields of a row by primary key (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateByIdAsync<M>(object id, UpdateFields<M> updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按主键更新单个字段
        /// Asynchronously update a single field of a row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateByIdAsync<M, F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按主键更新两个字段
        /// Asynchronously update two fields of a row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateByIdAsync<M, F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按主键更新三个字段
        /// Asynchronously update three fields of a row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <typeparam name="F3">第三个字段类型 / third field type</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="field3">第三个字段选择表达式 / the selector of field 3</param>
        /// <param name="field3Value">第三个字段新值 / the new value of field 3</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateByIdAsync<M, F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件更新数据（未指定字段集时更新全部字段）
        /// Asynchronously update rows matching a LambdaWrapper (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据（提供字段值）/ the entity data providing field values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual async Task<bool> UpdateAsync<M>(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件更新部分字段
        /// Asynchronously update some fields of rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件更新部分字段（强类型字段表达式版本）
        /// Asynchronously update some fields of rows matching a LambdaWrapper (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件更新单个字段
        /// Asynchronously update a single field of rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件更新两个字段
        /// Asynchronously update two fields of rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件更新三个字段
        /// Asynchronously update three fields of rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <typeparam name="F3">第三个字段类型 / third field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="field3">第三个字段选择表达式 / the selector of field 3</param>
        /// <param name="field3Value">第三个字段新值 / the new value of field 3</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F1, F2, F3>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件更新数据（未指定字段集时更新全部字段）
        /// Asynchronously update rows matching a Lambda expression (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据（提供字段值）/ the entity data providing field values</param>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual async Task<bool> UpdateAsync<M>(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件更新部分字段
        /// Asynchronously update some fields of rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件更新部分字段（强类型字段表达式版本）
        /// Asynchronously update some fields of rows matching a Lambda expression (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件更新单个字段
        /// Asynchronously update a single field of rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件更新两个字段
        /// Asynchronously update two fields of rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件更新三个字段
        /// Asynchronously update three fields of rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <typeparam name="F3">第三个字段类型 / third field type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="field3">第三个字段选择表达式 / the selector of field 3</param>
        /// <param name="field3Value">第三个字段新值 / the new value of field 3</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件更新数据（未指定字段集时更新全部字段）
        /// Asynchronously update rows matching a FilterWrapper (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据（提供字段值）/ the entity data providing field values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual async Task<bool> UpdateAsync<M>(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件更新部分字段
        /// Asynchronously update some fields of rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件更新部分字段（强类型字段表达式版本）
        /// Asynchronously update some fields of rows matching a FilterWrapper (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields<M> updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件更新单个字段
        /// Asynchronously update a single field of rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件更新两个字段
        /// Asynchronously update two fields of rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件更新三个字段
        /// Asynchronously update three fields of rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <typeparam name="F3">第三个字段类型 / third field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="field3">第三个字段选择表达式 / the selector of field 3</param>
        /// <param name="field3Value">第三个字段新值 / the new value of field 3</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按数据过滤器条件更新数据（未指定字段集时更新全部字段）
        /// Asynchronously update rows matching a DataFilter (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据（提供字段值）/ the entity data providing field values</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual async Task<bool> UpdateAsync<M>(M data, DataFilter filter, QueryFields? updateFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按数据过滤器条件更新部分字段
        /// Asynchronously update some fields of rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields updateFields, DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按数据过滤器条件更新部分字段（强类型字段表达式版本）
        /// Asynchronously update some fields of rows matching a DataFilter (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields<M> updateFields, DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按数据过滤器条件更新单个字段
        /// Asynchronously update a single field of rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按数据过滤器条件更新两个字段
        /// Asynchronously update two fields of rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按数据过滤器条件更新三个字段
        /// Asynchronously update three fields of rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <typeparam name="F3">第三个字段类型 / third field type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="field3">第三个字段选择表达式 / the selector of field 3</param>
        /// <param name="field3Value">第三个字段新值 / the new value of field 3</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> UpdateAsync<M, F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步插入或更新单条数据：主键有值（数值 &gt; 0 或非空字符串）视为更新，否则视为插入
        /// Asynchronously insert or update one row: an entity whose primary key has a value (number &gt; 0 or non-empty string) is updated, otherwise inserted
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据 / the entity data</param>
        /// <param name="updateFields">走更新路径时待更新的字段集，null 表示全部字段 / fields to update on the update path; null updates all fields</param>
        /// <param name="insertId">走插入路径时是否显式插入自增主键（默认 false）/ whether to insert the auto-increment primary key explicitly on the insert path (default false)</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实际执行的插入/更新是否影响了行（或有自增主键回填成功）/ whether the executed insert/update affected rows (or wrote back an auto-increment key)</returns>
        public virtual async Task<bool> InsertOrUpdateAsync<M>(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string primaryKey = mapping.PrimaryKey!;
            object? pkValue = EmitTool.GetPropertyValue(data, primaryKey);
            bool isUpdate = false;
            if (pkValue != null)
            {
                if (GenericTool.IsNumber(pkValue) && Convert.ToDecimal(pkValue) > 0)
                {
                    isUpdate = true;
                }
                else if (pkValue.GetType() == typeof(string) && !string.IsNullOrEmpty(pkValue.ToString()))
                {
                    isUpdate = true;
                }
            }

            if (isUpdate)
            {
                return await UpdateByIdAsync<M>(data, updateFields, commandTimeout, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await InsertAsync<M>(data, insertId, commandTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 异步分批插入数据列表
        /// Asynchronously insert a data list in batches
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dataList">实体数据列表 / the list of entities</param>
        /// <param name="batchSize">单条命令批量大小 / batch size of one execute command</param>
        /// <param name="insertId">是否显式插入自增主键（默认 false）/ whether to insert the auto-increment primary key explicitly (default false)</param>
        /// <param name="batchHandler">每批执行后的回调，返回 false 终止后续批次 / callback after each batch; returning false stops further batches</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>累计受影响行数 &gt; 0 返回 true；输入为空集合返回 false / true when the total affected rows &gt; 0; false for an empty input list</returns>
        public virtual async Task<bool> InsertBatchAsync<M>(IEnumerable<M> dataList, int batchSize = 500, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            if (!dataList.Any())
            {
                return false;
            }
            int effects = 0;

            List<M> toInsertList = dataList.ToList();
            int count = toInsertList.Count;
            for (int i = 0; i < count; i += batchSize)
            {
                int start = i;
                int end = i + batchSize - 1;
                if (end > count - 1)
                {
                    end = count - 1;
                }
                List<M> batchDataList = toInsertList.GetRange(start, end - start + 1);
                SqlInfo sqlInfo = SqlTemplate.BuildSqlOfInsert<M>(batchDataList, insertId);
                int effs = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                    await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                effects += effs;

                if (batchHandler != null)
                {
                    BatchDataOperateState<M> stateInfo = new BatchDataOperateState<M>
                    {
                        DataList = batchDataList,
                        BatchIndex = i,
                        Effects = effs
                    };
                    bool isContinue = batchHandler(stateInfo);
                    if (!isContinue)
                    {
                        break;
                    }
                }
            }
            return effects > 0;
        }

        /// <summary>
        /// 异步按主键分批更新数据列表
        /// Asynchronously update a data list in batches by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dataList">实体数据列表 / the list of entities</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="batchSize">单条命令批量大小 / batch size of one execute command</param>
        /// <param name="batchHandler">每批执行后的回调，返回 false 终止后续批次 / callback after each batch; returning false stops further batches</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>累计受影响行数 &gt; 0 返回 true；输入为空集合返回 false / true when the total affected rows &gt; 0; false for an empty input list</returns>
        public virtual async Task<bool> UpdateBatchAsync<M>(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 500, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            if (!dataList.Any())
            {
                return false;
            }
            int effects = 0;

            List<M> toUpdateList = dataList.ToList();
            int count = toUpdateList.Count;
            for (int i = 0; i < count; i += batchSize)
            {
                int start = i;
                int end = i + batchSize - 1;
                if (end > count - 1)
                {
                    end = count - 1;
                }
                List<M> batchDataList = toUpdateList.GetRange(start, end - start + 1);
                SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(batchDataList, updateFields);
                int effs = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                    await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                effects += effs;

                if (batchHandler != null)
                {
                    BatchDataOperateState<M> stateInfo = new BatchDataOperateState<M>
                    {
                        DataList = batchDataList,
                        BatchIndex = i,
                        Effects = effs
                    };
                    bool isContinue = batchHandler(stateInfo);
                    if (!isContinue)
                    {
                        break;
                    }
                }
            }
            return effects > 0;
        }

        /// <summary>
        /// 异步分批插入或更新数据列表：先按主键是否有值拆分为待插入/待更新两组，再分别批量执行
        /// Asynchronously insert or update a data list in batches: rows are first split by primary-key presence into insert/update groups, then executed batch by batch
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dataList">实体数据列表 / the list of entities</param>
        /// <param name="updateFields">走更新路径时待更新的字段集，null 表示全部字段 / fields to update on the update path; null updates all fields</param>
        /// <param name="batchSize">单条命令批量大小 / batch size of one execute command</param>
        /// <param name="insertId">走插入路径时是否显式插入自增主键（默认 false）/ whether to insert the auto-increment primary key explicitly on the insert path (default false)</param>
        /// <param name="batchHandler">每批执行后的回调（插入与更新阶段各自触发），返回 false 终止该阶段后续批次 / callback after each batch (fired for both phases); returning false stops further batches of that phase</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>任一阶段影响行数 &gt; 0 返回 true；输入为空集合返回 false / true when either phase affected rows; false for an empty input list</returns>
        public virtual async Task<bool> InsertOrUpdateBatchAsync<M>(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 500, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            bool result = false;

            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string primaryKey = mapping.PrimaryKey!;
            List<M> toInsertList = new List<M>();
            List<M> toUpdateList = new List<M>();

            foreach (M item in dataList)
            {
                bool isUpdate = false;
                object? pkValue = EmitTool.GetPropertyValue(item, primaryKey);
                if (pkValue != null && GenericTool.IsNumber(pkValue) && Convert.ToDecimal(pkValue) > 0)
                {
                    isUpdate = true;
                }
                else if (pkValue != null && pkValue.GetType() == typeof(string) && !string.IsNullOrEmpty(pkValue.ToString()))
                {
                    isUpdate = true;
                }

                if (isUpdate)
                {
                    toUpdateList.Add(item);
                }
                else
                {
                    toInsertList.Add(item);
                }
            }

            if (toInsertList.Any())
            {
                result = result || await InsertBatchAsync<M>(toInsertList, batchSize, insertId, batchHandler, commandTimeout, cancellationToken).ConfigureAwait(false);
            }
            if (toUpdateList.Any())
            {
                result = result || await UpdateBatchAsync<M>(toUpdateList, updateFields, batchSize, batchHandler, commandTimeout, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// 异步按主键删除单条数据
        /// Asynchronously delete one row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> DeleteByIdAsync<M>(object key, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(key);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件删除数据（删除所有满足条件的行）
        /// Asynchronously delete all rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> DeleteAsync<M>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件删除数据（删除所有满足条件的行）
        /// Asynchronously delete all rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> DeleteAsync<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件删除数据（删除所有满足条件的行）
        /// Asynchronously delete all rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> DeleteAsync<M>(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按数据过滤器条件删除数据（删除所有满足条件的行）
        /// Asynchronously delete all rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> DeleteAsync<M>(DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按主键集合批量删除数据
        /// Asynchronously delete rows in bulk by a primary key list
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="keyList">主键值集合 / the list of primary key values</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual async Task<bool> DeleteByIdsAsync<M>(IEnumerable keyList, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(keyList);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件统计行数
        /// Asynchronously count rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>满足条件的行数 / the number of matching rows</returns>
        public virtual async Task<long> CountAsync<M>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 Lambda 表达式条件统计行数
        /// Asynchronously count rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>满足条件的行数 / the number of matching rows</returns>
        public virtual async Task<long> CountAsync<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件统计行数；wrapper 为 null 时统计全表
        /// Asynchronously count rows matching a FilterWrapper; a null wrapper counts the whole table
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器，null 表示无条件 / the condition wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>满足条件的行数 / the number of matching rows</returns>
        public virtual async Task<long> CountAsync<M>(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper != null ? wrapper.CommandTimeout : null, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步按数据过滤器条件统计行数
        /// Asynchronously count rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>满足条件的行数 / the number of matching rows</returns>
        public virtual async Task<long> CountAsync<M>(DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
