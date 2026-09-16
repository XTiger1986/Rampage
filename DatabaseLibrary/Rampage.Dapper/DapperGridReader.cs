using Dapper;
using Rampage.Data;
using Rampage.Utilities;
using System;

namespace Rampage.Dapper
{
    /// <summary>
    /// 
    /// </summary>
    public class DapperGridReader : IDataGridReader
    {

        /// <summary>
        /// Dapper GridReader
        /// </summary>
        internal SqlMapper.GridReader? DataReader
        {
            get;
            set;
        }


        public DapperGridReader(SqlMapper.GridReader xReader)
        {
            this.DataReader = xReader;
        }

        public V? ExecuteScalar<V>(string columnName)
        {
            return ExecuteScalar<V>(columnName, default(V));
        }

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

        public List<M> SelectList<M>() where M : class
        {
            List<M> list = DataReader!.Read<M>().ToList();
            return list;
        }

        public List<dynamic> SelectList()
        {
            List<dynamic> list = DataReader!.Read().ToList();
            return list;
        }

        public M? SelectOne<M>() where M : class
        {
            M data = DataReader!.ReadSingleOrDefault<M>();
            return data;
        }

        public dynamic? SelectOne()
        {
            dynamic data = DataReader!.ReadSingleOrDefault();
            return data;
        }

        public void Dispose()
        {
            if (this.DataReader != null)
            {
                this.DataReader.Dispose();
            }
        }

    }
}
