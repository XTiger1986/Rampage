using System;

namespace Rampage.Data
{
    public interface IDataGridReader : IDisposable
    {

        /// <summary>
        /// select a single value
        /// </summary>
        /// <param name="columnName"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        V? ExecuteScalar<V>(string columnName);

        /// <summary>
        /// select a single value
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="V"></typeparam>
        /// <returns></returns>
        V? ExecuteScalar<V>(string columnName, V? defaultValue);

        /// <summary>
        /// select one data
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        M? SelectOne<M>() where M : class;

        /// <summary>
        /// select one data
        /// </summary>
        /// <returns></returns>
        dynamic? SelectOne();

        /// <summary>
        /// select data list
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        List<M> SelectList<M>() where M : class;

        /// <summary>
        /// select data list
        /// </summary>
        /// <returns></returns>
        List<dynamic> SelectList();

    }
}
