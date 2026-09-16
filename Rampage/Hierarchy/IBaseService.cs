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

    public interface IBaseService
    {

    }

    /// <summary>
    /// business object base interface
    /// </summary>
    /// <typeparam name="M">model-table mapping type</typeparam>
    /// <typeparam name="D">Dao type</typeparam>
    public interface IBaseService<M, D> : IBaseService
        where M : class
        where D : ISuperDao<M>
    {

        void ChangeDatabase(string databaseName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iso"></param>
        void BeginTransaction(IsolationLevel iso = IsolationLevel.ReadCommitted);
        /// <summary>
        /// 
        /// </summary>
        void CommitTransaction();
        /// <summary>
        /// 
        /// </summary>
        void RollbackTransaction();

        /// <summary>
        /// query data list by a LambdaWrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        List<M> SelectList(LambdaWrapper<M> wrapper);

        /// <summary>
        /// query data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        List<R> SelectList<R>(LambdaWrapper<M> wrapper) where R : class;

        /// <summary>
        /// query data list by a Lambda Expression
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        List<M> SelectList(Expression<Func<M, bool>> condition);

        /// <summary>
        /// query data list by a Lambda Expression
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="condition"></param>
        /// <returns></returns>
        List<R> SelectList<R>(Expression<Func<M, bool>> condition) where R : class;

        /// <summary>
        /// query data list by a FilterWrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        List<M> SelectList(FilterWrapper<M>? wrapper);

        /// <summary>
        /// query data list by a FilterWrapper
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        List<R> SelectList<R>(FilterWrapper<M>? wrapper) where R : class;

        /// <summary>
        /// query data list by a Data Filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        List<M> SelectList(DataFilter filter);

        /// <summary>
        /// query data list by a Data Filter
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        List<R> SelectList<R>(DataFilter filter) where R : class;

        /// <summary>
        /// query page data list by a LambdaWrapper
        /// </summary>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        List<M> SelectPage(PagerInfo pagerInfo, LambdaWrapper<M> wrapper);

        /// <summary>
        /// query page data list by a LambdaWrapper
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper) where R : class;

        /// <summary>
        /// query page data list by a Lambda Expression
        /// </summary>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="condition"></param>
        /// <returns></returns>
        List<M> SelectPage(PagerInfo pagerInfo, Expression<Func<M, bool>> condition);

        /// <summary>
        /// query page data list by a Lambda Expression
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="condition"></param>
        /// <returns></returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition) where R : class;

        /// <summary>
        /// query page data list by a FilterWrapper
        /// </summary>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        List<M> SelectPage(PagerInfo pagerInfo, FilterWrapper<M>? wrapper);

        /// <summary>
        /// query page data list by a FilterWrapper
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper) where R : class;

        /// <summary>
        /// query page data list by a Data Filter
        /// </summary>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="filter"></param>
        /// <returns></returns>
        List<M> SelectPage(PagerInfo pagerInfo, DataFilter filter);

        /// <summary>
        /// query page data list by a Data Filter
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="pagerInfo">if custom RecordCount value, the command should not execute count command, or else RecordCount value'll be set automatically</param>
        /// <param name="filter"></param>
        /// <returns></returns>
        List<R> SelectPage<R>(PagerInfo pagerInfo, DataFilter filter) where R : class;

        /// <summary>
        /// query data list by primary key list
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="resultFields"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        List<M> SelectByIds(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null);

        /// <summary>
        /// query data list by primary key list
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="keyList"></param>
        /// <param name="resultFields"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        List<R> SelectByIds<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) where R : class;

        /// <summary>
        /// query one data by a LambdaWrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        M? SelectOne(LambdaWrapper<M> wrapper);

        /// <summary>
        /// query one data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        R? SelectOne<R>(LambdaWrapper<M> wrapper) where R : class;

        /// <summary>
        /// query one data by a Lambda Expression
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        M? SelectOne(Expression<Func<M, bool>> condition);

        /// <summary>
        /// query one data by a Lambda Expression
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="condition"></param>
        /// <returns></returns>
        R? SelectOne<R>(Expression<Func<M, bool>> condition) where R : class;

        /// <summary>
        /// query one data by a FilterWrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        M? SelectOne(FilterWrapper<M> wrapper);

        /// <summary>
        /// query one data by a FilterWrapper
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        R? SelectOne<R>(FilterWrapper<M> wrapper) where R : class;

        /// <summary>
        /// query one data by a Data Filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        M? SelectOne(DataFilter filter);

        /// <summary>
        /// query one data by a Data Filter
        /// </summary>
        /// <typeparam name="R">result type</typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        R? SelectOne<R>(DataFilter filter) where R : class;

        /// <summary>
        /// query one data by primary key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="resultFields"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        M? SelectById(object key, QueryFields? resultFields = null, int? commandTimeout = null);

        /// <summary>
        /// query one data by primary key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="resultFields"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        R? SelectById<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null) where R : class;

        /// <summary>
        /// insert one data, autoincrement column's mapping property value will be set automatically
        /// </summary>
        /// <param name="data"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        bool Insert(M data, bool insertId = false, int? commandTimeout = null);

        /// <summary>
        /// update one data by primary key
        /// </summary>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        bool UpdateById(M data, QueryFields? updateFields = null, int? commandTimeout = null);

        /// <summary>
        /// update one data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        bool Update(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null);

        /// <summary>
        /// update one data by a Lambda Expression
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        bool Update(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null);

        /// <summary>
        /// update one data by a FilterWrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        bool Update(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null);

        /// <summary>
        /// update one data by a Data Filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <returns></returns>
        bool Update(M data, DataFilter filter, QueryFields? updateFields = null);

        /// <summary>
        /// insert or update one data (update data if primary key has value, or else insert data)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        bool InsertOrUpdate(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null);

        /// <summary>
        /// insert batch data list
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="batchSize">batch size of one execute command</param>
        /// <param name="batchHandler">Callback function for each batch of data operation</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        bool InsertBatch(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null);

        /// <summary>
        /// update batch data list by primary key
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="batchSize">batch size of one execute command</param>
        /// <param name="batchHandler">Callback function for each batch of data operation</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        bool UpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null);

        /// <summary>
        /// insert or update batch data list (update data if primary key has value, or else insert data)
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="updateFields">Specify the fields to be update</param>
        /// <param name="batchSize">batch size of one execute command</param>
        /// <param name="batchHandler">Callback function for each batch of data operation</param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        bool InsertOrUpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null);

        /// <summary>
        /// delete one data by primary key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        bool DeleteById(object key, int? commandTimeout = null);

        /// <summary>
        /// delete one data by primary key
        /// </summary>
        /// <param name="data"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        bool DeleteById(M data, int? commandTimeout = null);

        /// <summary>
        /// delete one data by a LambdaWrapper
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        bool Delete(LambdaWrapper<M> wrapper);

        /// <summary>
        /// delete one data by a Lambda Expression
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        bool Delete(Expression<Func<M, bool>> condition);

        /// <summary>
        /// delete one data by a FilterWrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        bool Delete(FilterWrapper<M> wrapper);

        /// <summary>
        /// delete one data by a Data Filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        bool Delete(DataFilter filter);

        /// <summary>
        /// delete batch data by primary key list
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        bool DeleteByIds(IEnumerable keyList, int? commandTimeout = null);

        /// <summary>
        /// count data by a LambdaWrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        long Count(LambdaWrapper<M> wrapper);

        /// <summary>
        /// count data by a Lambda Expression
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        long Count(Expression<Func<M, bool>> condition);

        /// <summary>
        /// count data by a FilterWrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        long Count(FilterWrapper<M>? wrapper = null);

        /// <summary>
        /// count data by a Data Filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        long Count(DataFilter filter);

    }

}
