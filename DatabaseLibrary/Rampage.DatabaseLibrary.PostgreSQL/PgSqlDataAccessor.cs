using Npgsql;
using Rampage.Data;
using Rampage.Data.Client;
using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.DatabaseLibrary.PostgreSQL
{
    /// <summary>PostgreSQL 原生 ADO.NET 数据访问器（Npgsql 驱动），实现 <see cref="IDataAccessor"/> / Native PostgreSQL ADO.NET data accessor (Npgsql driver); implements <see cref="IDataAccessor"/>.</summary>
    /// <remarks>
    /// 行为要点 / Behavior notes:
    /// 1. SQL 参数前缀为 "@"；参数对象支持 IDictionary&lt;string,object&gt; / IDictionary&lt;string,dynamic&gt; / 匿名对象 / ExpandoObject / 普通 POCO（后三者按属性反射组装，null 属性写入 DBNull）。
    ///    SQL parameter prefix is "@"; the parameter object may be a dictionary, anonymous type, ExpandoObject or plain POCO (the latter three are assembled from properties via reflection; null properties become DBNull).
    /// 2. DateTime 属性值被钳制到 [0001-01-01, 9999-12-31 23:59:59]（.NET DateTime 有效域；PG timestamp 实际支持到公元前 4713 年，见 <see cref="GetParameter"/>）。
    ///    DateTime property values are clamped to [0001-01-01, 9999-12-31 23:59:59] (the .NET DateTime domain; PG timestamp actually reaches 4713 BC, see <see cref="GetParameter"/>).
    /// 3. 连接未打开时各方法会先自动打开连接 / every method opens the connection first when it is not open.
    /// 4. ExecuteReader / ExecuteGridReader 返回的包装器持有命令对象，释放包装器会连带释放 reader 与命令。
    ///    The wraps returned by ExecuteReader / ExecuteGridReader own the command; disposing them disposes the reader and the command too.
    /// </remarks>
    public class PgSqlDataAccessor : IDataAccessor
    {

        /// <summary>确保连接已打开（未打开则异步打开）/ Ensures the connection is open (opens it asynchronously when closed).</summary>
        private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>在已打开的连接上创建 NpgsqlCommand 并按参数对象类型组装参数 / Creates an NpgsqlCommand on an open connection and assembles parameters according to the parameter object's type.</summary>
        /// <param name="connection">已打开的连接 / the open connection.</param>
        /// <param name="sql">SQL 语句 / the SQL text.</param>
        /// <param name="param">参数对象 / the parameter object.</param>
        /// <param name="transaction">所属事务 / the owning transaction.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>组装好的命令 / the assembled command.</returns>
        private NpgsqlCommand BuildCommand(NpgsqlConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = sql;
            if (transaction != null)
            {
                command.Transaction = (NpgsqlTransaction)transaction;
            }
            if (commandType != null)
            {
                command.CommandType = commandType.Value;
            }
            if (commandTimeout != null)
            {
                command.CommandTimeout = commandTimeout.Value;
            }

            if (param != null)
            {
                Type paramType = param.GetType();

                // 字典：键即参数名，null 值转 DBNull / dictionary: keys become parameter names, null values become DBNull
                if (param is IDictionary<string, object> dictionary)
                {
                    foreach (var kvp in dictionary)
                    {
                        var parameter = new NpgsqlParameter("@" + kvp.Key, kvp.Value ?? DBNull.Value);
                        command.Parameters.Add(parameter);
                    }
                }
                // dynamic 值字典：同上 / dictionary with dynamic values: same as above
                else if (param is IDictionary<string, dynamic> dynamicDictionary)
                {
                    foreach (var kvp in dynamicDictionary)
                    {
                        var parameter = new NpgsqlParameter("@" + kvp.Key, kvp.Value ?? DBNull.Value);
                        command.Parameters.Add(parameter);
                    }
                }
                // 匿名对象 / ExpandoObject：按属性组装 / anonymous type / ExpandoObject: assembled from properties
                else if (GenericTool.IsAnonymousType(paramType) || param is System.Dynamic.ExpandoObject)
                {
                    var properties = ReflectionTool.GetPropertyList(paramType);

                    foreach (var property in properties)
                    {
                        var parameter = GetParameter(param, property);
                        command.Parameters.Add(parameter);
                    }
                }
                // 普通 POCO：同样按属性组装（与匿名对象分支等价）/ plain POCO: also assembled from properties (equivalent to the anonymous branch)
                else
                {
                    var properties = ReflectionTool.GetPropertyList(paramType);

                    foreach (var property in properties)
                    {
                        var parameter = GetParameter(param, property);
                        command.Parameters.Add(parameter);
                    }
                }
            }

            return command;
        }

        /// <summary>同步创建命令：转换连接、必要时打开连接，再组装命令 / Sync command factory: casts the connection, opens it when needed, then assembles the command.</summary>
        /// <param name="conn">必须为 NpgsqlConnection / must be an NpgsqlConnection.</param>
        /// <returns>组装好的命令 / the assembled command.</returns>
        private NpgsqlCommand CreateCommand(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            NpgsqlConnection connection = (NpgsqlConnection)conn;
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            return BuildCommand(connection, sql, param, transaction, commandType, commandTimeout);
        }

        /// <summary>异步创建命令：转换连接、异步打开连接，再组装命令 / Async command factory: casts the connection, opens it asynchronously when needed, then assembles the command.</summary>
        /// <param name="conn">必须为 NpgsqlConnection / must be an NpgsqlConnection.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>组装好的命令 / the assembled command.</returns>
        private async Task<NpgsqlCommand> CreateCommandAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            NpgsqlConnection connection = (NpgsqlConnection)conn;
            await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            return BuildCommand(connection, sql, param, transaction, commandType, commandTimeout);
        }

        /// <summary>把读取器的全部结果集装入 DataSet（表名 Table1..N，不启用约束检查）/ Loads every result set of the reader into a DataSet (tables named Table1..N, constraints not enforced).</summary>
        /// <param name="reader">数据读取器 / the data reader.</param>
        /// <returns>含全部结果集的 DataSet / a DataSet containing all result sets.</returns>
        private static DataSet LoadDataSet(DbDataReader reader)
        {
            DataSet ds = new DataSet();
            int i = 0;
            while (!reader.IsClosed)
            {
                DataTable table = new DataTable("Table" + (i + 1));
                ds.Tables.Add(table);
                ds.EnforceConstraints = false;
                table.Load(reader);
                i++;
            }
            return ds;
        }

        /// <summary>按属性生成 NpgsqlParameter：属性名即参数名（前缀 "@"），null 写 DBNull，DateTime 做范围钳制 / Builds an NpgsqlParameter from a property: the property name is the parameter name (prefixed "@"), null becomes DBNull, DateTime is range-clamped.</summary>
        /// <param name="param">参数对象 / the parameter object.</param>
        /// <param name="property">属性信息 / the property info.</param>
        /// <returns>生成好的参数 / the built parameter.</returns>
        private NpgsqlParameter GetParameter(object? param, PropertyInfo property)
        {
            if (param == null)
            {
                return new NpgsqlParameter("@" + property.Name, DBNull.Value);
            }

            var value = EmitTool.GetPropertyValue(param, property.Name);
            if (value == null)
            {
                return new NpgsqlParameter("@" + property.Name, DBNull.Value);
            }
            TypeCode prpTypeCode = Type.GetTypeCode(property.PropertyType);
            object resovleValue;
            switch (prpTypeCode)
            {
                case TypeCode.DateTime:
                    // 钳制到 .NET DateTime 有效域 [0001, 9999]（防御性，Npgsql 本身支持更宽的范围）/ clamped to the .NET DateTime domain [0001, 9999] (defensive; Npgsql itself supports a wider range)
                    DateTime minTime = new DateTime(1, 1, 1);
                    DateTime maxTime = new DateTime(9999, 12, 31, 23, 59, 59);
                    DateTime dt = Convert.ToDateTime(value);
                    if (dt < minTime)
                    {
                        resovleValue = minTime;
                    }
                    else if (dt > maxTime)
                    {
                        resovleValue = maxTime;
                    }
                    else
                    {
                        resovleValue = value;
                    }
                    break;
                default:
                    resovleValue = value;
                    break;
            }
            var parameter = new NpgsqlParameter("@" + property.Name, resovleValue);
            return parameter;
        }

        /// <summary>执行非查询语句，返回受影响行数 / Executes a non-query statement and returns the affected row count.</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection；未打开时自动打开 / the connection; must be an NpgsqlConnection and is opened automatically when closed.</param>
        /// <param name="param">SQL 参数：匿名对象 / POCO（按属性反射组装）、字典或 ExpandoObject；null 表示无参数 / SQL parameters: anonymous type / POCO (assembled from properties), dictionary or ExpandoObject; null for none.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型；null 时使用驱动默认（Text）/ the command type; null uses the provider default (Text).</param>
        /// <param name="commandTimeout">命令超时（秒）；null 使用驱动默认 / the command timeout in seconds; null uses the provider default.</param>
        /// <returns>受影响的行数 / the number of affected rows.</returns>
        public int Execute(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (NpgsqlCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>异步执行非查询语句，返回受影响行数 / Asynchronously executes a non-query statement and returns the affected row count.</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection；未打开时自动打开 / the connection; must be an NpgsqlConnection and is opened automatically when closed.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>受影响的行数 / the number of affected rows.</returns>
        public async Task<int> ExecuteAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (NpgsqlCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>用 NpgsqlDataAdapter 填充 DataSet（多结果集时由 Fill 依次生成 Table、Table1... 表）/ Fills a DataSet via NpgsqlDataAdapter (multiple result sets become Table, Table1... via Fill).</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>填充后的 DataSet / the filled DataSet.</returns>
        public DataSet ExecuteDataSet(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            DataSet ds = new DataSet();
            using (NpgsqlCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                IDataAdapter adapter = new NpgsqlDataAdapter(command);
                adapter.Fill(ds);
            }
            return ds;
        }

        /// <summary>异步执行查询并把全部结果集装入 DataSet（表名 Table1..N，不走 DataAdapter）/ Asynchronously executes the query and loads all result sets into a DataSet (tables named Table1..N, without a DataAdapter).</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>含全部结果集的 DataSet / a DataSet containing all result sets.</returns>
        public async Task<DataSet> ExecuteDataSetAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (NpgsqlCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    return LoadDataSet(reader);
                }
            }
        }

        /// <summary>执行多结果集查询，返回多结果集读取器包装器（包装器持有命令，释放时连带释放命令）/ Executes a multi-result-set query returning a grid-reader wrap (the wrap owns the command and disposes it).</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>多结果集读取器包装器 / the grid-reader wrap.</returns>
        public GridReaderWrap ExecuteGridReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            NpgsqlCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout);
            var reader = command.ExecuteReader();
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return new GridReaderWrap(wrap);
        }

        /// <summary>异步执行多结果集查询，返回多结果集读取器包装器（包装器持有命令，释放时连带释放命令）/ Asynchronously executes a multi-result-set query returning a grid-reader wrap (the wrap owns the command and disposes it).</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>多结果集读取器包装器 / the grid-reader wrap.</returns>
        public async Task<GridReaderWrap> ExecuteGridReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            NpgsqlCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return new GridReaderWrap(wrap);
        }

        /// <summary>执行查询，返回 DataReader 包装器（包装器持有命令，释放时连带释放命令与 reader）/ Executes the query returning a DataReader wrap (the wrap owns the command; disposing it releases the reader and command).</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>DataReader 包装器 / the DataReader wrap.</returns>
        public DataReaderWrap ExecuteReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            NpgsqlCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout);
            var reader = command.ExecuteReader();
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return wrap;
        }

        /// <summary>异步执行查询，返回 DataReader 包装器（包装器持有命令，释放时连带释放命令与 reader）/ Asynchronously executes the query returning a DataReader wrap (the wrap owns the command; disposing it releases the reader and command).</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>DataReader 包装器 / the DataReader wrap.</returns>
        public async Task<DataReaderWrap> ExecuteReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            NpgsqlCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return wrap;
        }

        /// <summary>执行查询，返回首行首列的值 / Executes the query returning the first column of the first row.</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>首行首列值；无结果时为 null / the first column of the first row; null when there is no result.</returns>
        public object? ExecuteScalar(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (NpgsqlCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return command.ExecuteScalar();
            }
        }

        /// <summary>异步执行查询，返回首行首列的值 / Asynchronously executes the query returning the first column of the first row.</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>首行首列值；无结果时为 null / the first column of the first row; null when there is no result.</returns>
        public async Task<object?> ExecuteScalarAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (NpgsqlCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>执行查询并把首行首列值转换为 <typeparamref name="T"/>（经 GenericTool.ConvertScalar）/ Executes the query converting the first column of the first row to <typeparamref name="T"/> (via GenericTool.ConvertScalar).</summary>
        /// <typeparam name="T">目标类型 / the target type.</typeparam>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>转换后的标量值；结果为 null 时返回 default(T) / the converted scalar; default(T) when the result is null.</returns>
        public T? ExecuteScalar<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            object? obj = null;
            using (NpgsqlCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                obj = command.ExecuteScalar();
            }
            if (obj == null)
            {
                return default(T);
            }
            return GenericTool.ConvertScalar<T>(obj);
        }

        /// <summary>异步执行查询并把首行首列值转换为 <typeparamref name="T"/> / Asynchronously executes the query converting the first column of the first row to <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">目标类型 / the target type.</typeparam>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>转换后的标量值；结果为 null 时返回 default / the converted scalar; default when the result is null.</returns>
        public async Task<T?> ExecuteScalarAsync<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            object? obj = await ExecuteScalarAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            if (obj == null)
            {
                return default(T);
            }
            return GenericTool.ConvertScalar<T>(obj);
        }

        /// <summary>查询全部行，返回 dynamic 列表 / Queries all rows returning a dynamic list.</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>行列表；无行为空列表 / the row list; an empty list when there are no rows.</returns>
        public List<dynamic> SelectList(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (DataReaderWrap xReader = ExecuteReader(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return xReader.SelectList();
            }
        }

        /// <summary>异步查询全部行，返回 dynamic 列表 / Asynchronously queries all rows returning a dynamic list.</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>行列表；无行为空列表 / the row list; an empty list when there are no rows.</returns>
        public async Task<List<dynamic>> SelectListAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (DataReaderWrap xReader = await ExecuteReaderAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return xReader.SelectList();
            }
        }

        /// <summary>查询全部行并映射为 <typeparamref name="M"/> 列表 / Queries all rows mapping them to a list of <typeparamref name="M"/>.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>映射结果列表 / the mapped list.</returns>
        public List<M> SelectList<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class
        {
            using (DataReaderWrap xReader = ExecuteReader(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return xReader.SelectList<M>();
            }
        }

        /// <summary>异步查询全部行并映射为 <typeparamref name="M"/> 列表 / Asynchronously queries all rows mapping them to a list of <typeparamref name="M"/>.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>映射结果列表 / the mapped list.</returns>
        public async Task<List<M>> SelectListAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            using (DataReaderWrap xReader = await ExecuteReaderAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return xReader.SelectList<M>();
            }
        }

        /// <summary>查询首行，无行时返回 null / Queries the first row; null when there is no row.</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>首行动态行对象；无行时为 null / the first row as a dynamic object; null when there is no row.</returns>
        public dynamic? SelectOne(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (DataReaderWrap xReader = ExecuteReader(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return xReader.SelectOne();
            }
        }

        /// <summary>异步查询首行，无行时返回 null / Asynchronously queries the first row; null when there is no row.</summary>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>首行动态行对象；无行时为 null / the first row as a dynamic object; null when there is no row.</returns>
        public async Task<dynamic?> SelectOneAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (DataReaderWrap xReader = await ExecuteReaderAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return xReader.SelectOne();
            }
        }

        /// <summary>查询首行并映射为 <typeparamref name="M"/>，无行时返回 null / Queries the first row mapping it to <typeparamref name="M"/>; null when there is no row.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>映射后的首行；无行时为 null / the mapped first row; null when there is no row.</returns>
        public M? SelectOne<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class
        {
            using (DataReaderWrap xReader = ExecuteReader(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return xReader.SelectOne<M>();
            }
        }

        /// <summary>异步查询首行并映射为 <typeparamref name="M"/>，无行时返回 null / Asynchronously queries the first row mapping it to <typeparamref name="M"/>; null when there is no row.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <param name="conn">数据库连接，必须为 NpgsqlConnection / the connection; must be an NpgsqlConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>映射后的首行；无行时为 null / the mapped first row; null when there is no row.</returns>
        public async Task<M?> SelectOneAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            using (DataReaderWrap xReader = await ExecuteReaderAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return xReader.SelectOne<M>();
            }
        }
    }
}
