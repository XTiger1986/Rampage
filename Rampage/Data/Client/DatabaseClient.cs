using Rampage.AppLog;
using Rampage.Data.Filter;
using Rampage.Data.Query;
using Rampage.ExceptionExtensions;
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
    /// 数据库客户端抽象基类：同步 CRUD、事务与连接管理的统一门面
    /// Abstract database client: unified facade for synchronous CRUD, transactions and connection management
    /// </summary>
    /// <remarks>
    /// 并发模型：实例内用 SemaphoreSlim 连接门（等待上限 30 秒）串行化同一 client 的全部数据库操作。
    /// 事务为计数式嵌套：仅最外层 CommitTransaction 真正提交；任一层 RollbackTransaction 立即回滚整个事务并置回滚标记，外层再提交将抛出 InvalidOperationException。
    /// 默认（KeepScopeConnection=false）无事务时每个操作从连接池借一条连接、用完归还；事务期间、外部注入连接或 KeepScopeConnection=true 时复用同一条连接。
    /// Dispose 时若有未提交事务会先回滚，再关闭并释放 Scope 连接；外部注入的连接归调用方所有，不会被释放。
    /// Concurrency: a SemaphoreSlim gate (30 s wait limit) serializes all operations on one client instance.
    /// Transactions are reference-counted nested scopes: only the outermost Commit commits; any Rollback aborts the whole transaction and poisons outer commits with InvalidOperationException.
    /// By default (KeepScopeConnection=false) each non-transactional operation borrows a pooled connection and returns it afterwards; a transaction, an injected external connection or KeepScopeConnection=true reuses one connection.
    /// Dispose rolls back any pending transaction and closes/disposes the scope connection; externally injected connections belong to the caller and are never disposed here.
    /// </remarks>
    public abstract partial class DatabaseClient : IDisposable
    {
        /// <summary>
        /// 数据库客户端依赖注入服务的名称前缀
        /// service name prefix in DependencyInjection
        /// </summary>
        public static readonly string SERVICE_NAME_PREFIX = "Rampage.DatabaseClient_";
        /// <summary>
        /// 分页查询在同一 GridReader 中追加的 COUNT(1) 结果集所用的列名
        /// Column name used by the COUNT(1) result set appended to the grid reader in paged queries
        /// </summary>
        public static readonly string RECORD_COUNT_RESULT_COLUMN = "_record_count_";

        #region Constructors

        /// <summary>
        /// 初始化数据库客户端（不指定连接，连接串稍后经 Name 从配置解析）
        /// Initialize the client without a custom connection; the connection string is resolved later via Name from configuration
        /// </summary>
        public DatabaseClient()
        {
        }

        /// <summary>
        /// 使用外部注入的数据库连接初始化客户端（连接归调用方所有，Dispose 不会释放它）
        /// Initialize the client with an externally supplied connection (owned by the caller; Dispose does not dispose it)
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
        /// 使用自定义连接串初始化客户端
        /// Initialize the client with a custom connection string
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
        /// <summary>
        /// 连接门：串行化同一 client 实例上的全部数据库操作
        /// Connection gate: serializes all database operations on this client instance
        /// </summary>
        private readonly SemaphoreSlim _connectionGate = new SemaphoreSlim(1, 1);
        /// <summary>
        /// 连接门等待超时（30 秒），超时抛 TimeoutException
        /// Connection gate wait timeout (30 s); exceeding it throws TimeoutException
        /// </summary>
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
        /// 当前 Scope 内复用的数据库连接；外部注入连接优先；事务期间与事务连接相同。
        /// The connection reused within the current scope; an externally injected connection wins; equals the transaction connection while a transaction is active.
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
        /// 当前数据库事务；无事务时为 null（嵌套期间各层共享最外层开启的同一事务对象）
        /// Current database transaction, or null when none (nested scopes share the single transaction object started by the outermost scope)
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

        /// <summary>
        /// 各数据库（按 Name）默认 DataAccessor 实例的缓存容器
        /// Cache container of default IDataAccessor instances per database (keyed by Name)
        /// </summary>
        protected static readonly ConcurrentDictionary<string, IDataAccessor?> DataAccessorContainer = new ConcurrentDictionary<string, IDataAccessor?>();

        /// <summary>
        /// 当前实例的数据库访问器（懒加载，见 <see cref="DataAccessor"/>）
        /// The database accessor of this instance (loaded lazily, see <see cref="DataAccessor"/>)
        /// </summary>
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
        public virtual void BeginTransaction()
        {
            BeginTransaction(DefaultIsolationLevel);
        }

        /// <summary>
        /// 开启数据库事务。已有事务时视为加入现有事务（嵌套计数），不再静默忽略。
        /// Start a database transaction. When one is active the call joins it (nesting counter) instead of being silently ignored.
        /// </summary>
        /// <param name="iso">事务隔离级别（仅最外层开启时生效）/ isolation level (applies only when starting the outermost transaction)</param>
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
        /// 清除活动事务状态（事务对象、事务连接与嵌套计数），不关闭 Scope 连接
        /// Clear active transaction state (transaction object, transaction connection and nesting counter) without closing the scope connection
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
                    // 提交失败时尽力回滚（回滚失败仅记录日志），最终仍抛出原始提交异常
                    // Best-effort rollback after a failed commit (rollback failure is only logged); the original commit exception is rethrown
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
        /// <param name="dataAccessor">当前已设置的访问器（通常为 null），由实现决定默认实例 / the currently set accessor (usually null); the implementation decides the default instance</param>
        /// <returns>默认的数据库访问器 / the default database accessor</returns>
        protected abstract IDataAccessor? SetDefaultDataAccessor(IDataAccessor? dataAccessor);

        /// <summary>
        /// 创建一条新的数据库连接实例
        /// Create a new database connection instance
        /// </summary>
        /// <exception cref="SuperException">ConnectionCreator 为 null 时抛出（错误码 EX_KEY_INVALID_ARGUMENT）/ thrown when ConnectionCreator is null (error code EX_KEY_INVALID_ARGUMENT)</exception>
        /// <returns>新创建、尚未打开的连接 / a newly created, not yet opened connection</returns>
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
        /// 获取或创建绑定当前 Scope 的连接（懒创建并保持打开状态）
        /// Get or create the scope-bound connection for this client instance (created lazily and kept open)
        /// </summary>
        /// <returns>已打开的 Scope 连接 / the opened scope connection</returns>
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
        /// 解析当前操作使用的连接，优先级：事务连接 &gt; 外部注入连接 &gt; Scope 连接
        /// Resolve the connection used by the current operation, priority: transaction connection &gt; external connection &gt; scope connection
        /// </summary>
        /// <returns>本次操作使用的共享连接 / the shared connection to use</returns>
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
        /// 为单次操作获取连接：事务/外部连接/KeepScopeConnection=true 时复用共享连接；
        /// 否则新建一条临时连接并通过 <paramref name="ownedConnection"/> 返回，调用方必须在操作（含未关闭的读取器）完成后释放它。
        /// Resolve the connection for one operation: a transaction / external connection / KeepScopeConnection=true reuses the shared connection;
        /// otherwise a temporary connection is created and returned via <paramref name="ownedConnection"/> — the caller
        /// must dispose it once the operation (including any open reader) has completed.
        /// </summary>
        /// <param name="ownedConnection">非 null 表示返回的连接为本操作私有、需由调用方释放 / non-null when the returned connection is owned by this operation and must be disposed by the caller</param>
        /// <returns>本次操作使用的连接 / the connection to use</returns>
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
        /// <see cref="AcquireOperationConnection"/> 的异步版本
        /// Async variant of <see cref="AcquireOperationConnection"/>
        /// </summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>元组：Connection 为本次使用的连接；Owned 非空时为需调用方释放的私有连接 / tuple: Connection is the connection to use; Owned, when non-null, is the private connection the caller must dispose</returns>
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
        /// 在连接门保护下对操作连接执行委托；操作期间持有连接门，结束后归还门并释放临时连接
        /// Execute an action on the operation connection. The connection gate is held for the duration of the action, then released together with any owned connection.
        /// </summary>
        /// <typeparam name="T">委托返回值类型 / action result type</typeparam>
        /// <param name="action">接收连接与当前事务的委托 / action receiving the connection and the current transaction</param>
        /// <returns>委托的返回值 / the value returned by the action</returns>
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
        /// 执行返回读取器类可释放对象的委托。连接门（及临时连接）被一直挂起，直到返回对象被释放，
        /// 因此读取器未关闭期间同连接上不会有其他操作交错执行。
        /// Execute an action that returns a reader-like disposable. The connection gate (and any per-operation
        /// connection) is held until the returned object is disposed, so no other operation can interleave
        /// on the same connection while the reader is still open.
        /// </summary>
        /// <typeparam name="T">读取器包装类型 / reader wrapper type</typeparam>
        /// <param name="action">接收连接与当前事务、返回读取器的委托 / action receiving the connection and transaction, returning the reader</param>
        /// <param name="attachDisposeCallback">把“归还门与临时连接”的回调挂接到返回对象上的委托 / callback used to attach the gate/owned-connection release logic onto the returned object</param>
        /// <returns>读取器包装对象 / the reader wrapper</returns>
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
        /// <see cref="ExecuteWithConnection{T}"/> 的异步版本
        /// Asynchronously execute an action on the operation connection (async variant of <see cref="ExecuteWithConnection{T}"/>)
        /// </summary>
        /// <typeparam name="T">委托返回值类型 / action result type</typeparam>
        /// <param name="action">接收连接、当前事务与取消令牌的委托 / action receiving the connection, the current transaction and a cancellation token</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>委托的返回值 / the value returned by the action</returns>
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
        /// <see cref="ExecuteWithReaderConnection{T}"/> 的异步版本：连接门与临时连接在读取器释放时才归还
        /// Async variant of <see cref="ExecuteWithReaderConnection{T}"/>: the connection gate and any owned connection are released only when the reader is disposed
        /// </summary>
        /// <typeparam name="T">读取器包装类型 / reader wrapper type</typeparam>
        /// <param name="action">接收连接、当前事务与取消令牌、返回读取器的委托 / action receiving the connection, transaction and cancellation token, returning the reader</param>
        /// <param name="attachDisposeCallback">把“归还门与临时连接”的回调挂接到返回对象上的委托 / callback used to attach the gate/owned-connection release logic onto the returned object</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>读取器包装对象 / the reader wrapper</returns>
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
        /// 异步解析并打开当前操作连接；连接为 <see cref="DbConnection"/> 时走 OpenAsync，否则同步降级
        /// Open the operation connection asynchronously when it is a <see cref="DbConnection"/>, falling back to synchronous Open otherwise
        /// </summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>本次操作使用的连接 / the connection to use</returns>
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
        /// 异步获取或创建绑定当前 Scope 的连接（懒创建并保持打开状态）
        /// Get or create the scope-bound connection asynchronously (created lazily and kept open)
        /// </summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>已打开的 Scope 连接 / the opened scope connection</returns>
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
        /// 尽可能异步打开连接；非 <see cref="DbConnection"/> 实现同步降级调用 Open
        /// Open a connection asynchronously when possible; non-<see cref="DbConnection"/> implementations fall back to synchronous Open
        /// </summary>
        /// <param name="conn">待打开的连接 / the connection to open</param>
        /// <param name="cancellationToken">取消令牌（仅异步路径生效）/ cancellation token (honoured on the async path only)</param>
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
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="fields">查询字段集，null 表示全部字段 / query fields; null selects all fields</param>
        /// <returns>格式化后的 SELECT 字段串 / the formatted SELECT field list</returns>
        public string FormatFields<M>(QueryFields? fields = null)
            where M : class
        {
            return SqlTemplate.BuildQueryFieldsString<M>(fields);
        }

        /// <summary>
        /// 得到格式化后的数据字段名
        /// Get the formatted data field name
        /// </summary>
        /// <param name="column">字段名 / column name</param>
        /// <returns>格式化后的字段名（如加引号或定界符）/ the formatted column name (e.g. quoted/delimited)</returns>
        public string FormatColumn(string column)
        {
            return SqlTemplate.FormatColumn(column);
        }

        /// <summary>
        /// 得到格式化后的数据表名
        /// Get the formatted table name
        /// </summary>
        /// <param name="tableName">表名 / table name</param>
        /// <returns>格式化后的表名（如加引号或定界符）/ the formatted table name (e.g. quoted/delimited)</returns>
        public string FormatTable(string tableName)
        {
            return SqlTemplate.FormatTable(tableName);
        }

        /// <summary>
        /// 得到格式化后的数据参数名
        /// Get the formatted data parameter name
        /// </summary>
        /// <param name="fieldName">字段名（将转换为参数前缀形式）/ field name (converted to the parameter prefix form)</param>
        /// <returns>格式化后的参数名 / the formatted parameter name</returns>
        public string FormatParameter(string fieldName)
        {
            return SqlTemplate.FormatParameter(fieldName);
        }

        /// <summary>
        /// 获取数据库当前时间
        /// Get the current time from the database
        /// </summary>
        /// <returns>数据库服务器当前时间 / the current time on the database server</returns>
        public virtual DateTime GetNow()
        {
            string sql = SqlTemplate.GetNowSql();
            return ExecuteScalar<DateTime>(sql);
        }

        #endregion

        #region Data Access

        /// <summary>
        /// 执行参数化 SQL（增/删/改或存储过程）
        /// Execute parameterized SQL (insert/delete/update or a stored procedure)
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 / the number of rows affected</returns>
        public virtual int Execute(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sql, param, trans, commandType, commandTimeout));
        }
     
        /// <summary>
        /// 执行返回单值的参数化 SQL
        /// Execute parameterized SQL that selects a single value
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>首行首列的值，无结果为 null / the first column of the first row, or null when there is no result</returns>
        public virtual object? ExecuteScalar(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// 执行返回单值的参数化 SQL 并转换为 T
        /// Execute parameterized SQL that selects a single value, converted to T
        /// </summary>
        /// <typeparam name="T">目标值类型 / target value type</typeparam>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>转换后的单值 / the single value converted to T</returns>
        public virtual T? ExecuteScalar<T>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<T>(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// 执行查询并返回读取器包装。调用方必须 using 释放返回的 <see cref="DataReaderWrap"/>，
        /// 其 Dispose 回调负责释放底层读取器、归还连接门与临时连接——读取器未关闭前同一 client 上的其他操作都会被阻塞。
        /// Execute a query and return a reader wrapper. The caller must dispose the returned
        /// <see cref="DataReaderWrap"/> (e.g. <c>using</c>); its dispose callback releases the underlying reader and
        /// returns the connection gate and any owned connection — until then every other operation on this client is blocked.
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>读取器包装 / the reader wrapper</returns>
        public virtual DataReaderWrap ExecuteReader(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithReaderConnection(
                (conn, trans) => DataAccessor!.ExecuteReader(conn, sql, param, trans, commandType, commandTimeout),
                (reader, onDisposed) => reader.AttachDisposeCallback(onDisposed));
        }

        /// <summary>
        /// 执行 SQL 并以 DataSet 返回结果
        /// Execute SQL and return the result as a DataSet
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>填充后的 DataSet / the filled DataSet</returns>
        public virtual DataSet ExecuteDataSet(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteDataSet(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// 查询并返回动态对象列表（属性与结果列对应）
        /// Return a sequence of dynamic objects with properties matching the columns
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>动态对象列表 / the list of dynamic rows</returns>
        public virtual List<dynamic> SelectList(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<dynamic>(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// 查询并返回单个动态对象（属性与结果列对应），无结果为 null
        /// Return a dynamic object with properties matching the columns, or null when no row matches
        /// </summary>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>单个动态对象，无匹配为 null / the dynamic row, or null when none</returns>
        public virtual dynamic? SelectOne(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// 查询并返回类型化为 M 的数据列表
        /// Execute a query, returning the data typed as M
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual List<M> SelectList<M>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
            where M : class
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// 执行单行查询，返回类型化为 M 的数据，无结果为 null
        /// Execute a single-row query, returning the data typed as M, or null when no row matches
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="sql">SQL 语句 / SQL statement</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>单个数据对象，无匹配为 null / the matched row, or null when none</returns>
        public virtual M? SelectOne<M>(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
            where M : class
        {
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sql, param, trans, commandType, commandTimeout));
        }

        /// <summary>
        /// 执行返回多个结果集的命令，供依次读取。调用方必须 using 释放返回的 <see cref="GridReaderWrap"/>，
        /// 其 Dispose 回调负责释放底层读取器、归还连接门与临时连接。
        /// Execute a command that returns multiple result sets and access each in turn. The caller must dispose the
        /// returned <see cref="GridReaderWrap"/>; its dispose callback releases the underlying reader and returns the connection gate and any owned connection.
        /// </summary>
        /// <param name="sql">SQL 语句（可含多个结果集）/ SQL statement (may produce multiple result sets)</param>
        /// <param name="param">SQL 参数对象 / SQL parameters</param>
        /// <param name="commandType">命令类型，默认 Text / command type, Text by default</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>多结果集读取器包装 / the grid reader wrapper</returns>
        public virtual GridReaderWrap ExecuteGridReader(string sql, object? param = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return ExecuteWithReaderConnection(
                (conn, trans) => DataAccessor!.ExecuteGridReader(conn, sql, param, trans, commandType, commandTimeout),
                (grid, onDisposed) => grid.AttachDisposeCallback(onDisposed));
        }

        #endregion

        #region Data Access Plus

        /// <summary>
        /// 按 LambdaWrapper 查询数据列表
        /// Query a data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual List<M> SelectList<M>(LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// 按 LambdaWrapper 查询数据列表，并映射为结果类型 R
        /// Query a data list by a LambdaWrapper, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual List<R> SelectList<M, R>(LambdaWrapper<M> wrapper) 
            where M : class 
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// 按 Lambda 表达式条件查询数据列表
        /// Query a data list by a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual List<M> SelectList<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按 Lambda 表达式条件查询数据列表，并映射为结果类型 R
        /// Query a data list by a Lambda expression, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual List<R> SelectList<M, R>(Expression<Func<M, bool>> condition, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按 FilterWrapper 查询数据列表；wrapper 为 null 时查询全部
        /// Query a data list by a FilterWrapper; a null wrapper queries all rows
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">过滤包装器，null 表示无条件 / the filter wrapper; null means no condition</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual List<M> SelectList<M>(FilterWrapper<M>? wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout));
        }

        /// <summary>
        /// 按 FilterWrapper 查询数据列表，并映射为结果类型 R；wrapper 为 null 时查询全部
        /// Query a data list by a FilterWrapper, mapped to the result type R; a null wrapper queries all rows
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">过滤包装器，null 表示无条件 / the filter wrapper; null means no condition</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual List<R> SelectList<M, R>(FilterWrapper<M>? wrapper) 
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper == null ? null : wrapper.CommandTimeout));
        }

        /// <summary>
        /// 按数据过滤器查询数据列表
        /// Query a data list by a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>数据列表 / the list of rows</returns>
        public virtual List<M> SelectList<M>(DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按数据过滤器查询数据列表，并映射为结果类型 R
        /// Query a data list by a DataFilter, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual List<R> SelectList<M, R>(DataFilter filter, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按 LambdaWrapper 分页查询数据列表
        /// Query a paged data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；已预置 RecordCount 时不再执行 COUNT，否则在同批 GridReader 中追加 COUNT(1) _record_count_ 结果集并自动回填 / pager info: the offset is (PageIndex-1)*PageSize; a preset RecordCount skips the COUNT query, otherwise a COUNT(1) _record_count_ result set is appended to the same grid reader and filled back automatically</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>当前页数据列表；总行数写入 pagerInfo.RecordCount / the rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>
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
        /// 按 LambdaWrapper 分页查询数据列表，并映射为结果类型 R
        /// Query a paged data list by a LambdaWrapper, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；已预置 RecordCount 时不再执行 COUNT，否则在同批 GridReader 中追加 COUNT(1) _record_count_ 结果集并自动回填 / pager info: the offset is (PageIndex-1)*PageSize; a preset RecordCount skips the COUNT query, otherwise a COUNT(1) _record_count_ result set is appended to the same grid reader and filled back automatically</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>当前页结果列表；总行数写入 pagerInfo.RecordCount / the mapped rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>
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
        /// 按 Lambda 表达式条件分页查询数据列表
        /// Query a paged data list by a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；已预置 RecordCount 时不再执行 COUNT，否则在同批 GridReader 中追加 COUNT(1) _record_count_ 结果集并自动回填 / pager info: the offset is (PageIndex-1)*PageSize; a preset RecordCount skips the COUNT query, otherwise a COUNT(1) _record_count_ result set is appended to the same grid reader and filled back automatically</param>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>当前页数据列表；总行数写入 pagerInfo.RecordCount / the rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>
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
        /// 按 Lambda 表达式条件分页查询数据列表，并映射为结果类型 R
        /// Query a paged data list by a Lambda expression, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；已预置 RecordCount 时不再执行 COUNT，否则在同批 GridReader 中追加 COUNT(1) _record_count_ 结果集并自动回填 / pager info: the offset is (PageIndex-1)*PageSize; a preset RecordCount skips the COUNT query, otherwise a COUNT(1) _record_count_ result set is appended to the same grid reader and filled back automatically</param>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>当前页结果列表；总行数写入 pagerInfo.RecordCount / the mapped rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>
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
        /// 按 FilterWrapper 分页查询数据列表；wrapper 为 null 时查询全部
        /// Query a paged data list by a FilterWrapper; a null wrapper pages all rows
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；已预置 RecordCount 时不再执行 COUNT，否则在同批 GridReader 中追加 COUNT(1) _record_count_ 结果集并自动回填 / pager info: the offset is (PageIndex-1)*PageSize; a preset RecordCount skips the COUNT query, otherwise a COUNT(1) _record_count_ result set is appended to the same grid reader and filled back automatically</param>
        /// <param name="wrapper">过滤包装器，null 表示无条件 / the filter wrapper; null means no condition</param>
        /// <returns>当前页数据列表；总行数写入 pagerInfo.RecordCount / the rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>
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
        /// 按 FilterWrapper 分页查询数据列表，并映射为结果类型 R；wrapper 为 null 时查询全部
        /// Query a paged data list by a FilterWrapper, mapped to the result type R; a null wrapper pages all rows
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；已预置 RecordCount 时不再执行 COUNT，否则在同批 GridReader 中追加 COUNT(1) _record_count_ 结果集并自动回填 / pager info: the offset is (PageIndex-1)*PageSize; a preset RecordCount skips the COUNT query, otherwise a COUNT(1) _record_count_ result set is appended to the same grid reader and filled back automatically</param>
        /// <param name="wrapper">过滤包装器，null 表示无条件 / the filter wrapper; null means no condition</param>
        /// <returns>当前页结果列表；总行数写入 pagerInfo.RecordCount / the mapped rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>
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
        /// 按数据过滤器分页查询数据列表
        /// Query a paged data list by a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；已预置 RecordCount 时不再执行 COUNT，否则在同批 GridReader 中追加 COUNT(1) _record_count_ 结果集并自动回填 / pager info: the offset is (PageIndex-1)*PageSize; a preset RecordCount skips the COUNT query, otherwise a COUNT(1) _record_count_ result set is appended to the same grid reader and filled back automatically</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>当前页数据列表；总行数写入 pagerInfo.RecordCount / the rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>
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
        /// 按数据过滤器分页查询数据列表，并映射为结果类型 R
        /// Query a paged data list by a DataFilter, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：偏移按 (PageIndex-1)*PageSize 计算；已预置 RecordCount 时不再执行 COUNT，否则在同批 GridReader 中追加 COUNT(1) _record_count_ 结果集并自动回填 / pager info: the offset is (PageIndex-1)*PageSize; a preset RecordCount skips the COUNT query, otherwise a COUNT(1) _record_count_ result set is appended to the same grid reader and filled back automatically</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>当前页结果列表；总行数写入 pagerInfo.RecordCount / the mapped rows of the current page; the total row count is written back to pagerInfo.RecordCount</returns>
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
        /// 按主键集合查询数据列表
        /// Query a data list by a primary key list
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="keyList">主键值集合 / the list of primary key values</param>
        /// <param name="resultFields">返回字段集，null 表示全部字段 / result fields; null selects all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>数据列表 / the list of matched rows</returns>
        public virtual List<M> SelectByIds<M>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectByIds<M>(keyList, resultFields);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按主键集合查询数据列表，并映射为结果类型 R
        /// Query a data list by a primary key list, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="keyList">主键值集合 / the list of primary key values</param>
        /// <param name="resultFields">返回字段集，null 表示全部字段 / result fields; null selects all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>结果列表 / the list of mapped rows</returns>
        public virtual List<R> SelectByIds<M, R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectByIds<M>(keyList, resultFields);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectList<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按 LambdaWrapper 查询单条数据（内部会把 wrapper.Limit 强制置为 1）
        /// Query one row by a LambdaWrapper (the wrapper's Limit is forced to 1 internally)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">查询包装器（会被修改 Limit）/ the query wrapper (its Limit is mutated)</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual M? SelectOne<M>(LambdaWrapper<M> wrapper) where M : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// 按 LambdaWrapper 查询单条数据，并映射为结果类型 R（内部会把 wrapper.Limit 强制置为 1）
        /// Query one row by a LambdaWrapper, mapped to the result type R (the wrapper's Limit is forced to 1 internally)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器（会被修改 Limit）/ the query wrapper (its Limit is mutated)</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
        public virtual R? SelectOne<M, R>(LambdaWrapper<M> wrapper) 
            where M : class
            where R : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// 按 Lambda 表达式条件查询单条数据（LIMIT 1）
        /// Query one row by a Lambda expression (LIMIT 1)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual M? SelectOne<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按 Lambda 表达式条件查询单条数据，并映射为结果类型 R（LIMIT 1）
        /// Query one row by a Lambda expression, mapped to the result type R (LIMIT 1)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
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
        /// 按 FilterWrapper 查询单条数据（内部会把 wrapper.Limit 强制置为 1）
        /// Query one row by a FilterWrapper (the wrapper's Limit is forced to 1 internally)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">过滤包装器（会被修改 Limit）/ the filter wrapper (its Limit is mutated)</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual M? SelectOne<M>(FilterWrapper<M> wrapper) where M : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// 按 FilterWrapper 查询单条数据，并映射为结果类型 R（内部会把 wrapper.Limit 强制置为 1）
        /// Query one row by a FilterWrapper, mapped to the result type R (the wrapper's Limit is forced to 1 internally)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">过滤包装器（会被修改 Limit）/ the filter wrapper (its Limit is mutated)</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
        public virtual R? SelectOne<M, R>(FilterWrapper<M> wrapper) 
            where M : class
            where R : class
        {
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// 按数据过滤器查询单条数据（LIMIT 1）
        /// Query one row by a DataFilter (LIMIT 1)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual M? SelectOne<M>(DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            wrapper.Limit = 1;
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelect<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按数据过滤器查询单条数据，并映射为结果类型 R（LIMIT 1）
        /// Query one row by a DataFilter, mapped to the result type R (LIMIT 1)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
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
        /// 按主键查询单条数据
        /// Query one row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段集，null 表示全部字段 / result fields; null selects all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>单条数据，无匹配为 null / the matched row, or null when none</returns>
        public virtual M? SelectById<M>(object key, QueryFields? resultFields = null, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectById<M>(key, resultFields);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<M>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按主键查询单条数据，并映射为结果类型 R
        /// Query one row by primary key, mapped to the result type R
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段集，null 表示全部字段 / result fields; null selects all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>单条结果，无匹配为 null / the mapped row, or null when none</returns>
        public virtual R? SelectById<M, R>(object key, QueryFields? resultFields = null, int? commandTimeout = null) 
            where M : class
            where R : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfSelectById<M>(key, resultFields);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.SelectOne<R>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 插入单条数据；有自增列时自增主键值会自动回写到实体属性
        /// Insert one row; the auto-increment key is written back to the entity's mapping property automatically
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据 / the entity data</param>
        /// <param name="insertId">是否显式插入自增主键（默认 false，由数据库生成）/ whether to insert the auto-increment primary key explicitly (default false, generated by the database)</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>无自增列时受影响行数 &gt; 0 返回 true；有自增列时成功回填主键即返回 true / effects &gt; 0 for tables without an auto-increment column; true once the generated key has been written back for auto-increment tables</returns>
        /// <remarks>
        /// 无自增列且 SQL 未预置参数时，直接把实体对象本身作为 Dapper 参数传入；
        /// 有自增列时改用 ExecuteScalar 取回生成的主键并回写（数据库返回类型与属性类型不一致时自动转换）。
        /// When the table has no auto-increment column and parameters were not pre-built, the entity object itself is passed as the Dapper parameter;
        /// with an auto-increment column, ExecuteScalar retrieves the generated key and writes it back (converting automatically when the database type differs from the property type).
        /// </remarks>
        public virtual bool Insert<M>(M data, bool insertId = false, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfInsert<M>(data, insertId);
            if (!sqlInfo.IsAutoIncrement)
            {
                // 未预置参数时直接把实体对象作为 Dapper 参数传入 / pass the entity itself as the Dapper parameter when parameters were not pre-built
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
        /// 按主键更新整条数据（未指定字段集时更新全部字段）
        /// Update one row by primary key (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据（主键取自该实体）/ the entity data (its primary key is used)</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual bool UpdateById<M>(M data, QueryFields? updateFields = null, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(data, updateFields);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按主键更新部分字段
        /// Update some fields of a row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool UpdateById<M>(object id, UpdateFields updateFields, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按主键更新部分字段（强类型字段表达式版本）
        /// Update some fields of a row by primary key (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool UpdateById<M>(object id, UpdateFields<M> updateFields, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按主键更新单个字段
        /// Update a single field of a row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool UpdateById<M, F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按主键更新两个字段
        /// Update two fields of a row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool UpdateById<M, F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value,  int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdateById<M>(updateFields, id);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按主键更新三个字段
        /// Update three fields of a row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <typeparam name="F3">第三个字段类型 / third field type</typeparam>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field3Value">第三个字段新值 / the new value of field 3</param>
        /// <param name="field3">第三个字段选择表达式 / the selector of field 3</param>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
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
        /// 按 LambdaWrapper 条件更新数据（未指定字段集时更新全部字段）
        /// Update rows matching a LambdaWrapper (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="data">实体数据（提供字段值）/ the entity data providing field values</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual bool Update<M>(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 LambdaWrapper 条件更新部分字段
        /// Update some fields of rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M>(UpdateFields updateFields, LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 LambdaWrapper 条件更新部分字段（强类型字段表达式版本）
        /// Update some fields of rows matching a LambdaWrapper (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M>(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 LambdaWrapper 条件更新单个字段
        /// Update a single field of rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M, F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 LambdaWrapper 条件更新两个字段
        /// Update two fields of rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M, F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 LambdaWrapper 条件更新三个字段
        /// Update three fields of rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <typeparam name="F3">第三个字段类型 / third field type</typeparam>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="field3">第三个字段选择表达式 / the selector of field 3</param>
        /// <param name="field3Value">第三个字段新值 / the new value of field 3</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
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
        /// 按 Lambda 表达式条件更新数据（未指定字段集时更新全部字段）
        /// Update rows matching a Lambda expression (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="data">实体数据（提供字段值）/ the entity data providing field values</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual bool Update<M>(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 Lambda 表达式条件更新部分字段
        /// Update some fields of rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M>(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 Lambda 表达式条件更新部分字段（强类型字段表达式版本）
        /// Update some fields of rows matching a Lambda expression (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M>(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 Lambda 表达式条件更新单个字段
        /// Update a single field of rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M, F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 Lambda 表达式条件更新两个字段
        /// Update two fields of rows matching a Lambda expression
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
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
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
        /// 按 Lambda 表达式条件更新三个字段
        /// Update three fields of rows matching a Lambda expression
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
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
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
        /// 按 FilterWrapper 条件更新数据（未指定字段集时更新全部字段）
        /// Update rows matching a FilterWrapper (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="data">实体数据（提供字段值）/ the entity data providing field values</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual bool Update<M>(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 FilterWrapper 条件更新部分字段
        /// Update some fields of rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M>(UpdateFields updateFields, FilterWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 FilterWrapper 条件更新部分字段（强类型字段表达式版本）
        /// Update some fields of rows matching a FilterWrapper (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M>(UpdateFields<M> updateFields, FilterWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 FilterWrapper 条件更新单个字段
        /// Update a single field of rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M, F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 FilterWrapper 条件更新两个字段
        /// Update two fields of rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M, F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(field1, field1Value);
            updateFields.Set(field2, field2Value);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 FilterWrapper 条件更新三个字段
        /// Update three fields of rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F1">第一个字段类型 / first field type</typeparam>
        /// <typeparam name="F2">第二个字段类型 / second field type</typeparam>
        /// <typeparam name="F3">第三个字段类型 / third field type</typeparam>
        /// <param name="field1">第一个字段选择表达式 / the selector of field 1</param>
        /// <param name="field1Value">第一个字段新值 / the new value of field 1</param>
        /// <param name="field2">第二个字段选择表达式 / the selector of field 2</param>
        /// <param name="field2Value">第二个字段新值 / the new value of field 2</param>
        /// <param name="field3">第三个字段选择表达式 / the selector of field 3</param>
        /// <param name="field3Value">第三个字段新值 / the new value of field 3</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
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
        /// 按数据过滤器条件更新数据（未指定字段集时更新全部字段）
        /// Update rows matching a DataFilter (all fields when no field set is specified)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="data">实体数据（提供字段值）/ the entity data providing field values</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        /// <remarks>未预置参数时直接把实体对象作为 Dapper 参数传入 / the entity object is passed directly as the Dapper parameter when parameters were not pre-built</remarks>
        public virtual bool Update<M>(M data, DataFilter filter, QueryFields? updateFields = null, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(data, wrapper, updateFields);
           int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, !sqlInfo.ParametersInitialized ? data : sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按数据过滤器条件更新部分字段
        /// Update some fields of rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M>(UpdateFields updateFields, DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按数据过滤器条件更新部分字段（强类型字段表达式版本）
        /// Update some fields of rows matching a DataFilter (strongly-typed field selector variant)
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateFields">待更新字段集 / fields to update</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M>(UpdateFields<M> updateFields, DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按数据过滤器条件更新单个字段
        /// Update a single field of rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateField">字段选择表达式 / the field selector expression</param>
        /// <param name="fieldValue">字段新值 / the new field value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Update<M, F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null) where M : class
        {
            UpdateFields<M> updateFields = UpdateFields<M>.Build(updateField, fieldValue);
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfUpdate<M>(updateFields, wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按数据过滤器条件更新两个字段
        /// Update two fields of rows matching a DataFilter
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
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
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
        /// 按数据过滤器条件更新三个字段
        /// Update three fields of rows matching a DataFilter
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
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
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
        /// 插入或更新单条数据：主键有值（数值 &gt; 0 或非空字符串）视为更新，否则视为插入
        /// Insert or update one row: an entity whose primary key has a value (number &gt; 0 or non-empty string) is updated, otherwise inserted
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="data">实体数据 / the entity data</param>
        /// <param name="updateFields">走更新路径时待更新的字段集，null 表示全部字段 / fields to update on the update path; null updates all fields</param>
        /// <param name="insertId">走插入路径时是否显式插入自增主键（默认 false）/ whether to insert the auto-increment primary key explicitly on the insert path (default false)</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>实际执行的插入/更新是否影响了行（或有自增主键回填成功）/ whether the executed insert/update affected rows (or wrote back an auto-increment key)</returns>
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
        /// 分批插入数据列表
        /// Insert a data list in batches
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dataList">实体数据列表 / the list of entities</param>
        /// <param name="batchSize">单条命令批量大小 / batch size of one execute command</param>
        /// <param name="insertId">
        /// 指定是否插入自增长主键
        /// Specify whether to force the insertion of an auto-increment primary key
        /// </param>
        /// <param name="batchHandler">每批执行后的回调，返回 false 终止后续批次 / callback after each batch; returning false stops further batches</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>累计受影响行数 &gt; 0 返回 true；输入为空集合返回 false / true when the total affected rows &gt; 0; false for an empty input list</returns>
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
        /// 按主键分批更新数据列表
        /// Update a data list in batches by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dataList">实体数据列表 / the list of entities</param>
        /// <param name="updateFields">待更新字段集，null 表示全部字段 / fields to update; null updates all fields</param>
        /// <param name="batchSize">单条命令批量大小 / batch size of one execute command</param>
        /// <param name="batchHandler">每批执行后的回调，返回 false 终止后续批次 / callback after each batch; returning false stops further batches</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>累计受影响行数 &gt; 0 返回 true；输入为空集合返回 false / true when the total affected rows &gt; 0; false for an empty input list</returns>
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
        /// 分批插入或更新数据列表：先按主键是否有值拆分为待插入/待更新两组，再分别批量执行
        /// Insert or update a data list in batches: rows are first split by primary-key presence into insert/update groups, then executed batch by batch
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="dataList">实体数据列表 / the list of entities</param>
        /// <param name="updateFields">走更新路径时待更新的字段集，null 表示全部字段 / fields to update on the update path; null updates all fields</param>
        /// <param name="batchSize">单条命令批量大小 / batch size of one execute command</param>
        /// <param name="insertId">走插入路径时是否显式插入自增主键（默认 false）/ whether to insert the auto-increment primary key explicitly on the insert path (default false)</param>
        /// <param name="batchHandler">每批执行后的回调（插入与更新阶段各自触发），返回 false 终止该阶段后续批次 / callback after each batch (fired for both phases); returning false stops further batches of that phase</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>任一阶段影响行数 &gt; 0 返回 true；输入为空集合返回 false / true when either phase affected rows; false for an empty input list</returns>
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
        /// 按主键删除单条数据
        /// Delete one row by primary key
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool DeleteById<M>(object key, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(key);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 LambdaWrapper 条件删除数据（删除所有满足条件的行）
        /// Delete all rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Delete<M>(LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 Lambda 表达式条件删除数据（删除所有满足条件的行）
        /// Delete all rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Delete<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 FilterWrapper 条件删除数据（删除所有满足条件的行）
        /// Delete all rows matching a FilterWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Delete<M>(FilterWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按数据过滤器条件删除数据（删除所有满足条件的行）
        /// Delete all rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool Delete<M>(DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(wrapper);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按主键集合批量删除数据
        /// Delete rows in bulk by a primary key list
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="keyList">主键值集合 / the list of primary key values</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>受影响行数 &gt; 0 返回 true / true when at least one row was affected</returns>
        public virtual bool DeleteByIds<M>(IEnumerable keyList, int? commandTimeout = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfDelete<M>(keyList);
            int effects = ExecuteWithConnection((conn, trans) => DataAccessor!.Execute(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
            return effects > 0;
        }

        /// <summary>
        /// 按 LambdaWrapper 条件统计行数
        /// Count rows matching a LambdaWrapper
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>满足条件的行数 / the number of matching rows</returns>
        public virtual long Count<M>(LambdaWrapper<M> wrapper) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper.CommandTimeout));
        }

        /// <summary>
        /// 按 Lambda 表达式条件统计行数
        /// Count rows matching a Lambda expression
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="condition">Lambda 查询条件 / the Lambda query condition</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>满足条件的行数 / the number of matching rows</returns>
        public virtual long Count<M>(Expression<Func<M, bool>> condition, int? commandTimeout = null) where M : class
        {
            LambdaWrapper<M> wrapper = new LambdaWrapper<M>(condition);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        /// <summary>
        /// 按 FilterWrapper 条件统计行数；wrapper 为 null 时统计全表
        /// Count rows matching a FilterWrapper; a null wrapper counts the whole table
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="wrapper">条件包装器，null 表示无条件 / the condition wrapper; null means no condition</param>
        /// <returns>满足条件的行数 / the number of matching rows</returns>
        public virtual long Count<M>(FilterWrapper<M>? wrapper = null) where M : class
        {
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, wrapper != null ? wrapper.CommandTimeout : null));
        }

        /// <summary>
        /// 按数据过滤器条件统计行数
        /// Count rows matching a DataFilter
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="commandTimeout">命令超时秒数，null 用驱动默认 / command timeout in seconds; null uses the provider default</param>
        /// <returns>满足条件的行数 / the number of matching rows</returns>
        public virtual long Count<M>(DataFilter filter, int? commandTimeout = null) where M : class
        {
            FilterWrapper<M> wrapper = new FilterWrapper<M>(filter);
            SqlInfo sqlInfo = SqlTemplate.BuildSqlOfCount<M>(wrapper);
            return ExecuteWithConnection((conn, trans) => DataAccessor!.ExecuteScalar<long>(conn, sqlInfo.Sql!, sqlInfo.GetParameters(), trans, CommandType.Text, commandTimeout));
        }

        #endregion

        #region Schema

        /// <summary>
        /// 获取表结构列表（按数据库类型分派到对应的系统表/系统视图查询）
        /// Get the list of table structures (dispatched per database type to the corresponding system-table query)
        /// </summary>
        /// <param name="tableNames">表名过滤列表（当前实现未使用，始终返回全部表）/ table-name filter (currently ignored; all tables are returned)</param>
        /// <returns>表结构列表；查询失败时记录日志并返回空列表 / the list of table structures; on failure the error is logged and an empty list is returned</returns>
        /// <exception cref="NotSupportedException">数据库类型未实现表结构查询时抛出 / thrown when the database type has no table-structure query</exception>
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

        /// <summary>
        /// 通过 SHOW TABLE STATUS / SHOW FULL COLUMNS 获取 MySQL 表结构（权限不足时记录日志并返回空列表）
        /// Get MySQL table structures via SHOW TABLE STATUS / SHOW FULL COLUMNS (logs and returns an empty list on insufficient permission)
        /// </summary>
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

        /// <summary>
        /// 通过 information_schema 获取 PostgreSQL（public 架构）表结构（失败时记录日志并返回空列表）
        /// Get PostgreSQL (public schema) table structures via information_schema (logs and returns an empty list on failure)
        /// </summary>
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

        /// <summary>
        /// 通过 INFORMATION_SCHEMA / sys 目录视图获取 SQL Server 表结构（失败时记录日志并返回空列表）
        /// Get SQL Server table structures via INFORMATION_SCHEMA / sys catalog views (logs and returns an empty list on failure)
        /// </summary>
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

        /// <summary>
        /// 通过 sqlite_master / PRAGMA table_info 获取 SQLite 表结构（SQLite 无表/列注释，Comment 恒为空；失败时记录日志并返回空列表）
        /// Get SQLite table structures via sqlite_master / PRAGMA table_info (SQLite has no table/column comments, Comment is always empty; logs and returns an empty list on failure)
        /// </summary>
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

        /// <summary>
        /// 通过 USER_TABLES / USER_TAB_COLUMNS 等数据字典获取 Oracle 表结构（失败时记录日志并返回空列表）
        /// Get Oracle table structures via the USER_TABLES / USER_TAB_COLUMNS data dictionary (logs and returns an empty list on failure)
        /// </summary>
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
        /// 获取模型映射表的表结构
        /// Get the table structure of a model's mapped table
        /// </summary>
        /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
        /// <returns>表结构；表不存在或查询失败返回 null / the table structure; null when the table is missing or the query fails</returns>
        public virtual DbTableInfo? GetTableInfo<M>()
            where M : class
        {
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            return GetTableInfo(mapping.TableName!);
        }

        /// <summary>
        /// 按表名获取表结构（忽略大小写匹配）
        /// Get the table structure by table name (matched case-insensitively)
        /// </summary>
        /// <param name="tableName">表名 / table name</param>
        /// <returns>表结构；表不存在或查询失败返回 null / the table structure; null when the table is missing or the query fails</returns>
        public virtual DbTableInfo? GetTableInfo(string tableName)
        {
            List<DbTableInfo> tableInfoList = GetTableInfos(new List<string> { tableName });

            DbTableInfo? tableInfo = tableInfoList.Find(t => StringTool.EqualsIgnoreCase(t.TableName, tableName));

            return tableInfo;
        }

        /// <summary>
        /// 获取数据表字段对应的C#类型（未识别的类型映射为 object）
        /// Get the C# type corresponding to the data table field (unrecognized types map to object)
        /// </summary>
        /// <param name="columnInfo">字段结构信息 / the column structure info</param>
        /// <returns>对应的 C# 类型 / the corresponding C# type</returns>
        /// <exception cref="NotSupportedException">数据库类型未实现类型映射时抛出 / thrown when the database type has no type mapping</exception>
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
        /// 获取数据表字段对应的数据参数类型（DbType；未识别的类型映射为 Object）
        /// Obtain the DbType corresponding to the table field (unrecognized types map to Object)
        /// </summary>
        /// <param name="columnInfo">字段结构信息 / the column structure info</param>
        /// <returns>对应的 <see cref="DbType"/> / the corresponding <see cref="DbType"/></returns>
        /// <exception cref="NotSupportedException">数据库类型未实现类型映射时抛出 / thrown when the database type has no type mapping</exception>
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
        /// 释放客户端：回滚未提交的事务，并关闭/释放 Scope 连接（外部注入连接归调用方所有，不在此释放）。
        /// 等待连接门最多 30 秒；超时则记录错误并绕过门做尽力回收，避免 Dispose 永久阻塞。
        /// Dispose the client: roll back any uncommitted transaction and close/dispose the scope connection
        /// (externally injected connections belong to the caller and are not disposed here).
        /// Waits up to 30 s for the connection gate; on timeout it logs the error and performs best-effort cleanup without the gate so Dispose never blocks forever.
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

        /// <summary>
        /// Dispose 的实际清理逻辑：回滚并释放未提交事务、清除事务状态、关闭并释放 Scope 连接（外部注入连接不处理）
        /// Actual cleanup for Dispose: roll back and dispose any pending transaction, clear transaction state, then close and dispose the scope connection (external connections are left alone)
        /// </summary>
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
