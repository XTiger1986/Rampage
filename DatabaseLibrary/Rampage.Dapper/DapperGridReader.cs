using Dapper;
using Rampage.Data;
using Rampage.Utilities;
using System;

namespace Rampage.Dapper
{
    /// <summary>包裹 Dapper <see cref="SqlMapper.GridReader"/> 的多结果集读取器，实现 <see cref="IDataGridReader"/> / Multi-result-set reader wrapping Dapper's <see cref="SqlMapper.GridReader"/>; implements <see cref="IDataGridReader"/>.</summary>
    /// <remarks>
    /// 每次读取（ExecuteScalar / SelectOne / SelectList）都会消费 GridReader 中的下一个结果集，须按 SQL 中结果集的输出顺序依次调用。
    /// Each read (ExecuteScalar / SelectOne / SelectList) consumes the next result set of the GridReader; calls must follow the order the SQL returns them.
    /// </remarks>
    public class DapperGridReader : IDataGridReader
    {

        /// <summary>被包裹的 Dapper GridReader / the wrapped Dapper GridReader.</summary>
        internal SqlMapper.GridReader? DataReader
        {
            get;
            set;
        }


        /// <summary>用 Dapper GridReader 初始化包装器 / Initializes the wrap with a Dapper GridReader.</summary>
        /// <param name="xReader">Dapper QueryMultiple 返回的 GridReader / the GridReader returned by Dapper QueryMultiple.</param>
        public DapperGridReader(SqlMapper.GridReader xReader)
        {
            this.DataReader = xReader;
        }

        /// <summary>读取下一个结果集的首行并取指定列，无行时返回 default(V) / Reads the first row of the next result set and picks the given column; default(V) when there is no row.</summary>
        /// <typeparam name="V">目标值类型 / the target value type.</typeparam>
        /// <param name="columnName">列名 / the column name.</param>
        /// <returns>列值；无行时为 default(V) / the column value; default(V) when there is no row.</returns>
        public V? ExecuteScalar<V>(string columnName)
        {
            return ExecuteScalar<V>(columnName, default(V));
        }

        /// <summary>读取下一个结果集的首行并取指定列，无行时返回给定默认值 / Reads the first row of the next result set and picks the given column; the supplied default when there is no row.</summary>
        /// <typeparam name="V">目标值类型 / the target value type.</typeparam>
        /// <param name="columnName">列名 / the column name.</param>
        /// <param name="defaultValue">无行时的返回值 / the value returned when there is no row.</param>
        /// <returns>列值；无行时为 <paramref name="defaultValue"/> / the column value; <paramref name="defaultValue"/> when there is no row.</returns>
        public V? ExecuteScalar<V>(string columnName, V? defaultValue)
        {
            dynamic data = DataReader!.ReadSingleOrDefault();
            if (data != null)
            {
                return GenericTool.GetDynamicItem<V>(data, columnName);
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>读取下一个结果集的全部行并映射为 <typeparamref name="M"/> 列表 / Reads all rows of the next result set mapping them to a list of <typeparamref name="M"/>.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <returns>映射结果列表 / the mapped list.</returns>
        public List<M> SelectList<M>() where M : class
        {
            List<M> list = DataReader!.Read<M>().ToList();
            return list;
        }

        /// <summary>读取下一个结果集的全部行，返回 dynamic 列表 / Reads all rows of the next result set returning a dynamic list.</summary>
        /// <returns>行列表 / the row list.</returns>
        public List<dynamic> SelectList()
        {
            List<dynamic> list = DataReader!.Read().ToList();
            return list;
        }

        /// <summary>读取下一个结果集的首行并映射为 <typeparamref name="M"/>，无行时返回 null / Reads the first row of the next result set mapping it to <typeparamref name="M"/>; null when there is no row.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <returns>映射后的首行；无行时为 null / the mapped first row; null when there is no row.</returns>
        public M? SelectOne<M>() where M : class
        {
            M data = DataReader!.ReadSingleOrDefault<M>();
            return data;
        }

        /// <summary>读取下一个结果集的首行，无行时返回 null / Reads the first row of the next result set; null when there is no row.</summary>
        /// <returns>首行动态行对象；无行时为 null / the first row as a dynamic object; null when there is no row.</returns>
        public dynamic? SelectOne()
        {
            dynamic data = DataReader!.ReadSingleOrDefault();
            return data;
        }

        /// <summary>释放被包裹的 GridReader / Disposes the wrapped GridReader.</summary>
        public void Dispose()
        {
            if (this.DataReader != null)
            {
                this.DataReader.Dispose();
            }
        }

    }
}
