using Rampage.Data;
using Rampage.Data.Client;
using Rampage.Data.Filter;
using Rampage.Data.Query;
using System.Collections;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Hierarchy
{

    /// <summary>
    /// <see cref="SuperDao{M}"/> 的异步 API 分部定义：全部方法一对一异步转调 DatabaseClient
    /// Asynchronous partial API of <see cref="SuperDao{M}"/>: every method delegates to DatabaseClient asynchronously
    /// </summary>
    /// <typeparam name="M">实体类型 / entity type</typeparam>
    public abstract partial class SuperDao<M>
        where M : class
    {

        #region IDao Async

        /// <summary>异步开启事务 / Asynchronously begin a transaction</summary>
        /// <param name="iso">事务隔离级别，默认 ReadCommitted / isolation level, ReadCommitted by default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        public Task BeginTransactionAsync(IsolationLevel iso = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.BeginTransactionAsync(iso, cancellationToken);
        }

        /// <summary>异步提交事务 / Asynchronously commit the transaction</summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CommitTransactionAsync(cancellationToken);
        }

        /// <summary>异步回滚事务 / Asynchronously roll back the transaction</summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            return DatabaseClient.RollbackTransactionAsync(cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件查询数据列表 / Asynchronously query a data list by a LambdaWrapper</summary>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public Task<List<M>> SelectListAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectListAsync(wrapper, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件查询数据列表，结果装配为指定类型 / Asynchronously query a data list by a LambdaWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public Task<List<R>> SelectListAsync<R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectListAsync<M, R>(wrapper, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件查询数据列表 / Asynchronously query a data list by a lambda expression condition</summary>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public Task<List<M>> SelectListAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectListAsync(condition, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件查询数据列表，结果装配为指定类型 / Asynchronously query a data list by a lambda expression condition, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public Task<List<R>> SelectListAsync<R>(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectListAsync<M, R>(condition, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件查询数据列表 / Asynchronously query a data list by a FilterWrapper</summary>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public Task<List<M>> SelectListAsync(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectListAsync(wrapper, cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件查询数据列表，结果装配为指定类型 / Asynchronously query a data list by a FilterWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public Task<List<R>> SelectListAsync<R>(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectListAsync<M, R>(wrapper, cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器查询数据列表（虚方法，可覆写定制）/ Asynchronously query a data list by a DataFilter (virtual, overridable)</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public virtual Task<List<M>> SelectListAsync(DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectListAsync<M>(filter, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器查询数据列表，结果装配为指定类型（虚方法，可覆写定制）/ Asynchronously query a data list by a DataFilter, materialized as R (virtual, overridable)</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public virtual Task<List<R>> SelectListAsync<R>(DataFilter filter, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectListAsync<M, R>(filter, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件分页查询数据列表 / Asynchronously query one page of data by a LambdaWrapper</summary>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        public Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectPageAsync<M>(pagerInfo, wrapper, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件分页查询数据列表，结果装配为指定类型 / Asynchronously query one page of data by a LambdaWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        public Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectPageAsync<M, R>(pagerInfo, wrapper, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件分页查询数据列表 / Asynchronously query one page of data by a lambda expression condition</summary>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        public Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectPageAsync(pagerInfo, condition, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件分页查询数据列表，结果装配为指定类型 / Asynchronously query one page of data by a lambda expression condition, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        public Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectPageAsync<M, R>(pagerInfo, condition, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件分页查询数据列表 / Asynchronously query one page of data by a FilterWrapper</summary>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        public Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectPageAsync(pagerInfo, wrapper, cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件分页查询数据列表，结果装配为指定类型 / Asynchronously query one page of data by a FilterWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        public Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectPageAsync<M, R>(pagerInfo, wrapper, cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器分页查询数据列表 / Asynchronously query one page of data by a DataFilter</summary>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        public Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectPageAsync<M>(pagerInfo, filter, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器分页查询数据列表，结果装配为指定类型 / Asynchronously query one page of data by a DataFilter, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：未预置 RecordCount 时自动执行 COUNT 并回填，已预置则跳过 / pager: COUNT runs and is written back when RecordCount is unset; a preset value skips it</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        public Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, DataFilter filter, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectPageAsync<M, R>(pagerInfo, filter, cancellationToken: cancellationToken);
        }

        /// <summary>异步按主键集合批量查询数据列表 / Asynchronously query a data list by a set of primary keys</summary>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public Task<List<M>> SelectByIdsAsync(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectByIdsAsync<M>(keyList, resultFields, commandTimeout, cancellationToken);
        }

        /// <summary>异步按主键集合批量查询数据列表，结果装配为指定类型 / Asynchronously query a data list by a set of primary keys, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        public Task<List<R>> SelectByIdsAsync<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectByIdsAsync<M, R>(keyList, resultFields, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件查询单条数据
        /// Asynchronously query one data row by a LambdaWrapper
        /// </summary>
        /// <remarks>实现会把 wrapper 的 Limit 改写为 1 后查询 / the implementation rewrites wrapper.Limit to 1 before querying</remarks>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<M?> SelectOneAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectOneAsync(wrapper, cancellationToken);
        }

        /// <summary>
        /// 异步按 LambdaWrapper 条件查询单条数据，结果装配为指定类型
        /// Asynchronously query one data row by a LambdaWrapper, materialized as R
        /// </summary>
        /// <remarks>实现会把 wrapper 的 Limit 改写为 1 后查询 / the implementation rewrites wrapper.Limit to 1 before querying</remarks>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<R?> SelectOneAsync<R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectOneAsync<M, R>(wrapper, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件查询单条数据 / Asynchronously query one data row by a lambda expression condition</summary>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<M?> SelectOneAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectOneAsync(condition, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件查询单条数据，结果装配为指定类型 / Asynchronously query one data row by a lambda expression condition, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<R?> SelectOneAsync<R>(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectOneAsync<M, R>(condition, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件查询单条数据
        /// Asynchronously query one data row by a FilterWrapper
        /// </summary>
        /// <remarks>实现会把 wrapper 的 Limit 改写为 1 后查询 / the implementation rewrites wrapper.Limit to 1 before querying</remarks>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<M?> SelectOneAsync(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectOneAsync(wrapper, cancellationToken);
        }

        /// <summary>
        /// 异步按 FilterWrapper 条件查询单条数据，结果装配为指定类型
        /// Asynchronously query one data row by a FilterWrapper, materialized as R
        /// </summary>
        /// <remarks>实现会把 wrapper 的 Limit 改写为 1 后查询 / the implementation rewrites wrapper.Limit to 1 before querying</remarks>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<R?> SelectOneAsync<R>(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectOneAsync<M, R>(wrapper, cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器查询单条数据 / Asynchronously query one data row by a DataFilter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<M?> SelectOneAsync(DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectOneAsync<M>(filter, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器查询单条数据，结果装配为指定类型 / Asynchronously query one data row by a DataFilter, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<R?> SelectOneAsync<R>(DataFilter filter, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectOneAsync<M, R>(filter, cancellationToken: cancellationToken);
        }

        /// <summary>异步按主键查询单条数据 / Asynchronously query one data row by primary key</summary>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<M?> SelectByIdAsync(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectByIdAsync<M>(key, resultFields, commandTimeout, cancellationToken);
        }

        /// <summary>异步按主键查询单条数据，结果装配为指定类型 / Asynchronously query one data row by primary key, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        public Task<R?> SelectByIdAsync<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectByIdAsync<M, R>(key, resultFields, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// 异步插入一条数据；自增主键列由数据库生成，插入后自动回填到实体属性
        /// Asynchronously insert one row; an auto-increment primary key is generated by the database and written back to the entity
        /// </summary>
        /// <param name="data">实体数据 / the entity to insert</param>
        /// <param name="insertId">是否连同主键一起插入（默认 false，主键由数据库生成）/ whether to insert the primary key too (false by default: generated by the database)</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> InsertAsync(M data, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.InsertAsync(data, insertId, commandTimeout, cancellationToken);
        }

        /// <summary>异步按主键更新一条数据（主键值取自实体属性）/ Asynchronously update one row by its primary key (taken from the entity)</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateByIdAsync(M data, QueryFields? updateFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(data, updateFields, commandTimeout, cancellationToken);
        }

        /// <summary>异步按主键更新指定字段集合 / Asynchronously update the given fields of one row by primary key</summary>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateByIdAsync(object id, UpdateFields updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync<M>(id, updateFields, commandTimeout, cancellationToken);
        }

        /// <summary>异步按主键更新指定字段集合（强类型）/ Asynchronously update the given fields of one row by primary key (strongly typed)</summary>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateByIdAsync(object id, UpdateFields<M> updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(id, updateFields, commandTimeout, cancellationToken);
        }

        /// <summary>异步按主键更新单个字段（Lambda 指定字段）/ Asynchronously update a single field by primary key (field selected by lambda)</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateByIdAsync<F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(id, updateField, fieldValue, commandTimeout, cancellationToken);
        }

        /// <summary>异步按主键更新两个字段 / Asynchronously update two fields by primary key</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateByIdAsync<F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(id, field1, field1Value, field2, field2Value, commandTimeout, cancellationToken);
        }

        /// <summary>异步按主键更新三个字段 / Asynchronously update three fields by primary key</summary>
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
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateByIdAsync<F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(id, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件更新数据（新值由实体承载）/ Asynchronously update rows matching a LambdaWrapper with entity values</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(data, wrapper, updateFields, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件更新指定字段集合 / Asynchronously update the given fields of rows matching a LambdaWrapper</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(UpdateFields updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, wrapper, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件更新指定字段集合（强类型）/ Asynchronously update the given fields of rows matching a LambdaWrapper (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, wrapper, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件更新单个字段 / Asynchronously update a single field of rows matching a LambdaWrapper</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, updateField, fieldValue, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件更新两个字段 / Asynchronously update two fields of rows matching a LambdaWrapper</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, field1, field1Value, field2, field2Value, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件更新三个字段 / Asynchronously update three fields of rows matching a LambdaWrapper</summary>
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
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F1, F2, F3>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, field1, field1Value, field2, field2Value, field3, field3Value, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件更新数据（新值由实体承载）/ Asynchronously update rows matching a lambda expression with entity values</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(data, condition, updateFields, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件更新指定字段集合 / Asynchronously update the given fields of rows matching a lambda expression</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, condition, commandTimeout, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件更新指定字段集合（强类型）/ Asynchronously update the given fields of rows matching a lambda expression (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, condition, commandTimeout, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件更新单个字段 / Asynchronously update a single field of rows matching a lambda expression</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(condition, updateField, fieldValue, commandTimeout, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件更新两个字段 / Asynchronously update two fields of rows matching a lambda expression</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(condition, field1, field1Value, field2, field2Value, commandTimeout, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件更新三个字段 / Asynchronously update three fields of rows matching a lambda expression</summary>
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
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(condition, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout, cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件更新数据（新值由实体承载）/ Asynchronously update rows matching a FilterWrapper with entity values</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(data, wrapper, updateFields, cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件更新指定字段集合 / Asynchronously update the given fields of rows matching a FilterWrapper</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(UpdateFields updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, wrapper, cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件更新指定字段集合（强类型）/ Asynchronously update the given fields of rows matching a FilterWrapper (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(UpdateFields<M> updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, wrapper, cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件更新单个字段 / Asynchronously update a single field of rows matching a FilterWrapper</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, updateField, fieldValue, cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件更新两个字段 / Asynchronously update two fields of rows matching a FilterWrapper</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, field1, field1Value, field2, field2Value, cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件更新三个字段 / Asynchronously update three fields of rows matching a FilterWrapper</summary>
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
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, field1, field1Value, field2, field2Value, field3, field3Value, cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器条件更新数据（新值由实体承载）/ Asynchronously update rows matching a DataFilter with entity values</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(M data, DataFilter filter, QueryFields? updateFields = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(data, filter, updateFields, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器条件更新指定字段集合 / Asynchronously update the given fields of rows matching a DataFilter</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(UpdateFields updateFields, DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, filter, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器条件更新指定字段集合（强类型）/ Asynchronously update the given fields of rows matching a DataFilter (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync(UpdateFields<M> updateFields, DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync<M>(updateFields, filter, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器条件更新单个字段 / Asynchronously update a single field of rows matching a DataFilter</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(filter, updateField, fieldValue, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器条件更新两个字段 / Asynchronously update two fields of rows matching a DataFilter</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(filter, field1, field1Value, field2, field2Value, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器条件更新三个字段 / Asynchronously update three fields of rows matching a DataFilter</summary>
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
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateAsync<F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(filter, field1, field1Value, field2, field2Value, field3, field3Value, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 异步插入或更新一条数据：主键有值走更新，无值走插入
        /// Asynchronously insert or update one row: update when the primary key has a value, insert otherwise
        /// </summary>
        /// <param name="data">实体数据 / the entity</param>
        /// <param name="updateFields">更新时要更新的字段；null 更新全部映射列 / fields to update on update; null updates all mapped columns</param>
        /// <param name="insertId">插入时是否连同主键插入 / whether to insert the primary key too when inserting</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> InsertOrUpdateAsync(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.InsertOrUpdateAsync(data, updateFields, insertId, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// 异步分批插入数据列表 / Asynchronously insert a list of rows in batches
        /// </summary>
        /// <param name="dataList">数据列表 / the rows to insert</param>
        /// <param name="batchSize">每批条数 / rows per batch</param>
        /// <param name="insertId">是否连同主键插入 / whether to insert primary keys too</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> InsertBatchAsync(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.InsertBatchAsync(dataList, batchSize, insertId, batchHandler, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// 异步按主键分批更新数据列表 / Asynchronously update rows in batches by primary key
        /// </summary>
        /// <param name="dataList">数据列表（主键取自实体属性）/ the rows, primary keys taken from the entities</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="batchSize">每批条数 / rows per batch</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> UpdateBatchAsync(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateBatchAsync(dataList, updateFields, batchSize, batchHandler, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// 异步分批插入或更新数据列表（主键有值走更新，无值走插入）
        /// Asynchronously insert or update rows in batches (update when the primary key has a value, insert otherwise)
        /// </summary>
        /// <param name="dataList">数据列表 / the rows</param>
        /// <param name="updateFields">更新时要更新的字段；null 更新全部映射列 / fields to update on update; null updates all mapped columns</param>
        /// <param name="batchSize">每批条数 / rows per batch</param>
        /// <param name="insertId">插入时是否连同主键插入 / whether to insert primary keys too when inserting</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> InsertOrUpdateBatchAsync(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.InsertOrUpdateBatchAsync(dataList, updateFields, batchSize, insertId, batchHandler, commandTimeout, cancellationToken);
        }

        /// <summary>异步按主键删除一条数据 / Asynchronously delete one row by primary key</summary>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> DeleteByIdAsync(object key, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteByIdAsync<M>(key, commandTimeout, cancellationToken);
        }

        public Task<bool> DeleteByIdAsync(M data, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteByIdAsync<M>(data, commandTimeout, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件删除数据 / Asynchronously delete rows matching a LambdaWrapper</summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> DeleteAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteAsync(wrapper, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件删除数据 / Asynchronously delete rows matching a lambda expression</summary>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> DeleteAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteAsync(condition, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件删除数据 / Asynchronously delete rows matching a FilterWrapper</summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> DeleteAsync(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteAsync(wrapper, cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器条件删除数据 / Asynchronously delete rows matching a DataFilter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> DeleteAsync(DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteAsync<M>(filter, cancellationToken: cancellationToken);
        }

        /// <summary>异步按主键集合批量删除数据 / Asynchronously delete rows by a set of primary keys</summary>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        public Task<bool> DeleteByIdsAsync(IEnumerable keyList, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteByIdsAsync<M>(keyList, commandTimeout, cancellationToken);
        }

        /// <summary>异步按 LambdaWrapper 条件统计记录数 / Asynchronously count rows matching a LambdaWrapper</summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>记录数任务 / task producing the row count</returns>
        public Task<long> CountAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CountAsync(wrapper, cancellationToken);
        }

        /// <summary>异步按 Lambda 表达式条件统计记录数 / Asynchronously count rows matching a lambda expression</summary>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>记录数任务 / task producing the row count</returns>
        public Task<long> CountAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CountAsync(condition, cancellationToken: cancellationToken);
        }

        /// <summary>异步按 FilterWrapper 条件统计记录数；wrapper 为 null 时统计全表 / Asynchronously count rows matching a FilterWrapper; a null wrapper counts the whole table</summary>
        /// <param name="wrapper">条件包装器；null 表示无条件 / the condition wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>记录数任务 / task producing the row count</returns>
        public Task<long> CountAsync(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CountAsync(wrapper, cancellationToken);
        }

        /// <summary>异步按 DataFilter 数据过滤器条件统计记录数 / Asynchronously count rows matching a DataFilter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>记录数任务 / task producing the row count</returns>
        public Task<long> CountAsync(DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CountAsync<M>(filter, cancellationToken: cancellationToken);
        }

        #endregion

    }

}
