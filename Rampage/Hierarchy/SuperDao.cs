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
    /// 
    /// </summary>
    public abstract partial class SuperDao<M> : ISuperDao<M>, IDisposable
        where M : class
    {

        protected DateTime LocalNow
        {
            get { return DateTime.Now; }
        }

        protected DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
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
        protected virtual string DatabaseName
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// Database Client Instance
        /// </summary>
        public DatabaseClient DatabaseClient
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        protected TableMapping ModelMapping
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        public SuperDao()
        {
            Initialize();
            AutoInject();
            InjectDatabaseClient();
        }

        public SuperDao(string databaseName)
        {
            DatabaseName = databaseName;
            Initialize();
            AutoInject();
            InjectDatabaseClient();
        }

        protected virtual void Initialize()
        {
            RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(GetType().FullName);
            ServiceProvider = handle.ServiceProvider;
            ServiceLocator = handle.Locator;
            ModelMapping = ModelSchemaStore.GetModelMapping<M>()!;
        }

        /// <summary>
        /// 
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
        /// ��ⲿ����ģ��δӳ�䵽�κοͻ���ʱ�Ļ��˲��ԣ��� DatabaseFactoryConfig.UnmappedModelFallback��
        /// Ĭ���׳��쳣�����Ǿ�Ĭ�䵽��һ���ͻ��ˣ������ A ��ı���д�� B �⡣
        /// </summary>
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
        /// 
        /// </summary>
        protected AutoInjecter? Injecter;

        /// <summary>
        /// 
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

        #region IDao

        public void BeginTransaction(IsolationLevel iso = IsolationLevel.ReadCommitted)
        {
            DatabaseClient.BeginTransaction(iso);
        }

        public void CommitTransaction()
        {
            DatabaseClient.CommitTransaction();
        }

        public void RollbackTransaction()
        {
            DatabaseClient.RollbackTransaction();
        }

        public List<M> SelectList(LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.SelectList(wrapper);
        }

        public List<R> SelectList<R>(LambdaWrapper<M> wrapper) where R : class
        {
            return DatabaseClient.SelectList<M, R>(wrapper);
        }

        public List<M> SelectList(Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.SelectList(condition);
        }

        public List<R> SelectList<R>(Expression<Func<M, bool>> condition) where R : class
        {
            return DatabaseClient.SelectList<M, R>(condition);
        }

        public List<M> SelectList(FilterWrapper<M>? wrapper)
        {
            return DatabaseClient.SelectList(wrapper);
        }

        public List<R> SelectList<R>(FilterWrapper<M>? wrapper) where R : class
        {
            return DatabaseClient.SelectList<M, R>(wrapper);
        }

        public virtual List<M> SelectList(DataFilter filter)
        {
            return DatabaseClient.SelectList<M>(filter);
        }

        public virtual List<R> SelectList<R>(DataFilter filter) where R : class
        {
            return DatabaseClient.SelectList<M, R>(filter);
        }

        public List<M> SelectPage(PagerInfo pagerInfo, LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.SelectPage<M>(pagerInfo, wrapper);
        }

        public List<R> SelectPage<R>(PagerInfo pagerInfo, LambdaWrapper<M> wrapper) where R : class
        {
            return DatabaseClient.SelectPage<M, R>(pagerInfo, wrapper);
        }

        public List<M> SelectPage(PagerInfo pagerInfo, Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.SelectPage(pagerInfo, condition);
        }

        public List<R> SelectPage<R>(PagerInfo pagerInfo, Expression<Func<M, bool>> condition) where R : class
        {
            return DatabaseClient.SelectPage<M, R>(pagerInfo, condition);
        }

        public List<M> SelectPage(PagerInfo pagerInfo, FilterWrapper<M>? wrapper)
        {
            return DatabaseClient.SelectPage(pagerInfo, wrapper);
        }

        public List<R> SelectPage<R>(PagerInfo pagerInfo, FilterWrapper<M>? wrapper) where R : class
        {
            return DatabaseClient.SelectPage<M, R>(pagerInfo, wrapper);
        }

        public List<M> SelectPage(PagerInfo pagerInfo, DataFilter filter)
        {
            return DatabaseClient.SelectPage<M>(pagerInfo, filter);
        }

        public List<R> SelectPage<R>(PagerInfo pagerInfo, DataFilter filter) where R : class
        {
            return DatabaseClient.SelectPage<M, R>(pagerInfo, filter);
        }

        public List<M> SelectByIds(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null)
        {
            return DatabaseClient.SelectByIds<M>(keyList, resultFields, commandTimeout);
        }

        public List<R> SelectByIds<R>(IEnumerable keyList, QueryFields? resultFields = null, int? commandTimeout = null) where R : class
        {
            return DatabaseClient.SelectByIds<M, R>(keyList, resultFields, commandTimeout);
        }

        public M? SelectOne(LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.SelectOne(wrapper);
        }

        public R? SelectOne<R>(LambdaWrapper<M> wrapper) where R : class
        {
            return DatabaseClient.SelectOne<M, R>(wrapper);
        }

        public M? SelectOne(Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.SelectOne(condition);
        }

        public R? SelectOne<R>(Expression<Func<M, bool>> condition) where R : class
        {
            return DatabaseClient.SelectOne<M, R>(condition);
        }

        public M? SelectOne(FilterWrapper<M> wrapper)
        {
            return DatabaseClient.SelectOne(wrapper);
        }

        public R? SelectOne<R>(FilterWrapper<M> wrapper) where R : class
        {
            return DatabaseClient.SelectOne<M, R>(wrapper);
        }

        public M? SelectOne(DataFilter filter)
        {
            return DatabaseClient.SelectOne<M>(filter);
        }

        public R? SelectOne<R>(DataFilter filter) where R : class
        {
            return DatabaseClient.SelectOne<M, R>(filter);
        }

        public M? SelectById(object key, QueryFields? resultFields = null, int? commandTimeout = null)
        {
            return DatabaseClient.SelectById<M>(key, resultFields, commandTimeout);
        }

        public R? SelectById<R>(object key, QueryFields? resultFields = null, int? commandTimeout = null) where R : class
        {
            return DatabaseClient.SelectById<M, R>(key, resultFields, commandTimeout);
        }

        public bool Insert(M data, bool insertId = false, int? commandTimeout = null)
        {
            return DatabaseClient.Insert(data, insertId, commandTimeout);
        }

        public bool UpdateById(M data, QueryFields? updateFields = null, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(data, updateFields, commandTimeout);
        }

        public bool UpdateById(object id, UpdateFields updateFields, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById<M>(id, updateFields, commandTimeout);
        }

        public bool UpdateById(object id, UpdateFields<M> updateFields, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(id, updateFields, commandTimeout);
        }

        public bool UpdateById<F>(object id, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(id, updateField, fieldValue, commandTimeout);
        }

        public bool UpdateById<F1, F2>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(id, field1, field1Value, field2, field2Value, commandTimeout);
        }

        public bool UpdateById<F1, F2, F3>(object id, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateById(id, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout);
        }

        public bool Update(M data, LambdaWrapper<M> wrapper, QueryFields? updateFields = null)
        {
            return DatabaseClient.Update(data, wrapper, updateFields);
        }

        public bool Update(UpdateFields updateFields, LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.Update(updateFields, wrapper);
        }

        public bool Update(UpdateFields<M> updateFields, LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.Update(updateFields, wrapper);
        }

        public bool Update<F>(LambdaWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return DatabaseClient.Update(wrapper, updateField, fieldValue);
        }

        public bool Update<F1, F2>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return DatabaseClient.Update(wrapper, field1, field1Value, field2, field2Value);
        }

        public bool Update<F1, F2, F3>(LambdaWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return DatabaseClient.Update(wrapper, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        public bool Update(M data, Expression<Func<M, bool>> condition, QueryFields? updateFields = null)
        {
            return DatabaseClient.Update(data, condition, updateFields);
        }

		public bool Update(UpdateFields updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null)
        {
            return DatabaseClient.Update(updateFields, condition, commandTimeout);
        }

        public bool Update(UpdateFields<M> updateFields, Expression<Func<M, bool>> condition, int? commandTimeout = null)
        {
            return DatabaseClient.Update(updateFields, condition, commandTimeout);
        }

        public bool Update<F>(Expression<Func<M, bool>> condition, Expression<Func<M, F>> updateField, F? fieldValue, int? commandTimeout = null)
        {
            return DatabaseClient.Update(condition, updateField, fieldValue, commandTimeout);
        }

        public bool Update<F1, F2>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, int? commandTimeout = null)
        {
            return DatabaseClient.Update(condition, field1, field1Value, field2, field2Value, commandTimeout);
        }

        public bool Update<F1, F2, F3>(Expression<Func<M, bool>> condition, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value, int? commandTimeout = null)
        {
            return DatabaseClient.Update(condition, field1, field1Value, field2, field2Value, field3, field3Value, commandTimeout);
        }

        public bool Update(M data, FilterWrapper<M> wrapper, QueryFields? updateFields = null)
        {
            return DatabaseClient.Update(data, wrapper, updateFields);
        }

        public bool Update(UpdateFields updateFields, FilterWrapper<M> wrapper)
        {
            return DatabaseClient.Update(updateFields, wrapper);
        }

        public bool Update(UpdateFields<M> updateFields, FilterWrapper<M> wrapper)
        {
            return DatabaseClient.Update(updateFields, wrapper);
        }

        public bool Update<F>(FilterWrapper<M> wrapper, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return DatabaseClient.Update(wrapper, updateField, fieldValue);
        }

        public bool Update<F1, F2>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return DatabaseClient.Update(wrapper, field1, field1Value, field2, field2Value);
        }

        public bool Update<F1, F2, F3>(FilterWrapper<M> wrapper, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return DatabaseClient.Update(wrapper, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        public bool Update(M data, DataFilter filter, QueryFields? updateFields = null)
        {
            return DatabaseClient.Update(data, filter, updateFields);
        }

        public bool Update(UpdateFields updateFields, DataFilter filter)
        {
            return DatabaseClient.Update(updateFields, filter);
        }

        public bool Update(UpdateFields<M> updateFields, DataFilter filter)
        {
            return DatabaseClient.Update<M>(updateFields, filter);
        }

        public bool Update<F>(DataFilter filter, Expression<Func<M, F>> updateField, F? fieldValue)
        {
            return DatabaseClient.Update(filter, updateField, fieldValue);
        }

        public bool Update<F1, F2>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value)
        {
            return DatabaseClient.Update(filter, field1, field1Value, field2, field2Value);
        }

        public bool Update<F1, F2, F3>(DataFilter filter, Expression<Func<M, F1>> field1, F1? field1Value, Expression<Func<M, F2>> field2, F2? field2Value, Expression<Func<M, F3>> field3, F3? field3Value)
        {
            return DatabaseClient.Update(filter, field1, field1Value, field2, field2Value, field3, field3Value);
        }

        public bool InsertOrUpdate(M data, QueryFields? updateFields = null, bool insertId = false, int? commandTimeout = null)
        {
            return DatabaseClient.InsertOrUpdate(data, updateFields, insertId, commandTimeout);
        }

        public bool InsertBatch(IEnumerable<M> dataList, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return DatabaseClient.InsertBatch(dataList, batchSize, insertId, batchHandler, commandTimeout);
        }

        public bool UpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return DatabaseClient.UpdateBatch(dataList, updateFields, batchSize, batchHandler, commandTimeout);
        }

        public bool InsertOrUpdateBatch(IEnumerable<M> dataList, QueryFields? updateFields = null, int batchSize = 1000, bool insertId = false, Func<BatchDataOperateState<M>, bool>? batchHandler = null, int? commandTimeout = null)
        {
            return DatabaseClient.InsertOrUpdateBatch(dataList, updateFields, batchSize, insertId, batchHandler, commandTimeout);
        }

        public bool DeleteById(object key, int? commandTimeout = null)
        {
            return DatabaseClient.DeleteById<M>(key, commandTimeout);
        }

        public bool DeleteById(M data, int? commandTimeout = null)
        {
            return DatabaseClient.DeleteById<M>(data, commandTimeout);
        }

        public bool Delete(LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.Delete(wrapper);
        }

        public bool Delete(Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.Delete(condition);
        }

        public bool Delete(FilterWrapper<M> wrapper)
        {
            return DatabaseClient.Delete(wrapper);
        }

        public bool Delete(DataFilter filter)
        {
            return DatabaseClient.Delete<M>(filter);
        }

        public bool DeleteByIds(IEnumerable keyList, int? commandTimeout = null)
        {
            return DatabaseClient.DeleteByIds<M>(keyList, commandTimeout);
        }

        public long Count(LambdaWrapper<M> wrapper)
        {
            return DatabaseClient.Count(wrapper);
        }

        public long Count(Expression<Func<M, bool>> condition)
        {
            return DatabaseClient.Count(condition);
        }

        public long Count(FilterWrapper<M>? wrapper = null)
        {
            return DatabaseClient.Count(wrapper);
        }

        public long Count(DataFilter filter)
        {
            return DatabaseClient.Count<M>(filter);
        }

        #endregion

        public void Dispose()
        {
            if (Injecter != null)
            {
                Injecter.Dispose();
            }
        }
    }

}
