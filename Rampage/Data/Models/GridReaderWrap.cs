using System;

namespace Rampage.Data
{
    /// <summary>
    /// Multiple result data reader wrap
    /// </summary>
    public class GridReaderWrap : IDisposable
    {

        public int Index = -1;

        /// <summary>
        /// 
        /// </summary>
        private GridReaderWrap()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xReader"></param>
        public GridReaderWrap(IDataGridReader xReader)
        {
            this.DataReader = xReader;
        }

        /// <summary>
        /// GridReader
        /// </summary>
        internal IDataGridReader? DataReader
        {
            get;
            set;
        }

        /// <summary>
        /// select a single value
        /// </summary>
        /// <param name="columnName"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        public V? ExecuteScalar<V>(string columnName)
        {
            Index++;
            return DataReader!.ExecuteScalar<V>(columnName);
        }

        /// <summary>
        /// select a single value
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        public V? ExecuteScalar<V>(string columnName, V? defaultValue)
        {
            Index++;
            return DataReader!.ExecuteScalar<V>(columnName, defaultValue);
        }

        /// <summary>
        /// select one data
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        public M? SelectOne<M>() where M : class
        {
            Index++;
            return DataReader!.SelectOne<M>();
        }

        /// <summary>
        /// select one data
        /// </summary>
        /// <returns></returns>
        public dynamic? SelectOne()
        {
            Index++;
            return DataReader!.SelectOne();
        }


        /// <summary>
        /// select data list
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        public List<M> SelectList<M>() where M : class
        {
            Index++;
            return DataReader!.SelectList<M>();
        }

        /// <summary>
        /// select data list
        /// </summary>
        /// <returns></returns>
        public List<dynamic> SelectList()
        {
            Index++;
            return DataReader!.SelectList();
        }

        #region IDisposable

        /// <summary>
        /// 
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
        /// Register a callback invoked exactly once when this grid reader is disposed
        /// (used by DatabaseClient to release the connection gate / per-operation connection).
        /// </summary>
        public void AttachDisposeCallback(Action callback)
        {
            _disposeCallback += callback;
        }

        #endregion


    }
}
