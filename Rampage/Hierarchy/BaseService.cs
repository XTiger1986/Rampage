using Microsoft.Extensions.DependencyInjection;
using Rampage.Caching;
using Rampage.Data;
using Rampage.Data.Client;
using Rampage.Data.Filter;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Localization;
using Rampage.Log;
using System;
using System.Collections;
using System.Data;
using System.Linq.Expressions;

namespace Rampage.Hierarchy
{

    /// <summary>
    /// 业务层服务基类 / Business-layer service base class.
    /// </summary>
    /// <remarks>
    /// 构造函数要求当前异步上下文已绑定环境 Scope（<see cref="RampageScope"/>，见 <see cref="Initialize"/>），
    /// 初始化时自动执行 [Auto]/[Singleton] 成员注入，并提供缓存编排（cache-aside）与多实体事务编排能力。
    /// The constructor requires an ambient scope to be bound (<see cref="RampageScope"/>, see <see cref="Initialize"/>);
    /// initialization performs [Auto]/[Singleton] member injection and provides cache-aside orchestration plus multi-entity transaction orchestration.
    /// </remarks>
    public abstract class BaseService : IBaseService
    {

        /// <summary>服务器本地当前时间 / The server's local current time.</summary>
        protected DateTime LocalNow
        {
            get { return DateTime.Now; }
        }

        /// <summary>UTC 当前时间 / The current UTC time.</summary>
        protected DateTime UtcNow
        {
            get {  return DateTime.UtcNow; }
        }


        /// <summary>当前 Scope 的服务提供者（Initialize 时绑定）/ The current scope's service provider (bound during Initialize).</summary>
        public virtual IServiceProvider? ServiceProvider
        {
            get;
            protected set;
        }

        /// <summary>
        /// 当前 Scope 的服务定位器（Initialize 时绑定）/ The current scope's service locator (bound during Initialize).
        /// </summary>
        public virtual IScopedServiceLocator? ServiceLocator
        {
            get;
            protected set;
        }

        /// <summary>
        /// 独立内存缓存（[Singleton] 从根容器注入）/ The stand-alone in-memory cache (injected from the root container via [Singleton]).
        /// </summary>
        [Singleton]
        protected IStandAloneCache MemoryCache
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 缓存编排（cache-aside）：未命中时执行数据函数并写回缓存 / Cache-aside orchestration: runs the data function and back-fills the cache on a miss.
        /// </summary>
        /// <typeparam name="T">缓存数据类型 / The cached data type.</typeparam>
        /// <param name="dataFunc">未命中时的数据加载函数 / The data loader invoked on a cache miss.</param>
        /// <param name="cacheKeyTemplate">缓存键模板（复合格式字符串）/ The cache key template (composite format string).</param>
        /// <param name="expiredSeconds">过期秒数；为 null 时使用缓存默认策略 / Expiry in seconds; null uses the cache default.</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / The cache key template arguments.</param>
        /// <returns>缓存或新加载的数据 / The cached or freshly loaded data.</returns>
        /// <remarks>dataFunc 返回 null 时不写缓存 / The result is not cached when dataFunc returns null.</remarks>
        protected virtual T? HandleCache<T>(Func<T?> dataFunc, string cacheKeyTemplate, double? expiredSeconds = null, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });

            T? result;
            if (!MemoryCache.TryGet<T>(cacheKey, out result))
            {
                result = dataFunc();
                if (result != null)
                {
                    if (expiredSeconds != null)
                    {
                        MemoryCache.Set(cacheKey, result, expiredSeconds.Value);
                    }
                    else
                    {
                        MemoryCache.Set(cacheKey, result);
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 按模板移除指定缓存项 / Removes the cache entry addressed by the given template.
        /// </summary>
        /// <param name="cacheKeyTemplate">缓存键模板（复合格式字符串）/ The cache key template (composite format string).</param>
        /// <param name="cacheKeyArgs">缓存键模板参数 / The cache key template arguments.</param>
        protected void RemoveCache(string cacheKeyTemplate, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });
            MemoryCache.Remove(cacheKey);
        }
        /// <summary>
        /// 按前缀批量移除缓存项 / Removes cache entries whose keys start with the given prefix.
        /// </summary>
        /// <param name="cacheKeyPrefix">缓存键前缀 / The cache key prefix.</param>
        protected void RemoveCacheByPrefix(string cacheKeyPrefix)
        {
            MemoryCache.RemoveBySearch(cacheKeyPrefix);
        }

        /// <summary>默认语言资源的惰性缓存字段 / Lazy backing field of the default language resource.</summary>
        protected LocalizationResource? _defaultLanguage;
        /// <summary>
        /// 默认语言资源（首次访问时惰性加载）/ The default language resource (lazily loaded on first access).
        /// </summary>
        protected virtual LocalizationResource DefaultLanguage
        {
            get
            {
                if (_defaultLanguage == null)
                {
                    _defaultLanguage = LocalizationManager.GetResource(null);
                }
                return _defaultLanguage!;
            }
        }

        /// <summary>
        /// 构造函数：要求已绑定环境 Scope 并完成自动注入 / Constructor: requires a bound ambient scope and performs auto-injection.
        /// </summary>
        /// <exception cref="InvalidOperationException">未绑定 RampageScope / No RampageScope is bound.</exception>
        public BaseService()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化：绑定当前环境 Scope 的解析器与定位器，然后执行自动注入 / Initialization: binds the ambient scope's provider and locator, then performs auto-injection.
        /// </summary>
        /// <exception cref="InvalidOperationException">未绑定 RampageScope / No RampageScope is bound.</exception>
        protected virtual void Initialize()
        {
            RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(GetType().FullName);
            ServiceProvider = handle.ServiceProvider;
            ServiceLocator = handle.Locator;
            AutoInject();
        }

        /// <summary>
        /// 当前实例使用的自动注入器（AutoInject 时创建）/ The auto-injecter used by this instance (created during AutoInject).
        /// </summary>
        protected AutoInjecter? autoInjecter;

        /// <summary>
        /// 执行 [Auto]/[Singleton] 成员注入 / Performs [Auto]/[Singleton] member injection on this instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">ServiceLocator 尚未绑定 / ServiceLocator is not bound yet.</exception>
        protected virtual void AutoInject()
        {
            if (ServiceLocator == null)
            {
                throw new InvalidOperationException($"{GetType().FullName}: ServiceLocator is null before AutoInject.");
            }
            autoInjecter = new AutoInjecter(ServiceLocator);
            autoInjecter.Build(this);
        }

        /// <summary>
        /// 释放资源（当前仅释放自动注入器）/ Disposes resources (currently only the auto-injecter).
        /// </summary>
        public virtual void Dispose()
        {
            if (autoInjecter != null)
            {
                autoInjecter.Dispose();
            }
        }

        #region Transaction (multi-entity orchestration)

        private Action? _txCommit;
        private Action? _txRollback;

        /// <summary>
        /// 多实体编排：通过任一参与写操作的 Dao 开启事务（同一 Scope 内各 Dao 共用 DatabaseClient），
        /// 开启后即可调用无参 <see cref="CommitTransaction"/> / <see cref="RollbackTransaction"/>。
        /// Multi-entity orchestration: opens a transaction via any participating Dao (all Daos in the same scope share one DatabaseClient);
        /// the parameterless CommitTransaction / RollbackTransaction can then be called.
        /// </summary>
        /// <param name="dao">开启事务的 Dao / The Dao that opens the transaction.</param>
        /// <param name="iso">事务隔离级别 / The transaction isolation level.</param>
        /// <typeparam name="TEntity">Dao 操作的实体类型 / The entity type operated by the Dao.</typeparam>
        protected void BeginTransaction<TEntity>(ISuperDao<TEntity> dao, IsolationLevel iso = IsolationLevel.ReadCommitted)
            where TEntity : class
        {
            if (dao == null)
            {
                throw new ArgumentNullException(nameof(dao));
            }
            dao.BeginTransaction(iso);
            _txCommit = dao.CommitTransaction;
            _txRollback = dao.RollbackTransaction;
        }

        /// <summary>
        /// 提交由 <see cref="BeginTransaction{TEntity}"/> 开启的事务 / Commits the transaction opened by <see cref="BeginTransaction{TEntity}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">当前无活动事务 / No transaction is currently active.</exception>
        protected void CommitTransaction()
        {
            if (_txCommit == null)
            {
                throw new InvalidOperationException("No active transaction. Call BeginTransaction(dao) first.");
            }
            try
            {
                _txCommit();
            }
            finally
            {
                ClearActiveTransaction();
            }
        }

        /// <summary>
        /// 回滚由 <see cref="BeginTransaction{TEntity}"/> 开启的事务；无活动事务时静默返回 / Rolls back the transaction opened by <see cref="BeginTransaction{TEntity}"/>; silently returns when no transaction is active.
        /// </summary>
        protected void RollbackTransaction()
        {
            if (_txRollback == null)
            {
                return;
            }
            try
            {
                _txRollback();
            }
            finally
            {
                ClearActiveTransaction();
            }
        }

        /// <summary>
        /// 显式指定 Dao 提交（不依赖 Begin 时缓存的委托）/ Commits via the explicitly given Dao (not the delegate cached at Begin).
        /// </summary>
        /// <param name="dao">执行提交的 Dao / The Dao that performs the commit.</param>
        /// <typeparam name="TEntity">Dao 操作的实体类型 / The entity type operated by the Dao.</typeparam>
        protected void CommitTransaction<TEntity>(ISuperDao<TEntity> dao)
            where TEntity : class
        {
            if (dao == null)
            {
                throw new ArgumentNullException(nameof(dao));
            }
            try
            {
                dao.CommitTransaction();
            }
            finally
            {
                ClearActiveTransaction();
            }
        }

        /// <summary>
        /// 显式指定 Dao 回滚（不依赖 Begin 时缓存的委托）/ Rolls back via the explicitly given Dao (not the delegate cached at Begin).
        /// </summary>
        /// <param name="dao">执行回滚的 Dao / The Dao that performs the rollback.</param>
        /// <typeparam name="TEntity">Dao 操作的实体类型 / The entity type operated by the Dao.</typeparam>
        protected void RollbackTransaction<TEntity>(ISuperDao<TEntity> dao)
            where TEntity : class
        {
            if (dao == null)
            {
                throw new ArgumentNullException(nameof(dao));
            }
            try
            {
                dao.RollbackTransaction();
            }
            finally
            {
                ClearActiveTransaction();
            }
        }

        /// <summary>清空 Begin 时缓存的提交/回滚委托 / Clears the commit/rollback delegates cached at Begin.</summary>
        private void ClearActiveTransaction()
        {
            _txCommit = null;
            _txRollback = null;
        }

        #endregion
    }


    /// <summary>
    /// 面向单一实体的业务层服务基类：聚合一个 Dao 并把数据操作转发给它 / Single-entity business-layer service base that aggregates one Dao and forwards data operations to it.
    /// </summary>
    /// <typeparam name="M">模型（表映射）类型 / The model (table-mapped) type.</typeparam>
    /// <typeparam name="D">该实体对应的 Dao 类型 / The Dao type for the entity.</typeparam>
    public abstract class BaseService<M, D> : BaseService, IBaseService<M, D>
        where M : class
        where D : ISuperDao<M>
    {

        /// <summary>
        /// 聚合的数据访问对象（构造时从定位器解析）/ The aggregated data access object (resolved from the locator at construction).
        /// </summary>
        private ISuperDao<M> Dao
        {
            get; set;
        }

        /// <summary>
        /// 构造函数：从当前 Scope 解析 D 类型的 Dao / Constructor: resolves the Dao of type D from the current scope.
        /// </summary>
        /// <exception cref="InvalidOperationException">未绑定 RampageScope / No RampageScope is bound.</exception>
        public BaseService() : base()
        {
            Dao = ServiceLocator!.Get<D>();
        }


        #region IServiceBase

        /// <summary>
        /// 切换 Dao 使用的数据库 / Switches the database used by the Dao.
        /// </summary>
        /// <param name="databaseName">目标数据库名 / The target database name.</param>
        public void ChangeDatabase(string databaseName)
        {
            Dao.ChangeDatabase(databaseName);
        }

        /// <summary>
        /// 在 Dao 上开启事务 / Begins a transaction on the Dao.
        /// </summary>
        /// <param name="iso">事务隔离级别 / The isolation level.</param>
        public void BeginTransaction(IsolationLevel iso = IsolationLevel.ReadCommitted)
        {
            Dao.BeginTransaction(iso);
        }

        /// <summary>
        /// 提交 Dao 事务 / Commits the Dao transaction.
        /// </summary>
        /// <remarks>使用 new 隐藏基类（多实体编排版）的提交方法，直接作用于本类聚合的 Dao / Hides the base-class (multi-entity orchestration) commit via new and acts directly on this class's aggregated Dao.</remarks>
        public new void CommitTransaction()
        {
            Dao.CommitTransaction();
        }

        /// <summary>
        /// 回滚 Dao 事务 / Rolls back the Dao transaction.
        /// </summary>
        /// <remarks>使用 new 隐藏基类（多实体编排版）的回滚方法，直接作用于本类聚合的 Dao / Hides the base-class (multi-entity orchestration) rollback via new and acts directly on this class's aggregated Dao.</remarks>
        public new void RollbackTransaction()
        {
            Dao.RollbackTransaction();
        }

        /// <summary>
        /// 按 LambdaWrapper 查询数据列表 / Query a data list by a LambdaWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体列表 / The entity list.</returns>
        public List<M> SelectList(LambdaWrapper<M> wrapper)
        {
            return Dao.SelectList(wrapper);
        }

        /// <summary>
        /// 按 Lambda 表达式查询数据列表 / Query a data list by a lambda expression.
        /// </summary>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>实体列表 / The entity list.</returns>
        public List<M> SelectList(Expression<Func<M, bool>> condition)
        {
            return Dao.SelectList(condition);
        }

        /// <summary>
        /// 按 LambdaWrapper 查询数据列表并映射为 R / Query a data list by a LambdaWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果列表 / The result list.</returns>
        public List<R> SelectList<R>(LambdaWrapper<M> wrapper) where R : class
        {
            return Dao.SelectList<R>(wrapper);
        }

        /// <summary>
        /// 按 Lambda 表达式查询数据列表并映射为 R / Query a data list by a lambda expression and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>结果列表 / The result list.</returns>
        public List<R> SelectList<R>(Expression<Func<M, bool>> condition) where R : class
        {
            return Dao.SelectList<R>(condition);
        }

        /// <summary>
        /// 按 FilterWrapper 查询数据列表 / Query a data list by a FilterWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体列表 / The entity list.</returns>
        public List<M> SelectList(FilterWrapper<M>? wrapper)
        {
            return Dao.SelectList(wrapper);
        }

        /// <summary>
        /// 按 FilterWrapper 查询数据列表并映射为 R / Query a data list by a FilterWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果列表 / The result list.</returns>
        public List<R> SelectList<R>(FilterWrapper<M>? wrapper) where R : class
        {
            return Dao.SelectList<R>(wrapper);
        }

        /// <summary>
        /// 按 DataFilter 查询数据列表 / Query a data list by a DataFilter.
        /// </summary>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>实体列表 / The entity list.</returns>
        public virtual List<M> SelectList(DataFilter filter)
        {
            return Dao.SelectList(filter);
        }

        /// <summary>
        /// 按 DataFilter 查询数据列表并映射为 R / Query a data list by a DataFilter and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>结果列表 / The result list.</returns>
        public virtual List<R> SelectList<R>(DataFilter filter) where R : class
        {
            return Dao.SelectList<R>(filter);
        }

        /// <summary>
        /// 按 LambdaWrapper 分页查询数据列表 / Query a paged data list by a LambdaWrapper.
        /// </summary>
        /// <param name="pagerInfo">分页信息（自定义 RecordCount 则不再执行 count 命令）/ Pager info (a custom RecordCount skips the count command).</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体列表 / The entity list.</returns>
        public List<M> SelectPage(PagerInfo pagerInfo, LambdaWrapper<M> wrapper)
        {
            return Dao.SelectPage(pagerInfo, wrapper);
        }

        /// <summary>
        /// 按 LambdaWrapper 分页查询数据列表并映射为 R / Query a paged data list by a LambdaWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="pagerInfo">分页信息（自定义 RecordCount 则不再执行 count 命令）/ Pager info (a custom RecordCount skips the count command).</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果列表 / The result list.</returns>
        public List<R> SelectPage<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper) where R : class
        {
            return Dao.SelectPage<R>(pagerInfo, wrapper);
        }

        /// <summary>
        /// 按 Lambda 表达式分页查询数据列表 / Query a paged data list by a lambda expression.
        /// </summary>
        /// <param name="pagerInfo">分页信息（自定义 RecordCount 则不再执行 count 命令）/ Pager info (a custom RecordCount skips the count command).</param>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>实体列表 / The entity list.</returns>
        public List<M> SelectPage(PagerInfo pagerInfo, Expression<Func<M, bool>> condition)
        {
            return Dao.SelectPage(pagerInfo, condition);
        }

        /// <summary>
        /// 按 Lambda 表达式分页查询数据列表并映射为 R / Query a paged data list by a lambda expression and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="pagerInfo">分页信息（自定义 RecordCount 则不再执行 count 命令）/ Pager info (a custom RecordCount skips the count command).</param>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>结果列表 / The result list.</returns>
        public List<R> SelectPage<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition) where R : class
        {
            return Dao.SelectPage<R>(pagerInfo, condition);
        }

        /// <summary>
        /// 按 FilterWrapper 分页查询数据列表 / Query a paged data list by a FilterWrapper.
        /// </summary>
        /// <param name="pagerInfo">分页信息（自定义 RecordCount 则不再执行 count 命令）/ Pager info (a custom RecordCount skips the count command).</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体列表 / The entity list.</returns>
        public List<M> SelectPage(PagerInfo pagerInfo, FilterWrapper<M>? wrapper)
        {
            return Dao.SelectPage(pagerInfo, wrapper);
        }

        /// <summary>
        /// 按 FilterWrapper 分页查询数据列表并映射为 R / Query a paged data list by a FilterWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="pagerInfo">分页信息（自定义 RecordCount 则不再执行 count 命令）/ Pager info (a custom RecordCount skips the count command).</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果列表 / The result list.</returns>
        public List<R> SelectPage<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper) where R : class
        {
            return Dao.SelectPage<R>(pagerInfo, wrapper);
        }

        /// <summary>
        /// 按 DataFilter 分页查询数据列表 / Query a paged data list by a DataFilter.
        /// </summary>
        /// <param name="pagerInfo">分页信息（自定义 RecordCount 则不再执行 count 命令）/ Pager info (a custom RecordCount skips the count command).</param>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>实体列表 / The entity list.</returns>
        public List<M> SelectPage(PagerInfo pagerInfo, DataFilter filter)
        {
            return Dao.SelectPage(pagerInfo, filter);
        }

        /// <summary>
        /// 按 DataFilter 分页查询数据列表并映射为 R / Query a paged data list by a DataFilter and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="pagerInfo">分页信息（自定义 RecordCount 则不再执行 count 命令）/ Pager info (a custom RecordCount skips the count command).</param>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>结果列表 / The result list.</returns>
        public List<R> SelectPage<R>(PagerInfo pagerInfo, DataFilter filter) where R : class
        {
            return Dao.SelectPage<R>(pagerInfo, filter);
        }

        /// <summary>
        /// 按主键列表批量查询数据 / Query a data list by primary keys.
        /// </summary>
        /// <param name="keyList">主键列表 / The primary key list.</param>
        /// <param name="resultFields">返回字段筛选 / The result fields filter.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>实体列表 / The entity list.</returns>
        public List<M> SelectByIds(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null)
        {
            return Dao.SelectByIds(keyList, resultFields, commandTimeout);
        }

        /// <summary>
        /// 按主键列表批量查询数据并映射为 R / Query a data list by primary keys and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="keyList">主键列表 / The primary key list.</param>
        /// <param name="resultFields">返回字段筛选 / The result fields filter.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>结果列表 / The result list.</returns>
        public List<R> SelectByIds<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) where R : class
        {
            return Dao.SelectByIds<R>(keyList, resultFields, commandTimeout);
        }

        /// <summary>
        /// 按 LambdaWrapper 查询单条数据 / Query one record by a LambdaWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        public M? SelectOne(LambdaWrapper<M> wrapper)
        {
            return Dao.SelectOne(wrapper);
        }

        /// <summary>
        /// 按 LambdaWrapper 查询单条数据并映射为 R / Query one record by a LambdaWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果或 null / The result or null.</returns>
        public R? SelectOne<R>(LambdaWrapper<M> wrapper) where R : class
        {
            return Dao.SelectOne<R>(wrapper);
        }

        /// <summary>
        /// 按 Lambda 表达式查询单条数据 / Query one record by a lambda expression.
        /// </summary>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        public M? SelectOne(Expression<Func<M, bool>> condition)
        {
            return Dao.SelectOne(condition);
        }

        /// <summary>
        /// 按 Lambda 表达式查询单条数据并映射为 R / Query one record by a lambda expression and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>结果或 null / The result or null.</returns>
        public R? SelectOne<R>(Expression<Func<M, bool>> condition) where R : class
        {
            return Dao.SelectOne<R>(condition);
        }

        /// <summary>
        /// 按 FilterWrapper 查询单条数据 / Query one record by a FilterWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        public M? SelectOne(FilterWrapper<M> wrapper)
        {
            return Dao.SelectOne(wrapper);
        }

        /// <summary>
        /// 按 FilterWrapper 查询单条数据并映射为 R / Query one record by a FilterWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果或 null / The result or null.</returns>
        public R? SelectOne<R>(FilterWrapper<M> wrapper) where R : class
        {
            return Dao.SelectOne<R>(wrapper);
        }

        /// <summary>
        /// 按 DataFilter 查询单条数据 / Query one record by a DataFilter.
        /// </summary>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        public M? SelectOne(DataFilter filter)
        {
            return Dao.SelectOne(filter);
        }

        /// <summary>
        /// 按 DataFilter 查询单条数据并映射为 R / Query one record by a DataFilter and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>结果或 null / The result or null.</returns>
        public R? SelectOne<R>(DataFilter filter) where R : class
        {
            return Dao.SelectOne<R>(filter);
        }

        /// <summary>
        /// 按主键查询单条数据 / Query one record by primary key.
        /// </summary>
        /// <param name="key">主键值 / The primary key value.</param>
        /// <param name="resultFields">返回字段筛选 / The result fields filter.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        public M? SelectById(object key, QueryFields? resultFields = null, int? commandTimeout = null)
        {
            return Dao.SelectById(key, resultFields, commandTimeout);
        }

        /// <summary>
        /// 按主键查询单条数据并映射为 R / Query one record by primary key and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="key">主键值 / The primary key value.</param>
        /// <param name="resultFields">返回字段筛选 / The result fields filter.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>结果或 null / The result or null.</returns>
        public R? SelectById<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null) where R : class
        {
            return Dao.SelectById<R>(key, resultFields, commandTimeout);
        }

        /// <summary>
        /// 插入一条数据（自增主键回填到实体）/ Insert one record (the autoincrement key is written back to the entity).
        /// </summary>
        /// <param name="data">实体数据 / The entity data.</param>
        /// <param name="insertId">是否插入主键列 / Whether to insert the primary key column.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Insert(M data, bool insertId = false, int? commandTimeout = null)
        {
            return Dao.Insert(data, insertId, commandTimeout);
        }

        /// <summary>
        /// 按主键更新一条数据 / Update one record by primary key.
        /// </summary>
        /// <param name="data">实体数据（取其主键定位行）/ The entity data (its primary key locates the row).</param>
        /// <param name="updateFields">更新字段筛选；为 null 时更新全部字段 / The fields to update; null updates all fields.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool UpdateById(M data, QueryFields? updateFields = null, int? commandTimeout = null)
        {
            return Dao.UpdateById(data, updateFields, commandTimeout);
        }

        /// <summary>
        /// 按主键与字段集合更新一条数据 / Update one record by primary key with an update-fields collection.
        /// </summary>
        /// <param name="id">主键值 / The primary key value.</param>
        /// <param name="updateFields">更新字段集合 / The fields to update.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool UpdateById(object id, UpdateFields updateFields, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, updateFields, commandTimeout);
        }

        /// <summary>
        /// 按主键与强类型字段集合更新一条数据 / Update one record by primary key with a strongly-typed update-fields collection.
        /// </summary>
        /// <param name="id">主键值 / The primary key value.</param>
        /// <param name="updateFields">更新字段集合 / The fields to update.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool UpdateById(object id, UpdateFields<M> updateFields, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, updateFields, commandTimeout);
        }

        /// <summary>
        /// 按主键更新指定单个字段 / Update one field by primary key.
        /// </summary>
        /// <typeparam name="F">字段类型 / The field type.</typeparam>
        /// <param name="id">主键值 / The primary key value.</param>
        /// <param name="updateField">更新字段表达式 / The field to update.</param>
        /// <param name="fieldValue">字段新值 / The new field value.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool UpdateById<F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, updateField, fieldValue, commandTimeout);
        }

        /// <summary>
        /// 按主键更新指定两个字段 / Update two fields by primary key.
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / The first field type.</typeparam>
        /// <typeparam name="F2">字段 2 类型 / The second field type.</typeparam>
        /// <param name="id">主键值 / The primary key value.</param>
        /// <param name="field1">字段 1 表达式 / The first field expression.</param>
        /// <param name="field1Value">字段 1 新值 / The first field's new value.</param>
        /// <param name="field2">字段 2 表达式 / The second field expression.</param>
        /// <param name="field2Value">字段 2 新值 / The second field's new value.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool UpdateById<F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, field1, field1Value, field2, field2Value, commandTimeout);
        }

        /// <summary>
        /// 按主键更新指定三个字段 / Update three fields by primary key.
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / The first field type.</typeparam>
        /// <typeparam name="F2">字段 2 类型 / The second field type.</typeparam>
        /// <typeparam name="F3">字段 3 类型 / The third field type.</typeparam>
        /// <param name="id">主键值 / The primary key value.</param>
        /// <param name="field1">字段 1 表达式 / The first field expression.</param>
        /// <param name="field1Value">字段 1 新值 / The first field's new value.</param>
        /// <param name="field2">字段 2 表达式 / The second field expression.</param>
        /// <param name="field2Value">字段 2 新值 / The second field's new value.</param>
        /// <param name="field3">字段 3 表达式 / The third field expression.</param>
        /// <param name="field3Value">字段 3 新值 / The third field's new value.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool UpdateById<F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout);
        }

        /// <summary>
        /// 按 LambdaWrapper 条件更新数据（条件命中多行则全部更新）/ Update records matching a LambdaWrapper (all matched rows when multiple).
        /// </summary>
        /// <param name="data">实体数据（提供更新字段值）/ The entity data providing the values.</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <param name="updateFields">更新字段筛选；为 null 时更新全部字段 / The fields to update; null updates all fields.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null)
        {
            return Dao.Update(data, wrapper, updateFields);
        }

        /// <summary>
        /// 按 Lambda 表达式条件更新数据 / Update records matching a lambda expression.
        /// </summary>
        /// <param name="data">实体数据（提供更新字段值）/ The entity data providing the values.</param>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <param name="updateFields">更新字段筛选；为 null 时更新全部字段 / The fields to update; null updates all fields.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null)
        {
            return Dao.Update(data, condition, updateFields);
        }

        /// <summary>
        /// 按 Lambda 表达式条件与字段集合更新数据 / Update by a lambda expression condition with an update-fields collection.
        /// </summary>
        /// <param name="updateFields">更新字段集合 / The fields to update.</param>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(UpdateFields updateFields, Expression<Func<M, bool>> condition)
        {
            return Dao.Update(updateFields, condition);
        }

        /// <summary>
        /// 按 Lambda 表达式条件与强类型字段集合更新数据 / Update by a lambda expression condition with a strongly-typed update-fields collection.
        /// </summary>
        /// <param name="updateFields">更新字段集合 / The fields to update.</param>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition)
        {
            return Dao.Update(updateFields, condition);
        }

        /// <summary>
        /// 按 Lambda 表达式条件更新指定单个字段 / Update one field by a lambda expression condition.
        /// </summary>
        /// <typeparam name="F">字段类型 / The field type.</typeparam>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <param name="updateField">更新字段表达式 / The field to update.</param>
        /// <param name="fieldValue">字段新值 / The new field value.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update<F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return Dao.Update(condition, updateField, fieldValue);
        }

        /// <summary>
        /// 按 Lambda 表达式条件更新指定两个字段 / Update two fields by a lambda expression condition.
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / The first field type.</typeparam>
        /// <typeparam name="F2">字段 2 类型 / The second field type.</typeparam>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <param name="field1">字段 1 表达式 / The first field expression.</param>
        /// <param name="field1Value">字段 1 新值 / The first field's new value.</param>
        /// <param name="field2">字段 2 表达式 / The second field expression.</param>
        /// <param name="field2Value">字段 2 新值 / The second field's new value.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update<F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return Dao.Update(condition, field1, field1Value, field2, field2Value);
        }

        /// <summary>
        /// 按 Lambda 表达式条件更新指定三个字段 / Update three fields by a lambda expression condition.
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / The first field type.</typeparam>
        /// <typeparam name="F2">字段 2 类型 / The second field type.</typeparam>
        /// <typeparam name="F3">字段 3 类型 / The third field type.</typeparam>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <param name="field1">字段 1 表达式 / The first field expression.</param>
        /// <param name="field1Value">字段 1 新值 / The first field's new value.</param>
        /// <param name="field2">字段 2 表达式 / The second field expression.</param>
        /// <param name="field2Value">字段 2 新值 / The second field's new value.</param>
        /// <param name="field3">字段 3 表达式 / The third field expression.</param>
        /// <param name="field3Value">字段 3 新值 / The third field's new value.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update<F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return Dao.Update(condition, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        /// <summary>
        /// 按 FilterWrapper 条件更新数据 / Update records matching a FilterWrapper.
        /// </summary>
        /// <param name="data">实体数据（提供更新字段值）/ The entity data providing the values.</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <param name="updateFields">更新字段筛选；为 null 时更新全部字段 / The fields to update; null updates all fields.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null)
        {
            return Dao.Update(data, wrapper, updateFields);
        }

        /// <summary>
        /// 按 FilterWrapper 条件与字段集合更新数据 / Update by a FilterWrapper condition with an update-fields collection.
        /// </summary>
        /// <param name="updateFields">更新字段集合 / The fields to update.</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(UpdateFields updateFields, FilterWrapper<M> wrapper)
        {
            return Dao.Update(updateFields, wrapper);
        }

        /// <summary>
        /// 按 FilterWrapper 条件与强类型字段集合更新数据 / Update by a FilterWrapper condition with a strongly-typed update-fields collection.
        /// </summary>
        /// <param name="updateFields">更新字段集合 / The fields to update.</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(UpdateFields<M> updateFields, FilterWrapper<M> wrapper)
        {
            return Dao.Update(updateFields, wrapper);
        }

        /// <summary>
        /// 按 FilterWrapper 条件更新指定单个字段 / Update one field by a FilterWrapper condition.
        /// </summary>
        /// <typeparam name="F">字段类型 / The field type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <param name="updateField">更新字段表达式 / The field to update.</param>
        /// <param name="fieldValue">字段新值 / The new field value.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update<F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return Dao.Update(wrapper, updateField, fieldValue);
        }

        /// <summary>
        /// 按 FilterWrapper 条件更新指定两个字段 / Update two fields by a FilterWrapper condition.
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / The first field type.</typeparam>
        /// <typeparam name="F2">字段 2 类型 / The second field type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <param name="field1">字段 1 表达式 / The first field expression.</param>
        /// <param name="field1Value">字段 1 新值 / The first field's new value.</param>
        /// <param name="field2">字段 2 表达式 / The second field expression.</param>
        /// <param name="field2Value">字段 2 新值 / The second field's new value.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update<F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return Dao.Update(wrapper, field1, field1Value, field2, field2Value);
        }

        /// <summary>
        /// 按 FilterWrapper 条件更新指定三个字段 / Update three fields by a FilterWrapper condition.
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / The first field type.</typeparam>
        /// <typeparam name="F2">字段 2 类型 / The second field type.</typeparam>
        /// <typeparam name="F3">字段 3 类型 / The third field type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <param name="field1">字段 1 表达式 / The first field expression.</param>
        /// <param name="field1Value">字段 1 新值 / The first field's new value.</param>
        /// <param name="field2">字段 2 表达式 / The second field expression.</param>
        /// <param name="field2Value">字段 2 新值 / The second field's new value.</param>
        /// <param name="field3">字段 3 表达式 / The third field expression.</param>
        /// <param name="field3Value">字段 3 新值 / The third field's new value.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update<F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return Dao.Update(wrapper, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        /// <summary>
        /// 按 DataFilter 条件更新数据 / Update records matching a DataFilter.
        /// </summary>
        /// <param name="data">实体数据（提供更新字段值）/ The entity data providing the values.</param>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <param name="updateFields">更新字段筛选；为 null 时更新全部字段 / The fields to update; null updates all fields.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(M data, DataFilter filter, QueryFields? updateFields = null)
        {
            return Dao.Update(data, filter, updateFields);
        }

        /// <summary>
        /// 按 DataFilter 条件与字段集合更新数据 / Update by a DataFilter condition with an update-fields collection.
        /// </summary>
        /// <param name="updateFields">更新字段集合 / The fields to update.</param>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(UpdateFields updateFields, DataFilter filter)
        {
            return Dao.Update(updateFields, filter);
        }

        /// <summary>
        /// 按 DataFilter 条件与强类型字段集合更新数据 / Update by a DataFilter condition with a strongly-typed update-fields collection.
        /// </summary>
        /// <param name="updateFields">更新字段集合 / The fields to update.</param>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update(UpdateFields<M> updateFields, DataFilter filter)
        {
            return Dao.Update(updateFields, filter);
        }

        /// <summary>
        /// 按 DataFilter 条件更新指定单个字段 / Update one field by a DataFilter condition.
        /// </summary>
        /// <typeparam name="F">字段类型 / The field type.</typeparam>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <param name="updateField">更新字段表达式 / The field to update.</param>
        /// <param name="fieldValue">字段新值 / The new field value.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update<F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return Dao.Update(filter, updateField, fieldValue);
        }

        /// <summary>
        /// 按 DataFilter 条件更新指定两个字段 / Update two fields by a DataFilter condition.
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / The first field type.</typeparam>
        /// <typeparam name="F2">字段 2 类型 / The second field type.</typeparam>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <param name="field1">字段 1 表达式 / The first field expression.</param>
        /// <param name="field1Value">字段 1 新值 / The first field's new value.</param>
        /// <param name="field2">字段 2 表达式 / The second field expression.</param>
        /// <param name="field2Value">字段 2 新值 / The second field's new value.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update<F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return Dao.Update(filter, field1, field1Value, field2, field2Value);
        }

        /// <summary>
        /// 按 DataFilter 条件更新指定三个字段 / Update three fields by a DataFilter condition.
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / The first field type.</typeparam>
        /// <typeparam name="F2">字段 2 类型 / The second field type.</typeparam>
        /// <typeparam name="F3">字段 3 类型 / The third field type.</typeparam>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <param name="field1">字段 1 表达式 / The first field expression.</param>
        /// <param name="field1Value">字段 1 新值 / The first field's new value.</param>
        /// <param name="field2">字段 2 表达式 / The second field expression.</param>
        /// <param name="field2Value">字段 2 新值 / The second field's new value.</param>
        /// <param name="field3">字段 3 表达式 / The third field expression.</param>
        /// <param name="field3Value">字段 3 新值 / The third field's new value.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Update<F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return Dao.Update(filter, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        /// <summary>
        /// 插入或更新一条数据（主键有值则更新，否则插入）/ Insert or update one record (update when the primary key has a value, otherwise insert).
        /// </summary>
        /// <param name="data">实体数据 / The entity data.</param>
        /// <param name="updateFields">更新字段筛选；为 null 时更新全部字段 / The fields to update; null updates all fields.</param>
        /// <param name="insertId">是否插入主键列 / Whether to insert the primary key column.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool InsertOrUpdate(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null)
        {
            return Dao.InsertOrUpdate(data, updateFields, insertId, commandTimeout);
        }

        /// <summary>
        /// 批量插入数据 / Batch insert records.
        /// </summary>
        /// <param name="dataList">实体数据列表 / The entity data list.</param>
        /// <param name="batchSize">单批命令行数 / Rows per command batch.</param>
        /// <param name="insertId">是否插入主键列 / Whether to insert the primary key column.</param>
        /// <param name="batchHandler">每批操作回调（返回 false 可中断）/ The per-batch callback (returning false aborts).</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool InsertBatch(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return Dao.InsertBatch(dataList, batchSize, insertId, batchHandler, commandTimeout);
        }

        /// <summary>
        /// 按主键批量更新数据 / Batch update records by primary key.
        /// </summary>
        /// <param name="dataList">实体数据列表 / The entity data list.</param>
        /// <param name="updateFields">更新字段筛选；为 null 时更新全部字段 / The fields to update; null updates all fields.</param>
        /// <param name="batchSize">单批命令行数 / Rows per command batch.</param>
        /// <param name="batchHandler">每批操作回调（返回 false 可中断）/ The per-batch callback (returning false aborts).</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool UpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return Dao.UpdateBatch(dataList, updateFields, batchSize, batchHandler, commandTimeout);
        }

        /// <summary>
        /// 批量插入或更新数据（主键有值则更新，否则插入）/ Batch insert-or-update records (update when the primary key has a value, otherwise insert).
        /// </summary>
        /// <param name="dataList">实体数据列表 / The entity data list.</param>
        /// <param name="updateFields">更新字段筛选；为 null 时更新全部字段 / The fields to update; null updates all fields.</param>
        /// <param name="batchSize">单批命令行数 / Rows per command batch.</param>
        /// <param name="insertId">是否插入主键列 / Whether to insert the primary key column.</param>
        /// <param name="batchHandler">每批操作回调（返回 false 可中断）/ The per-batch callback (returning false aborts).</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool InsertOrUpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return Dao.InsertOrUpdateBatch(dataList, updateFields, batchSize, insertId, batchHandler, commandTimeout);
        }

        /// <summary>
        /// 按主键删除一条数据 / Delete one record by primary key.
        /// </summary>
        /// <param name="key">主键值 / The primary key value.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool DeleteById(object key, int? commandTimeout = null)
        {
            return Dao.DeleteById(key, commandTimeout);
        }

        /// <summary>
        /// 按实体主键删除一条数据 / Delete one record by the entity's primary key.
        /// </summary>
        /// <param name="data">实体数据（取其主键定位行）/ The entity data (its primary key locates the row).</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool DeleteById(M data, int? commandTimeout = null)
        {
            return Dao.DeleteById(data, commandTimeout);
        }

        /// <summary>
        /// 按 LambdaWrapper 条件删除数据 / Delete records matching a LambdaWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Delete(LambdaWrapper<M> wrapper)
        {
            return Dao.Delete(wrapper);
        }

        /// <summary>
        /// 按 Lambda 表达式条件删除数据 / Delete records matching a lambda expression.
        /// </summary>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Delete(Expression<Func<M, bool>> condition)
        {
            return Dao.Delete(condition);
        }

        /// <summary>
        /// 按 FilterWrapper 条件删除数据 / Delete records matching a FilterWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Delete(FilterWrapper<M> wrapper)
        {
            return Dao.Delete(wrapper);
        }

        /// <summary>
        /// 按 DataFilter 条件删除数据 / Delete records matching a DataFilter.
        /// </summary>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool Delete(DataFilter filter)
        {
            return Dao.Delete(filter);
        }

        /// <summary>
        /// 按主键列表批量删除数据 / Delete a list of records by primary keys.
        /// </summary>
        /// <param name="keyList">主键列表 / The primary key list.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        public bool DeleteByIds(IEnumerable keyList, int? commandTimeout = null)
        {
            return Dao.DeleteByIds(keyList, commandTimeout);
        }

        /// <summary>
        /// 按 LambdaWrapper 条件统计数量 / Count records matching a LambdaWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>记录数 / The record count.</returns>
        public long Count(LambdaWrapper<M> wrapper)
        {
            return Dao.Count(wrapper);
        }

        /// <summary>
        /// 按 Lambda 表达式条件统计数量 / Count records matching a lambda expression.
        /// </summary>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>记录数 / The record count.</returns>
        public long Count(Expression<Func<M, bool>> condition)
        {
            return Dao.Count(condition);
        }

        /// <summary>
        /// 按 FilterWrapper 条件统计数量；无条件下统计全表 / Count records matching a FilterWrapper, or the whole table when omitted.
        /// </summary>
        /// <param name="wrapper">查询条件包装；为 null 时统计全表 / The query condition wrapper; null counts the whole table.</param>
        /// <returns>记录数 / The record count.</returns>
        public long Count(FilterWrapper<M>? wrapper = null)
        {
            return Dao.Count(wrapper);
        }

        /// <summary>
        /// 按 DataFilter 条件统计数量 / Count records matching a DataFilter.
        /// </summary>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>记录数 / The record count.</returns>
        public long Count(DataFilter filter)
        {
            return Dao.Count(filter);
        }

        #endregion

    }

}
