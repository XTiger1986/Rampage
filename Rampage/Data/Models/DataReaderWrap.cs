using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Data;
using System.Dynamic;

namespace Rampage.Data
{
    /// <summary>
    /// IDataReader 包装器：在原生读取器之上提供类型转换取值、实体装配与结果集遍历能力
    /// A DataReader wrap providing typed value access, entity materialization and result-set traversal over IDataReader
    /// </summary>
    /// <remarks>
    /// Dispose 时会关闭并释放内部 reader、经 SetOwnedCommand / SetOwnedConnection 绑定的命令与连接，
    /// 并触发 AttachDisposeCallback 注册的回调（DatabaseClient 依赖该回调释放信号量门与每次操作的临时连接）。
    /// 调用方必须使用 using 或显式 Dispose，否则连接与门将一直被占用。
    /// </remarks>
    public class DataReaderWrap : IDataGridReader, IDisposable
    {
        /// <summary>
        /// 私有构造：禁止外部无参构造
        /// Private ctor: prevents parameterless construction
        /// </summary>
        private DataReaderWrap()
        {
        }

        /// <summary>
        /// 用一个 IDataReader 构造包装器并缓存其字段名列表
        /// Construct the wrap with an IDataReader and cache its field names
        /// </summary>
        /// <param name="reader">被包装的读取器 / the reader to wrap</param>
        public DataReaderWrap(IDataReader reader)
        {
            m_InnerReader = reader;
            SetCurrentFieldNames();
        }

        /// <summary>
        /// 被包装的内部读取器
        /// The wrapped inner reader
        /// </summary>
        protected IDataReader? m_InnerReader;
        private IDbConnection? _ownedConnection;
        private IDbCommand? _ownedCommand;
        /// <summary>
        /// 被包装的内部读取器
        /// The wrapped inner reader
        /// </summary>
        public IDataReader InnerReader
        {
            get
            {
                return m_InnerReader!;
            }
        }

        /// <summary>
        /// 绑定一个生命周期与本次读取绑定的连接（短生命周期的操作连接）
        /// Bind a connection whose lifetime is tied to this reader (short-lived operation connections)
        /// </summary>
        /// <param name="connection">随 reader 一起释放的连接 / the connection disposed together with the reader</param>
        internal void SetOwnedConnection(IDbConnection? connection)
        {
            _ownedConnection = connection;
        }

        /// <summary>
        /// 绑定一个生命周期与本次读取绑定的命令
        /// Bind a command whose lifetime is tied to this reader
        /// </summary>
        /// <param name="command">随 reader 一起释放的命令 / the command disposed together with the reader</param>
        public void SetOwnedCommand(IDbCommand? command)
        {
            _ownedCommand = command;
        }

        private Action? _disposeCallback;
        private int _disposed;

        /// <summary>
        /// 注册 Dispose 时恰好执行一次的回调
        /// （DatabaseClient 用它释放连接信号量门与每次操作的临时连接）
        /// Register a callback invoked exactly once when this reader is disposed
        /// (used by DatabaseClient to release the connection gate / per-operation connection)
        /// </summary>
        /// <param name="callback">回调 / the callback</param>
        public void AttachDisposeCallback(Action callback)
        {
            _disposeCallback += callback;
        }

        /// <summary>
        /// 推进到下一行（DataReader.Read）
        /// Advance to the next row (DataReader.Read)
        /// </summary>
        /// <returns>是否读到数据行 / true when a row was read</returns>
        public virtual bool Read()
        {
            return m_InnerReader!.Read();
        }

        /// <summary>
        /// 推进到下一个结果集并刷新字段名缓存（DataReader.NextResult）
        /// Advance to the next result set and refresh the field-name cache (DataReader.NextResult)
        /// </summary>
        /// <returns>是否还有结果集 / true when another result set is available</returns>
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
        /// 检查当前结果集是否包含指定字段（逐列遍历，可能影响读取效率）
        /// Check whether the current result set contains a field (linear scan; may reduce reading efficiency)
        /// </summary>
        /// <param name="name">字段名 / field name</param>
        /// <returns>是否包含该字段 / whether the field exists</returns>
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
        /// 当前结果集的全部字段名缓存（构造及 NextResult 成功时刷新）
        /// Cached field names of the current result set (refreshed on construction and successful NextResult)
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
        /// 获取字段在当前结果集中的序号（逐列遍历，可能影响读取效率）
        /// Get the ordinal of a field in the current result set (linear scan; may reduce reading efficiency)
        /// </summary>
        /// <param name="name">字段名 / field name</param>
        /// <returns>字段序号；不存在时返回 -1 / the ordinal, or -1 when absent</returns>
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
        /// 获取字段在当前结果集中的序号；字段不存在时会抛出异常
        /// Get the ordinal of a field in the current result set; throws when the field does not exist
        /// </summary>
        /// <param name="name">字段名 / field name</param>
        /// <returns>字段序号 / the ordinal</returns>
        public int GetOrdinal(string name)
        {
            return m_InnerReader!.GetOrdinal(name);
        }

        /// <summary>
        /// 按字段名获取字段值（注意：须确保字段存在于结果集中）
        /// Get a field value by name (notice: make sure the field exists in the result set)
        /// </summary>
        /// <param name="name">字段名 / field name</param>
        /// <returns>字段值；为 DBNull 时返回 null / the value, or null when DBNull</returns>
        public virtual object? Get(string name)
        {
            return Get(name, null);
        }

        /// <summary>
        /// 按序号获取字段值
        /// Get a field value by ordinal
        /// </summary>
        /// <param name="index">字段序号 / field ordinal</param>
        /// <returns>字段值；为 DBNull 时返回 null / the value, or null when DBNull</returns>
        public virtual object? Get(int index)
        {
            return Get(index, null);
        }

        /// <summary>
        /// 按字段名获取字段值（注意：须确保字段存在于结果集中）
        /// Get a field value by name (notice: make sure the field exists in the result set)
        /// </summary>
        /// <param name="name">字段名 / field name</param>
        /// <param name="defaultValue">为 DBNull 时返回的默认值 / value returned when DBNull</param>
        /// <returns>字段值 / the field value</returns>
        public virtual object? Get(string name, object? defaultValue)
        {
            int index = GetOrdinal(name);

            return Get(index, defaultValue);
        }

        /// <summary>
        /// 按序号获取字段值
        /// Get a field value by ordinal
        /// </summary>
        /// <param name="index">字段序号 / field ordinal</param>
        /// <param name="defaultValue">为 DBNull 时返回的默认值 / value returned when DBNull</param>
        /// <returns>字段值 / the field value</returns>
        public virtual object? Get(int index, object? defaultValue)
        {

            if (m_InnerReader!.IsDBNull(index) == false)
                return m_InnerReader[index];
            else
                return defaultValue;
        }

        /// <summary>
        /// 按字段名获取字段值并转换为目标类型（注意：须确保字段存在于结果集中）
        /// Get a field value by name converted to a target type (notice: make sure the field exists in the result set)
        /// </summary>
        /// <param name="name">字段名 / field name</param>
        /// <param name="defaultValue">为 DBNull 或转换失败时返回的默认值 / value returned when DBNull or conversion fails</param>
        /// <param name="resultType">目标类型（可为 Nullable 的基础类型）/ target type (may be the underlying type of a Nullable)</param>
        /// <returns>转换后的字段值 / the converted field value</returns>
        public virtual object? Get(string name, object? defaultValue, Type resultType)
        {
            int index = GetOrdinal(name);

            return Get(index, defaultValue, resultType);
        }

        /// <summary>
        /// 按序号获取字段值并转换为目标类型：
        /// Nullable 取基础类型；枚举按名称忽略大小写解析；Guid 解析失败返回默认值；
        /// 其余基元类型按 TypeCode 调用对应的强类型读取方法
        /// Get a field value by ordinal and convert it to a target type:
        /// Nullable uses its underlying type; enums parse by name case-insensitively; Guid falls back
        /// to the default on parse failure; other primitives dispatch by TypeCode to typed reader methods
        /// </summary>
        /// <param name="index">字段序号；小于 0 时直接返回默认值 / field ordinal; values below 0 return the default immediately</param>
        /// <param name="defaultValue">为 DBNull 或转换失败时返回的默认值 / value returned when DBNull or conversion fails</param>
        /// <param name="type">目标类型；null 视为 object / target type; null is treated as object</param>
        /// <returns>转换后的字段值 / the converted field value</returns>
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
                        //IDataReader 并无提供获取有符号 Byte 的方法，使用 GetInt16 代替 / IDataReader has no sbyte getter, read via GetInt16 instead
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
        /// 按字段名获取强类型字段值（注意：须确保字段存在于结果集中）
        /// Get a strongly typed field value by name (notice: make sure the field exists in the result set)
        /// </summary>
        /// <typeparam name="T">目标类型 / target type</typeparam>
        /// <param name="name">字段名 / field name</param>
        /// <returns>字段值；为 DBNull 时返回类型默认值 / the value, or the type default when DBNull</returns>
        public virtual T? Get<T>(string name)
        {
            return Get<T>(name, default(T));
        }

        /// <summary>
        /// 按序号获取强类型字段值
        /// Get a strongly typed field value by ordinal
        /// </summary>
        /// <typeparam name="T">目标类型 / target type</typeparam>
        /// <param name="index">字段序号 / field ordinal</param>
        /// <returns>字段值；为 DBNull 时返回类型默认值 / the value, or the type default when DBNull</returns>
        public virtual T? Get<T>(int index)
        {
            return Get<T>(index, default(T));
        }

        /// <summary>
        /// 按字段名获取强类型字段值（注意：须确保字段存在于结果集中）
        /// Get a strongly typed field value by name (notice: make sure the field exists in the result set)
        /// </summary>
        /// <typeparam name="T">目标类型 / target type</typeparam>
        /// <param name="name">字段名 / field name</param>
        /// <param name="defaultValue">为 DBNull 时返回的默认值 / value returned when DBNull</param>
        /// <returns>字段值 / the field value</returns>
        public virtual T? Get<T>(string name, T? defaultValue)
        {
            return (T?)Get(name, defaultValue, typeof(T));
        }

        /// <summary>
        /// 按序号获取强类型字段值
        /// Get a strongly typed field value by ordinal
        /// </summary>
        /// <typeparam name="T">目标类型 / target type</typeparam>
        /// <param name="index">字段序号 / field ordinal</param>
        /// <param name="defaultValue">为 DBNull 时返回的默认值 / value returned when DBNull</param>
        /// <returns>字段值 / the field value</returns>
        public virtual T? Get<T>(int index, T? defaultValue)
        {
            return (T?)Get(index, defaultValue, typeof(T));
        }

        /// <summary>
        /// 按字段名获取可空值类型字段值（注意：须确保字段存在于结果集中）
        /// Get a nullable value-type field value by name (notice: make sure the field exists in the result set)
        /// </summary>
        /// <typeparam name="T">基础值类型 / underlying value type</typeparam>
        /// <param name="name">字段名 / field name</param>
        /// <returns>可空字段值；为 DBNull 时为 null / the nullable value, or null when DBNull</returns>
        public virtual Nullable<T> GetNullable<T>(string name) where T : struct
        {
            return Get<Nullable<T>>(name);
        }

        /// <summary>
        /// 按序号获取可空值类型字段值
        /// Get a nullable value-type field value by ordinal
        /// </summary>
        /// <typeparam name="T">基础值类型 / underlying value type</typeparam>
        /// <param name="index">字段序号 / field ordinal</param>
        /// <returns>可空字段值；为 DBNull 时为 null / the nullable value, or null when DBNull</returns>
        public virtual Nullable<T> GetNullable<T>(int index) where T : struct
        {
            return Get<Nullable<T>>(index);
        }

        /// <summary>
        /// 将当前行装配为实体：结果集字段先按实体属性名、再按表字段名匹配映射，
        /// DBNull 单元格与未映射字段直接跳过
        /// Materialize the current row into an entity: result fields match mappings by property
        /// name first, then by column name; DBNull cells and unmapped fields are skipped
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <param name="mapping">表映射信息；为 null 时从 ModelSchemaStore 解析 / the table mapping, resolved from ModelSchemaStore when null</param>
        /// <returns>装配出的实体 / the materialized entity</returns>
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
        /// 将当前行装配为动态对象（ExpandoObject）；DBNull 单元格跳过
        /// Materialize the current row into a dynamic object (ExpandoObject); DBNull cells are skipped
        /// </summary>
        /// <returns>动态对象 / the dynamic object</returns>
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

        /// <summary>
        /// 读取当前结果集第一行指定列的单值，随后推进到下一个结果集
        /// Read the named column of the first row in the current result set, then advance to the next result set
        /// </summary>
        /// <param name="columnName">列名 / column name</param>
        /// <typeparam name="V">值类型 / value type</typeparam>
        /// <returns>单值；无行或为 DBNull 时返回类型默认值 / the value, or the type default when no row or DBNull</returns>
        public V? ExecuteScalar<V>(string columnName)
        {
            return ExecuteScalar<V>(columnName, default(V));
        }

        /// <summary>
        /// 读取当前结果集第一行指定列的单值，随后推进到下一个结果集
        /// Read the named column of the first row in the current result set, then advance to the next result set
        /// </summary>
        /// <param name="columnName">列名 / column name</param>
        /// <param name="defaultValue">无行或为 DBNull 时返回的默认值 / value returned when no row or DBNull</param>
        /// <typeparam name="V">值类型 / value type</typeparam>
        /// <returns>单值 / the single value</returns>
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

        /// <summary>
        /// 读取当前结果集的全部行并装配为实体列表，随后推进到下一个结果集
        /// Read all rows of the current result set into an entity list, then advance to the next result set
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <returns>实体列表 / the entity list</returns>
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

        /// <summary>
        /// 读取当前结果集的全部行并装配为动态对象列表，随后推进到下一个结果集
        /// Read all rows of the current result set into a list of dynamic objects, then advance to the next result set
        /// </summary>
        /// <returns>动态对象列表 / the dynamic list</returns>
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

        /// <summary>
        /// 读取当前结果集的第一行并装配为实体，随后推进到下一个结果集
        /// Read the first row of the current result set into an entity, then advance to the next result set
        /// </summary>
        /// <typeparam name="M">实体类型 / entity type</typeparam>
        /// <returns>实体；无数据时为 null / the entity, or null when no row</returns>
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

        /// <summary>
        /// 读取当前结果集的第一行并装配为动态对象，随后推进到下一个结果集
        /// Read the first row of the current result set into a dynamic object, then advance to the next result set
        /// </summary>
        /// <returns>动态对象；无数据时为 null / the dynamic object, or null when no row</returns>
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
        /// 关闭并释放内部 reader、绑定的命令与连接，最后触发 Dispose 回调（幂等，仅执行一次）
        /// Close and dispose the inner reader, the owned command and connection, then run dispose callbacks (idempotent, runs exactly once)
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
