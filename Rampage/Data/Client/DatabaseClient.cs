using Rampage.Data.Filter;
using Rampage.Data.Query;
using Rampage.ExceptionExtensions;
using Rampage.Log;
using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace Rampage.Data.Client
{

    /// <summary>
    /// 数据库客户端
    /// Database Client
    /// </summary>
    public abstract partial class DatabaseClient : IDisposable
    {
        /// <summary>
        /// 数据库客户端依赖注入服务的名称前缀
        /// service name prefix in DependencyInjection
        /// </summary>
        public static readonly string SERVICE_NAME_PREFIX = "Rampage.DatabaseClient_";
        public static readonly string RECORD_COUNT_RESULT_COLUMN = "_record_count_";

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public DatabaseClient()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn">
        /// 自定义数据库连接
        /// Custom database connection
        /// </param>
        public DatabaseClient(IDbConnection conn)
        {
            _externalConnection = conn;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connString">
        /// 自定义数据库连接串
        /// Custom database connection string
        /// </param>
        public DatabaseClient(string connString)
        {
            ConnectionString = connString;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 数据库类型
        /// database type
        /// </summary>
        public abstract DatabaseType DatabaseType { get; }

        /// <summary>
        /// 指定数据库标识名称
        /// specify database name
        /// </summary>
        public string? Name
        {
            get;
            protected set;
        }

        private string? _connectionString;
        /// <summary>
        /// 数据库连接串
        /// database connection string
        /// </summary>
        public string? ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    if (!string.IsNullOrEmpty(Name))
                    {
                        _connectionString = DatabaseFactory.Config!.Clients![Name].ConnectionString;
                    }
                }
                return _connectionString;
            }
            internal set
            {
                _connectionString = value;
            }
        }

        private IDbConnection? _externalConnection;
        private IDbConnection? _scopeConnection;
        private IDbConnection? _transactionConnection;
        private readonly SemaphoreSlim _connectionGate = new SemaphoreSlim(1, 1);
        private static readonly TimeSpan ConnectionGateTimeout = TimeSpan.FromSeconds(30);

        private void EnterConnectionGate()
        {
            if (!_connectionGate.Wait(ConnectionGateTimeout))
            {
                throw new TimeoutException(
                    $"DatabaseClient '{Name}' connection gate wait exceeded {ConnectionGateTimeout.TotalSeconds:0}s.");
            }
        }

        /// <summary>
        /// 当前 Scope 内复用的数据库连接；外部注入连接优先；事务期间与 Scope 连接相同。
        /// </summary>
        public IDbConnection? Connection
        {
            get
            {
                if (_transactionConnection != null)
                {
                    return _transactionConnection;
                }
                if (_externalConnection != null)
                {
                    return _externalConnection;
                }
                return EnsureScopeConnection();
            }
            protected set
            {
                _externalConnection = value;
            }
        }

        private IDbTransaction? _transaction;
        /// <summary>
        /// 当前的事务库事务
        /// current database transaction
        /// </summary>
        public IDbTransaction? Transaction
        {
            get
            {
                return _transaction;
            }
            protected set
            {
                _transaction = value;
            }
        }

        /// <summary>
        /// 创建数据库连接的委托函数
        /// Delegate function for creating a database connection
        /// </summary>
        public abstract DatabaseConnector? ConnectionCreator
        {
            get;
        }

        /// <summary>
        /// SQL语句构建器
        /// SQL Statement Builder
        /// </summary>
        public virtual SqlTemplate SqlTemplate
        {
            get;
            protected set;
        } = default!;

        protected static readonly ConcurrentDictionary<string, IDataAccessor?> DataAccessorContainer = new ConcurrentDictionary<string, IDataAccessor?>();

        protected IDataAccessor? _dataAccessor = null;
        /// <summary>
        /// 数据库访问器
        /// Database Accessor
        /// </summary>
        public virtual IDataAccessor? DataAccessor
        {
            get
            {
                if (_dataAccessor == null)
                {
                    if (!string.IsNullOrEmpty(Name))
                    {
                        if (!DataAccessorContainer.TryGetValue(Name, out IDataAccessor? accessor))
                        {
                            Type? dataAccessType = DatabaseFactory.GetDataAccessType(Name);
                            if (dataAccessType != null)
                            {
                                accessor = (IDataAccessor)EmitTool.CreateModel(dataAccessType)!;
                            }
                            DataAccessorContainer.TryAdd(Name, accessor);
                        }
                        _dataAccessor = accessor;
                    }
                }
                if (_dataAccessor == null)
                {
                    _dataAccessor = SetDefaultDataAccessor(_dataAccessor);
                }
                return _dataAccessor;
            }
            set
            {
                _dataAccessor = value;
            }
        }

        #endregion

        #region Transaction

        /// <summary>
        /// 默认事务隔离级别（原为 ReadUncommitted，存在脏读风险且各数据库语义不一致）
        /// Default isolation level (was ReadUncommitted: dirty reads and inconsistent semantics across databases)
        /// </summary>
        public const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;

        /// <summary>
        /// 事务嵌套深度：外层 BeginTransaction 为 1，内层每次 +1；只有回到 0 才真正提交。
        /// Nesting depth of BeginTransaction calls; only the outermost Commit actually commits.
        /// </summary>
        private int _transactionDepth;

        /// <summary>
        /// 内层作用域已回滚，外层再提交时必须失败而不是静默成功。
        /// Set when an inner scope rolled back; the outer Commit must fail instead of silently succeeding.
        /// </summary>
        private bool _transactionRolledBackByInner;

        /// <summary>
        /// 当前事务嵌套深度（0 表示无事务）
        /// Current transaction nesting depth (0 = no active transaction)
        /// </summary>
        public int TransactionDepth => _transactionDepth;

        /// <summary>
        /// 开启数据库事务，使用 DefaultIsolationLevel（ReadCommitted）
        /// Start a database transaction, using DefaultIsolationLevel (ReadCommitted)
        /// </summary>
        /// <returns></returns>
        public virtual void BeginTransaction()
        {
            BeginTransaction(DefaultIsolationLevel);
        }

        /// <summary>
        /// 开启数据库事务。已有事务时视为加入现有事务（嵌套计数），不再静默忽略。
        /// Start a database transaction. When one is active the call joins it (nesting counter) instead of being silently ignored.
        /// </summary>
        /// <param name="iso">Isolation Level</param>
        /// <returns></returns>
        public virtual void BeginTransaction(IsolationLevel iso)
        {
            EnterConnectionGate();
            try
            {
                if (Transaction != null)
                {
                    _transactionDepth++;
                    return;
                }

                IDbConnection conn = _externalConnection ?? EnsureScopeConnection();
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                _transactionConnection = conn;
                Transaction = conn.BeginTransaction(iso);
                _transactionDepth = 1;
                _transactionRolledBackByInner = false;
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        /// <summary>
        /// Clear active transaction state without closing the scope connection.
        /// </summary>
        protected virtual void ReleaseTransactionConnection()
        {
            Transaction = null;
            _transactionConnection = null;
            _transactionDepth = 0;
        }

        /// <summary>
        /// 提交数据库事务。嵌套场景下只有最外层调用真正提交；内层已回滚时抛出异常。
        /// 无论 Commit 是否成功，事务对象都会被释放，连接不会残留在事务状态。
        /// Commit the transaction. Only the outermost call commits; throws if an inner scope already rolled back.
        /// The transaction object is always disposed, so the connection never stays in a dangling transaction state.
        /// </summary>
        public virtual void CommitTransaction()
        {
            EnterConnectionGate();
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
                    transaction.Commit();
                }
                catch
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception rollbackEx)
                    {
                        RampageLogger.Error($"DatabaseClient '{Name}': rollback after failed commit also failed.", rollbackEx);
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
        /// 回滚数据库事务。任意层级调用都会立即回滚整个事务。
        /// Rollback the transaction. A rollback at any nesting level rolls back the whole transaction immediately.
        /// </summary>
        public virtual void RollbackTransaction()
        {
            EnterConnectionGate();
            try
            {
                if (Transaction == null)
                {
                    // 外层在 catch 中回滚，内层已回滚过：清除标记，正常返回
                    _transactionRolledBackByInner = false;
                    return;
                }

                IDbTransaction transaction = Transaction;
                bool nested = _transactionDepth > 1;
                try
                {
                    transaction.Rollback();
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

        #region Functions

        /// <summary>
        /// 设置默认的数据库访问器
        /// Set the default database accessor
        /// </summary>
        /// <param name="dataAccessor"></param>
        protected abstract IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor);

        /// <summary>
        /// Create a new database connection instance.
        /// </summary>
        protected virtual IDbConnection CreateNewConnection()
        {
            if (ConnectionCreator == null)
            {
                throw new SuperException(Globals.EX_KEY_INVALID_ARGUMENT, nameof(ConnectionCreator));
            }
            return ConnectionCreator.CreateConnection();
        }

        private bool? _keepScopeConnection;
        /// <summary>
        /// 是否在整个 Scope（通常是一次 HTTP 请求）内独占一条连接。
        /// false（默认）：无事务时每个操作从连接池借一条连接、用完立即归还，连接占用时间从"整个请求"缩短到"单条 SQL"，
        ///   多实例集群下显著降低对 max_connections 的压力；
        /// true：沿用旧行为，Scope 内复用同一连接（Sqlite 默认 true，:memory: 数据库依赖连接常开）。
        /// 配置：DatabaseFactory:KeepScopeConnection（全局）或 Clients[i].KeepScopeConnection（单库）。
        /// Whether one connection is held for the whole scope (legacy) or a pooled connection is borrowed per operation (default).
        /// </summary>
        protected virtual bool KeepScopeConnection
        {
            get
            {
                if (_keepScopeConnection == null)
                {
                    bool? value = null;
                    if (!string.IsNullOrEmpty(Name))
                    {
                        DatabaseFactoryConfig? config = DatabaseFactory.Config;
                        if (config?.Clients != null && config.Clients.TryGetValue(Name, out DatabaseClientConfig? clientConfig))
                        {
                            value = clientConfig.KeepScopeConnection;
                        }
                        value ??= config?.KeepScopeConnection;
                    }
                    value ??= DatabaseType == DatabaseType.Sqlite;
                    _keepScopeConnection = value.Value;
                }
                return _keepScopeConnection.Value;
            }
        }

        /// <summary>
        /// Get or create the scope-bound connection for this client instance.
        /// </summary>
        protected virtual IDbConnection EnsureScopeConnection()
        {
            if (_scopeConnection == null)
            {
                _scopeConnection = CreateNewConnection();
            }
            if (_scopeConnection.State != ConnectionState.Open)
            {
                _scopeConnection.Open();
            }
            return _scopeConnection;
        }

        /// <summary>
        /// Resolve the connection used by the current operation (transaction / external / scope connection).
        /// </summary>
        protected virtual IDbConnection GetOperationConnection()
        {
            if (_transactionConnection != null)
            {
                return _transactionConnection;
            }
            if (_externalConnection != null)
            {
                if (_externalConnection.State != ConnectionState.Open)
                {
                    _externalConnection.Open();
                }
                return _externalConnection;
            }
            return EnsureScopeConnection();
        }

        /// <summary>
        /// Resolve the connection for one operation. When <paramref name="ownedConnection"/> is non-null the caller
        /// must dispose it once the operation (including any open reader) has completed.
        /// </summary>
        protected virtual IDbConnection AcquireOperationConnection(out IDbConnection? ownedConnection)
        {
            ownedConnection = null;
            if (_transactionConnection != null || _externalConnection != null || KeepScopeConnection)
            {
                return GetOperationConnection();
            }

            IDbConnection conn = CreateNewConnection();
            try
            {
                conn.Open();
            }
            catch
            {
                conn.Dispose();
                throw;
            }
            ownedConnection = conn;
            return conn;
        }

        /// <summary>
        /// Async variant of <see cref="AcquireOperationConnection"/>.
        /// </summary>
        protected virtual async Task<(IDbConnection Connection, IDbConnection? Owned)> AcquireOperationConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_transactionConnection != null || _externalConnection != null || KeepScopeConnection)
            {
                IDbConnection shared = await GetOperationConnectionAsync(cancellationToken).ConfigureAwait(false);
                return (shared, null);
            }

            IDbConnection conn = CreateNewConnection();
            try
            {
                await EnsureConnectionOpenAsync(conn, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                conn.Dispose();
                throw;
            }
            return (conn, conn);
        }

        /// <summary>
        /// Execute an action on the operation connection. The connection gate is held for the duration of the action.
        /// </summary>
        protected T ExecuteWithConnection<T>(Func<IDbConnection, IDbTransaction?, T> action)
        {
            EnterConnectionGate();
            IDbConnection? owned = null;
            try
            {
                IDbConnection conn = AcquireOperationConnection(out owned);
                return action(conn, Transaction);
            }
            finally
            {
                owned?.Dispose();
                _connectionGate.Release();
            }
        }

        /// <summary>
        /// Execute an action that returns a reader-like disposable. The connection gate (and any per-operation
        /// connection) is held until the returned object is disposed, so no other operation can interleave
        /// on the same connection while the reader is still open.
        /// </summary>
        protected T ExecuteWithReaderConnection<T>(Func<IDbConnection, IDbTransaction?, T> action, Action<T, Action> attachDisposeCallback)
            where T : class, IDisposable
        {
            EnterConnectionGate();
            IDbConnection? owned = null;
            bool handedOff = false;
            try
            {
                IDbConnection conn = AcquireOperationConnection(out owned);
                T reader = action(conn, Transaction);
                IDbConnection? ownedForCallback = owned;
                attachDisposeCallback(reader, () =>
                {
                    ownedForCallback?.Dispose();
                    _connectionGate.Release();
                });
                handedOff = true;
                return reader;
            }
            finally
            {
                if (!handedOff)
                {
                    owned?.Dispose();
                    _connectionGate.Release();
                }
            }
        }

        /// <summary>
        /// Asynchronously execute an action on the operation connection.
        /// </summary>
        protected async Task<T> ExecuteWithConnectionAsync<T>(Func<IDbConnection, IDbTransaction?, CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        {
            await _connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            IDbConnection? owned = null;
            try
            {
                (IDbConnection conn, owned) = await AcquireOperationConnectionAsync(cancellationToken).ConfigureAwait(false);
                return await action(conn, Transaction, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                owned?.Dispose();
                _connectionGate.Release();
            }
        }

        /// <summary>
        /// Async variant of <see cref="ExecuteWithReaderConnection{T}"/>.
        /// </summary>
        protected async Task<T> ExecuteWithReaderConnectionAsync<T>(Func<IDbConnection, IDbTransaction?, CancellationToken, Task<T>> action, Action<T, Action> attachDisposeCallback, CancellationToken cancellationToken = default)
            where T : class, IDisposable
        {
            await _connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            IDbConnection? owned = null;
            bool handedOff = false;
            try
            {
                (IDbConnection conn, owned) = await AcquireOperationConnectionAsync(cancellationToken).ConfigureAwait(false);
                T reader = await action(conn, Transaction, cancellationToken).ConfigureAwait(false);
                IDbConnection? ownedForCallback = owned;
                attachDisposeCallback(reader, () =>
                {
                    ownedForCallback?.Dispose();
                    _connectionGate.Release();
                });
                handedOff = true;
                return reader;
            }
            finally
            {
                if (!handedOff)
                {
                    owned?.Dispose();
                    _connectionGate.Release();
                }
            }
        }

        /// <summary>
        /// Open the operation connection asynchronously when it is a <see cref="DbConnection"/>.
        /// </summary>
        protected virtual async Task<IDbConnection> GetOperationConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_transactionConnection != null)
            {
                return _transactionConnection;
            }
            if (_externalConnection != null)
            {
                await EnsureConnectionOpenAsync(_externalConnection, cancellationToken).ConfigureAwait(false);
                return _externalConnection;
            }
            return await EnsureScopeConnectionAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get or create the scope-bound connection asynchronously.
        /// </summary>
        protected virtual async Task<IDbConnection> EnsureScopeConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_scopeConnection == null)
            {
                _scopeConnection = CreateNewConnection();
            }
            await EnsureConnectionOpenAsync(_scopeConnection, cancellationToken).ConfigureAwait(false);
            return _scopeConnection;
        }

        /// <summary>
        /// Open a connection asynchronously when possible; otherwise fall back to synchronous Open.
        /// </summary>
        protected static async Task EnsureConnectionOpenAsync(IDbConnection conn, CancellationToken cancellationToken = default)
        {
            if (conn.State == ConnectionState.Open)
            {
                return;
            }

            if (conn is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                conn.Open();
            }
        }

        /// <summary>
        /// 得到格式化后的指定查询字段集的SQL语句
        /// SQL statement for the specified query field set after formatting
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="fields"></param>
        /// <returns></returns>
        public string FormatFields<M>(QueryFields? fields = null)
            where M : class
        {
            return SqlTemplate.BuildQueryFieldsString<M>(fields);
        }

        /// <summary>
        /// 得到格式化后的数据字段名
        /// Get the formatted data field name
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public string FormatColumn(string column)
        {
            return SqlTemplate.FormatColumn(column);
        }

        /// <summary>
        /// 得到格式化后的数据表名
        /// Get the formatted table name
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string FormatTable(string tableName)
        {
            return SqlTemplate.FormatTable(tableName);
        }

        /// <summary>
        /// 得到格式化后的数据参数名
        /// Get the formatted data parameter name
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string FormatParameter(string fieldName)
        {
            return SqlTemplate.FormatParameter(fieldName);
        }

        /// <summary>
        /// 获取数据库当前时间
        /// Get the current time from the database
        /// </summary>
        /// <returns></returns>
        public virtual DateTime GetNow()
        {
            string sql = SqlTemplate.GetNowSql();
            return ExecuteScalar<DateTime>(sql);
        }

        #endregion

        #region Data Access

        /// <summary>
        /// Execute parameterized SQL.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns>The number of rows affected.</returns>
        public virtual int Execute(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sql, param, trans, commandType, commandTimeout));
        }
     
        /// <summary>
        /// Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        public virtual object? ExecuteScalar(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        public virtual T? ExecuteScalar<T>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<T>(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// Execute a query and return a reader wrapper. The caller must dispose the returned
        /// <see cref="DataReaderWrap"/> (e.g. <c>using</c>) to release the underlying reader and connection resources.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public virtual DataReaderWrap ExecuteReader(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithReaderConnection(
                (conn, trans) => DataAccessor!.ExecuteReader(conn, sql, param, trans, commandType, commandTimeout),
                (reader, onDisposed) => reader.AttachDisposeCallback(onDisposed));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandType"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual DataSet ExecuteDataSet(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteDataSet(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// Return a sequence of dynamic objects with properties matching the columns.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public virtual List<dynamic> SelectList(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<dynamic>(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// Return a dynamic object with properties matching the columns.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public virtual dynamic? SelectOne(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// Executes a query, returning the data typed as T.
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        public virtual List<M> SelectList<M>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
            where M : class
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// Executes a single-row query, returning the data typed as T.
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        public virtual M? SelectOne<M>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
            where M : class
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public virtual GridReaderWrap ExecuteGridReader(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithReaderConnection(
                (conn, trans) => DataAccessor!.ExecuteGridReader(conn, sql, param, trans, commandType, commandTimeout),
                (grid, onDisposed) => grid.AttachDisposeCallback(onDisposed));
        }

        #endregion

        #region Data Access Plus

        /// <summary>
        /// query data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual List<M> SelectList<M>(LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// query data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual List<R> SelectList<M, R>(LambdaWrapper<M> wrapper) 
            where M : class 
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// query data list by a Lambda Expression
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="condition"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<M> SelectList<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query data list by a Lambda Expression
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="condition"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<R> SelectList<M, R>(Expression<Func<M, bool>> condition, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query data list by a FilterWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual List<M> SelectList<M>(FilterWrapper<M>? wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout));
        }

        /// <summary>
        /// query data list by a FilterWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual List<R> SelectList<M, R>(FilterWrapper<M>? wrapper) 
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout));
        }

        /// <summary>
        /// query data list by a Data Filter
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="filter"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<M> SelectList<M>(DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query data list by a Data Filter
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="filter"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<R> SelectList<M, R>(DataFilter filter, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query page data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual List<M> SelectPage<M>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return ExecuteWithConnection((conn, trans) =>
            {
                using (GridReaderWrap grid = DataAccessor!.ExecuteGridReader(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout))
                {
                    List<M> dataList = grid.SelectList<M>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>(RECORD_COUNT_RESULT_COLUMN);
                    }
                    return dataList;
                }
            });
        }

        /// <summary>
        /// query page data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual List<R> SelectPage<M, R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper)
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return ExecuteWithConnection((conn, trans) =>
            {
                using (GridReaderWrap grid = DataAccessor!.ExecuteGridReader(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout))
                {
                    List<R> dataList = grid.SelectList<R>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>(RECORD_COUNT_RESULT_COLUMN);
                    }
                    return dataList;
                }
            });
        }

        /// <summary>
        /// query page data list by a Lambda Expression
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="condition"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<M> SelectPage<M>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper <M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return ExecuteWithConnection((conn, trans) =>
            {
                using (GridReaderWrap grid = DataAccessor!.ExecuteGridReader(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout))
                {
                    List<M> dataList = grid.SelectList<M>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>(RECORD_COUNT_RESULT_COLUMN);
                    }
                    return dataList;
                }
            });
        }

        /// <summary>
        /// query page data list by a Lambda Expression
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="condition"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<R> SelectPage<M, R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, int? commandTimeout = null)
            where M : class
            where R : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return ExecuteWithConnection((conn, trans) =>
            {
                using (GridReaderWrap grid = DataAccessor!.ExecuteGridReader(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout))
                {
                    List<R> dataList = grid.SelectList<R>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>(RECORD_COUNT_RESULT_COLUMN);
                    }
                    return dataList;
                }
            });
        }

        /// <summary>
        /// query page data list by a FilterWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual List<M> SelectPage<M>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return ExecuteWithConnection((conn, trans) =>
            {
                using (GridReaderWrap grid = DataAccessor!.ExecuteGridReader(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout))
                {
                    List<M> dataList = grid.SelectList<M>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>(RECORD_COUNT_RESULT_COLUMN);
                    }
                    return dataList;
                }
            });
        }

        /// <summary>
        /// query page data list by a FilterWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual List<R> SelectPage<M, R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper)
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return ExecuteWithConnection((conn, trans) =>
            {
                using (GridReaderWrap grid = DataAccessor!.ExecuteGridReader(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout))
                {
                    List<R> dataList = grid.SelectList<R>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>(RECORD_COUNT_RESULT_COLUMN);
                    }
                    return dataList;
                }
            });
        }

        /// <summary>
        /// query page data list by a Data Filter
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="filter"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<M> SelectPage<M>(PagerInfo pagerInfo, DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return ExecuteWithConnection((conn, trans) =>
            {
                using (GridReaderWrap grid = DataAccessor!.ExecuteGridReader(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout))
                {
                    List<M> dataList = grid.SelectList<M>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>(RECORD_COUNT_RESULT_COLUMN);
                    }
                    return dataList;
                }
            });
        }

        /// <summary>
        /// query page data list by a Data Filter
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="filter"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<R> SelectPage<M, R>(PagerInfo pagerInfo, DataFilter filter, int? commandTimeout = null)
            where M : class
            where R : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper, pagerInfo);
            return ExecuteWithConnection((conn, trans) =>
            {
                using (GridReaderWrap grid = DataAccessor!.ExecuteGridReader(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout))
                {
                    List<R> dataList = grid.SelectList<R>();
                    if (pagerInfo.RecordCount == null)
                    {
                        pagerInfo.RecordCount = grid.ExecuteScalar<long>(RECORD_COUNT_RESULT_COLUMN);
                    }
                    return dataList;
                }
            });
        }

        /// <summary>
        /// query data list by primary key list
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="keyList"></param>
        /// <param name="resultFields"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<M> SelectByIds<M>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectByIds<M>(keyList, resultFields);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query data list by primary key list
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="keyList"></param>
        /// <param name="resultFields"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual List<R> SelectByIds<M, R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectByIds<M>(keyList, resultFields);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query one data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual M? SelectOne<M>(LambdaWrapper<M> wrapper) where M : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// query one data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual R? SelectOne<M, R>(LambdaWrapper<M> wrapper) 
            where M : class
            where R : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// query one data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="condition"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual M? SelectOne<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query one data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="condition"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual R? SelectOne<M, R>(Expression<Func<M, bool>> condition, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query one data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual M? SelectOne<M>(FilterWrapper<M> wrapper) where M : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// query one data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual R? SelectOne<M, R>(FilterWrapper<M> wrapper) 
            where M : class
            where R : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// query one data by a Data Filter
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="filter"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual M? SelectOne<M>(DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query one data by a Data Filter
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="filter"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual R? SelectOne<M, R>(DataFilter filter, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query one data by primary key
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="key"></param>
        /// <param name="resultFields"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual M? SelectById<M>(object key, QueryFields? resultFields = null, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectById<M>(key, resultFields);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// query one data by primary key
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="key"></param>
        /// <param name="resultFields"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual R? SelectById<M, R>(object key, QueryFields? resultFields = null, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectById<M>(key, resultFields);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// insert one data, autoincrement column's mapping property value will be set automatically
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="data"></param>
        /// <param name="insertId">Whether to customise the auto-increment primary key for insert (default false)</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Insert<M>(M data, bool insertId = false, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfInsert<M>(data, insertId);
            if (!sqlInfo.IsAutoIncrement)
            {
                int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
                return effects > 0;
            }
            else
            {
                object? dbIdValue = ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
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
        /// update one data by primary key
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool UpdateById<M>(M data, QueryFields? updateFields = null, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(data, updateFields);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update some fields of data by primary key
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="updateFields"></param>
        /// <param name="id"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool UpdateById<M>(object id, UpdateFields updateFields, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update some fields of data by primary key
        /// </summary>
        /// <typeparam name="M">model-table mapping type</typeparam>
        /// <param name="updateFields"></param>
        /// <param name="id"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool UpdateById<M>(object id, UpdateFields<M> updateFields, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update single field of data by primary key
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="fieldValue"></param>
        /// <param name="updateField"></param>
        /// <param name="id"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool UpdateById<M, F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update two fields of data by primary key
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <param name="field1Value"></param>
        /// <param name="field1"></param>
        /// <param name="field2Value"></param>
        /// <param name="field2"></param>
        /// <param name="id"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool UpdateById<M, F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value,  int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update three fields of data by primary key
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <typeparam name="F3"></typeparam>
        /// <param name="field1Value"></param>
        /// <param name="field1"></param>
        /// <param name="field2Value"></param>
        /// <param name="field2"></param>
        /// <param name="field3Value"></param>
        /// <param name="field3"></param>
        /// <param name="id"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool UpdateById<M, F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update one data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        public virtual bool Update<M>(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update some fields of data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="updateFields"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M>(UpdateFields updateFields, LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update some fields of data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="updateFields"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M>(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update single field of data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="updateField"></param>
        /// <param name="fieldValue"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M, F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update two fields of data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <param name="field1"></param>
        /// <param name="field1Value"></param>
        /// <param name="field2"></param>
        /// <param name="field2Value"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M, F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update two fields of data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <typeparam name="F3"></typeparam>
        /// <param name="field1"></param>
        /// <param name="field1Value"></param>
        /// <param name="field2"></param>
        /// <param name="field2Value"></param>
        /// <param name="field3"></param>
        /// <param name="field3Value"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M, F1, F2, F3>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update one data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="condition"></param>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M>(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update one data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="condition"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M>(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update one data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="condition"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M>(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update single field of data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="condition"></param>
        /// <param name="updateField"></param>
        /// <param name="fieldValue"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M, F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update two fields of data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <param name="condition"></param>
        /// <param name="field1"></param>
        /// <param name="field1Value"></param>
        /// <param name="field2"></param>
        /// <param name="field2Value"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M, F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update two fields of data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <typeparam name="F3"></typeparam>
        /// <param name="condition"></param>
        /// <param name="field1"></param>
        /// <param name="field1Value"></param>
        /// <param name="field2"></param>
        /// <param name="field2Value"></param>
        /// <param name="field3"></param>
        /// <param name="field3Value"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M, F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update one data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        public virtual bool Update<M>(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update some fields of data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="updateFields"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M>(UpdateFields updateFields, FilterWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        ///  update some fields of data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="updateFields"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M>(UpdateFields<M> updateFields, FilterWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update single field of data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="updateField"></param>
        /// <param name="fieldValue"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M, F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update two fields of data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <param name="field1"></param>
        /// <param name="field1Value"></param>
        /// <param name="field2"></param>
        /// <param name="field2Value"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M, F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update three fields of data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <typeparam name="F3"></typeparam>
        /// <param name="field1"></param>
        /// <param name="field1Value"></param>
        /// <param name="field2"></param>
        /// <param name="field2Value"></param>
        /// <param name="field3"></param>
        /// <param name="field3Value"></param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Update<M, F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update one data by a Data Filter
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="filter"></param>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M>(M data, DataFilter filter, QueryFields? updateFields = null, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
           int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update one data by a Data Filter
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="filter"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M>(UpdateFields updateFields, DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update one data by a Data Filter
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="filter"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M>(UpdateFields<M> updateFields, DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update single field by a Data Filter
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F"></typeparam>
        /// <param name="filter"></param>
        /// <param name="updateField"></param>
        /// <param name="fieldValue"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M, F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update two field by a Data Filter
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <param name="filter"></param>
        /// <param name="field1"></param>
        /// <param name="field1Value"></param>
        /// <param name="field2"></param>
        /// <param name="field2Value"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M, F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// update two field by a Data Filter
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="F1"></typeparam>
        /// <typeparam name="F2"></typeparam>
        /// <typeparam name="F3"></typeparam>
        /// <param name="filter"></param>
        /// <param name="field1"></param>
        /// <param name="field1Value"></param>
        /// <param name="field2"></param>
        /// <param name="field2Value"></param>
        /// <param name="field3"></param>
        /// <param name="field3Value"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool Update<M, F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            updateFields.Set(field3, field3Value);
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// insert or update one data (update data if primary key has value, or else insert data)
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool InsertOrUpdate<M>(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null) where M : class
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
                return UpdateById<M>(data, updateFields, commandTimeout);
            }
            else
            {
                return Insert<M>(data, insertId, commandTimeout);
            }
        }

        /// <summary>
        /// insert batch data list
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="dataList"></param>
        /// <param name="batchSize">batch size of one execute command</param>
        /// <param name="insertId">
        /// 指定是否插入自增长主键 
        /// Specify whether to force the insertion of an auto-increment primary key
        /// </param>
        /// <param name="batchHandler">Callback function for each batch of data operation</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool InsertBatch<M>(IEnumerable<M> dataList, int batchSize = 500, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null) where M : class
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
                int effs = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
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
        /// update batch data list by primary key
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="dataList"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="batchSize">batch size of one execute command</param>
        /// <param name="batchHandler">Callback function for each batch of data operation</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool UpdateBatch<M>(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 500, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null) where M : class
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
                int effs = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
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
        /// insert or update batch data list (update data if primary key has value, or else insert data)
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="dataList"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="batchSize">batch size of one execute command</param>
        /// <param name="batchHandler">Callback function for each batch of data operation</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool InsertOrUpdateBatch<M>(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 500, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null) where M : class
        {
            bool result = false;

            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            string primaryKey = mapping.PrimaryKey!;
            List<M> toInsertList = new List<M>();
            List<M> toUpdateList = new List<M>();

            foreach(M item in dataList)
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
                result = result || InsertBatch<M>(toInsertList, batchSize, insertId, batchHandler, commandTimeout);
            }
            if (toUpdateList.Any())
            {
                result = result || UpdateBatch<M>(toUpdateList, updateFields, batchSize, batchHandler, commandTimeout);
            }

            return result;
        }

        /// <summary>
        /// delete one data by primary key
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="key"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool DeleteById<M>(object key, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(key);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// delete one data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Delete<M>(LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// delete one data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="condition"></param>
        /// <returns></returns>
        public virtual bool Delete<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// delete one data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual bool Delete<M>(FilterWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// delete one data by a Data Filter
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual bool Delete<M>(DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// delete batch data list by primary key
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="keyList"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual bool DeleteByIds<M>(IEnumerable keyList, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(keyList);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// count data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual long Count<M>(LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// count data by a Lambda Expression
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="condition"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public virtual long Count<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// count data by a FilterWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public virtual long Count<M>(FilterWrapper<M>? wrapper = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper != null ? wrapper.CommandTimeout : null));
        }

        /// <summary>
        /// count data by a Data Filter
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="filter"></param>
        /// <param name="commandTimeout"></param>
        /// 
        /// <returns></returns>
        public virtual long Count<M>(DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        #endregion

        #region Schema

        /// <summary>
        /// 获取表结构列表
        /// Get the list of table structures
        /// </summary>
        /// <param name="tableNames"></param>
        /// <returns></returns>
        public virtual List<DbTableInfo> GetTableInfos(IList<string>? tableNames = null)
        {
            List<DbTableInfo> tableInfos;

            switch (DatabaseType)
            {
                case DatabaseType.MySQL:
                    tableInfos = GetTableInfos_MySql();
                    break;
                case DatabaseType.PostgreSQL:
                    tableInfos = GetTableInfos_PostgreSQL();
                    break;
                case DatabaseType.Oracle:
                    tableInfos = GetTableInfos_Oracle();
                    break;
                case DatabaseType.SqlServer:
                    tableInfos = GetTableInfos_SqlServer();
                    break;
                case DatabaseType.Sqlite:
                    tableInfos = GetTableInfos_SQLite();
                    break;
                default:
                    throw new NotSupportedException();
            }

            return tableInfos;
        }

        private List<DbTableInfo> GetTableInfos_MySql(IList<string>? tableNames = null)
        {
            List<DbTableInfo> tableInfos = new List<DbTableInfo>();
            try
            {
                string tableSql = "SHOW TABLE STATUS;";
                DataSet tableDs = ExecuteDataSet(tableSql);
                if (tableDs.Tables.Count == 0 || tableDs.Tables[0].Rows.Count == 0)
                {
                    return tableInfos;
                }

                foreach (DataRow dr in tableDs.Tables[0].Rows)
                {
                    string? tableName = dr["Name"].ToString();
                    string? comment = dr["Comment"].ToString();

                    List<DbColumnInfo> columns = new List<DbColumnInfo>();
                    string columnSql = $"SHOW FULL COLUMNS FROM `{tableName!.Replace("`", "``")}`;";
                    DataSet columnDs = ExecuteDataSet(columnSql);
                    if (columnDs.Tables.Count > 0 && columnDs.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow colDr in columnDs.Tables[0].Rows)
                        {
                            string fullType = colDr["Type"].ToString()!;
                            int typeLeftBracketIndex = fullType.LastIndexOf("(");
                            int typeRightBracketIndex = fullType.LastIndexOf(")");
                            bool typeHasBracket = typeLeftBracketIndex > 0 && typeRightBracketIndex > 0;
                            string dataType = typeHasBracket ? fullType.Substring(0, typeLeftBracketIndex) : fullType;
                            long dataLen = typeHasBracket ? Convert.ToInt64(fullType.Substring(typeLeftBracketIndex + 1, typeRightBracketIndex - typeLeftBracketIndex - 1)) : 0;

                            string colKey = colDr["KEY"] == DBNull.Value ? string.Empty : colDr["KEY"].ToString()!;
                            string colExtra = colDr["Extra"] == DBNull.Value ? string.Empty : colDr["Extra"].ToString()!;

                            DbColumnInfo columnInfo = new DbColumnInfo
                            {
                                ColumnName = colDr["Field"].ToString(),
                                DataType = dataType,
                                CharacterLength = 0,
                                IsNullable = !StringTool.EqualsIgnoreCase(colDr["Null"].ToString(), "NO"),
                                PrimaryKey = colKey.Contains("PRI"),
                                AutoIncrement = colExtra.Contains("auto_increment"),
                                Comment = colDr["Comment"].ToString()
                            };
                            columns.Add(columnInfo);
                        }
                    }

                    DbTableInfo tableInfo = new DbTableInfo
                    {
                        TableName = tableName,
                        Comment = comment,
                        Columns = columns
                    };

                    tableInfos.Add(tableInfo);
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error("Insufficient permission to execute MySQL's commands", ex);
            }

            return tableInfos;
        }

        private List<DbTableInfo> GetTableInfos_PostgreSQL(IList<string>? tableNames = null)
        {
            List<DbTableInfo> tableInfos = new List<DbTableInfo>();
            try
            {
                string tableSql = @"
            SELECT 
                table_name AS ""Name"",
                obj_description(('""' || table_schema || '"".""' || table_name || '""')::regclass) AS ""Comment""
                    FROM information_schema.tables
            WHERE table_schema = 'public'
                      AND table_type = 'BASE TABLE'
                    ORDER BY table_name; ";

                DataSet tableDs = ExecuteDataSet(tableSql);
                if (tableDs.Tables.Count == 0 || tableDs.Tables[0].Rows.Count == 0)
                {
                    return tableInfos;
                }

                foreach (DataRow dr in tableDs.Tables[0].Rows)
                {
                    string? tableName = dr["Name"].ToString();
                    string? comment = dr["Comment"].ToString();

                    List<DbColumnInfo> columns = new List<DbColumnInfo>();

                    string columnSql = $@"
                SELECT
                    c.column_name AS ""Field"",
                    c.data_type AS ""Type"",
                    c.is_nullable AS ""Null"",
                    CASE WHEN pk.column_name IS NOT NULL THEN 'PRI' ELSE '' END AS ""KEY"",
                    CASE WHEN c.is_identity = 'YES' THEN 'auto_increment' ELSE '' END AS ""Extra"",
                    col_description(('public.""' || c.table_name || '""')::regclass, c.ordinal_position) AS ""Comment"",
                    c.character_maximum_length
                FROM information_schema.columns c
                LEFT JOIN (
                    SELECT ku.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage ku
                        ON tc.constraint_name = ku.constraint_name
                    WHERE tc.table_name = '{tableName!.Replace("'", "''")}'
                      AND tc.constraint_type = 'PRIMARY KEY'
                ) pk ON c.column_name = pk.column_name
                WHERE c.table_schema = 'public'
                  AND c.table_name = '{tableName!.Replace("'", "''")}'
                ORDER BY c.ordinal_position;";

                    DataSet columnDs = ExecuteDataSet(columnSql);
                    if (columnDs.Tables.Count > 0 && columnDs.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow colDr in columnDs.Tables[0].Rows)
                        {
                            string fullType = colDr["Type"].ToString()!;
                            string dataType = fullType;
                            long dataLen = 0;

                            if (colDr["character_maximum_length"] != DBNull.Value)
                            {
                                dataLen = Convert.ToInt64(colDr["character_maximum_length"]);
                            }

                            string colKey = colDr["KEY"] == DBNull.Value ? string.Empty : colDr["KEY"].ToString()!;
                            string colExtra = colDr["Extra"] == DBNull.Value ? string.Empty : colDr["Extra"].ToString()!;

                            DbColumnInfo columnInfo = new DbColumnInfo
                            {
                                ColumnName = colDr["Field"].ToString(),
                                DataType = dataType,
                                CharacterLength = dataLen,
                                IsNullable = !StringTool.EqualsIgnoreCase(colDr["Null"].ToString(), "NO"),
                                PrimaryKey = colKey.Contains("PRI"),
                                AutoIncrement = colExtra.Contains("auto_increment"),
                                Comment = colDr["Comment"].ToString()
                            };
                            columns.Add(columnInfo);
                        }
                    }

                    DbTableInfo tableInfo = new DbTableInfo
                    {
                        TableName = tableName,
                        Comment = comment,
                        Columns = columns
                    };

                    tableInfos.Add(tableInfo);
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error("Insufficient permission to execute PostgreSQL's commands", ex);
            }

            return tableInfos;
        }

        private List<DbTableInfo> GetTableInfos_SqlServer(IList<string>? tableNames = null)
        {
            List<DbTableInfo> tableInfos = new List<DbTableInfo>();
            try
            {
                string tableSql = @"
            SELECT 
                t.TABLE_NAME AS TableName,
                ISNULL(CAST(p.value AS NVARCHAR(500)), '') AS TableComment
            FROM INFORMATION_SCHEMA.TABLES t
            LEFT JOIN sys.extended_properties p 
                ON p.major_id = OBJECT_ID(t.TABLE_SCHEMA + '.' + t.TABLE_NAME) 
                AND p.minor_id = 0 
                AND p.name = 'MS_Description'
            WHERE t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY t.TABLE_NAME;";

                DataSet tableDs = ExecuteDataSet(tableSql);
                if (tableDs.Tables.Count == 0 || tableDs.Tables[0].Rows.Count == 0)
                {
                    return tableInfos;
                }

                foreach (DataRow dr in tableDs.Tables[0].Rows)
                {
                    string tableName = dr["TableName"].ToString()!;
                    string? tableComment = dr["TableComment"].ToString();

                    List<DbColumnInfo> columns = new List<DbColumnInfo>();

                    string columnSql = $@"
                SELECT 
                    c.name AS ColumnName,
                    ty.name AS DataType,
                    c.max_length AS CharacterLength,
                    c.is_nullable AS IsNullable,
                    CASE WHEN ic.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
                    c.is_identity AS IsAutoIncrement,
                    ISNULL(CAST(ep.value AS NVARCHAR(500)), '') AS ColumnComment
                FROM sys.columns c
                INNER JOIN sys.tables t ON c.object_id = t.object_id
                INNER JOIN sys.types ty ON c.system_type_id = ty.system_type_id AND c.user_type_id = ty.user_type_id
                LEFT JOIN sys.index_columns ic 
                    ON ic.object_id = t.object_id 
                    AND ic.column_id = c.column_id 
                    AND ic.is_included_column = 0
                LEFT JOIN sys.indexes i 
                    ON ic.object_id = i.object_id 
                    AND ic.index_id = i.index_id 
                    AND i.is_primary_key = 1
                LEFT JOIN sys.extended_properties ep 
                    ON ep.major_id = t.object_id 
                    AND ep.minor_id = c.column_id 
                    AND ep.name = 'MS_Description'
                WHERE t.name = '{tableName.Replace("'", "''")}'
                ORDER BY c.column_id;";

                    DataSet columnDs = ExecuteDataSet(columnSql);
                    if (columnDs.Tables.Count > 0 && columnDs.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow colDr in columnDs.Tables[0].Rows)
                        {
                            string columnName = colDr["ColumnName"].ToString()!;
                            string dataType = colDr["DataType"].ToString()!;
                            long dataLen = 0;

                            if (colDr["CharacterLength"] != DBNull.Value)
                            {
                                int len = Convert.ToInt32(colDr["CharacterLength"]);
                                if (string.Equals(dataType, "nvarchar", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(dataType, "nchar", StringComparison.OrdinalIgnoreCase))
                                {
                                    dataLen = len < 0 ? 0 : len / 2;
                                }
                                else
                                {
                                    dataLen = len < 0 ? 0 : len;
                                }
                            }

                            bool isNullable = Convert.ToBoolean(colDr["IsNullable"]);
                            bool isPrimaryKey = Convert.ToInt32(colDr["IsPrimaryKey"]) == 1;
                            bool isAutoIncrement = Convert.ToBoolean(colDr["IsAutoIncrement"]);
                            string columnComment = colDr["ColumnComment"].ToString() ?? string.Empty;

                            DbColumnInfo columnInfo = new DbColumnInfo
                            {
                                ColumnName = columnName,
                                DataType = dataType,
                                CharacterLength = dataLen,
                                IsNullable = isNullable,
                                PrimaryKey = isPrimaryKey,
                                AutoIncrement = isAutoIncrement,
                                Comment = columnComment
                            };
                            columns.Add(columnInfo);
                        }
                    }

                    DbTableInfo tableInfo = new DbTableInfo
                    {
                        TableName = tableName,
                        Comment = tableComment,
                        Columns = columns
                    };

                    tableInfos.Add(tableInfo);
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error("Failed to get SQL Server table structure", ex);
            }

            return tableInfos;
        }

        private List<DbTableInfo> GetTableInfos_SQLite(IList<string>? tableNames = null)
        {
            List<DbTableInfo> tableInfos = new List<DbTableInfo>();
            try
            {
                string tableSql = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
                DataSet tableDs = ExecuteDataSet(tableSql);
                if (tableDs.Tables.Count == 0 || tableDs.Tables[0].Rows.Count == 0)
                {
                    return tableInfos;
                }

                foreach (DataRow dr in tableDs.Tables[0].Rows)
                {
                    string? tableName = dr["name"].ToString();
                    if (string.IsNullOrEmpty(tableName)) continue;

                    string? comment = string.Empty;

                    List<DbColumnInfo> columns = new List<DbColumnInfo>();

                    string columnSql = $"PRAGMA table_info([{tableName!.Replace("]", "]]")}]);";
                    DataSet columnDs = ExecuteDataSet(columnSql);
                    if (columnDs.Tables.Count > 0 && columnDs.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow colDr in columnDs.Tables[0].Rows)
                        {
                            string columnName = colDr["name"].ToString()!;

                            string fullType = colDr["type"].ToString()!;
                            string dataType = fullType.Split('(')[0].Trim();
                            long dataLen = 0;

                            if (fullType.Contains('(') && fullType.Contains(')'))
                            {
                                int start = fullType.IndexOf('(') + 1;
                                int end = fullType.IndexOf(')');
                                if (long.TryParse(fullType.Substring(start, end - start), out long len))
                                {
                                    dataLen = len;
                                }
                            }

                            bool isNullable = !Convert.ToBoolean(colDr["notnull"]);
                            bool isPrimaryKey = Convert.ToBoolean(colDr["pk"]);
                            bool isAutoIncrement = isPrimaryKey && string.Equals(dataType, "INTEGER", StringComparison.OrdinalIgnoreCase);

                            string columnComment = string.Empty;

                            DbColumnInfo columnInfo = new DbColumnInfo
                            {
                                ColumnName = columnName,
                                DataType = dataType,
                                CharacterLength = dataLen,
                                IsNullable = isNullable,
                                PrimaryKey = isPrimaryKey,
                                AutoIncrement = isAutoIncrement,
                                Comment = columnComment
                            };
                            columns.Add(columnInfo);
                        }
                    }

                    DbTableInfo tableInfo = new DbTableInfo
                    {
                        TableName = tableName,
                        Comment = comment,
                        Columns = columns
                    };

                    tableInfos.Add(tableInfo);
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error("Failed to get SQLite table structure", ex);
            }

            return tableInfos;
        }

        private List<DbTableInfo> GetTableInfos_Oracle(IList<string>? tableNames = null)
        {
            List<DbTableInfo> tableInfos = new List<DbTableInfo>();
            try
            {
                string tableSql = @"
            SELECT
                t.TABLE_NAME AS TableName,
                NVL(c.COMMENTS, '') AS TableComment
            FROM USER_TABLES t
            LEFT JOIN USER_TAB_COMMENTS c ON t.TABLE_NAME = c.TABLE_NAME
            ORDER BY t.TABLE_NAME;";

                DataSet tableDs = ExecuteDataSet(tableSql);
                if (tableDs.Tables.Count == 0 || tableDs.Tables[0].Rows.Count == 0)
                {
                    return tableInfos;
                }

                foreach (DataRow dr in tableDs.Tables[0].Rows)
                {
                    string tableName = dr["TableName"].ToString()!;
                    string? tableComment = dr["TableComment"].ToString();

                    List<DbColumnInfo> columns = new List<DbColumnInfo>();

                    string columnSql = $@"
                SELECT
                    utc.COLUMN_NAME AS ColumnName,
                    utc.DATA_TYPE AS DataType,
                    utc.DATA_LENGTH AS CharacterLength,
                    utc.DATA_PRECISION AS NumericPrecision,
                    utc.DATA_SCALE AS NumericScale,
                    CASE WHEN utc.NULLABLE = 'Y' THEN 1 ELSE 0 END AS IsNullable,
                    CASE WHEN uc.CONSTRAINT_TYPE = 'P' THEN 1 ELSE 0 END AS IsPrimaryKey,
                    CASE WHEN utc.IDENTITY_COLUMN = 'YES' THEN 1 ELSE 0 END AS IsAutoIncrement,
                    NVL(ucc.COMMENTS, '') AS ColumnComment
                FROM USER_TAB_COLUMNS utc
                LEFT JOIN USER_COL_COMMENTS ucc
                    ON utc.TABLE_NAME = ucc.TABLE_NAME AND utc.COLUMN_NAME = ucc.COLUMN_NAME
                LEFT JOIN USER_CONS_COLUMNS uccol
                    ON utc.TABLE_NAME = uccol.TABLE_NAME AND utc.COLUMN_NAME = uccol.COLUMN_NAME
                LEFT JOIN USER_CONSTRAINTS uc
                    ON uccol.CONSTRAINT_NAME = uc.CONSTRAINT_NAME AND uc.CONSTRAINT_TYPE = 'P'
                WHERE utc.TABLE_NAME = '{tableName.Replace("'", "''")}'
                ORDER BY utc.COLUMN_ID;";

                    DataSet columnDs = ExecuteDataSet(columnSql);
                    if (columnDs.Tables.Count > 0 && columnDs.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow colDr in columnDs.Tables[0].Rows)
                        {
                            DbColumnInfo columnInfo = new DbColumnInfo
                            {
                                ColumnName = colDr["ColumnName"].ToString(),
                                DataType = colDr["DataType"].ToString(),
                                CharacterLength = colDr["CharacterLength"] == DBNull.Value ? 0 : Convert.ToInt64(colDr["CharacterLength"]),
                                NumericPrecision = colDr["NumericPrecision"] == DBNull.Value ? 0 : Convert.ToInt32(colDr["NumericPrecision"]),
                                NumericScale = colDr["NumericScale"] == DBNull.Value ? 0 : Convert.ToInt32(colDr["NumericScale"]),
                                IsNullable = Convert.ToInt32(colDr["IsNullable"]) == 1,
                                PrimaryKey = Convert.ToInt32(colDr["IsPrimaryKey"]) == 1,
                                AutoIncrement = Convert.ToInt32(colDr["IsAutoIncrement"]) == 1,
                                Comment = colDr["ColumnComment"].ToString()
                            };
                            columns.Add(columnInfo);
                        }
                    }

                    DbTableInfo tableInfo = new DbTableInfo
                    {
                        TableName = tableName,
                        Comment = tableComment,
                        Columns = columns
                    };

                    tableInfos.Add(tableInfo);
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error("Failed to get Oracle table structure", ex);
            }

            return tableInfos;
        }

        /// <summary>
        /// 获取表结构
        /// Get the table structure
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        public virtual DbTableInfo? GetTableInfo<M>()
            where M : class
        {
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            return GetTableInfo(mapping.TableName!);
        }

        /// <summary>
        /// 获取表结构
        /// Get the table structure
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public virtual DbTableInfo? GetTableInfo(string tableName)
        {
            List<DbTableInfo> tableInfoList = GetTableInfos(new List<string> { tableName });

            DbTableInfo? tableInfo = tableInfoList.Find(t => StringTool.EqualsIgnoreCase(t.TableName, tableName));

            return tableInfo;
        }

        /// <summary>
        /// 获取数据表字段对应的C#类型
        /// Get the C# type corresponding to the data table field
        /// </summary>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        public virtual Type GetColumnType(DbColumnInfo columnInfo)
        {
            Type type;
            switch (DatabaseType)
            {
                case DatabaseType.MySQL:
                    
                    switch (columnInfo.DataType!.ToLower())
                    {
                        case "bigint":
                            type = columnInfo.IsUnsigned ? typeof(ulong) : typeof(long);
                            break;
                        case "int":
                        case "integer":
                        case "mediumint":
                            type = columnInfo.IsUnsigned ? typeof(uint) : typeof(int);
                            break;
                        case "smallint":
                            type = columnInfo.IsUnsigned ? typeof(ushort) : typeof(short);
                            break;
                        case "tinyint":
                            type = columnInfo.IsUnsigned ? typeof(byte) : typeof(sbyte);
                            break;
                        case "guid":
                            type = typeof(Guid);
                            break;
                        case "smalldatetime":
                        case "date":
                        case "datetime":
                        case "timestamp":
                            type = typeof(DateTime);
                            break;
                        case "float":
                            type = typeof(float);
                            break;
                        case "double":
                            type = typeof(double);
                            break;
                        case "numeric":
                        case "smallmoney":
                        case "decimal":
                        case "money":
                            type = typeof(decimal);
                            break;
                        case "bit":
                        case "bool":
                        case "boolean":
                            type = typeof(bool);
                            break;
                        case "image":
                        case "binary":
                        case "varbinary":
                        case "blob":
                        case "mediumblob":
                        case "longblob":
                            type = typeof(byte[]);
                            break;
                        case "char":
                        case "varchar":
                        case "text":
                        case "mediumtext":
                        case "longtext":
                        case "tinytext":
                            type = typeof(string);
                            break;
                        default:
                            type = typeof(object);
                            break;
                    }

                    break;
                case DatabaseType.PostgreSQL:
                    switch (columnInfo.DataType!.ToLower())
                    {
                        case "smallint":
                        case "int2":
                            type = typeof(short);
                            break;
                        case "integer":
                        case "int":
                        case "int4":
                            type = typeof(int);
                            break;
                        case "bigint":
                        case "int8":
                            type = typeof(long);
                            break;
                        case "real":
                        case "float4":
                            type = typeof(float);
                            break;
                        case "double precision":
                        case "float8":
                            type = typeof(double);
                            break;
                        case "numeric":
                        case "decimal":
                            type = typeof(decimal);
                            break;
                        case "money":
                            type = typeof(decimal);
                            break;
                        case "boolean":
                        case "bool":
                            type = typeof(bool);
                            break;
                        case "char":
                        case "character":
                        case "varchar":
                        case "character varying":
                        case "text":
                            type = typeof(string);
                            break;
                        case "date":
                            type = typeof(DateTime);
                            break;
                        case "time":
                        case "time without time zone":
                            type = typeof(TimeSpan);
                            break;
                        case "timestamp":
                        case "timestamp without time zone":
                        case "timestamp with time zone":
                        case "timestamptz":
                            type = typeof(DateTime);
                            break;
                        case "interval":
                            type = typeof(TimeSpan);
                            break;
                        case "bytea":
                            type = typeof(byte[]);
                            break;
                        case "uuid":
                            type = typeof(Guid);
                            break;
                        case "json":
                        case "jsonb":
                            type = typeof(string);
                            break;
                        case "inet":
                        case "cidr":
                        case "macaddr":
                            type = typeof(string);
                            break;
                        default:
                            type = typeof(object);
                            break;
                    }
                    break;
                case DatabaseType.Oracle:
                    type = GetOracleColumnType(columnInfo);
                    break;
                default:
                    throw new NotSupportedException();
            }

            return type;
        }

        /// <summary>
        /// 获取数据表字段对应的数据参数类型
        /// Obtain the data parameter types corresponding to the table field
        /// </summary>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        public virtual DbType GetColumnDbType(DbColumnInfo columnInfo)
        {
            DbType type;

            switch (DatabaseType)
            {
                case DatabaseType.MySQL:
                   
                    switch (columnInfo.DataType!.ToLower())
                    {
                        case "bigint":
                            type = columnInfo.IsUnsigned ? DbType.UInt64 : DbType.Int64;
                            break;
                        case "int":
                        case "integer":
                        case "mediumint":
                            type = columnInfo.IsUnsigned ? DbType.UInt32 : DbType.Int32;
                            break;
                        case "smallint":
                            type = columnInfo.IsUnsigned ? DbType.UInt16 : DbType.Int16;
                            break;
                        case "tinyint":
                            type = columnInfo.IsUnsigned ? DbType.Byte : DbType.SByte;
                            break;
                        case "guid":
                            type = DbType.Guid;
                            break;
                        case "smalldatetime":
                        case "date":
                        case "datetime":
                        case "timestamp":
                            type = DbType.DateTime;
                            break;
                        case "float":
                            type = DbType.Single;
                            break;
                        case "double":
                            type = DbType.Double;
                            break;
                        case "numeric":
                        case "smallmoney":
                        case "decimal":
                        case "money":
                            type = DbType.Decimal;
                            break;
                        case "bit":
                        case "bool":
                        case "boolean":
                            type = DbType.Boolean;
                            break;
                        case "image":
                        case "binary":
                        case "varbinary":
                        case "blob":
                        case "mediumblob":
                        case "longblob":
                            type = DbType.Binary;
                            break;
                        case "char":
                        case "varchar":
                        case "text":
                        case "mediumtext":
                        case "longtext":
                        case "tinytext":
                            type = DbType.String;
                            break;
                        default:
                            type = DbType.Object;
                            break;
                    }
                    
                    break;

                case DatabaseType.PostgreSQL:
                    switch (columnInfo.DataType!.ToLower())
                    {
                        case "bigint":
                        case "int8":
                            type = DbType.Int64;
                            break;
                        case "integer":
                        case "int":
                        case "int4":
                            type = DbType.Int32;
                            break;
                        case "smallint":
                        case "int2":
                            type = DbType.Int16;
                            break;
                        case "oid":
                            type = DbType.UInt32;
                            break;
                        case "numeric":
                        case "decimal":
                            type = DbType.Decimal;
                            break;
                        case "double precision":
                        case "float8":
                            type = DbType.Double;
                            break;
                        case "real":
                        case "float4":
                            type = DbType.Single;
                            break;
                        case "boolean":
                        case "bool":
                            type = DbType.Boolean;
                            break;
                        case "char":
                        case "character":
                        case "varchar":
                        case "character varying":
                        case "text":
                            type = DbType.String;
                            break;
                        case "citext":
                            type = DbType.String;
                            break;
                        case "timestamp":
                        case "timestamp without time zone":
                        case "timestamptz":
                        case "timestamp with time zone":
                        case "date":
                        case "time":
                        case "time without time zone":
                        case "timetz":
                        case "time with time zone":
                        case "interval":
                            type = DbType.DateTime;
                            break;
                        case "bytea":
                            type = DbType.Binary;
                            break;
                        case "uuid":
                            type = DbType.Guid;
                            break;
                        case "json":
                        case "jsonb":
                            type = DbType.String;
                            break;

                        default:
                            type = DbType.Object;
                            break;
                    }
                    break;

                case DatabaseType.Oracle:
                    type = GetOracleColumnDbType(columnInfo);
                    break;

                default:
                    throw new NotSupportedException();
            }

            return type;
        }

        private static string NormalizeOracleDataType(string? dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
            {
                return string.Empty;
            }

            string normalized = dataType.Trim().ToLower();
            int parenIndex = normalized.IndexOf('(');
            if (parenIndex > 0)
            {
                normalized = normalized.Substring(0, parenIndex).Trim();
            }
            return normalized;
        }

        private static Type GetOracleColumnType(DbColumnInfo columnInfo)
        {
            string dataType = NormalizeOracleDataType(columnInfo.DataType);
            switch (dataType)
            {
                case "number":
                    if (columnInfo.NumericScale == 0)
                    {
                        if (columnInfo.NumericPrecision > 0 && columnInfo.NumericPrecision <= 4)
                        {
                            return typeof(short);
                        }
                        if (columnInfo.NumericPrecision > 4 && columnInfo.NumericPrecision <= 9)
                        {
                            return typeof(int);
                        }
                        if (columnInfo.NumericPrecision > 9 && columnInfo.NumericPrecision <= 18)
                        {
                            return typeof(long);
                        }
                    }
                    return typeof(decimal);
                case "integer":
                case "int":
                    return typeof(int);
                case "smallint":
                    return typeof(short);
                case "float":
                case "binary_float":
                    return typeof(float);
                case "binary_double":
                case "double precision":
                    return typeof(double);
                case "varchar2":
                case "nvarchar2":
                case "char":
                case "nchar":
                case "clob":
                case "nclob":
                case "long":
                case "rowid":
                case "urowid":
                    return typeof(string);
                case "date":
                case "timestamp":
                case "timestamp with time zone":
                case "timestamp with local time zone":
                case "interval day to second":
                case "interval year to month":
                    return typeof(DateTime);
                case "blob":
                case "bfile":
                case "raw":
                case "long raw":
                    return typeof(byte[]);
                default:
                    return typeof(object);
            }
        }

        private static DbType GetOracleColumnDbType(DbColumnInfo columnInfo)
        {
            Type columnType = GetOracleColumnType(columnInfo);
            switch (Type.GetTypeCode(columnType))
            {
                case TypeCode.Int16:
                    return DbType.Int16;
                case TypeCode.Int32:
                    return DbType.Int32;
                case TypeCode.Int64:
                    return DbType.Int64;
                case TypeCode.Single:
                    return DbType.Single;
                case TypeCode.Double:
                    return DbType.Double;
                case TypeCode.Decimal:
                    return DbType.Decimal;
                case TypeCode.DateTime:
                    return DbType.DateTime;
                case TypeCode.String:
                    return DbType.String;
                default:
                    if (columnType == typeof(byte[]))
                    {
                        return DbType.Binary;
                    }
                    return DbType.Object;
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // Dispose 也走超时等待，避免与业务线程互相死锁时应用池线程永久挂死
            if (!_connectionGate.Wait(ConnectionGateTimeout))
            {
                RampageLogger.Error(
                    $"DatabaseClient '{Name}' Dispose gate timeout: best-effort cleanup without the connection gate.",
                    new TimeoutException("DatabaseClient Dispose connection gate timeout"));
                // 兜底：scope 已结束，占住闸门的操作结果也无法交回调用方；
                // 尽力回收事务与连接，避免带事务的连接长期泄漏（闸门持有方的进行中操作将收到连接已关闭错误）
                try
                {
                    CleanupOnDispose();
                }
                catch (Exception ex)
                {
                    RampageLogger.Error(ex);
                }
                return;
            }
            try
            {
                CleanupOnDispose();
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        private void CleanupOnDispose()
        {
            if (Transaction != null)
            {
                try
                {
                    Transaction.Rollback();
                }
                catch (Exception ex)
                {
                    RampageLogger.Error(ex);
                }
                finally
                {
                    Transaction.Dispose();
                }
            }

            ReleaseTransactionConnection();

            if (_scopeConnection != null)
            {
                try
                {
                    if (_scopeConnection.State != ConnectionState.Closed)
                    {
                        _scopeConnection.Close();
                    }
                    _scopeConnection.Dispose();
                }
                catch (Exception ex)
                {
                    RampageLogger.Error(ex);
                }
                finally
                {
                    _scopeConnection = null;
                }
            }
        }

        #endregion
    }
}
