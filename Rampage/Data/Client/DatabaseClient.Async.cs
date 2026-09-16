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
    /// Asynchronous API for <see cref="DatabaseClient"/>.
    /// </summary>
    public abstract partial class DatabaseClient
    {
        #region Transaction Async

        /// <summary>
        /// Asynchronously start a database transaction, using DefaultIsolationLevel (ReadCommitted).
        /// </summary>
        public virtual Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return BeginTransactionAsync(DefaultIsolationLevel, cancellationToken);
        }

        /// <summary>
        /// Asynchronously start a database transaction.
        /// </summary>
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
        /// Asynchronously commit the database transaction. See <see cref="CommitTransaction"/> for nesting semantics.
        /// </summary>
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
        /// Asynchronously rollback the database transaction. See <see cref="RollbackTransaction"/> for nesting semantics.
        /// </summary>
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
        /// Asynchronously get the current time from the database.
        /// </summary>
        public virtual Task<DateTime> GetNowAsync(CancellationToken cancellationToken = default)
        {
            string sql = SqlTemplate.GetNowSql();
            return ExecuteScalarAsync<DateTime>(sql, cancellationToken: cancellationToken)!;
        }

        /// <summary>
        /// Asynchronously execute parameterized SQL.
        /// </summary>
        public virtual async Task<int> ExecuteAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously execute parameterized SQL that selects a single value.
        /// </summary>
        public virtual async Task<object?> ExecuteScalarAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously execute parameterized SQL that selects a single value.
        /// </summary>
        public virtual async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync<T>(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously execute a query and return a reader wrapper. The caller must dispose the returned
        /// <see cref="DataReaderWrap"/> (e.g. <c>using</c>) to release the underlying reader and connection resources.
        /// </summary>
        public virtual async Task<DataReaderWrap> ExecuteReaderAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithReaderConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteReaderAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                (reader, onDisposed) => reader.AttachDisposeCallback(onDisposed),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously execute SQL and return a DataSet.
        /// </summary>
        public virtual async Task<DataSet> ExecuteDataSetAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteDataSetAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously return a sequence of dynamic objects with properties matching the columns.
        /// </summary>
        public virtual async Task<List<dynamic>> SelectListAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously return a dynamic object with properties matching the columns.
        /// </summary>
        public virtual async Task<dynamic?> SelectOneAsync(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a query, returning the data typed as T.
        /// </summary>
        public virtual async Task<List<M>> SelectListAsync<M>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a single-row query, returning the data typed as T.
        /// </summary>
        public virtual async Task<M?> SelectOneAsync<M>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
            where M : class
        {
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sql, param, trans, commandType, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously execute a command that returns multiple result sets.
        /// </summary>
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
        /// Asynchronously query data list by a LambdaWrapper
        /// </summary>
        public virtual async Task<List<M>> SelectListAsync<M>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously query data list by a LambdaWrapper
        /// </summary>
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
        /// Asynchronously query data list by a Lambda Expression
        /// </summary>
        public virtual async Task<List<M>> SelectListAsync<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously query data list by a Lambda Expression
        /// </summary>
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
        /// Asynchronously query data list by a FilterWrapper
        /// </summary>
        public virtual async Task<List<M>> SelectListAsync<M>(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously query data list by a FilterWrapper
        /// </summary>
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
        /// Asynchronously query data list by a Data Filter
        /// </summary>
        public virtual async Task<List<M>> SelectListAsync<M>(DataFilter filter, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously query data list by a Data Filter
        /// </summary>
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
        /// Asynchronously query page data list by a LambdaWrapper
        /// </summary>

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
        /// Asynchronously query page data list by a LambdaWrapper
        /// </summary>

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
        /// Asynchronously query page data list by a Lambda Expression
        /// </summary>

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
        /// Asynchronously query page data list by a Lambda Expression
        /// </summary>

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
        /// Asynchronously query page data list by a FilterWrapper
        /// </summary>

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
        /// Asynchronously query page data list by a FilterWrapper
        /// </summary>

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
        /// Asynchronously query page data list by a Data Filter
        /// </summary>

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
        /// Asynchronously query page data list by a Data Filter
        /// </summary>

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
        /// Asynchronously query data list by primary key list
        /// </summary>
        public virtual async Task<List<M>> SelectByIdsAsync<M>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectByIds<M>(keyList, resultFields);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectListAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously query data list by primary key list
        /// </summary>
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
        /// Asynchronously query one data by a LambdaWrapper
        /// </summary>
        public virtual async Task<M?> SelectOneAsync<M>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously query one data by a LambdaWrapper
        /// </summary>
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
        /// Asynchronously query one data by a Lambda Expression
        /// </summary>
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
        /// Asynchronously query one data by a Lambda Expression
        /// </summary>
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
        /// Asynchronously query one data by a FilterWrapper
        /// </summary>
        public virtual async Task<M?> SelectOneAsync<M>(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously query one data by a FilterWrapper
        /// </summary>
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
        /// Asynchronously query one data by a Data Filter
        /// </summary>
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
        /// Asynchronously query one data by a Data Filter
        /// </summary>
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
        /// Asynchronously query one data by primary key
        /// </summary>
        public virtual async Task<M?> SelectByIdAsync<M>(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectById<M>(key, resultFields);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.SelectOneAsync<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously query one data by primary key
        /// </summary>
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
        /// Asynchronously insert one data; autoincrement column's mapping property value will be set automatically
        /// </summary>
        public virtual async Task<bool> InsertAsync<M>(M data, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfInsert<M>(data, insertId);
            if (!sqlInfo.IsAutoIncrement)
            {
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
        /// Asynchronously update one data by primary key
        /// </summary>
        public virtual async Task<bool> UpdateByIdAsync<M>(M data, QueryFields? updateFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(data, updateFields);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously update some fields of data by primary key
        /// </summary>
        public virtual async Task<bool> UpdateByIdAsync<M>(object id, UpdateFields updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously update some fields of data by primary key
        /// </summary>
        public virtual async Task<bool> UpdateByIdAsync<M>(object id, UpdateFields<M> updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously update single field of data by primary key
        /// </summary>
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
        /// Asynchronously update two fields of data by primary key
        /// </summary>
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
        /// Asynchronously update three fields of data by primary key
        /// </summary>
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
        /// Asynchronously update one data by a LambdaWrapper
        /// </summary>
        public virtual async Task<bool> UpdateAsync<M>(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously update some fields of data by a LambdaWrapper
        /// </summary>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously update some fields of data by a LambdaWrapper
        /// </summary>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously update single field of data by a LambdaWrapper
        /// </summary>
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
        /// Asynchronously update two fields of data by a LambdaWrapper
        /// </summary>
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
        /// Asynchronously update three fields of data by a LambdaWrapper
        /// </summary>
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
        /// Asynchronously update one data by a Lambda Expression
        /// </summary>
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
        /// Asynchronously update one data by a Lambda Expression
        /// </summary>
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
        /// Asynchronously update one data by a Lambda Expression
        /// </summary>
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
        /// Asynchronously update single field of data by a Lambda Expression
        /// </summary>
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
        /// Asynchronously update two fields of data by a Lambda Expression
        /// </summary>
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
        /// Asynchronously update three fields of data by a Lambda Expression
        /// </summary>
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
        /// Asynchronously update one data by a FilterWrapper
        /// </summary>
        public virtual async Task<bool> UpdateAsync<M>(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously update some fields of data by a FilterWrapper
        /// </summary>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously update some fields of data by a FilterWrapper
        /// </summary>
        public virtual async Task<bool> UpdateAsync<M>(UpdateFields<M> updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously update single field of data by a FilterWrapper
        /// </summary>
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
        /// Asynchronously update two fields of data by a FilterWrapper
        /// </summary>
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
        /// Asynchronously update three fields of data by a FilterWrapper
        /// </summary>
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
        /// Asynchronously update one data by a Data Filter
        /// </summary>
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
        /// Asynchronously update one data by a Data Filter
        /// </summary>
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
        /// Asynchronously update one data by a Data Filter
        /// </summary>
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
        /// Asynchronously update single field by a Data Filter
        /// </summary>
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
        /// Asynchronously update two fields by a Data Filter
        /// </summary>
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
        /// Asynchronously update three fields by a Data Filter
        /// </summary>
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
        /// Asynchronously insert or update one data (update data if primary key has value, or else insert data)
        /// </summary>
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
        /// Asynchronously insert batch data list
        /// </summary>
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
        /// Asynchronously update batch data list by primary key
        /// </summary>
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
        /// Asynchronously insert or update batch data list (update data if primary key has value, or else insert data)
        /// </summary>
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
        /// Asynchronously delete one data by primary key
        /// </summary>
        public virtual async Task<bool> DeleteByIdAsync<M>(object key, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(key);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously delete data by a LambdaWrapper
        /// </summary>
        public virtual async Task<bool> DeleteAsync<M>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously delete data by a Lambda Expression
        /// </summary>
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
        /// Asynchronously delete data by a FilterWrapper
        /// </summary>
        public virtual async Task<bool> DeleteAsync<M>(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously delete data by a Data Filter
        /// </summary>
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
        /// Asynchronously delete batch data list by primary key
        /// </summary>
        public virtual async Task<bool> DeleteByIdsAsync<M>(IEnumerable keyList, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(keyList);
            int effects = await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteAsync(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return effects > 0;
        }

        /// <summary>
        /// Asynchronously count data by a LambdaWrapper
        /// </summary>
        public virtual async Task<long> CountAsync<M>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously count data by a Lambda Expression
        /// </summary>
        public virtual async Task<long> CountAsync<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously count data by a FilterWrapper
        /// </summary>
        public virtual async Task<long> CountAsync<M>(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return await ExecuteWithConnectionAsync(async (conn, trans, ct) =>
                await DataAccessor!.ExecuteScalarAsync<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper != null ? wrapper.CommandTimeout : null, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously count data by a Data Filter
        /// </summary>
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
