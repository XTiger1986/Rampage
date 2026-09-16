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
    /// Asynchronous API for <see cref="SuperDao{M}"/>.
    /// </summary>
    public abstract partial class SuperDao<M>
        where M : class
    {

        #region IDao Async

        public Task BeginTransactionAsync(IsolationLevel iso = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.BeginTransactionAsync(iso, cancellationToken);
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CommitTransactionAsync(cancellationToken);
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            return DatabaseClient.RollbackTransactionAsync(cancellationToken);
        }

        public Task<List<M>> SelectListAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectListAsync(wrapper, cancellationToken);
        }

        public Task<List<R>> SelectListAsync<R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectListAsync<M, R>(wrapper, cancellationToken);
        }

        public Task<List<M>> SelectListAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectListAsync(condition, cancellationToken: cancellationToken);
        }

        public Task<List<R>> SelectListAsync<R>(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectListAsync<M, R>(condition, cancellationToken: cancellationToken);
        }

        public Task<List<M>> SelectListAsync(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectListAsync(wrapper, cancellationToken);
        }

        public Task<List<R>> SelectListAsync<R>(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectListAsync<M, R>(wrapper, cancellationToken);
        }

        public virtual Task<List<M>> SelectListAsync(DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectListAsync<M>(filter, cancellationToken: cancellationToken);
        }

        public virtual Task<List<R>> SelectListAsync<R>(DataFilter filter, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectListAsync<M, R>(filter, cancellationToken: cancellationToken);
        }

        public Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectPageAsync<M>(pagerInfo, wrapper, cancellationToken);
        }

        public Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectPageAsync<M, R>(pagerInfo, wrapper, cancellationToken);
        }

        public Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectPageAsync(pagerInfo, condition, cancellationToken: cancellationToken);
        }

        public Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectPageAsync<M, R>(pagerInfo, condition, cancellationToken: cancellationToken);
        }

        public Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectPageAsync(pagerInfo, wrapper, cancellationToken);
        }

        public Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectPageAsync<M, R>(pagerInfo, wrapper, cancellationToken);
        }

        public Task<List<M>> SelectPageAsync(PagerInfo pagerInfo, DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectPageAsync<M>(pagerInfo, filter, cancellationToken: cancellationToken);
        }

        public Task<List<R>> SelectPageAsync<R>(PagerInfo pagerInfo, DataFilter filter, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectPageAsync<M, R>(pagerInfo, filter, cancellationToken: cancellationToken);
        }

        public Task<List<M>> SelectByIdsAsync(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectByIdsAsync<M>(keyList, resultFields, commandTimeout, cancellationToken);
        }

        public Task<List<R>> SelectByIdsAsync<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectByIdsAsync<M, R>(keyList, resultFields, commandTimeout, cancellationToken);
        }

        public Task<M?> SelectOneAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectOneAsync(wrapper, cancellationToken);
        }

        public Task<R?> SelectOneAsync<R>(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectOneAsync<M, R>(wrapper, cancellationToken);
        }

        public Task<M?> SelectOneAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectOneAsync(condition, cancellationToken: cancellationToken);
        }

        public Task<R?> SelectOneAsync<R>(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectOneAsync<M, R>(condition, cancellationToken: cancellationToken);
        }

        public Task<M?> SelectOneAsync(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectOneAsync(wrapper, cancellationToken);
        }

        public Task<R?> SelectOneAsync<R>(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectOneAsync<M, R>(wrapper, cancellationToken);
        }

        public Task<M?> SelectOneAsync(DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectOneAsync<M>(filter, cancellationToken: cancellationToken);
        }

        public Task<R?> SelectOneAsync<R>(DataFilter filter, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectOneAsync<M, R>(filter, cancellationToken: cancellationToken);
        }

        public Task<M?> SelectByIdAsync(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.SelectByIdAsync<M>(key, resultFields, commandTimeout, cancellationToken);
        }

        public Task<R?> SelectByIdAsync<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where R : class
        {
            return DatabaseClient.SelectByIdAsync<M, R>(key, resultFields, commandTimeout, cancellationToken);
        }

        public Task<bool> InsertAsync(M data, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.InsertAsync(data, insertId, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateByIdAsync(M data, QueryFields? updateFields = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(data, updateFields, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateByIdAsync(object id, UpdateFields updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync<M>(id, updateFields, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateByIdAsync(object id, UpdateFields<M> updateFields, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(id, updateFields, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateByIdAsync<F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(id, updateField, fieldValue, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateByIdAsync<F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(id, field1, field1Value, field2, field2Value, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateByIdAsync<F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateByIdAsync(id, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateAsync(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(data, wrapper, updateFields, cancellationToken);
        }

        public Task<bool> UpdateAsync(UpdateFields updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, wrapper, cancellationToken);
        }

        public Task<bool> UpdateAsync(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, wrapper, cancellationToken);
        }

        public Task<bool> UpdateAsync<F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, updateField, fieldValue, cancellationToken);
        }

        public Task<bool> UpdateAsync<F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, field1, field1Value, field2, field2Value, cancellationToken);
        }

        public Task<bool> UpdateAsync<F1, F2, F3>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, field1, field1Value, field2, field2Value, field3, field3Value, cancellationToken);
        }

        public Task<bool> UpdateAsync(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(data, condition, updateFields, cancellationToken: cancellationToken);
        }

        public Task<bool> UpdateAsync(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, condition, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateAsync(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, condition, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateAsync<F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(condition, updateField, fieldValue, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateAsync<F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(condition, field1, field1Value, field2, field2Value, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateAsync<F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(condition, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateAsync(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(data, wrapper, updateFields, cancellationToken);
        }

        public Task<bool> UpdateAsync(UpdateFields updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, wrapper, cancellationToken);
        }

        public Task<bool> UpdateAsync(UpdateFields<M> updateFields, FilterWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, wrapper, cancellationToken);
        }

        public Task<bool> UpdateAsync<F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, updateField, fieldValue, cancellationToken);
        }

        public Task<bool> UpdateAsync<F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, field1, field1Value, field2, field2Value, cancellationToken);
        }

        public Task<bool> UpdateAsync<F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(wrapper, field1, field1Value, field2, field2Value, field3, field3Value, cancellationToken);
        }

        public Task<bool> UpdateAsync(M data, DataFilter filter, QueryFields? updateFields = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(data, filter, updateFields, cancellationToken: cancellationToken);
        }

        public Task<bool> UpdateAsync(UpdateFields updateFields, DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(updateFields, filter, cancellationToken: cancellationToken);
        }

        public Task<bool> UpdateAsync(UpdateFields<M> updateFields, DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync<M>(updateFields, filter, cancellationToken: cancellationToken);
        }

        public Task<bool> UpdateAsync<F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(filter, updateField, fieldValue, cancellationToken: cancellationToken);
        }

        public Task<bool> UpdateAsync<F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(filter, field1, field1Value, field2, field2Value, cancellationToken: cancellationToken);
        }

        public Task<bool> UpdateAsync<F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateAsync(filter, field1, field1Value, field2, field2Value, field3, field3Value, cancellationToken: cancellationToken);
        }

        public Task<bool> InsertOrUpdateAsync(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.InsertOrUpdateAsync(data, updateFields, insertId, commandTimeout, cancellationToken);
        }

        public Task<bool> InsertBatchAsync(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.InsertBatchAsync(dataList, batchSize, insertId, batchHandler, commandTimeout, cancellationToken);
        }

        public Task<bool> UpdateBatchAsync(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.UpdateBatchAsync(dataList, updateFields, batchSize, batchHandler, commandTimeout, cancellationToken);
        }

        public Task<bool> InsertOrUpdateBatchAsync(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.InsertOrUpdateBatchAsync(dataList, updateFields, batchSize, insertId, batchHandler, commandTimeout, cancellationToken);
        }

        public Task<bool> DeleteByIdAsync(object key, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteByIdAsync<M>(key, commandTimeout, cancellationToken);
        }

        public Task<bool> DeleteByIdAsync(M data, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteByIdAsync<M>(data, commandTimeout, cancellationToken);
        }

        public Task<bool> DeleteAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteAsync(wrapper, cancellationToken);
        }

        public Task<bool> DeleteAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteAsync(condition, cancellationToken: cancellationToken);
        }

        public Task<bool> DeleteAsync(FilterWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteAsync(wrapper, cancellationToken);
        }

        public Task<bool> DeleteAsync(DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteAsync<M>(filter, cancellationToken: cancellationToken);
        }

        public Task<bool> DeleteByIdsAsync(IEnumerable keyList, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.DeleteByIdsAsync<M>(keyList, commandTimeout, cancellationToken);
        }

        public Task<long> CountAsync(LambdaWrapper<M> wrapper, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CountAsync(wrapper, cancellationToken);
        }

        public Task<long> CountAsync(Expression<Func<M, bool>> condition, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CountAsync(condition, cancellationToken: cancellationToken);
        }

        public Task<long> CountAsync(FilterWrapper<M>? wrapper = null, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CountAsync(wrapper, cancellationToken);
        }

        public Task<long> CountAsync(DataFilter filter, CancellationToken cancellationToken = default)
        {
            return DatabaseClient.CountAsync<M>(filter, cancellationToken: cancellationToken);
        }

        #endregion

    }

}
