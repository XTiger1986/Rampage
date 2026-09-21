using Rampage.Data;
using Rampage.Data.Client;
using Rampage.Data.Filter;
using Rampage.Data.Query;
using System;
using System.Collections;
using System.Data;
using System.Linq.Expressions;

namespace Rampage.Hierarchy
{

    /// <summary>
    /// 数据访问门面接口：定义实体 M 的增删改查、分页、事务与批量操作
    /// Data-access facade interface: query/mutation, paging, transaction and batch operations for entity M
    /// </summary>
    /// <typeparam name="M">模型-表映射类型 / model-table mapping type</typeparam>
    public partial interface ISuperDao<M>
        where M : class
    {
        /// <summary>按名称切换数据库客户端；名称不存在时抛出异常 / Switch the database client by name; throws when the name is unknown</summary>
        /// <param name="databaseName">数据库客户端名 / database client name</param>
        void ChangeDatabase(string databaseName);

        /// <summary>开启事务 / Begin a transaction</summary>
        /// <param name="iso">事务隔离级别，默认 ReadCommitted / isolation level, ReadCommitted by default</param>
        void BeginTransaction(IsolationLevel iso = IsolationLevel.ReadCommitted);
        /// <summary>提交事务 / Commit the transaction</summary>
        void CommitTransaction();
        /// <summary>回滚事务 / Roll back the transaction</summary>
        void RollbackTransaction();

        /// <summary>
        /// 按 LambdaWrapper 条件查询数据列表
        /// Query a data list by a LambdaWrapper
        /// </summary>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>数据列表 / the result list</returns>
        List<M> SelectList(LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 LambdaWrapper 条件查询数据列表，结果装配为指定类型
        /// Query a data list by a LambdaWrapper, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>数据列表 / the result list</returns>
        List<R> SelectList<R>(LambdaWrapper<M> wrapper) where R : class;

        /// <summary>
        /// 按 Lambda 表达式条件查询数据列表
        /// Query a data list by a Lambda Expression
        /// </summary>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>数据列表 / the result list</returns>
        List<M> SelectList(Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 Lambda 表达式条件查询数据列表，结果装配为指定类型
        /// Query a data list by a Lambda Expression, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>数据列表 / the result list</returns>
        List<R> SelectList<R>(Expression<Func<M, bool>> condition) where R : class;

        /// <summary>
        /// 按 FilterWrapper 条件查询数据列表
        /// Query a data list by a FilterWrapper
        /// </summary>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <returns>数据列表 / the result list</returns>
        List<M> SelectList(FilterWrapper<M>? wrapper);

        /// <summary>
        /// 按 FilterWrapper 条件查询数据列表，结果装配为指定类型
        /// Query a data list by a FilterWrapper, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <returns>数据列表 / the result list</returns>
        List<R> SelectList<R>(FilterWrapper<M>? wrapper) where R : class;

        /// <summary>
        /// 按 DataFilter 数据过滤器查询数据列表
        /// Query a data list by a Data Filter
        /// </summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>数据列表 / the result list</returns>
        List<M> SelectList(DataFilter filter);

        /// <summary>
        /// 按 DataFilter 数据过滤器查询数据列表，结果装配为指定类型
        /// Query a data list by a Data Filter, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>数据列表 / the result list</returns>
        List<R> SelectList<R>(DataFilter filter) where R : class;

        /// <summary>
        /// 按 LambdaWrapper 条件分页查询数据列表
        /// Query page data list by a LambdaWrapper
        /// </summary>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        List<M> SelectPage(PagerInfo pagerInfo, LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 LambdaWrapper 条件分页查询数据列表，结果装配为指定类型
        /// Query page data list by a LambdaWrapper, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper) where R : class;

        /// <summary>
        /// 按 Lambda 表达式条件分页查询数据列表
        /// Query page data list by a Lambda Expression
        /// </summary>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        List<M> SelectPage(PagerInfo pagerInfo, Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 Lambda 表达式条件分页查询数据列表，结果装配为指定类型
        /// Query page data list by a Lambda Expression, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition) where R : class;

        /// <summary>
        /// 按 FilterWrapper 条件分页查询数据列表
        /// Query page data list by a FilterWrapper
        /// </summary>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        List<M> SelectPage(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null);

        /// <summary>
        /// 按 FilterWrapper 条件分页查询数据列表，结果装配为指定类型
        /// Query page data list by a FilterWrapper, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="wrapper">查询包装器；null 表示无条件 / the query wrapper; null means no condition</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null) where R : class;

        /// <summary>
        /// 按 DataFilter 数据过滤器分页查询数据列表
        /// Query page data list by a Data Filter
        /// </summary>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        List<M> SelectPage(PagerInfo pagerInfo, DataFilter filter);

        /// <summary>
        /// 按 DataFilter 数据过滤器分页查询数据列表，结果装配为指定类型
        /// Query page data list by a Data Filter, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="pagerInfo">分页信息：已预置 RecordCount 时不再执行 count 命令，否则自动执行并回填 RecordCount / if a custom RecordCount value is set, no count command executes; otherwise RecordCount is set automatically</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>当前页数据列表 / the page of data</returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, DataFilter filter) where R : class;

        /// <summary>
        /// 按主键集合批量查询数据列表
        /// Query data list by primary key list
        /// </summary>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>数据列表 / the result list</returns>
        List<M> SelectByIds(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null);

        /// <summary>
        /// 按主键集合批量查询数据列表，结果装配为指定类型
        /// Query data list by primary key list, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>数据列表 / the result list</returns>
        List<R> SelectByIds<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) where R : class;

        /// <summary>
        /// 按 LambdaWrapper 条件查询单条数据
        /// Query one data by a LambdaWrapper
        /// </summary>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        M? SelectOne(LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 LambdaWrapper 条件查询单条数据，结果装配为指定类型
        /// Query one data by a LambdaWrapper, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        R? SelectOne<R>(LambdaWrapper<M> wrapper) where R : class;

        /// <summary>
        /// 按 Lambda 表达式条件查询单条数据
        /// Query one data by a Lambda Expression
        /// </summary>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        M? SelectOne(Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 Lambda 表达式条件查询单条数据，结果装配为指定类型
        /// Query one data by a Lambda Expression, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="condition">查询条件表达式 / the condition expression</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        R? SelectOne<R>(Expression<Func<M, bool>> condition) where R : class;

        /// <summary>
        /// 按 FilterWrapper 条件查询单条数据
        /// Query one data by a FilterWrapper
        /// </summary>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        M? SelectOne(FilterWrapper<M> wrapper);

        /// <summary>
        /// 按 FilterWrapper 条件查询单条数据，结果装配为指定类型
        /// Query one data by a FilterWrapper, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="wrapper">查询包装器 / the query wrapper</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        R? SelectOne<R>(FilterWrapper<M> wrapper) where R : class;

        /// <summary>
        /// 按 DataFilter 数据过滤器查询单条数据
        /// Query one data by a Data Filter
        /// </summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        M? SelectOne(DataFilter filter);

        /// <summary>
        /// 按 DataFilter 数据过滤器查询单条数据，结果装配为指定类型
        /// Query one data by a Data Filter, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        R? SelectOne<R>(DataFilter filter) where R : class;

        /// <summary>
        /// 按主键查询单条数据
        /// Query one data by primary key
        /// </summary>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        M? SelectById(object key, QueryFields? resultFields = null, int? commandTimeout = null);

        /// <summary>
        /// 按主键查询单条数据，结果装配为指定类型
        /// Query one data by primary key, materialized as R
        /// </summary>
        /// <typeparam name="R">结果类型 / result type</typeparam>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="resultFields">返回字段；null 返回全部列 / result fields; null selects all columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>实体；无匹配时为 null / the entity, or null when no match</returns>
        R? SelectById<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null) where R : class;

        /// <summary>
        /// 插入一条数据；自增列对应的属性值会在插入后自动回填
        /// Insert one data; the auto-increment column's mapped property value is set automatically
        /// </summary>
        /// <param name="data">实体数据 / the entity to insert</param>
        /// <param name="insertId">是否连同主键一起插入（默认 false，主键由数据库生成）/ whether to insert the primary key too (false by default: generated by the database)</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool Insert(M data, bool insertId = false, int? commandTimeout = null);

        /// <summary>
        /// 按主键更新一条数据（主键值取自实体属性）
        /// Update one data by primary key
        /// </summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / specify the fields to update; null updates all mapped columns</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool UpdateById(M data, QueryFields? updateFields = null, int? commandTimeout = null);

        /// <summary>
        /// 按主键更新指定字段集合
        /// Update one data by primary key
        /// </summary>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">要更新的字段集合 / specify the fields to update</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool UpdateById(object id, UpdateFields updateFields, int? commandTimeout = null);

        /// <summary>
        /// 按主键更新指定字段集合（强类型）
        /// Update one data by primary key
        /// </summary>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateFields">要更新的字段集合 / specify the fields to update</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool UpdateById(object id, UpdateFields<M> updateFields, int? commandTimeout = null);

        /// <summary>
        /// 按主键更新单个字段（Lambda 指定字段）
        /// Update single field by primary key
        /// </summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool UpdateById<F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null);

        /// <summary>
        /// 按主键更新两个字段
        /// Update two fields by primary key
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="id">主键值 / the primary key value</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool UpdateById<F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null);

        /// <summary>
        /// 按主键更新三个字段
        /// Update three fields by primary key
        /// </summary>
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
        bool UpdateById<F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null);

        /// <summary>
        /// 按 LambdaWrapper 条件更新数据（新值由实体承载）
        /// Update one data by a LambdaWrapper
        /// </summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / specify the fields to update; null updates all mapped columns</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null);

        /// <summary>
        /// 按 LambdaWrapper 条件更新指定字段集合
        /// Update some fields of data by a LambdaWrapper
        /// </summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(UpdateFields updateFields, LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 LambdaWrapper 条件更新指定字段集合（强类型）
        /// Update some fields of data by a LambdaWrapper
        /// </summary>
        /// <param name="updateFields">要更新的字段集合 / the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 LambdaWrapper 条件更新单个字段
        /// Update single field of data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update<F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue);

        /// <summary>
        /// 按 LambdaWrapper 条件更新两个字段
        /// Update two fields of data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update<F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value);

        /// <summary>
        /// 按 LambdaWrapper 条件更新三个字段
        /// Update three fields of data by a LambdaWrapper
        /// </summary>
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
        bool Update<F1, F2, F3>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value);

        /// <summary>
        /// 按 Lambda 表达式条件更新数据（新值由实体承载）
        /// Update one data by a Lambda Expression
        /// </summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / specify the fields to update; null updates all mapped columns</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null);

        /// <summary>
        /// 按 Lambda 表达式条件更新指定字段集合
        /// Update one data by a Lambda Expression
        /// </summary>
        /// <param name="updateFields">要更新的字段集合 / specify the fields to update</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null);

        /// <summary>
        /// 按 Lambda 表达式条件更新指定字段集合（强类型）
        /// Update one data by a Lambda Expression
        /// </summary>
        /// <param name="updateFields">要更新的字段集合 / specify the fields to update</param>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null);

        /// <summary>
        /// 按 Lambda 表达式条件更新单个字段
        /// Update single field by a Lambda Expression
        /// </summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update<F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null);

        /// <summary>
        /// 按 Lambda 表达式条件更新两个字段
        /// Update two fields by a Lambda Expression
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update<F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null);

        /// <summary>
        /// 按 Lambda 表达式条件更新三个字段
        /// Update three fields by a Lambda Expression
        /// </summary>
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
        bool Update<F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null);

        /// <summary>
        /// 按 FilterWrapper 条件更新数据（新值由实体承载）
        /// Update one data by a FilterWrapper
        /// </summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / specify the fields to update; null updates all mapped columns</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null);

        /// <summary>
        /// 按 FilterWrapper 条件更新指定字段集合
        /// Update one data by a FilterWrapper
        /// </summary>
        /// <param name="updateFields">要更新的字段集合 / specify the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(UpdateFields updateFields, FilterWrapper<M> wrapper);

        /// <summary>
        /// 按 FilterWrapper 条件更新指定字段集合（强类型）
        /// Update one data by a FilterWrapper
        /// </summary>
        /// <param name="updateFields">要更新的字段集合 / specify the fields to update</param>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(UpdateFields<M> updateFields, FilterWrapper<M> wrapper);

        /// <summary>
        /// 按 FilterWrapper 条件更新单个字段
        /// Update single field by a FilterWrapper
        /// </summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update<F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue);

        /// <summary>
        /// 按 FilterWrapper 条件更新两个字段
        /// Update two fields by a FilterWrapper
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update<F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value);

        /// <summary>
        /// 按 FilterWrapper 条件更新三个字段
        /// Update three fields by a FilterWrapper
        /// </summary>
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
        bool Update<F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value);

        /// <summary>
        /// 按 DataFilter 数据过滤器条件更新数据（新值由实体承载）
        /// Update one data by a Data Filter
        /// </summary>
        /// <param name="data">承载新值的实体数据 / the entity carrying new values</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / specify the fields to update; null updates all mapped columns</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(M data, DataFilter filter, QueryFields? updateFields = null);

        /// <summary>
        /// 按 DataFilter 数据过滤器条件更新指定字段集合
        /// Update one data by a Data Filter
        /// </summary>
        /// <param name="updateFields">要更新的字段集合 / specify the fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(UpdateFields updateFields, DataFilter filter);

        /// <summary>
        /// 按 DataFilter 数据过滤器条件更新指定字段集合（强类型）
        /// Update one data by a Data Filter
        /// </summary>
        /// <param name="updateFields">要更新的字段集合 / specify the fields to update</param>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update(UpdateFields<M> updateFields, DataFilter filter);

        /// <summary>
        /// 按 DataFilter 数据过滤器条件更新单个字段
        /// Update single field by a Data Filter
        /// </summary>
        /// <typeparam name="F">字段类型 / field type</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="updateField">字段选择表达式 / the field selector</param>
        /// <param name="fieldValue">新字段值 / the new value</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update<F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue);

        /// <summary>
        /// 按 DataFilter 数据过滤器条件更新两个字段
        /// Update two fields by a Data Filter
        /// </summary>
        /// <typeparam name="F1">字段 1 类型 / type of field 1</typeparam>
        /// <typeparam name="F2">字段 2 类型 / type of field 2</typeparam>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <param name="field1">字段 1 选择表达式 / selector of field 1</param>
        /// <param name="field1Value">字段 1 新值 / new value of field 1</param>
        /// <param name="field2">字段 2 选择表达式 / selector of field 2</param>
        /// <param name="field2Value">字段 2 新值 / new value of field 2</param>
        /// <returns>是否成功 / true on success</returns>
        bool Update<F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value);

        /// <summary>
        /// 按 DataFilter 数据过滤器条件更新三个字段
        /// Update three fields by a Data Filter
        /// </summary>
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
        bool Update<F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value);

        /// <summary>
        /// 插入或更新一条数据（主键有值走更新，无值走插入）
        /// Insert or update one data (update data if primary key has value, or else insert data)
        /// </summary>
        /// <param name="data">实体数据 / the entity</param>
        /// <param name="updateFields">更新时要更新的字段；null 更新全部映射列 / specify the fields to update on update; null updates all mapped columns</param>
        /// <param name="insertId">插入时是否连同主键插入 / whether to insert the primary key too when inserting</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool InsertOrUpdate(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null);

        /// <summary>
        /// 分批插入数据列表
        /// Insert batch data list
        /// </summary>
        /// <param name="dataList">数据列表 / the rows to insert</param>
        /// <param name="batchSize">每批条数 / batch size of one execute command</param>
        /// <param name="insertId">是否连同主键插入 / whether to insert primary keys too</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool InsertBatch(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null);

        /// <summary>
        /// 按主键分批更新数据列表
        /// Update batch data list by primary key
        /// </summary>
        /// <param name="dataList">数据列表（主键取自实体属性）/ the rows, primary keys taken from the entities</param>
        /// <param name="updateFields">要更新的字段；null 更新全部映射列 / specify the fields to update; null updates all mapped columns</param>
        /// <param name="batchSize">每批条数 / batch size of one execute command</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool UpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null);

        /// <summary>
        /// 分批插入或更新数据列表（主键有值走更新，无值走插入）
        /// Insert or update batch data list (update data if primary key has value, or else insert data)
        /// </summary>
        /// <param name="dataList">数据列表 / the rows</param>
        /// <param name="updateFields">更新时要更新的字段；null 更新全部映射列 / specify the fields to update on update; null updates all mapped columns</param>
        /// <param name="batchSize">每批条数 / batch size of one execute command</param>
        /// <param name="insertId">插入时是否连同主键插入 / whether to insert primary keys too when inserting</param>
        /// <param name="batchHandler">每批操作后的回调（携带本批数据与影响行数），返回 false 中止后续批次 / callback per batch (with the batch data and affected rows); return false to stop remaining batches</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool InsertOrUpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null);

        /// <summary>
        /// 按主键删除一条数据
        /// Delete one data by primary key
        /// </summary>
        /// <param name="key">主键值 / the primary key value</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool DeleteById(object key, int? commandTimeout = null);

        /// <summary>
        /// 按主键删除一条数据（主键值取自实体属性）
        /// Delete one data by primary key
        /// </summary>
        /// <param name="data">承载主键的实体数据 / the entity carrying the primary key</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool DeleteById(M data, int? commandTimeout = null);

        /// <summary>
        /// 按 LambdaWrapper 条件删除数据
        /// Delete data by a LambdaWrapper
        /// </summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        bool Delete(LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 Lambda 表达式条件删除数据
        /// Delete data by a Lambda Expression
        /// </summary>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <returns>是否成功 / true on success</returns>
        bool Delete(Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 FilterWrapper 条件删除数据
        /// Delete data by a FilterWrapper
        /// </summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>是否成功 / true on success</returns>
        bool Delete(FilterWrapper<M> wrapper);

        /// <summary>
        /// 按 DataFilter 数据过滤器条件删除数据
        /// Delete data by a Data Filter
        /// </summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>是否成功 / true on success</returns>
        bool Delete(DataFilter filter);

        /// <summary>
        /// 按主键集合批量删除数据
        /// Delete batch data by primary key list
        /// </summary>
        /// <param name="keyList">主键值集合 / primary key values</param>
        /// <param name="commandTimeout">命令超时秒数；null 使用默认值 / command timeout in seconds; null for default</param>
        /// <returns>是否成功 / true on success</returns>
        bool DeleteByIds(IEnumerable keyList, int? commandTimeout = null);

        /// <summary>
        /// 按 LambdaWrapper 条件统计记录数
        /// Count data by a LambdaWrapper
        /// </summary>
        /// <param name="wrapper">条件包装器 / the condition wrapper</param>
        /// <returns>记录数 / the row count</returns>
        long Count(LambdaWrapper<M> wrapper);

        /// <summary>
        /// 按 Lambda 表达式条件统计记录数
        /// Count data by a Lambda Expression
        /// </summary>
        /// <param name="condition">条件表达式 / the condition expression</param>
        /// <returns>记录数 / the row count</returns>
        long Count(Expression<Func<M, bool>> condition);

        /// <summary>
        /// 按 FilterWrapper 条件统计记录数；wrapper 为 null 时统计全表
        /// Count data by a FilterWrapper; a null wrapper counts the whole table
        /// </summary>
        /// <param name="wrapper">条件包装器；null 表示无条件 / the condition wrapper; null means no condition</param>
        /// <returns>记录数 / the row count</returns>
        long Count(FilterWrapper<M>? wrapper = null);

        /// <summary>
        /// 按 DataFilter 数据过滤器条件统计记录数
        /// Count data by a Data Filter
        /// </summary>
        /// <param name="filter">数据过滤器 / the data filter</param>
        /// <returns>记录数 / the row count</returns>
        long Count(DataFilter filter);


    }

}
