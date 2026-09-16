using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Data;
using System.Dynamic;

namespace Rampage.Data
{
    /// <summary>
    /// a DataReader wrap
    /// </summary>
    public class DataReaderWrap : IDataGridReader, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private DataReaderWrap()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        public DataReaderWrap(IDataReader reader)
        {
            m_InnerReader = reader;
            SetCurrentFieldNames();
        }

        /// <summary>
        /// 
        /// </summary>
        protected IDataReader? m_InnerReader;
        private IDbConnection? _ownedConnection;
        private IDbCommand? _ownedCommand;
        /// <summary>
        /// 
        /// </summary>
        public IDataReader InnerReader
        {
            get
            {
                return m_InnerReader!;
            }
        }

        /// <summary>
        /// Bind a connection whose lifetime is tied to this reader (short-lived operation connections).
        /// </summary>
        internal void SetOwnedConnection(IDbConnection? connection)
        {
            _ownedConnection = connection;
        }

        /// <summary>
        /// Bind a command whose lifetime is tied to this reader.
        /// </summary>
        public void SetOwnedCommand(IDbCommand? command)
        {
            _ownedCommand = command;
        }

        private Action? _disposeCallback;
        private int _disposed;

        /// <summary>
        /// Register a callback invoked exactly once when this reader is disposed
        /// (used by DatabaseClient to release the connection gate / per-operation connection).
        /// </summary>
        public void AttachDisposeCallback(Action callback)
        {
            _disposeCallback += callback;
        }

        /// <summary>
        /// DataReader.Read()
        /// </summary>
        public virtual bool Read()
        {
            return m_InnerReader!.Read();
        }

        /// <summary>
        /// DataReader.NextResult()
        /// </summary>
        public virtual bool NextResult()
        {
            bool r = m_InnerReader!.NextResult();
            if (r)
            {
                SetCurrentFieldNames();
            }
            return r;
        }

        /// <summary>
        /// check if DataReader result contains field (notice: May lead to low reading efficiency)
        /// </summary>
        /// <param name="name">field name</param>
        /// <returns></returns>
        public bool ContainsField(string name)
        {
            for (int i = 0; i < m_InnerReader!.FieldCount; i++)
            {
                if (m_InnerReader.GetName(i) == name)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public string[]? CurrentFieldNames
        {
            get;
            private set;
        }

        private void SetCurrentFieldNames()
        {
            CurrentFieldNames = new string[m_InnerReader!.FieldCount];
            for (int i = 0; i < m_InnerReader!.FieldCount; i++)
            {
                CurrentFieldNames[i] = m_InnerReader.GetName(i);
            }
        }

        /// <summary>
        /// get index of field in DataReader result (notice: May lead to low reading efficiency)
        /// </summary>
        /// <param name="name">field name</param>
        /// <returns></returns>
        public int IndexOf(string name)
        {
            for (int i = 0; i < m_InnerReader!.FieldCount; i++)
            {
                if (m_InnerReader.GetName(i) == name)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// get index of field in DataReader result (notice: If there is no field, an error may be occur)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetOrdinal(string name)
        {
            return m_InnerReader!.GetOrdinal(name);
        }

        /// <summary>
        /// get field value (notice: make sure field exists in DataReader result)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual object? Get(string name)
        {
            return Get(name, null);
        }

        /// <summary>
        /// get field value 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual object? Get(int index)
        {
            return Get(index, null);
        }

        /// <summary>
        /// get field value (notice: make sure field exists in DataReader result)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns>字段的值</returns>
        public virtual object? Get(string name, object? defaultValue)
        {
            int index = GetOrdinal(name);

            return Get(index, defaultValue);
        }

        /// <summary>
        /// get field value
        /// </summary>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns>字段的值</returns>
        public virtual object? Get(int index, object? defaultValue)
        {

            if (m_InnerReader!.IsDBNull(index) == false)
                return m_InnerReader[index];
            else
                return defaultValue;
        }

        /// <summary>
        /// get field value (notice: make sure field exists in DataReader result)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <param name="resultType"></param>
        /// <returns></returns>
        public virtual object? Get(string name, object? defaultValue, Type resultType)
        {
            int index = GetOrdinal(name);

            return Get(index, defaultValue, resultType);
        }

        /// <summary>
        /// get field value
        /// </summary>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual object? Get(int index, object? defaultValue, Type type)
        {
            if (index < 0)
                return defaultValue;
            if (m_InnerReader!.IsDBNull(index))
                return defaultValue;
            if (type == null)
            {
                type = typeof(object);
            }

            object? result = null;

            Type resultType = GenericTool.GetUnderlyingType(type);

            if (resultType.IsEnum)
            {
                object obj = m_InnerReader.GetValue(index);
                object tempObject = Enum.Parse(resultType, obj!.ToString()!, true);
                result = tempObject;
            }
            else if (resultType == typeof(Guid))
            {
                object obj = m_InnerReader.GetValue(index);
                Guid tempGuid;
                if (Guid.TryParse(obj.ToString(), out tempGuid))
                {
                    result = (object)tempGuid;
                }
                else
                {
                    result = defaultValue;
                }
            }
            else
            {
                TypeCode typeCode = Type.GetTypeCode(resultType);

                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        result = m_InnerReader.GetBoolean(index);
                        break;
                    case TypeCode.Byte:
                        result = m_InnerReader.GetByte(index);
                        break;
                    case TypeCode.SByte:
                        //IDataReader并无提供获取有符号Byte的方法，使用GetInt16代替
                        result = Convert.ToSByte(m_InnerReader.GetInt16(index));
                        break;
                    case TypeCode.Char:
                        result = m_InnerReader.GetChar(index);
                        break;
                    case TypeCode.DateTime:
                        result = m_InnerReader.GetDateTime(index);
                        break;
                    case TypeCode.Decimal:
                        result = m_InnerReader.GetDecimal(index);
                        break;
                    case TypeCode.Double:
                        result = m_InnerReader.GetDouble(index);
                        break;
                    case TypeCode.Single:
                        result = m_InnerReader.GetFloat(index);
                        break;
                    case TypeCode.Int16:
                        result = m_InnerReader.GetInt16(index);
                        break;
                    case TypeCode.Int32:
                        result = m_InnerReader.GetInt32(index);
                        break;
                    case TypeCode.Int64:
                        result = m_InnerReader.GetInt64(index);
                        break;
                    case TypeCode.String:
                        result = m_InnerReader.GetString(index);
                        break;
                    case TypeCode.Object:
                        result = m_InnerReader.GetValue(index);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// get field value (notice: make sure field exists in DataReader result)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual T? Get<T>(string name)
        {
            return Get<T>(name, default(T));
        }

        /// <summary>
        /// get field value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual T? Get<T>(int index)
        {
            return Get<T>(index, default(T));
        }

        /// <summary>
        /// get field value (notice: make sure field exists in DataReader result)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual T? Get<T>(string name, T? defaultValue)
        {
            return (T?)Get(name, defaultValue, typeof(T));
        }

        /// <summary>
        /// get field value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual T? Get<T>(int index, T? defaultValue)
        {
            return (T?)Get(index, defaultValue, typeof(T));
        }

        /// <summary>
        /// get field Nullable value (notice: make sure field exists in DataReader result)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Nullable<T> GetNullable<T>(string name) where T : struct
        {
            return Get<Nullable<T>>(name);
        }

        /// <summary>
        /// get field Nullable value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual Nullable<T> GetNullable<T>(int index) where T : struct
        {
            return Get<Nullable<T>>(index);
        }

        /// <summary>
        /// get model data from data reader row
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        public virtual M GetData<M>(TableMapping? mapping = null) where M : class
        {
            if (mapping == null)
            {
                mapping = ModelSchemaStore.GetModelMapping<M>()!;
            }

            M model = EmitTool.CreateModel<M>()!;
            for (int i = 0; i < m_InnerReader!.FieldCount; i++)
            {
                if (m_InnerReader.IsDBNull(i))
                {
                    continue;
                }
                string fieldName = m_InnerReader.GetName(i);
                FieldMapping? property = mapping.GetPropertyMapping(fieldName);
                if (property == null)
                {
                    property = mapping.GetColumnMapping(fieldName);
                }
                if (property == null)
                {
                    continue;
                }

                object? v = Get(i, null, property.PropertyType);
                if (v != null)
                {
                    EmitTool.SetPropertyValue(model, property.PropertyName, v);
                }
            }

            return model;
        }

        /// <summary>
        /// get dynamic data from data reader row
        /// </summary>
        /// <returns></returns>
        public virtual dynamic GetData()
        {
            dynamic model = new ExpandoObject();

            for (int i = 0; i < m_InnerReader!.FieldCount; i++)
            {
                if (m_InnerReader.IsDBNull(i))
                {
                    continue;
                }
                string fieldName = m_InnerReader.GetName(i);
                object v = m_InnerReader.GetValue(i);
                GenericTool.SetDynamicItem(model, fieldName, v);
            }
            return model;
        }

        public V? ExecuteScalar<V>(string columnName)
        {
            return ExecuteScalar<V>(columnName, default(V));
        }

        public V? ExecuteScalar<V>(string columnName, V? defaultValue)
        {
            V? v;
            if (Read())
            {
                v = Get<V>(columnName, defaultValue);
            }
            else
            {
                v = defaultValue;
            }
            NextResult();
            return v;
        }

        public List<M> SelectList<M>() where M : class
        {
            List<M> list = new List<M>();
            TableMapping mapping = ModelSchemaStore.GetModelMapping<M>()!;
            while (Read())
            {
                M model = GetData<M>(mapping);
                list.Add(model);
            }
            //List<M> list = InnerReader.SelectList<M>(CurrentFieldNames != null ? new QueryFields(CurrentFieldNames) : null);
            NextResult();
            return list;
        }

        public List<dynamic> SelectList()
        {
            List<dynamic> list = new List<dynamic>();
            while (Read())
            {
                dynamic model = GetData();

                list.Add(model);
            }
            NextResult();
            return list;
        }

        public M? SelectOne<M>() where M : class
        {
            M? model;
            if (Read())
            {
                model = GetData<M>();
                //model = InnerReader.SelectOne<M>(CurrentFieldNames != null ? new QueryFields(CurrentFieldNames) : null);
            }
            else
            {
                model = default(M);
            }
            NextResult();
            return model;
        }

        public dynamic? SelectOne()
        {
            dynamic? model;
            if (Read())
            {
                model = GetData();
            }
            else
            {
                model = null;
            }
            NextResult();
            return model;
        }



        #region IDisposable

        /// <summary>
        /// 
        /// </summary>
        public virtual void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                if (m_InnerReader != null)
                {
                    if (m_InnerReader.IsClosed == false)
                    {
                        m_InnerReader.Close();
                    }
                    m_InnerReader.Dispose();
                }

                if (_ownedCommand != null)
                {
                    _ownedCommand.Dispose();
                    _ownedCommand = null;
                }

                if (_ownedConnection != null)
                {
                    _ownedConnection.Dispose();
                    _ownedConnection = null;
                }
            }
            finally
            {
                Action? callback = _disposeCallback;
                _disposeCallback = null;
                callback?.Invoke();
            }
        }

        #endregion
    }
}
