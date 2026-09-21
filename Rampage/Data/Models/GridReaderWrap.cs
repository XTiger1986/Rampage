using System;

namespace Rampage.Data
{
    /// <summary>
    /// 多结果集读取器包装器：每个读取方法按调用顺序消费内部读取器的一个结果集
    /// Multiple-result data reader wrap: each read method consumes one result set of the wrapped reader in call order
    /// </summary>
    /// <remarks>
    /// Dispose 会释放内部 DataReader 并触发 AttachDisposeCallback 注册的回调
    /// （DatabaseClient 依赖该回调释放连接信号量门与每次操作的临时连接）。
    /// 调用方必须使用 using 或显式 Dispose。
    /// </remarks>
    public class GridReaderWrap : IDisposable
    {

        /// <summary>
        /// 已消费的结果集游标（从 -1 开始，每调用一次读取方法加 1）
        /// Cursor of consumed result sets (starts at -1; each read call advances it by 1)
        /// </summary>
        public int Index = -1;

        /// <summary>
        /// 私有构造：禁止外部无参构造
        /// Private ctor: prevents parameterless construction
        /// </summary>
        private GridReaderWrap()
        {
        }

        /// <summary>
        /// 用一个多结果集读取器构造包装器
        /// Construct the wrap with a multi-result reader
        /// </summary>
        /// <param name="xReader">被包装的读取器 / the reader to wrap</param>
        public GridReaderWrap(IDataGridReader xReader)
        {
            this.DataReader = xReader;
        }

        /// <summary>
        /// 被包装的多结果集读取器
        /// The wrapped grid reader
        /// </summary>
        internal IDataGridReader? DataReader
        {
            get;
            set;
        }

        /// <summary>
        /// 消费下一个结果集并读取其单值（游标自动后移）
        /// Consume the next result set and select a single value from it (the cursor advances automatically)
        /// </summary>
        /// <param name="columnName">列名 / column name</param>
        /// <typeparam name="V">值类型 / value type</typeparam>
        /// <returns>单值；无行或为 DBNull 时返回类型默认值 / the value, or the type default when no row or DBNull</returns>
        public V? ExecuteScalar<V>(string columnName)
        {
            Index++;
            return DataReader!.ExecuteScalar<V>(columnName);
        }

        /// <summary>
        /// 消费下一个结果集并读取其单值（游标自动后移）
        /// Consume the next result set and select a single value from it (the cursor advances automatically)
        /// </summary>
        /// <param name="columnName">列名 / column name</param>
        /// <param name="defaultValue">无行或为 DBNull 时返回的默认值 / value returned when no row or DBNull</param>
        /// <typeparam name="V">值类型 / value type</typeparam>
        /// <returns>单值 / the single value</returns>
        public V? ExecuteScalar<V>(string columnName, V? defaultValue)
        {
            Index++;
            return DataReader!.ExecuteScalar<V>(columnName, defaultValue);
        }

        /// <summary>
        /// 消费下一个结果集并读取其第一行装配为实体（游标自动后移）
        /// Consume the next result set and select its first row as an entity (the cursor advances automatically)
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <returns>实体；无数据时为 null / the entity, or null when no row</returns>
        public M? SelectOne<M>() where M : class
        {
            Index++;
            return DataReader!.SelectOne<M>();
        }

        /// <summary>
        /// 消费下一个结果集并读取其第一行装配为动态对象（游标自动后移）
        /// Consume the next result set and select its first row as a dynamic object (the cursor advances automatically)
        /// </summary>
        /// <returns>动态对象；无数据时为 null / the dynamic object, or null when no row</returns>
        public dynamic? SelectOne()
        {
            Index++;
            return DataReader!.SelectOne();
        }


        /// <summary>
        /// 消费下一个结果集并读取其全部行装配为实体列表（游标自动后移）
        /// Consume the next result set and select all its rows as an entity list (the cursor advances automatically)
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <returns>实体列表 / the entity list</returns>
        public List<M> SelectList<M>() where M : class
        {
            Index++;
            return DataReader!.SelectList<M>();
        }

        /// <summary>
        /// 消费下一个结果集并读取其全部行装配为动态对象列表（游标自动后移）
        /// Consume the next result set and select all its rows as a list of dynamic objects (the cursor advances automatically)
        /// </summary>
        /// <returns>动态对象列表 / the dynamic list</returns>
        public List<dynamic> SelectList()
        {
            Index++;
            return DataReader!.SelectList();
        }

        #region IDisposable

        /// <summary>
        /// 释放内部读取器并触发 Dispose 回调（幂等，仅执行一次）
        /// Dispose the wrapped reader and run dispose callbacks (idempotent, runs exactly once)
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                if (this.DataReader != null)
                {
                    this.DataReader.Dispose();
                }
            }
            finally
            {
                Action? callback = _disposeCallback;
                _disposeCallback = null;
                callback?.Invoke();
            }
        }

        private Action? _disposeCallback;
        private int _disposed;

        /// <summary>
        /// 注册 Dispose 时恰好执行一次的回调
        /// （DatabaseClient 用它释放连接信号量门与每次操作的临时连接）
        /// Register a callback invoked exactly once when this grid reader is disposed
        /// (used by DatabaseClient to release the connection gate / per-operation connection)
        /// </summary>
        /// <param name="callback">回调 / the callback</param>
        public void AttachDisposeCallback(Action callback)
        {
            _disposeCallback += callback;
        }

        #endregion


    }
}
