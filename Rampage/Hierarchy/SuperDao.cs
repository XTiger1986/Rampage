using Microsoft.Extensions.DependencyInjection;
using Rampage.Data;
using Rampage.Data.Client;
using Rampage.Data.Filter;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Localization;
using Rampage.Utilities;
using System;
using System.Collections;
using System.Data;
using System.Linq.Expressions;

namespace Rampage.Hierarchy
{


    /// <summary>
    /// 数据访问门面基类：构造时自动完成初始化（绑定当前 RampageScope）、属性注入与数据库客户端解析，
    /// 全部数据操作一对一转调 <see cref="DatabaseClient"/>
    /// Data-access facade base class: construction auto-initializes (binding to the current RampageScope),
    /// performs property injection and resolves the database client; every operation delegates to <see cref="DatabaseClient"/>
    /// </summary>
    /// <typeparam name="M">实体类型 / entity type</typeparam>
    /// <remarks>
    /// 构造流程：Initialize()（要求已绑定 RampageScope，否则抛异常）→ AutoInject()（属性注入）→ InjectDatabaseClient()（解析数据库客户端）。
    /// 多库环境下按 DatabaseFactory.Config.MappingModelTypes 将实体映射到客户端；单库直接取唯一客户端；
    /// 未映射的实体按 UnmappedModelFallback 策略处理（Throw / FirstClient / 指定客户端名）。
    /// 异步 API 见 SuperDao.Async.cs 分部定义。
    /// </remarks>
    public abstract partial class SuperDao<M> : ISuperDao<M>, IDisposable
        where M : class
    {

        /// <summary>
        /// 服务器本地当前时间（供 SQL 默认值等场景使用）
        /// Current server-local time (for SQL defaults etc.)
        /// </summary>
        protected DateTime LocalNow
        {
            get { return DateTime.Now; }
        }

        /// <summary>
        /// UTC 当前时间（供 SQL 默认值等场景使用）
        /// Current UTC time (for SQL defaults etc.)
        /// </summary>
        protected DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }

        /// <summary>
        /// 当前作用域的服务提供者
        /// Service provider of the current scope
        /// </summary>
        public virtual IServiceProvider? ServiceProvider
        {
            get;
            protected set;
        }

        /// <summary>
        /// 当前作用域的服务定位器
        /// Scoped service locator of the current scope
        /// </summary>
        public virtual IScopedServiceLocator? ServiceLocator
        {
            get;
            protected set;
        }


        /// <summary>
        /// 当前绑定的数据库客户端名称
        /// Name of the currently bound database client
        /// </summary>
        protected virtual string DatabaseName
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 数据库客户端实例（所有数据操作的最终执行者）
        /// Database client instance (final executor of all data operations)
        /// </summary>
        public DatabaseClient DatabaseClient
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 实体 M 的表映射信息
        /// Table mapping of entity M
        /// </summary>
        protected TableMapping ModelMapping
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 构造并自动初始化：绑定当前 RampageScope、注入属性、按映射解析数据库客户端
        /// Construct and auto-initialize: bind to the current RampageScope, inject properties, resolve the client by mapping
        /// </summary>
        public SuperDao()
        {
            Initialize();
            AutoInject();
            InjectDatabaseClient();
        }

        /// <summary>
        /// 构造并显式指定数据库客户端名（多库环境按名称绑定）
        /// Construct with an explicit database client name (bound by name in multi-database setups)
        /// </summary>
        /// <param name="databaseName">数据库客户端名 / database client name</param>
        public SuperDao(string databaseName)
        {
            DatabaseName = databaseName;
            Initialize();
            AutoInject();
            InjectDatabaseClient();
        }

        /// <summary>
        /// 初始化：要求当前已绑定 RampageScope（否则抛异常），
        /// 取出 ServiceProvider / ServiceLocator，并解析实体 M 的表映射
        /// Initialize: requires a bound RampageScope (throws otherwise), reads
        /// ServiceProvider / ServiceLocator and resolves the table mapping of M
        /// </summary>
        protected virtual void Initialize()
        {
            RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(GetType().FullName);
            ServiceProvider = handle.ServiceProvider;
            ServiceLocator = handle.Locator;
            ModelMapping = ModelSchemaStore.GetModelMapping<M>()!;
        }

        /// <summary>
        /// 解析并绑定 DatabaseClient：
        /// 单库直接取唯一客户端；已指定 DatabaseName 则按名称切换；
        /// 多库未指定名称时按 MappingModelTypes 匹配实体；均未命中走 ResolveUnmappedFallback 兜底策略
        /// Resolve and bind the DatabaseClient: a single database uses the only client;
        /// an explicit DatabaseName switches by name; multiple databases match the entity via
        /// MappingModelTypes; otherwise the ResolveUnmappedFallback policy applies
        /// </summary>
        private void InjectDatabaseClient()
        {
            List<DatabaseClient>? databases = null;
            if (ServiceLocator != null)
            {
                databases = ServiceLocator!.GetList<DatabaseClient>();

                if (databases.Count == 1)
                {
                    DatabaseClient = databases[0];
                    return;
                }

                if (string.IsNullOrEmpty(DatabaseName))
                {
                    int di = databases.FindIndex(d =>
                    {
                        if (DatabaseFactory.Config != null && DatabaseFactory.Config.MappingModelTypes != null && DatabaseFactory.Config.MappingModelTypes.ContainsKey(d.Name!))
                        {
                            Dictionary<string, Type> mappingModelTypeDict = DatabaseFactory.Config.MappingModelTypes[d.Name!];
                            return mappingModelTypeDict.ContainsKey(ModelMapping.ModelType!.FullName!);
                        }

                        return false;
                    });
                    if (di >= 0)
                    {
                        DatabaseClient = databases[di];
                    }
                    else
                    {
                        DatabaseClient = ResolveUnmappedFallback(databases);
                    }
                    DatabaseName = DatabaseClient.Name!;
                }
                else
                {
                    ChangeDatabase(DatabaseName);
                }
            }
        }

        /// <summary>
        /// 实体未映射到任何数据库客户端时的兜底处理（依据 DatabaseFactoryConfig.UnmappedModelFallback 策略）：
        /// 缺省或 "Throw" 抛出异常；"FirstClient" 取第一个客户端；其他值视为指定客户端名做精确匹配。
        /// Fallback handling when the model maps to no database client (per DatabaseFactoryConfig.UnmappedModelFallback):
        /// default or "Throw" raises; "FirstClient" takes the first client; any other value must name an existing client.
        /// </summary>
        /// <param name="databases">候选数据库客户端列表 / candidate database clients</param>
        /// <returns>按策略选中的数据库客户端 / the client chosen by the policy</returns>
        /// <exception cref="InvalidOperationException">策略为 Throw、或策略指定的客户端名不存在时抛出 / thrown for the Throw policy or an unknown client name</exception>
        private DatabaseClient ResolveUnmappedFallback(List<DatabaseClient> databases)
        {
            string? policy = DatabaseFactory.Config?.UnmappedModelFallback;
            string modelName = ModelMapping?.ModelType?.FullName ?? typeof(M).FullName!;
            string clientNames = string.Join(", ", databases.Select(d => d.Name));

            if (string.IsNullOrWhiteSpace(policy) || StringTool.EqualsIgnoreCase(policy, "Throw"))
            {
                throw new InvalidOperationException(
                    $"{GetType().FullName}: model '{modelName}' is not mapped to any database client (available: {clientNames}). " +
                    "Specify the database via SuperDao(string databaseName), ensure the table exists with matching columns, " +
                    "or configure DatabaseFactory:UnmappedModelFallback (\"FirstClient\" or a client name).");
            }

            if (StringTool.EqualsIgnoreCase(policy, "FirstClient"))
            {
                return databases[0];
            }

            DatabaseClient? named = databases.Find(d => StringTool.Equals(d.Name, policy));
            if (named == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().FullName}: DatabaseFactory:UnmappedModelFallback '{policy}' does not match any database client (available: {clientNames}).");
            }
            return named;
        }

        /// <summary>
        /// 按名称切换数据库客户端；名称不存在时抛出异常
        /// Switch the database client by name; throws when the name is unknown
        /// </summary>
        /// <param name="databaseName">数据库客户端名 / database client name</param>
        /// <exception cref="InvalidOperationException">名称未注册任何客户端时抛出 / thrown when no client is registered under the name</exception>
        public void ChangeDatabase(string databaseName)
        {
            List<DatabaseClient> databases = ServiceLocator!.GetList<DatabaseClient>();
            DatabaseClient? client = databases.Find(d => StringTool.Equals(d.Name, databaseName));
            if (client == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().FullName}: database client '{databaseName}' not found (available: {string.Join(", ", databases.Select(d => d.Name))}).");
            }
            DatabaseClient = client;
            DatabaseName = client.Name!;
        }

        /// <summary>
        /// 属性注入器实例（Dispose 时释放）
        /// The property injecter (disposed on Dispose)
        /// </summary>
        protected AutoInjecter? Injecter;

        /// <summary>
        /// 对当前实例执行属性注入（ServiceLocator 为空时抛异常）
        /// Perform property injection on this instance (throws when ServiceLocator is null)
        /// </summary>
        private void AutoInject()
        {
            if (ServiceLocator == null)
            {
                throw new InvalidOperationException($"{GetType().FullName}: ServiceLocator is null before AutoInject.");
            }
            Injecter = new AutoInjecter(ServiceLocator);
            Injecter.Build(this);
        }

        /// <summary>
        /// 默认语言资源缓存
        /// Cached default language resource
        /// </summary>
        protected LocalizationResource? _defaultLanguage;
        /// <summary>
        /// 默认语言资源（首次访问时从 LocalizationManager 加载并缓存）
        /// Default language resource (loaded from LocalizationManager and cached on first access)
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

        #region IDao

        /// <summary>开启事务 / Begin a transaction</summary>
        /// <param name="iso">事务隔离级别，默认 ReadCommitted / isolation level, ReadCommitted by default</param>
        public void BeginTransaction(IsolationLevel iso = IsolationLevel.ReadCommitted)
        {
            DatabaseClient.BeginTransaction(iso);
        }

        /// <summary>提交事务 / Commit the transaction</summary>
        public void CommitTransaction()
        {
            DatabaseClient.CommitTransaction();
        }

        /// <summary>回滚事务 / Roll back the transaction</summary>
        public void RollbackTransaction()
        {
            DatabaseClient.RollbackTransaction();
        }

        /// <summary>按 LambdaWrapper 条件查询数据列表 / Query a data list by a LambdaWrapper</summary>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>数据列表 / the result list</returns>
        public List<M> SelectList(LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.SelectList(wrapper);
        }

        /// <summary>按 LambdaWrapper 条件查询数据列表，结果装配为指定类型 / Query a data list by a LambdaWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>数据列表 / the result list</returns>
        public List<R> SelectList<R>(LambdaWrapper<M> wrapper) where R : class
        {
            return DatabaseClient.SelectList<M, R>(wrapper);
        }

        /// <summary>按 Lambda 表达式条件查询数据列表 / Query a data list by a lambda expression condition</summary>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>数据列表 / the result list</returns>
        public List<M> SelectList(Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.SelectList(condition);
        }

        /// <summary>按 Lambda 表达式条件查询数据列表，结果装配为指定类型 / Query a data list by a lambda expression condition, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>数据列表 / the result list</returns>
        public List<R> SelectList<R>(Expression<Func<M, bool>> condition) where R : class
        {
            return DatabaseClient.SelectList<M, R>(condition);
        }

        /// <summary>按 FilterWrapper 条件查询数据列表 / Query a data list by a FilterWrapper</summary>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <returns>数据列表 / the result list</returns>
        public List<M> SelectList(FilterWrapper<M>? wrapper)
        {
            return DatabaseClient.SelectList(wrapper);
        }

        /// <summary>按 FilterWrapper 条件查询数据列表，结果装配为指定类型 / Query a data list by a FilterWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <returns>数据列表 / the result list</returns>
        public List<R> SelectList<R>(FilterWrapper<M>? wrapper) where R : class
        {
            return DatabaseClient.SelectList<M, R>(wrapper);
        }

        /// <summary>按 DataFilter 数据过滤器查询数据列表（虚方法，可覆写定制）/ Query a data list by a DataFilter (virtual, overridable)</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>数据列表 / the result list</returns>
        public virtual List<M> SelectList(DataFilter filter)
        {
            return DatabaseClient.SelectList<M>(filter);
        }

        /// <summary>按 DataFilter 数据过滤器查询数据列表，结果装配为指定类型（虚方法，可覆写定制）/ Query a data list by a DataFilter, materialized as R (virtual, overridable)</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>数据列表 / the result list</returns>
        public virtual List<R> SelectList<R>(DataFilter filter) where R : class
        {
            return DatabaseClient.SelectList<M, R>(filter);
        }

        /// <summary>按 LambdaWrapper 条件分页查询数据列表 / Query one page of data by a LambdaWrapper</summary>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填（含 TotalPage），已预置则跳过 count / pager: when RecordCount is unset a COUNT runs and is written back (including TotalPage); a preset value skips it</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        public List<M> SelectPage(PagerInfo pagerInfo, LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.SelectPage<M>(pagerInfo, wrapper);
        }

        /// <summary>按 LambdaWrapper 条件分页查询数据列表，结果装配为指定类型 / Query one page of data by a LambdaWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        public List<R> SelectPage<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper) where R : class
        {
            return DatabaseClient.SelectPage<M, R>(pagerInfo, wrapper);
        }

        /// <summary>按 Lambda 表达式条件分页查询数据列表 / Query one page of data by a lambda expression condition</summary>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        public List<M> SelectPage(PagerInfo pagerInfo, Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.SelectPage(pagerInfo, condition);
        }

        /// <summary>按 Lambda 表达式条件分页查询数据列表，结果装配为指定类型 / Query one page of data by a lambda expression condition, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        public List<R> SelectPage<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition) where R : class
        {
            return DatabaseClient.SelectPage<M, R>(pagerInfo, condition);
        }

        /// <summary>按 FilterWrapper 条件分页查询数据列表 / Query one page of data by a FilterWrapper</summary>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        public List<M> SelectPage(PagerInfo pagerInfo, FilterWrapper<M>? wrapper)
        {
            return DatabaseClient.SelectPage(pagerInfo, wrapper);
        }

        /// <summary>按 FilterWrapper 条件分页查询数据列表，结果装配为指定类型 / Query one page of data by a FilterWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        public List<R> SelectPage<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper) where R : class
        {
            return DatabaseClient.SelectPage<M, R>(pagerInfo, wrapper);
        }

        /// <summary>按 DataFilter 数据过滤器分页查询数据列表 / Query one page of data by a DataFilter</summary>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        public List<M> SelectPage(PagerInfo pagerInfo, DataFilter filter)
        {
            return DatabaseClient.SelectPage<M>(pagerInfo, filter);
        }

        /// <summary>按 DataFilter 数据过滤器分页查询数据列表，结果装配为指定类型 / Query one page of data by a DataFilter, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        public List<R> SelectPage<R>(PagerInfo pagerInfo, DataFilter filter) where R : class
        {
            return DatabaseClient.SelectPage<M, R>(pagerInfo, filter);
        }

        /// <summary>按主键集合批量查询数据列表 / Query a data list by a set of primary keys</summary>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>数据列表 / the result list</returns>
        public List<M> SelectByIds(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null)
        {
            return DatabaseClient.SelectByIds<M>(keyList, resultFields, commandTimeout);
        }

        /// <summary>按主键集合批量查询数据列表，结果装配为指定类型 / Query a data list by a set of primary keys, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>数据列表 / the result list</returns>
        public List<R> SelectByIds<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) where R : class
        {
            return DatabaseClient.SelectByIds<M, R>(keyList, resultFields, commandTimeout);
        }

        /// <summary>
        /// 按 LambdaWrapper 条件查询单条数据
        /// Query one data row by a LambdaWrapper
        /// </summary>
        /// <remarks>实现会把 wrapper 的 Limit 改写为 1 后查询 / the implementation rewrites wrapper.Limit to 1 before querying</remarks>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public M? SelectOne(LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.SelectOne(wrapper);
        }

        /// <summary>
        /// 按 LambdaWrapper 条件查询单条数据，结果装配为指定类型
        /// Query one data row by a LambdaWrapper, materialized as R
        /// </summary>
        /// <remarks>实现会把 wrapper 的 Limit 改写为 1 后查询 / the implementation rewrites wrapper.Limit to 1 before querying</remarks>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public R? SelectOne<R>(LambdaWrapper<M> wrapper) where R : class
        {
            return DatabaseClient.SelectOne<M, R>(wrapper);
        }

        /// <summary>按 Lambda 表达式条件查询单条数据 / Query one data row by a lambda expression condition</summary>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public M? SelectOne(Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.SelectOne(condition);
        }

        /// <summary>按 Lambda 表达式条件查询单条数据，结果装配为指定类型 / Query one data row by a lambda expression condition, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public R? SelectOne<R>(Expression<Func<M, bool>> condition) where R : class
        {
            return DatabaseClient.SelectOne<M, R>(condition);
        }

        /// <summary>
        /// 按 FilterWrapper 条件查询单条数据
        /// Query one data row by a FilterWrapper
        /// </summary>
        /// <remarks>实现会把 wrapper 的 Limit 改写为 1 后查询 / the implementation rewrites wrapper.Limit to 1 before querying</remarks>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public M? SelectOne(FilterWrapper<M> wrapper)
        {
            return DatabaseClient.SelectOne(wrapper);
        }

        /// <summary>
        /// 按 FilterWrapper 条件查询单条数据，结果装配为指定类型
        /// Query one data row by a FilterWrapper, materialized as R
        /// </summary>
        /// <remarks>实现会把 wrapper 的 Limit 改写为 1 后查询 / the implementation rewrites wrapper.Limit to 1 before querying</remarks>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public R? SelectOne<R>(FilterWrapper<M> wrapper) where R : class
        {
            return DatabaseClient.SelectOne<M, R>(wrapper);
        }

        /// <summary>按 DataFilter 数据过滤器查询单条数据 / Query one data row by a DataFilter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public M? SelectOne(DataFilter filter)
        {
            return DatabaseClient.SelectOne<M>(filter);
        }

        /// <summary>按 DataFilter 数据过滤器查询单条数据，结果装配为指定类型 / Query one data row by a DataFilter, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public R? SelectOne<R>(DataFilter filter) where R : class
        {
            return DatabaseClient.SelectOne<M, R>(filter);
        }

        /// <summary>按主键查询单条数据 / Query one data row by primary key</summary>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public M? SelectById(object key, QueryFields? resultFields = null, int? commandTimeout = null)
        {
            return DatabaseClient.SelectById<M>(key, resultFields, commandTimeout);
        }

        /// <summary>按主键查询单条数据，结果装配为指定类型 / Query one data row by primary key, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        public R? SelectById<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null) where R : class
        {
            return DatabaseClient.SelectById<M, R>(key, resultFields, commandTimeout);
        }

        /// <summary>
        /// 插入一条数据；自增主键列由数据库生成，插入后自动回填到实体属性
        /// Insert one row; an auto-increment primary key is generated by the database and written back to the entity
        /// </summary>
        /// <param name="data">实体数据 / the entity to insert</param>
        /// <param name="insertId">是否连同主键一起插入（默认 false，主键由数据库生成）/ whether to insert the primary key too (false by default: generated by the database)</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Insert(M data, bool insertId = false, int? commandTimeout = null)
        {
            return DatabaseClient.Insert(data, insertId, commandTimeout);
        }

        /// <summary>按主键更新一条数据（主键值取自实体属性）/ Update one row by its primary key (taken from the entity)</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool UpdateById(M data, QueryFields? updateFields = null, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(data, updateFields, commandTimeout);
        }

        /// <summary>按主键更新指定字段集合 / Update the given fields of one row by primary key</summary>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool UpdateById(object id, UpdateFields updateFields, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById<M>(id, updateFields, commandTimeout);
        }

        /// <summary>按主键更新指定字段集合（强类型）/ Update the given fields of one row by primary key (strongly typed)</summary>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool UpdateById(object id, UpdateFields<M> updateFields, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(id, updateFields, commandTimeout);
        }

        /// <summary>按主键更新单个字段（Lambda 指定字段）/ Update a single field by primary key (field selected by lambda)</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool UpdateById<F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(id, updateField, fieldValue, commandTimeout);
        }

        /// <summary>按主键更新两个字段 / Update two fields by primary key</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool UpdateById<F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(id, field1, field1Value, field2, field2Value, commandTimeout);
        }

        /// <summary>按主键更新三个字段 / Update three fields by primary key</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <typeparam name="F3">字段 3 类型 / type of field 3</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="field3">字段 3 选择表达式 / selector of field 3</param>
        /// <param name="field3Value">字段 3 新值 / new value of field 3</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool UpdateById<F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(id, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout);
        }

        /// <summary>按 LambdaWrapper 条件更新数据（新值由实体承载）/ Update rows matching a LambdaWrapper with entity values</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null)
        {
            return DatabaseClient.Update(data, wrapper, updateFields);
        }

        /// <summary>按 LambdaWrapper 条件更新指定字段集合 / Update the given fields of rows matching a LambdaWrapper</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(UpdateFields updateFields, LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.Update(updateFields, wrapper);
        }

        /// <summary>按 LambdaWrapper 条件更新指定字段集合（强类型）/ Update the given fields of rows matching a LambdaWrapper (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.Update(updateFields, wrapper);
        }

        /// <summary>按 LambdaWrapper 条件更新单个字段 / Update a single field of rows matching a LambdaWrapper</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return DatabaseClient.Update(wrapper, updateField, fieldValue);
        }

        /// <summary>按 LambdaWrapper 条件更新两个字段 / Update two fields of rows matching a LambdaWrapper</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return DatabaseClient.Update(wrapper, field1, field1Value, field2, field2Value);
        }

        /// <summary>按 LambdaWrapper 条件更新三个字段 / Update three fields of rows matching a LambdaWrapper</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <typeparam name="F3">字段 3 类型 / type of field 3</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="field3">字段 3 选择表达式 / selector of field 3</param>
        /// <param name="field3Value">字段 3 新值 / new value of field 3</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F1, F2, F3>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return DatabaseClient.Update(wrapper, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        /// <summary>按 Lambda 表达式条件更新数据（新值由实体承载）/ Update rows matching a lambda expression with entity values</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null)
        {
            return DatabaseClient.Update(data, condition, updateFields);
        }

        /// <summary>按 Lambda 表达式条件更新指定字段集合 / Update the given fields of rows matching a lambda expression</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
		public bool Update(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null)
        {
            return DatabaseClient.Update(updateFields, condition, commandTimeout);
        }

        /// <summary>按 Lambda 表达式条件更新指定字段集合（强类型）/ Update the given fields of rows matching a lambda expression (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null)
        {
            return DatabaseClient.Update(updateFields, condition, commandTimeout);
        }

        /// <summary>按 Lambda 表达式条件更新单个字段 / Update a single field of rows matching a lambda expression</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null)
        {
            return DatabaseClient.Update(condition, updateField, fieldValue, commandTimeout);
        }

        /// <summary>按 Lambda 表达式条件更新两个字段 / Update two fields of rows matching a lambda expression</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null)
        {
            return DatabaseClient.Update(condition, field1, field1Value, field2, field2Value, commandTimeout);
        }

        /// <summary>按 Lambda 表达式条件更新三个字段 / Update three fields of rows matching a lambda expression</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <typeparam name="F3">字段 3 类型 / type of field 3</typeparam>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="field3">字段 3 选择表达式 / selector of field 3</param>
        /// <param name="field3Value">字段 3 新值 / new value of field 3</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null)
        {
            return DatabaseClient.Update(condition, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout);
        }

        /// <summary>按 FilterWrapper 条件更新数据（新值由实体承载）/ Update rows matching a FilterWrapper with entity values</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null)
        {
            return DatabaseClient.Update(data, wrapper, updateFields);
        }

        /// <summary>按 FilterWrapper 条件更新指定字段集合 / Update the given fields of rows matching a FilterWrapper</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(UpdateFields updateFields, FilterWrapper<M> wrapper)
        {
            return DatabaseClient.Update(updateFields, wrapper);
        }

        /// <summary>按 FilterWrapper 条件更新指定字段集合（强类型）/ Update the given fields of rows matching a FilterWrapper (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(UpdateFields<M> updateFields, FilterWrapper<M> wrapper)
        {
            return DatabaseClient.Update(updateFields, wrapper);
        }

        /// <summary>按 FilterWrapper 条件更新单个字段 / Update a single field of rows matching a FilterWrapper</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return DatabaseClient.Update(wrapper, updateField, fieldValue);
        }

        /// <summary>按 FilterWrapper 条件更新两个字段 / Update two fields of rows matching a FilterWrapper</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return DatabaseClient.Update(wrapper, field1, field1Value, field2, field2Value);
        }

        /// <summary>按 FilterWrapper 条件更新三个字段 / Update three fields of rows matching a FilterWrapper</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <typeparam name="F3">字段 3 类型 / type of field 3</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="field3">字段 3 选择表达式 / selector of field 3</param>
        /// <param name="field3Value">字段 3 新值 / new value of field 3</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return DatabaseClient.Update(wrapper, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        /// <summary>按 DataFilter 数据过滤器条件更新数据（新值由实体承载）/ Update rows matching a DataFilter with entity values</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(M data, DataFilter filter, QueryFields? updateFields = null)
        {
            return DatabaseClient.Update(data, filter, updateFields);
        }

        /// <summary>按 DataFilter 数据过滤器条件更新指定字段集合 / Update the given fields of rows matching a DataFilter</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(UpdateFields updateFields, DataFilter filter)
        {
            return DatabaseClient.Update(updateFields, filter);
        }

        /// <summary>按 DataFilter 数据过滤器条件更新指定字段集合（强类型）/ Update the given fields of rows matching a DataFilter (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update(UpdateFields<M> updateFields, DataFilter filter)
        {
            return DatabaseClient.Update<M>(updateFields, filter);
        }

        /// <summary>按 DataFilter 数据过滤器条件更新单个字段 / Update a single field of rows matching a DataFilter</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return DatabaseClient.Update(filter, updateField, fieldValue);
        }

        /// <summary>按 DataFilter 数据过滤器条件更新两个字段 / Update two fields of rows matching a DataFilter</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return DatabaseClient.Update(filter, field1, field1Value, field2, field2Value);
        }

        /// <summary>按 DataFilter 数据过滤器条件更新三个字段 / Update three fields of rows matching a DataFilter</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <typeparam name="F3">字段 3 类型 / type of field 3</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="field3">字段 3 选择表达式 / selector of field 3</param>
        /// <param name="field3Value">字段 3 新值 / new value of field 3</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Update<F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return DatabaseClient.Update(filter, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        /// <summary>
        /// 插入或更新一条数据：主键有值走更新，无值走插入
        /// Insert or update one row: update when the primary key has a value, insert otherwise
        /// </summary>
        /// <param name="data">实体数据 / the entity</param>
        /// <param name="updateFields">更新时要更新的字段；null 更新全部映射列 / fields to update on update; null updates all mapped columns</param>
        /// <param name="insertId">插入时是否连同主键插入 / whether to insert the primary key too when inserting</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool InsertOrUpdate(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null)
        {
            return DatabaseClient.InsertOrUpdate(data, updateFields, insertId, commandTimeout);
        }

        /// <summary>
        /// 分批插入数据列表 / Insert a list of rows in batches
        /// </summary>
        /// <param name="dataList">数据列表 / the rows to insert</param>
        /// <param name="batchSize">每批条数 / rows per batch</param>
        /// <param name="insertId">是否连同主键插入 / whether to insert primary keys too</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool InsertBatch(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return DatabaseClient.InsertBatch(dataList, batchSize, insertId, batchHandler, commandTimeout);
        }

        /// <summary>
        /// 按主键分批更新数据列表 / Update rows in batches by primary key
        /// </summary>
        /// <param name="dataList">数据列表（主键取自实体属性）/ the rows, primary keys taken from the entities</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="batchSize">每批条数 / rows per batch</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool UpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateBatch(dataList, updateFields, batchSize, batchHandler, commandTimeout);
        }

        /// <summary>
        /// 分批插入或更新数据列表（主键有值走更新，无值走插入）
        /// Insert or update rows in batches (update when the primary key has a value, insert otherwise)
        /// </summary>
        /// <param name="dataList">数据列表 / the rows</param>
        /// <param name="updateFields">更新时要更新的字段；null 更新全部映射列 / fields to update on update; null updates all mapped columns</param>
        /// <param name="batchSize">每批条数 / rows per batch</param>
        /// <param name="insertId">插入时是否连同主键插入 / whether to insert primary keys too when inserting</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool InsertOrUpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return DatabaseClient.InsertOrUpdateBatch(dataList, updateFields, batchSize, insertId, batchHandler, commandTimeout);
        }

        /// <summary>按主键删除一条数据 / Delete one row by primary key</summary>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool DeleteById(object key, int? commandTimeout = null)
        {
            return DatabaseClient.DeleteById<M>(key, commandTimeout);
        }

        public bool DeleteById(M data, int? commandTimeout = null)
        {
            return DatabaseClient.DeleteById<M>(data, commandTimeout);
        }

        /// <summary>按 LambdaWrapper 条件删除数据 / Delete rows matching a LambdaWrapper</summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Delete(LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.Delete(wrapper);
        }

        /// <summary>按 Lambda 表达式条件删除数据 / Delete rows matching a lambda expression</summary>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Delete(Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.Delete(condition);
        }

        /// <summary>按 FilterWrapper 条件删除数据 / Delete rows matching a FilterWrapper</summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Delete(FilterWrapper<M> wrapper)
        {
            return DatabaseClient.Delete(wrapper);
        }

        /// <summary>按 DataFilter 数据过滤器条件删除数据 / Delete rows matching a DataFilter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>是否成功 / true on success</returns>
        public bool Delete(DataFilter filter)
        {
            return DatabaseClient.Delete<M>(filter);
        }

        /// <summary>按主键集合批量删除数据 / Delete rows by a set of primary keys</summary>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        public bool DeleteByIds(IEnumerable keyList, int? commandTimeout = null)
        {
            return DatabaseClient.DeleteByIds<M>(keyList, commandTimeout);
        }

        /// <summary>按 LambdaWrapper 条件统计记录数 / Count rows matching a LambdaWrapper</summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>记录数 / the row count</returns>
        public long Count(LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.Count(wrapper);
        }

        /// <summary>按 Lambda 表达式条件统计记录数 / Count rows matching a lambda expression</summary>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <returns>记录数 / the row count</returns>
        public long Count(Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.Count(condition);
        }

        /// <summary>按 FilterWrapper 条件统计记录数；wrapper 为 null 时统计全表 / Count rows matching a FilterWrapper; a null wrapper counts the whole table</summary>
        /// <param name="wrapper">条件包装器；null 表示无条件 / the condition wrapper; null means no condition</param>
        /// <returns>记录数 / the row count</returns>
        public long Count(FilterWrapper<M>? wrapper = null)
        {
            return DatabaseClient.Count(wrapper);
        }

        /// <summary>按 DataFilter 数据过滤器条件统计记录数 / Count rows matching a DataFilter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>记录数 / the row count</returns>
        public long Count(DataFilter filter)
        {
            return DatabaseClient.Count<M>(filter);
        }

        #endregion

        /// <summary>释放属性注入器持有的资源 / Dispose resources held by the property injecter</summary>
        public void Dispose()
        {
            if (Injecter != null)
            {
                Injecter.Dispose();
            }
        }
    }

}
