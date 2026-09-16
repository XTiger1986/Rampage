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
    /// business object base
    /// </summary>
    public abstract class BaseService : IBaseService
    {

        protected DateTime LocalNow
        {
            get { return DateTime.Now; }
        }

        protected DateTime UtcNow
        {
            get {  return DateTime.UtcNow; }
        }


        public virtual IServiceProvider? ServiceProvider
        {
            get;
            protected set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual IScopedServiceLocator? ServiceLocator
        {
            get;
            protected set;
        }

        /// <summary>
        /// 
        /// </summary>
        [Singleton]
        protected IStandAloneCache MemoryCache
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
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
        /// 
        /// </summary>
        /// <param name="cacheKeyTemplate"></param>
        /// <param name="cacheKeyArgs"></param>
        protected void RemoveCache(string cacheKeyTemplate, params object?[]? cacheKeyArgs)
        {
            string cacheKey = string.Format(cacheKeyTemplate, cacheKeyArgs ?? new object?[] { });
            MemoryCache.Remove(cacheKey);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheKeyPrefix"></param>
        protected void RemoveCacheByPrefix(string cacheKeyPrefix)
        {
            MemoryCache.RemoveBySearch(cacheKeyPrefix);
        }

        protected LocalizationResource? _defaultLanguage;
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
        /// 
        /// </summary>
        public BaseService()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(GetType().FullName);
            ServiceProvider = handle.ServiceProvider;
            ServiceLocator = handle.Locator;
            AutoInject();
        }

        /// <summary>
        /// 
        /// </summary>
        protected AutoInjecter? autoInjecter;

        /// <summary>
        /// 
        /// </summary>
        protected virtual void AutoInject()
        {
            if (ServiceLocator == null)
            {
                throw new InvalidOperationException($"{GetType().FullName}: ServiceLocator is null before AutoInject.");
            }
            autoInjecter = new AutoInjecter(ServiceLocator);
            autoInjecter.Build(this);
        }

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
        /// 多实体编排服务：通过任一参与写操作的 Dao 开启事务（�?Scope 内各 Dao 共用 DatabaseClient）�?        /// 开启后可调用无�?<see cref="CommitTransaction"/> / <see cref="RollbackTransaction"/>�?        /// </summary>
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
        /// 提交�?<see cref="BeginTransaction{TEntity}"/> 开启的事务�?        /// </summary>
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
        /// 回滚�?<see cref="BeginTransaction{TEntity}"/> 开启的事务�?        /// </summary>
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
        /// 显式指定 Dao 提交（不依赖 Begin 时缓存的委托）�?        /// </summary>
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
        /// 显式指定 Dao 回滚�?        /// </summary>
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

        private void ClearActiveTransaction()
        {
            _txCommit = null;
            _txRollback = null;
        }

        #endregion
    }


    /// <summary>
    /// business object base
    /// </summary>
    /// <typeparam name="D"></typeparam>
    /// <typeparam name="M"></typeparam>
    public abstract class BaseService<M, D> : BaseService, IBaseService<M, D>
        where M : class
        where D : ISuperDao<M>
    {

        /// <summary>
        /// 
        /// </summary>
        private ISuperDao<M> Dao
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public BaseService() : base()
        {
            Dao = ServiceLocator!.Get<D>();
        }


        #region IServiceBase

        public void ChangeDatabase(string databaseName)
        {
            Dao.ChangeDatabase(databaseName);
        }

        public void BeginTransaction(IsolationLevel iso = IsolationLevel.ReadCommitted)
        {
            Dao.BeginTransaction(iso);
        }

        public new void CommitTransaction()
        {
            Dao.CommitTransaction();
        }

        public new void RollbackTransaction()
        {
            Dao.RollbackTransaction();
        }

        public List<M> SelectList(LambdaWrapper<M> wrapper)
        {
            return Dao.SelectList(wrapper);
        }

        public List<M> SelectList(Expression<Func<M, bool>> condition)
        {
            return Dao.SelectList(condition);
        }

        public List<R> SelectList<R>(LambdaWrapper<M> wrapper) where R : class
        {
            return Dao.SelectList<R>(wrapper);
        }

        public List<R> SelectList<R>(Expression<Func<M, bool>> condition) where R : class
        {
            return Dao.SelectList<R>(condition);
        }

        public List<M> SelectList(FilterWrapper<M>? wrapper)
        {
            return Dao.SelectList(wrapper);
        }

        public List<R> SelectList<R>(FilterWrapper<M>? wrapper) where R : class
        {
            return Dao.SelectList<R>(wrapper);
        }

        public virtual List<M> SelectList(DataFilter filter)
        {
            return Dao.SelectList(filter);
        }

        public virtual List<R> SelectList<R>(DataFilter filter) where R : class
        {
            return Dao.SelectList<R>(filter);
        }

        public List<M> SelectPage(PagerInfo pagerInfo, LambdaWrapper<M> wrapper)
        {
            return Dao.SelectPage(pagerInfo, wrapper);
        }

        public List<R> SelectPage<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper) where R : class
        {
            return Dao.SelectPage<R>(pagerInfo, wrapper);
        }

        public List<M> SelectPage(PagerInfo pagerInfo, Expression<Func<M, bool>> condition)
        {
            return Dao.SelectPage(pagerInfo, condition);
        }

        public List<R> SelectPage<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition) where R : class
        {
            return Dao.SelectPage<R>(pagerInfo, condition);
        }

        public List<M> SelectPage(PagerInfo pagerInfo, FilterWrapper<M>? wrapper)
        {
            return Dao.SelectPage(pagerInfo, wrapper);
        }

        public List<R> SelectPage<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper) where R : class
        {
            return Dao.SelectPage<R>(pagerInfo, wrapper);
        }

        public List<M> SelectPage(PagerInfo pagerInfo, DataFilter filter)
        {
            return Dao.SelectPage(pagerInfo, filter);
        }

        public List<R> SelectPage<R>(PagerInfo pagerInfo, DataFilter filter) where R : class
        {
            return Dao.SelectPage<R>(pagerInfo, filter);
        }

        public List<M> SelectByIds(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null)
        {
            return Dao.SelectByIds(keyList, resultFields, commandTimeout);
        }

        public List<R> SelectByIds<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) where R : class
        {
            return Dao.SelectByIds<R>(keyList, resultFields, commandTimeout);
        }

        public M? SelectOne(LambdaWrapper<M> wrapper)
        {
            return Dao.SelectOne(wrapper);
        }

        public R? SelectOne<R>(LambdaWrapper<M> wrapper) where R : class
        {
            return Dao.SelectOne<R>(wrapper);
        }

        public M? SelectOne(Expression<Func<M, bool>> condition)
        {
            return Dao.SelectOne(condition);
        }

        public R? SelectOne<R>(Expression<Func<M, bool>> condition) where R : class
        {
            return Dao.SelectOne<R>(condition);
        }

        public M? SelectOne(FilterWrapper<M> wrapper)
        {
            return Dao.SelectOne(wrapper);
        }

        public R? SelectOne<R>(FilterWrapper<M> wrapper) where R : class
        {
            return Dao.SelectOne<R>(wrapper);
        }

        public M? SelectOne(DataFilter filter)
        {
            return Dao.SelectOne(filter);
        }

        public R? SelectOne<R>(DataFilter filter) where R : class
        {
            return Dao.SelectOne<R>(filter);
        }

        public M? SelectById(object key, QueryFields? resultFields = null, int? commandTimeout = null)
        {
            return Dao.SelectById(key, resultFields, commandTimeout);
        }

        public R? SelectById<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null) where R : class
        {
            return Dao.SelectById<R>(key, resultFields, commandTimeout);
        }

        public bool Insert(M data, bool insertId = false, int? commandTimeout = null)
        {
            return Dao.Insert(data, insertId, commandTimeout);
        }

        public bool UpdateById(M data, QueryFields? updateFields = null, int? commandTimeout = null)
        {
            return Dao.UpdateById(data, updateFields, commandTimeout);
        }

        public bool UpdateById(object id, UpdateFields updateFields, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, updateFields, commandTimeout);
        }

        public bool UpdateById(object id, UpdateFields<M> updateFields, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, updateFields, commandTimeout);
        }

        public bool UpdateById<F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, updateField, fieldValue, commandTimeout);
        }

        public bool UpdateById<F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, field1, field1Value, field2, field2Value, commandTimeout);
        }

        public bool UpdateById<F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null)
        {
            return Dao.UpdateById(id, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout);
        }

        public bool Update(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null)
        {
            return Dao.Update(data, wrapper, updateFields);
        }

        public bool Update(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null)
        {
            return Dao.Update(data, condition, updateFields);
        }

        public bool Update(UpdateFields updateFields, Expression<Func<M, bool>> condition)
        {
            return Dao.Update(updateFields, condition);
        }

        public bool Update(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition)
        {
            return Dao.Update(updateFields, condition);
        }

        public bool Update<F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return Dao.Update(condition, updateField, fieldValue);
        }

        public bool Update<F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return Dao.Update(condition, field1, field1Value, field2, field2Value);
        }

        public bool Update<F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return Dao.Update(condition, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        public bool Update(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null)
        {
            return Dao.Update(data, wrapper, updateFields);
        }

        public bool Update(UpdateFields updateFields, FilterWrapper<M> wrapper)
        {
            return Dao.Update(updateFields, wrapper);
        }

        public bool Update(UpdateFields<M> updateFields, FilterWrapper<M> wrapper)
        {
            return Dao.Update(updateFields, wrapper);
        }

        public bool Update<F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return Dao.Update(wrapper, updateField, fieldValue);
        }

        public bool Update<F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return Dao.Update(wrapper, field1, field1Value, field2, field2Value);
        }

        public bool Update<F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return Dao.Update(wrapper, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        public bool Update(M data, DataFilter filter, QueryFields? updateFields = null)
        {
            return Dao.Update(data, filter, updateFields);
        }

        public bool Update(UpdateFields updateFields, DataFilter filter)
        {
            return Dao.Update(updateFields, filter);
        }

        public bool Update(UpdateFields<M> updateFields, DataFilter filter)
        {
            return Dao.Update(updateFields, filter);
        }

        public bool Update<F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return Dao.Update(filter, updateField, fieldValue);
        }

        public bool Update<F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return Dao.Update(filter, field1, field1Value, field2, field2Value);
        }

        public bool Update<F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return Dao.Update(filter, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        public bool InsertOrUpdate(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null)
        {
            return Dao.InsertOrUpdate(data, updateFields, insertId, commandTimeout);
        }

        public bool InsertBatch(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return Dao.InsertBatch(dataList, batchSize, insertId, batchHandler, commandTimeout);
        }

        public bool UpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return Dao.UpdateBatch(dataList, updateFields, batchSize, batchHandler, commandTimeout);
        }

        public bool InsertOrUpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return Dao.InsertOrUpdateBatch(dataList, updateFields, batchSize, insertId, batchHandler, commandTimeout);
        }

        public bool DeleteById(object key, int? commandTimeout = null)
        {
            return Dao.DeleteById(key, commandTimeout);
        }

        public bool DeleteById(M data, int? commandTimeout = null)
        {
            return Dao.DeleteById(data, commandTimeout);
        }

        public bool Delete(LambdaWrapper<M> wrapper)
        {
            return Dao.Delete(wrapper);
        }

        public bool Delete(Expression<Func<M, bool>> condition)
        {
            return Dao.Delete(condition);
        }

        public bool Delete(FilterWrapper<M> wrapper)
        {
            return Dao.Delete(wrapper);
        }

        public bool Delete(DataFilter filter)
        {
            return Dao.Delete(filter);
        }

        public bool DeleteByIds(IEnumerable keyList, int? commandTimeout = null)
        {
            return Dao.DeleteByIds(keyList, commandTimeout);
        }

        public long Count(LambdaWrapper<M> wrapper)
        {
            return Dao.Count(wrapper);
        }

        public long Count(Expression<Func<M, bool>> condition)
        {
            return Dao.Count(condition);
        }

        public long Count(FilterWrapper<M>? wrapper = null)
        {
            return Dao.Count(wrapper);
        }

        public long Count(DataFilter filter)
        {
            return Dao.Count(filter);
        }

        #endregion

    }

}
