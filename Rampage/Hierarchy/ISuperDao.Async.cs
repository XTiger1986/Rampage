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
    /// <see cref="ISuperDao{M}"/> 的异步 API 分部定义
    /// Asynchronous partial API of <see cref="ISuperDao{M}"/>
    /// </summary>
    /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
    public partial interface ISuperDao<M>
        where M : class
    {
        /// <summary>异步开启事务 / Asynchronously begin a transaction</summary>
        /// <param name="iso">事务隔离级别，默认 ReadCommitted / isolation level, ReadCommitted by default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        Task BeginTransactionAsync(IsolationLevel iso = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

        /// <summary>异步提交事务 / Asynchronously commit the transaction</summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>异步回滚事务 / Asynchronously roll back the transaction</summary>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件查询数据列表 / Asynchronously query a data list by a LambdaWrapper</summary>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<M>> SelectListAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件查询数据列表，结果装配为指定类型 / Asynchronously query a data list by a LambdaWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<R>> SelectListAsync<R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 Lambda 表达式条件查询数据列表 / Asynchronously query a data list by a lambda expression condition</summary>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<M>> SelectListAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件查询数据列表，结果装配为指定类型 / Asynchronously query a data list by a lambda expression condition, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<R>> SelectListAsync<R>(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 FilterWrapper 条件查询数据列表 / Asynchronously query a data list by a FilterWrapper</summary>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<M>> SelectListAsync(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件查询数据列表，结果装配为指定类型 / Asynchronously query a data list by a FilterWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<R>> SelectListAsync<R>(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 DataFilter 数据过滤器查询数据列表 / Asynchronously query a data list by a DataFilter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<M>> SelectListAsync(DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器查询数据列表，结果装配为指定类型 / Asynchronously query a data list by a DataFilter, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<R>> SelectListAsync<R>(DataFilter filter, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 LambdaWrapper 条件分页查询数据列表 / Asynchronously query page data list by a LambdaWrapper</summary>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件分页查询数据列表，结果装配为指定类型 / Asynchronously query page data list by a LambdaWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 Lambda 表达式条件分页查询数据列表 / Asynchronously query page data list by a Lambda Expression</summary>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件分页查询数据列表，结果装配为指定类型 / Asynchronously query page data list by a Lambda Expression, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 FilterWrapper 条件分页查询数据列表 / Asynchronously query page data list by a FilterWrapper</summary>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件分页查询数据列表，结果装配为指定类型 / Asynchronously query page data list by a FilterWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 DataFilter 数据过滤器分页查询数据列表 / Asynchronously query page data list by a Data Filter</summary>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器分页查询数据列表，结果装配为指定类型 / Asynchronously query page data list by a Data Filter, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>当前页数据列表任务 / task producing the page of data</returns>
        Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, DataFilter filter, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按主键集合批量查询数据列表 / Asynchronously query a data list by primary key list</summary>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<M>> SelectByIdsAsync(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按主键集合批量查询数据列表，结果装配为指定类型 / Asynchronously query a data list by primary key list, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>数据列表任务 / task producing the result list</returns>
        Task<List<R>> SelectByIdsAsync<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 LambdaWrapper 条件查询单条数据 / Asynchronously query one data by a LambdaWrapper</summary>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<M?> SelectOneAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件查询单条数据，结果装配为指定类型 / Asynchronously query one data by a LambdaWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<R?> SelectOneAsync<R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 Lambda 表达式条件查询单条数据 / Asynchronously query one data by a Lambda Expression</summary>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<M?> SelectOneAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件查询单条数据，结果装配为指定类型 / Asynchronously query one data by a Lambda Expression, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<R?> SelectOneAsync<R>(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 FilterWrapper 条件查询单条数据 / Asynchronously query one data by a FilterWrapper</summary>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<M?> SelectOneAsync(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件查询单条数据，结果装配为指定类型 / Asynchronously query one data by a FilterWrapper, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<R?> SelectOneAsync<R>(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按 DataFilter 数据过滤器查询单条数据 / Asynchronously query one data by a Data Filter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<M?> SelectOneAsync(DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器查询单条数据，结果装配为指定类型 / Asynchronously query one data by a Data Filter, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<R?> SelectOneAsync<R>(DataFilter filter, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步按主键查询单条数据 / Asynchronously query one data by primary key</summary>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<M?> SelectByIdAsync(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按主键查询单条数据，结果装配为指定类型 / Asynchronously query one data by primary key, materialized as R</summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>实体任务；无匹配时为 null / task producing the entity, or null when no match</returns>
        Task<R?> SelectByIdAsync<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where R : class;

        /// <summary>异步插入一条数据；自增主键列由数据库生成，插入后自动回填到实体属性 / Asynchronously insert one data; the auto-increment column's mapped property value is set automatically</summary>
        /// <param name="data">实体数据 / the entity to insert</param>
        /// <param name="insertId">是否连同主键插入 / whether to insert primary keys too</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> InsertAsync(M data, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按主键更新一条数据（主键值取自实体属性）/ Asynchronously update one data by primary key (taken from the entity)</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateByIdAsync(M data, QueryFields? updateFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按主键更新指定字段集合 / Asynchronously update one data by primary key</summary>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateByIdAsync(object id, UpdateFields updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按主键更新指定字段集合（强类型）/ Asynchronously update one data by primary key (strongly typed)</summary>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateByIdAsync(object id, UpdateFields<M> updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按主键更新单个字段（Lambda 指定字段）/ Asynchronously update single field by primary key</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateByIdAsync<F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default);

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
        Task<bool> UpdateByIdAsync<F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default);

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
        Task<bool> UpdateByIdAsync<F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件更新数据（新值由实体承载）/ Asynchronously update one data by a LambdaWrapper</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件更新指定字段集合 / Asynchronously update some fields of data by a LambdaWrapper</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(UpdateFields updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件更新指定字段集合（强类型）/ Asynchronously update some fields of data by a LambdaWrapper (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件更新单个字段 / Asynchronously update single field of data by a LambdaWrapper</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync<F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件更新两个字段 / Asynchronously update two fields of data by a LambdaWrapper</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync<F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件更新三个字段 / Asynchronously update three fields of data by a LambdaWrapper</summary>
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
        Task<bool> UpdateAsync<F1, F2, F3>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件更新数据（新值由实体承载）/ Asynchronously update one data by a Lambda Expression</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件更新指定字段集合 / Asynchronously update one data by a Lambda Expression</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件更新指定字段集合（强类型）/ Asynchronously update one data by a Lambda Expression (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件更新单个字段 / Asynchronously update single field by a Lambda Expression</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync<F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件更新两个字段 / Asynchronously update two fields by a Lambda Expression</summary>
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
        Task<bool> UpdateAsync<F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件更新三个字段 / Asynchronously update three fields by a Lambda Expression</summary>
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
        Task<bool> UpdateAsync<F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件更新数据（新值由实体承载）/ Asynchronously update one data by a FilterWrapper</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件更新指定字段集合 / Asynchronously update one data by a FilterWrapper</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(UpdateFields updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件更新指定字段集合（强类型）/ Asynchronously update one data by a FilterWrapper (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(UpdateFields<M> updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件更新单个字段 / Asynchronously update single field by a FilterWrapper</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync<F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件更新两个字段 / Asynchronously update two fields by a FilterWrapper</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync<F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件更新三个字段 / Asynchronously update three fields by a FilterWrapper</summary>
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
        Task<bool> UpdateAsync<F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器条件更新数据（新值由实体承载）/ Asynchronously update one data by a Data Filter</summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(M data, DataFilter filter, QueryFields? updateFields = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器条件更新指定字段集合 / Asynchronously update one data by a Data Filter</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(UpdateFields updateFields, DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器条件更新指定字段集合（强类型）/ Asynchronously update one data by a Data Filter (strongly typed)</summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync(UpdateFields<M> updateFields, DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器条件更新单个字段 / Asynchronously update single field by a Data Filter</summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync<F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器条件更新两个字段 / Asynchronously update two fields by a Data Filter</summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateAsync<F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器条件更新三个字段 / Asynchronously update three fields by a Data Filter</summary>
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
        Task<bool> UpdateAsync<F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default);

        /// <summary>异步插入或更新一条数据（主键有值走更新，无值走插入）/ Asynchronously insert or update one data (update data if primary key has value, or else insert data)</summary>
        /// <param name="data">实体数据 / the entity</param>
        /// <param name="updateFields">更新时要更新的字段；null 更新全部映射列 / fields to update on update; null updates all mapped columns</param>
        /// <param name="insertId">插入时是否连同主键插入 / whether to insert the primary key too when inserting</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> InsertOrUpdateAsync(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步分批插入数据列表 / Asynchronously insert batch data list</summary>
        /// <param name="dataList">数据列表 / the rows to insert</param>
        /// <param name="batchSize">每批条数 / batch size of one execute command</param>
        /// <param name="insertId">是否连同主键插入 / whether to insert primary keys too</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> InsertBatchAsync(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按主键分批更新数据列表 / Asynchronously update batch data list by primary key</summary>
        /// <param name="data">数据列表（主键取自实体属性）/ the rows, primary keys taken from the entities</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / fields to update; null updates all mapped columns</param>
        /// <param name="batchSize">每批条数 / batch size of one execute command</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> UpdateBatchAsync(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步分批插入或更新数据列表（主键有值走更新，无值走插入）/ Asynchronously insert or update batch data list (update data if primary key has value, or else insert data)</summary>
        /// <param name="dataList">数据列表 / the rows</param>
        /// <param name="updateFields">更新时要更新的字段；null 更新全部映射列 / fields to update on update; null updates all mapped columns</param>
        /// <param name="batchSize">每批条数 / batch size of one execute command</param>
        /// <param name="insertId">插入时是否连同主键插入 / whether to insert the primary key too when inserting</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> InsertOrUpdateBatchAsync(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按主键删除一条数据 / Asynchronously delete one data by primary key</summary>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> DeleteByIdAsync(object key, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按主键删除一条数据（主键值取自实体属性）/ Asynchronously delete one data by primary key</summary>
        /// <param name="data">实体数据 / the entity</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> DeleteByIdAsync(M data, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件删除数据 / Asynchronously delete one data by a LambdaWrapper</summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> DeleteAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件删除数据 / Asynchronously delete one data by a Lambda Expression</summary>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> DeleteAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件删除数据 / Asynchronously delete one data by a FilterWrapper</summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> DeleteAsync(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器条件删除数据 / Asynchronously delete one data by a Data Filter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> DeleteAsync(DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>异步按主键集合批量删除数据 / Asynchronously delete batch data by primary key list</summary>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>是否成功 / task producing true on success</returns>
        Task<bool> DeleteByIdsAsync(IEnumerable keyList, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 LambdaWrapper 条件统计记录数 / Asynchronously count data by a LambdaWrapper</summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>记录数任务 / task producing the row count</returns>
        Task<long> CountAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>异步按 Lambda 表达式条件统计记录数 / Asynchronously count data by a Lambda Expression</summary>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>记录数任务 / task producing the row count</returns>
        Task<long> CountAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>异步按 FilterWrapper 条件统计记录数；wrapper 为 null 时统计全表 / Asynchronously count data by a FilterWrapper; a null wrapper counts the whole table</summary>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>记录数任务 / task producing the row count</returns>
        Task<long> CountAsync(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default);

        /// <summary>异步按 DataFilter 数据过滤器条件统计记录数 / Asynchronously count data by a Data Filter</summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="cancellationToken">取消令牌 / cancellation token</param>
        /// <returns>记录数任务 / task producing the row count</returns>
        Task<long> CountAsync(DataFilter filter, CancellationToken cancellationToken = default);
    }

}
