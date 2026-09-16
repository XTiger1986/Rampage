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
    /// Asynchronous API for <see cref="ISuperDao{M}"/>.
    /// </summary>
    /// <typeparam name="M">model-table mapping type</typeparam>
    public partial interface ISuperDao<M>
        where M : class
    {
        /// <summary>
        /// Asynchronously begin a transaction.
        /// </summary>
        Task BeginTransactionAsync(IsolationLevel iso = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously commit the transaction.
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously rollback the transaction.
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query data list by a LambdaWrapper.
        /// </summary>
        Task<List<M>> SelectListAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query data list by a LambdaWrapper.
        /// </summary>
        Task<List<R>> SelectListAsync<R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query data list by a Lambda Expression.
        /// </summary>
        Task<List<M>> SelectListAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query data list by a Lambda Expression.
        /// </summary>
        Task<List<R>> SelectListAsync<R>(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query data list by a FilterWrapper.
        /// </summary>
        Task<List<M>> SelectListAsync(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query data list by a FilterWrapper.
        /// </summary>
        Task<List<R>> SelectListAsync<R>(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query data list by a Data Filter.
        /// </summary>
        Task<List<M>> SelectListAsync(DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query data list by a Data Filter.
        /// </summary>
        Task<List<R>> SelectListAsync<R>(DataFilter filter, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query page data list by a LambdaWrapper.
        /// </summary>
        Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query page data list by a LambdaWrapper.
        /// </summary>
        Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query page data list by a Lambda Expression.
        /// </summary>
        Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query page data list by a Lambda Expression.
        /// </summary>
        Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query page data list by a FilterWrapper.
        /// </summary>
        Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query page data list by a FilterWrapper.
        /// </summary>
        Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query page data list by a Data Filter.
        /// </summary>
        Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query page data list by a Data Filter.
        /// </summary>
        Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, DataFilter filter, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query data list by primary key list.
        /// </summary>
        Task<List<M>> SelectByIdsAsync(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query data list by primary key list.
        /// </summary>
        Task<List<R>> SelectByIdsAsync<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query one data by a LambdaWrapper.
        /// </summary>
        Task<M?> SelectOneAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query one data by a LambdaWrapper.
        /// </summary>
        Task<R?> SelectOneAsync<R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query one data by a Lambda Expression.
        /// </summary>
        Task<M?> SelectOneAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query one data by a Lambda Expression.
        /// </summary>
        Task<R?> SelectOneAsync<R>(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query one data by a FilterWrapper.
        /// </summary>
        Task<M?> SelectOneAsync(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query one data by a FilterWrapper.
        /// </summary>
        Task<R?> SelectOneAsync<R>(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query one data by a Data Filter.
        /// </summary>
        Task<M?> SelectOneAsync(DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query one data by a Data Filter.
        /// </summary>
        Task<R?> SelectOneAsync<R>(DataFilter filter, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously query one data by primary key.
        /// </summary>
        Task<M?> SelectByIdAsync(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously query one data by primary key.
        /// </summary>
        Task<R?> SelectByIdAsync<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where R : class;

        /// <summary>
        /// Asynchronously insert one data.
        /// </summary>
        Task<bool> InsertAsync(M data, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by primary key.
        /// </summary>
        Task<bool> UpdateByIdAsync(M data, QueryFields? updateFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by primary key.
        /// </summary>
        Task<bool> UpdateByIdAsync(object id, UpdateFields updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by primary key.
        /// </summary>
        Task<bool> UpdateByIdAsync(object id, UpdateFields<M> updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update single field by primary key.
        /// </summary>
        Task<bool> UpdateByIdAsync<F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update two fields by primary key.
        /// </summary>
        Task<bool> UpdateByIdAsync<F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update three fields by primary key.
        /// </summary>
        Task<bool> UpdateByIdAsync<F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a LambdaWrapper.
        /// </summary>
        Task<bool> UpdateAsync(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update some fields of data by a LambdaWrapper.
        /// </summary>
        Task<bool> UpdateAsync(UpdateFields updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update some fields of data by a LambdaWrapper.
        /// </summary>
        Task<bool> UpdateAsync(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update single field of data by a LambdaWrapper.
        /// </summary>
        Task<bool> UpdateAsync<F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update two fields of data by a LambdaWrapper.
        /// </summary>
        Task<bool> UpdateAsync<F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update three fields of data by a LambdaWrapper.
        /// </summary>
        Task<bool> UpdateAsync<F1, F2, F3>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a Lambda Expression.
        /// </summary>
        Task<bool> UpdateAsync(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a Lambda Expression.
        /// </summary>
        Task<bool> UpdateAsync(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a Lambda Expression.
        /// </summary>
        Task<bool> UpdateAsync(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update single field by a Lambda Expression.
        /// </summary>
        Task<bool> UpdateAsync<F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update two fields by a Lambda Expression.
        /// </summary>
        Task<bool> UpdateAsync<F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update three fields by a Lambda Expression.
        /// </summary>
        Task<bool> UpdateAsync<F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a FilterWrapper.
        /// </summary>
        Task<bool> UpdateAsync(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a FilterWrapper.
        /// </summary>
        Task<bool> UpdateAsync(UpdateFields updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a FilterWrapper.
        /// </summary>
        Task<bool> UpdateAsync(UpdateFields<M> updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update single field by a FilterWrapper.
        /// </summary>
        Task<bool> UpdateAsync<F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update two fields by a FilterWrapper.
        /// </summary>
        Task<bool> UpdateAsync<F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update three fields by a FilterWrapper.
        /// </summary>
        Task<bool> UpdateAsync<F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a Data Filter.
        /// </summary>
        Task<bool> UpdateAsync(M data, DataFilter filter, QueryFields? updateFields = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a Data Filter.
        /// </summary>
        Task<bool> UpdateAsync(UpdateFields updateFields, DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update one data by a Data Filter.
        /// </summary>
        Task<bool> UpdateAsync(UpdateFields<M> updateFields, DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update single field by a Data Filter.
        /// </summary>
        Task<bool> UpdateAsync<F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update two fields by a Data Filter.
        /// </summary>
        Task<bool> UpdateAsync<F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update three fields by a Data Filter.
        /// </summary>
        Task<bool> UpdateAsync<F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously insert or update one data.
        /// </summary>
        Task<bool> InsertOrUpdateAsync(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously insert batch data list.
        /// </summary>
        Task<bool> InsertBatchAsync(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously update batch data list by primary key.
        /// </summary>
        Task<bool> UpdateBatchAsync(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously insert or update batch data list.
        /// </summary>
        Task<bool> InsertOrUpdateBatchAsync(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously delete one data by primary key.
        /// </summary>
        Task<bool> DeleteByIdAsync(object key, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously delete one data by primary key.
        /// </summary>
        Task<bool> DeleteByIdAsync(M data, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously delete one data by a LambdaWrapper.
        /// </summary>
        Task<bool> DeleteAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously delete one data by a Lambda Expression.
        /// </summary>
        Task<bool> DeleteAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously delete one data by a FilterWrapper.
        /// </summary>
        Task<bool> DeleteAsync(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously delete one data by a Data Filter.
        /// </summary>
        Task<bool> DeleteAsync(DataFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously delete batch data by primary key list.
        /// </summary>
        Task<bool> DeleteByIdsAsync(IEnumerable keyList, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously count data by a LambdaWrapper.
        /// </summary>
        Task<long> CountAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously count data by a Lambda Expression.
        /// </summary>
        Task<long> CountAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously count data by a FilterWrapper.
        /// </summary>
        Task<long> CountAsync(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously count data by a Data Filter.
        /// </summary>
        Task<long> CountAsync(DataFilter filter, CancellationToken cancellationToken = default);
    }

}
