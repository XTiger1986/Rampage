using Rampage.Data.Query;
using Rampage.Data;
using System;
using System.Linq.Expressions;
using System.Data;
using Rampage.Data.Client;
using Rampage.Data.Filter;
using System.Collections;

namespace Rampage.Hierarchy
{

    /// <summary>
    /// 业务层服务标记接口 / Marker interface for business-layer services.
    /// </summary>
    public interface IBaseService
    {

    }

    /// <summary>
    /// 单实体业务层服务接口：面向一个模型类型及其 Dao 的数据操作契约 / Single-entity business-layer service interface: the data-operation contract for one model type and its Dao.
    /// </summary>
    /// <typeparam name="M">模型（表映射）类型 / The model (table-mapped) type.</typeparam>
    /// <typeparam name="D">该实体对应的 Dao 类型 / The Dao type for the entity.</typeparam>
    public interface IBaseService<M, D> : IBaseService
        where M : class
        where D : ISuperDao<M>
    {

        /// <summary>
        /// 切换 Dao 使用的数据库 / Switches the database used by the Dao.
        /// </summary>
        /// <param name="databaseName">目标数据库名 / The target database name.</param>
        void ChangeDatabase(string databaseName);

        /// <summary>
        /// 在 Dao 上开启事务 / Begins a transaction on the Dao.
        /// </summary>
        /// <param name="iso">事务隔离级别 / The isolation level.</param>
        void BeginTransaction(IsolationLevel iso = IsolationLevel.ReadCommitted);
        /// <summary>
        /// 提交 Dao 事务 / Commits the Dao transaction.
        /// </summary>
        void CommitTransaction();
        /// <summary>
        /// 回滚 Dao 事务 / Rolls back the Dao transaction.
        /// </summary>
        void RollbackTransaction();

        /// <summary>
        /// 按 LambdaWrapper 查询数据列表 / Query a data list by a LambdaWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体列表 / The entity list.</returns>
        List<M> SelectList(LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 LambdaWrapper 查询数据列表并映射为 R / Query a data list by a LambdaWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果列表 / The result list.</returns>
        List<R> SelectList<R>(LambdaWrapper<M> wrapper) where R : class;

        /// <summary>
        /// 按 Lambda 表达式查询数据列表 / Query a data list by a lambda expression.
        /// </summary>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>实体列表 / The entity list.</returns>
        List<M> SelectList(Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 Lambda 表达式查询数据列表并映射为 R / Query a data list by a lambda expression and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>结果列表 / The result list.</returns>
        List<R> SelectList<R>(Expression<Func<M, bool>> condition) where R : class;

        /// <summary>
        /// 按 FilterWrapper 查询数据列表 / Query a data list by a FilterWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体列表 / The entity list.</returns>
        List<M> SelectList(FilterWrapper<M>? wrapper);

        /// <summary>
        /// 按 FilterWrapper 查询数据列表并映射为 R / Query a data list by a FilterWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果列表 / The result list.</returns>
        List<R> SelectList<R>(FilterWrapper<M>? wrapper) where R : class;

        /// <summary>
        /// 按 DataFilter 查询数据列表 / Query a data list by a DataFilter.
        /// </summary>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>实体列表 / The entity list.</returns>
        List<M> SelectList(DataFilter filter);

        /// <summary>
        /// 按 DataFilter 查询数据列表并映射为 R / Query a data list by a DataFilter and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>结果列表 / The result list.</returns>
        List<R> SelectList<R>(DataFilter filter) where R : class;

        /// <summary>
        /// 按 LambdaWrapper 分页查询数据列表 / Query a paged data list by a LambdaWrapper.
        /// </summary>
        /// <param name="pagerInfo">分页信息；自定义 RecordCount 时不再执行 count 命令，否则自动填充 RecordCount / The pager info; a custom RecordCount skips the count command, otherwise RecordCount is set automatically.</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体列表 / The entity list.</returns>
        List<M> SelectPage(PagerInfo pagerInfo, LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 LambdaWrapper 分页查询数据列表并映射为 R / Query a paged data list by a LambdaWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="pagerInfo">分页信息；自定义 RecordCount 时不再执行 count 命令，否则自动填充 RecordCount / The pager info; a custom RecordCount skips the count command, otherwise RecordCount is set automatically.</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果列表 / The result list.</returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper) where R : class;

        /// <summary>
        /// 按 Lambda 表达式分页查询数据列表 / Query a paged data list by a lambda expression.
        /// </summary>
        /// <param name="pagerInfo">分页信息；自定义 RecordCount 时不再执行 count 命令，否则自动填充 RecordCount / The pager info; a custom RecordCount skips the count command, otherwise RecordCount is set automatically.</param>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>实体列表 / The entity list.</returns>
        List<M> SelectPage(PagerInfo pagerInfo, Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 Lambda 表达式分页查询数据列表并映射为 R / Query a paged data list by a lambda expression and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="pagerInfo">分页信息；自定义 RecordCount 时不再执行 count 命令，否则自动填充 RecordCount / The pager info; a custom RecordCount skips the count command, otherwise RecordCount is set automatically.</param>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>结果列表 / The result list.</returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition) where R : class;

        /// <summary>
        /// 按 FilterWrapper 分页查询数据列表 / Query a paged data list by a FilterWrapper.
        /// </summary>
        /// <param name="pagerInfo">分页信息；自定义 RecordCount 时不再执行 count 命令，否则自动填充 RecordCount / The pager info; a custom RecordCount skips the count command, otherwise RecordCount is set automatically.</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体列表 / The entity list.</returns>
        List<M> SelectPage(PagerInfo pagerInfo, FilterWrapper<M>? wrapper);

        /// <summary>
        /// 按 FilterWrapper 分页查询数据列表并映射为 R / Query a paged data list by a FilterWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="pagerInfo">分页信息；自定义 RecordCount 时不再执行 count 命令，否则自动填充 RecordCount / The pager info; a custom RecordCount skips the count command, otherwise RecordCount is set automatically.</param>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果列表 / The result list.</returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper) where R : class;

        /// <summary>
        /// 按 DataFilter 分页查询数据列表 / Query a paged data list by a DataFilter.
        /// </summary>
        /// <param name="pagerInfo">分页信息；自定义 RecordCount 时不再执行 count 命令，否则自动填充 RecordCount / The pager info; a custom RecordCount skips the count command, otherwise RecordCount is set automatically.</param>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>实体列表 / The entity list.</returns>
        List<M> SelectPage(PagerInfo pagerInfo, DataFilter filter);

        /// <summary>
        /// 按 DataFilter 分页查询数据列表并映射为 R / Query a paged data list by a DataFilter and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="pagerInfo">分页信息；自定义 RecordCount 时不再执行 count 命令，否则自动填充 RecordCount / The pager info; a custom RecordCount skips the count command, otherwise RecordCount is set automatically.</param>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>结果列表 / The result list.</returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, DataFilter filter) where R : class;

        /// <summary>
        /// 按主键列表批量查询数据 / Query a data list by primary keys.
        /// </summary>
        /// <param name="keyList">主键列表 / The primary key list.</param>
        /// <param name="resultFields">返回字段筛选 / The result fields filter.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>实体列表 / The entity list.</returns>
        List<M> SelectByIds(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null);

        /// <summary>
        /// 按主键列表批量查询数据并映射为 R / Query a data list by primary keys and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="keyList">主键列表 / The primary key list.</param>
        /// <param name="resultFields">返回字段筛选 / The result fields filter.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>结果列表 / The result list.</returns>
        List<R> SelectByIds<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) where R : class;

        /// <summary>
        /// 按 LambdaWrapper 查询单条数据 / Query one record by a LambdaWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        M? SelectOne(LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 LambdaWrapper 查询单条数据并映射为 R / Query one record by a LambdaWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果或 null / The result or null.</returns>
        R? SelectOne<R>(LambdaWrapper<M> wrapper) where R : class;

        /// <summary>
        /// 按 Lambda 表达式查询单条数据 / Query one record by a lambda expression.
        /// </summary>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        M? SelectOne(Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 Lambda 表达式查询单条数据并映射为 R / Query one record by a lambda expression and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>结果或 null / The result or null.</returns>
        R? SelectOne<R>(Expression<Func<M, bool>> condition) where R : class;

        /// <summary>
        /// 按 FilterWrapper 查询单条数据 / Query one record by a FilterWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        M? SelectOne(FilterWrapper<M> wrapper);

        /// <summary>
        /// 按 FilterWrapper 查询单条数据并映射为 R / Query one record by a FilterWrapper and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>结果或 null / The result or null.</returns>
        R? SelectOne<R>(FilterWrapper<M> wrapper) where R : class;

        /// <summary>
        /// 按 DataFilter 查询单条数据 / Query one record by a DataFilter.
        /// </summary>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        M? SelectOne(DataFilter filter);

        /// <summary>
        /// 按 DataFilter 查询单条数据并映射为 R / Query one record by a DataFilter and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>结果或 null / The result or null.</returns>
        R? SelectOne<R>(DataFilter filter) where R : class;

        /// <summary>
        /// 按主键查询单条数据 / Query one record by primary key.
        /// </summary>
        /// <param name="key">主键值 / The primary key value.</param>
        /// <param name="resultFields">返回字段筛选 / The result fields filter.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>实体或 null / The entity or null.</returns>
        M? SelectById(object key, QueryFields? resultFields = null, int? commandTimeout = null);

        /// <summary>
        /// 按主键查询单条数据并映射为 R / Query one record by primary key and map to R.
        /// </summary>
        /// <typeparam name="R">结果映射类型 / The result type.</typeparam>
        /// <param name="key">主键值 / The primary key value.</param>
        /// <param name="resultFields">返回字段筛选 / The result fields filter.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>结果或 null / The result or null.</returns>
        R? SelectById<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null) where R : class;

        /// <summary>
        /// 插入一条数据，自增列映射属性将自动回填 / Insert one record; the autoincrement column's mapped property is set automatically.
        /// </summary>
        /// <param name="data">实体数据 / The entity data.</param>
        /// <param name="insertId">是否插入主键列 / Whether to insert the primary key column.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool Insert(M data, bool insertId = false, int? commandTimeout = null);

        /// <summary>
        /// 按主键更新一条数据 / Update one record by primary key.
        /// </summary>
        /// <param name="data">实体数据（取其主键定位行）/ The entity data (its primary key locates the row).</param>
        /// <param name="updateFields">更新字段筛选 / The fields to update.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool UpdateById(M data, QueryFields? updateFields = null, int? commandTimeout = null);

        /// <summary>
        /// 按 LambdaWrapper 条件更新数据 / Update records matching a LambdaWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <param name="data">实体数据（提供更新字段值）/ The entity data providing the values.</param>
        /// <param name="updateFields">更新字段筛选 / The fields to update.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool Update(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null);

        /// <summary>
        /// 按 Lambda 表达式条件更新数据 / Update records matching a lambda expression.
        /// </summary>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <param name="data">实体数据（提供更新字段值）/ The entity data providing the values.</param>
        /// <param name="updateFields">更新字段筛选 / The fields to update.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool Update(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null);

        /// <summary>
        /// 按 FilterWrapper 条件更新数据 / Update records matching a FilterWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <param name="data">实体数据（提供更新字段值）/ The entity data providing the values.</param>
        /// <param name="updateFields">更新字段筛选 / The fields to update.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool Update(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null);

        /// <summary>
        /// 按 DataFilter 条件更新数据 / Update records matching a DataFilter.
        /// </summary>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <param name="data">实体数据（提供更新字段值）/ The entity data providing the values.</param>
        /// <param name="updateFields">更新字段筛选 / The fields to update.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool Update(M data, DataFilter filter, QueryFields? updateFields = null);

        /// <summary>
        /// 插入或更新一条数据（主键有值则更新，否则插入）/ Insert or update one record (update when the primary key has a value, otherwise insert).
        /// </summary>
        /// <param name="data">实体数据 / The entity data.</param>
        /// <param name="updateFields">更新字段筛选 / The fields to update.</param>
        /// <param name="insertId">是否插入主键列 / Whether to insert the primary key column.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool InsertOrUpdate(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null);

        /// <summary>
        /// 批量插入数据 / Batch insert records.
        /// </summary>
        /// <param name="dataList">实体数据列表 / The entity data list.</param>
        /// <param name="batchSize">单批命令行数 / Rows per command batch.</param>
        /// <param name="insertId">是否插入主键列 / Whether to insert the primary key column.</param>
        /// <param name="batchHandler">每批数据操作回调 / Callback for each batch operation.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool InsertBatch(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null);

        /// <summary>
        /// 按主键批量更新数据 / Batch update records by primary key.
        /// </summary>
        /// <param name="dataList">实体数据列表 / The entity data list.</param>
        /// <param name="updateFields">更新字段筛选 / The fields to update.</param>
        /// <param name="batchSize">单批命令行数 / Rows per command batch.</param>
        /// <param name="batchHandler">每批数据操作回调 / Callback for each batch operation.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool UpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null);

        /// <summary>
        /// 批量插入或更新数据（主键有值则更新，否则插入）/ Batch insert-or-update records (update when the primary key has a value, otherwise insert).
        /// </summary>
        /// <param name="dataList">实体数据列表 / The entity data list.</param>
        /// <param name="updateFields">更新字段筛选 / The fields to update.</param>
        /// <param name="batchSize">单批命令行数 / Rows per command batch.</param>
        /// <param name="insertId">是否插入主键列 / Whether to insert the primary key column.</param>
        /// <param name="batchHandler">每批数据操作回调 / Callback for each batch operation.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool InsertOrUpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null);

        /// <summary>
        /// 按主键删除一条数据 / Delete one record by primary key.
        /// </summary>
        /// <param name="key">主键值 / The primary key value.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool DeleteById(object key, int? commandTimeout = null);

        /// <summary>
        /// 按实体主键删除一条数据 / Delete one record by the entity's primary key.
        /// </summary>
        /// <param name="data">实体数据（取其主键定位行）/ The entity data (its primary key locates the row).</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool DeleteById(M data, int? commandTimeout = null);

        /// <summary>
        /// 按 LambdaWrapper 条件删除数据 / Delete records matching a LambdaWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool Delete(LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 Lambda 表达式条件删除数据 / Delete records matching a lambda expression.
        /// </summary>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool Delete(Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 FilterWrapper 条件删除数据 / Delete records matching a FilterWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool Delete(FilterWrapper<M> wrapper);

        /// <summary>
        /// 按 DataFilter 条件删除数据 / Delete records matching a DataFilter.
        /// </summary>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool Delete(DataFilter filter);

        /// <summary>
        /// 按主键列表批量删除数据 / Delete a list of records by primary keys.
        /// </summary>
        /// <param name="keyList">主键列表 / The primary key list.</param>
        /// <param name="commandTimeout">命令超时（秒）/ The command timeout in seconds.</param>
        /// <returns>是否成功 / Whether the operation succeeded.</returns>
        bool DeleteByIds(IEnumerable keyList, int? commandTimeout = null);

        /// <summary>
        /// 按 LambdaWrapper 条件统计数量 / Count records matching a LambdaWrapper.
        /// </summary>
        /// <param name="wrapper">查询条件包装 / The query condition wrapper.</param>
        /// <returns>记录数 / The record count.</returns>
        long Count(LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 Lambda 表达式条件统计数量 / Count records matching a lambda expression.
        /// </summary>
        /// <param name="condition">Lambda 查询条件 / The lambda query condition.</param>
        /// <returns>记录数 / The record count.</returns>
        long Count(Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 FilterWrapper 条件统计数量；无条件下统计全表 / Count records matching a FilterWrapper, or the whole table when omitted.
        /// </summary>
        /// <param name="wrapper">查询条件包装；为 null 时统计全表 / The query condition wrapper; null counts the whole table.</param>
        /// <returns>记录数 / The record count.</returns>
        long Count(FilterWrapper<M>? wrapper = null);

        /// <summary>
        /// 按 DataFilter 条件统计数量 / Count records matching a DataFilter.
        /// </summary>
        /// <param name="filter">数据过滤器 / The data filter.</param>
        /// <returns>记录数 / The record count.</returns>
        long Count(DataFilter filter);

    }

}
