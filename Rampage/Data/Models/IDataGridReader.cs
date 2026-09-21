using System;

namespace Rampage.Data
{
    /// <summary>
    /// 多结果集读取器抽象：每个读取方法按调用顺序消费一个结果集
    /// Abstraction for multi-result-set readers: each read method consumes one result set in call order
    /// </summary>
    public interface IDataGridReader : IDisposable
    {

        /// <summary>
        /// 读取当前结果集的单值
        /// Select a single value from the current result set
        /// </summary>
        /// <param name="columnName">列名 / column name</param>
        /// <typeparam name="V">值类型 / value type</typeparam>
        /// <returns>单值 / the single value</returns>
        V? ExecuteScalar<V>(string columnName);

        /// <summary>
        /// 读取当前结果集的单值
        /// Select a single value from the current result set
        /// </summary>
        /// <param name="columnName">列名 / column name</param>
        /// <param name="defaultValue">无行或为 DBNull 时返回的默认值 / value returned when no row or DBNull</param>
        /// <typeparam name="V">值类型 / value type</typeparam>
        /// <returns>单值 / the single value</returns>
        V? ExecuteScalar<V>(string columnName, V? defaultValue);

        /// <summary>
        /// 读取当前结果集的第一行并装配为实体
        /// Select one data row from the current result set
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <returns>实体；无数据时为 null / the entity, or null when no row</returns>
        M? SelectOne<M>() where M : class;

        /// <summary>
        /// 读取当前结果集的第一行并装配为动态对象
        /// Select one data row from the current result set as a dynamic object
        /// </summary>
        /// <returns>动态对象；无数据时为 null / the dynamic object, or null when no row</returns>
        dynamic? SelectOne();

        /// <summary>
        /// 读取当前结果集的全部行并装配为实体列表
        /// Select all rows of the current result set as an entity list
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <returns>实体列表 / the entity list</returns>
        List<M> SelectList<M>() where M : class;

        /// <summary>
        /// 读取当前结果集的全部行并装配为动态对象列表
        /// Select all rows of the current result set as a list of dynamic objects
        /// </summary>
        /// <returns>动态对象列表 / the dynamic list</returns>
        List<dynamic> SelectList();

    }
}
