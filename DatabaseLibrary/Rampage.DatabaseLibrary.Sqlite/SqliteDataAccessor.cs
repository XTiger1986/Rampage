using Microsoft.Data.Sqlite;
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

namespace Rampage.DatabaseLibrary.Sqlite
{
    /// <summary>SQLite 原生 ADO.NET 数据访问器（Microsoft.Data.Sqlite 驱动），实现 <see cref="IDataAccessor"/> / Native SQLite ADO.NET data accessor (Microsoft.Data.Sqlite driver); implements <see cref="IDataAccessor"/>.</summary>
    /// <remarks>
    /// 行为要点（与其他数据库实现存在差异）/ Behavior notes (differences from the other database implementations):
    /// 1. SQL 参数前缀为 "@"；参数对象支持 IDictionary&lt;string,object&gt; / IDictionary&lt;string,dynamic&gt; / 匿名对象 / ExpandoObject / 普通 POCO（后三者按属性反射组装，null 属性写入 DBNull）。
    ///    SQL parameter prefix is "@"; the parameter object may be a dictionary, anonymous type, ExpandoObject or plain POCO (the latter three are assembled from properties via reflection; null properties become DBNull).
    /// 2. 不做 DateTime 钳制（<see cref="GetParameter"/> 仅透传原值；代码中留有 TODO）/ no DateTime clamping (<see cref="GetParameter"/> passes values through; a TODO remains in code).
    /// 3. ExecuteDataSet 同步/异步均逐个装载全部结果集（无 Table 命名）。
    ///    ExecuteDataSet (sync and async) loads every result set in turn (unnamed tables).
    /// 4. ExecuteScalar&lt;T&gt; 结果为 null / DBNull 时返回 default，其余经 ConvertScalar 转换 / ExecuteScalar&lt;T&gt; yields default for null / DBNull results and converts others via ConvertScalar.
    /// 5. 连接未打开时各方法会先自动打开连接 / every method opens the connection first when it is not open.
    /// 6. ExecuteReader / ExecuteGridReader 返回的包装器持有命令对象，释放包装器会连带释放 reader 与命令。
    ///    The wraps returned by ExecuteReader / ExecuteGridReader own the command; disposing them disposes the reader and the command too.
    /// </remarks>
    public class SqliteDataAccessor : IDataAccessor
    {

        /// <summary>确保连接已打开（未打开则异步打开）/ Ensures the connection is open (opens it asynchronously when closed).</summary>
        private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>在已打开的连接上创建 SqliteCommand 并按参数对象类型组装参数 / Creates a SqliteCommand on an open connection and assembles parameters according to the parameter object's type.</summary>
        /// <param name="connection">已打开的连接 / the open connection.</param>
        /// <param name="sql">SQL 语句 / the SQL text.</param>
        /// <param name="param">参数对象 / the parameter object.</param>
        /// <param name="transaction">所属事务 / the owning transaction.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>组装好的命令 / the assembled command.</returns>
        private SqliteCommand BuildCommand(SqliteConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;
            if (transaction != null)
            {
                command.Transaction = (SqliteTransaction)transaction;
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
                        var parameter = new SqliteParameter("@" + kvp.Key, kvp.Value ?? DBNull.Value);
                        command.Parameters.Add(parameter);
                    }
                }
                // dynamic 值字典：同上 / dictionary with dynamic values: same as above
                else if (param is IDictionary<string, dynamic> dynamicDictionary)
                {
                    foreach (var kvp in dynamicDictionary)
                    {
                        var parameter = new SqliteParameter("@" + kvp.Key, kvp.Value ?? DBNull.Value);
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
        /// <param name="conn">必须为 SqliteConnection / must be a SqliteConnection.</param>
        /// <returns>组装好的命令 / the assembled command.</returns>
        private SqliteCommand CreateCommand(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            SqliteConnection connection = (SqliteConnection)conn;
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            return BuildCommand(connection, sql, param, transaction, commandType, commandTimeout);
        }

        /// <summary>异步创建命令：转换连接、异步打开连接，再组装命令 / Async command factory: casts the connection, opens it asynchronously when needed, then assembles the command.</summary>
        /// <param name="conn">必须为 SqliteConnection / must be a SqliteConnection.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>组装好的命令 / the assembled command.</returns>
        private async Task<SqliteCommand> CreateCommandAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            SqliteConnection connection = (SqliteConnection)conn;
            await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            return BuildCommand(connection, sql, param, transaction, commandType, commandTimeout);
        }

        /// <summary>按属性生成 SqliteParameter：属性名即参数名（前缀 "@"），null 写 DBNull；不做 DateTime 钳制 / Builds a SqliteParameter from a property: the property name is the parameter name (prefixed "@"), null becomes DBNull; DateTime is not clamped.</summary>
        /// <param name="param">参数对象 / the parameter object.</param>
        /// <param name="property">属性信息 / the property info.</param>
        /// <returns>生成好的参数 / the built parameter.</returns>
        private SqliteParameter GetParameter(object? param, PropertyInfo property)
        {
            if (param == null)
            {
                return new SqliteParameter("@" + property.Name, DBNull.Value);
            }

            var value = EmitTool.GetPropertyValue(param, property.Name);
            if (value == null)
            {
                return new SqliteParameter("@" + property.Name, DBNull.Value);
            }
            TypeCode prpTypeCode = Type.GetTypeCode(property.PropertyType);
            object resovleValue;
            switch (prpTypeCode)
            {
                // TODO
                default:
                    resovleValue = value;
                    break;
            }
            var parameter = new SqliteParameter("@" + property.Name, resovleValue);
            return parameter;
        }

        /// <summary>执行非查询语句，返回受影响行数 / Executes a non-query statement and returns the affected row count.</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection；未打开时自动打开 / the connection; must be a SqliteConnection and is opened automatically when closed.</param>
        /// <param name="param">SQL 参数：匿名对象 / POCO（按属性反射组装）、字典或 ExpandoObject；null 表示无参数 / SQL parameters: anonymous type / POCO (assembled from properties), dictionary or ExpandoObject; null for none.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型；null 时使用驱动默认（Text）/ the command type; null uses the provider default (Text).</param>
        /// <param name="commandTimeout">命令超时（秒）；null 使用驱动默认 / the command timeout in seconds; null uses the provider default.</param>
        /// <returns>受影响的行数 / the number of affected rows.</returns>
        public int Execute(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (SqliteCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>异步执行非查询语句，返回受影响行数 / Asynchronously executes a non-query statement and returns the affected row count.</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection；未打开时自动打开 / the connection; must be a SqliteConnection and is opened automatically when closed.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>受影响的行数 / the number of affected rows.</returns>
        public async Task<int> ExecuteAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (SqliteCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>执行查询并装入 DataSet；与其他实现不同，此处只装载第一个结果集（单表、无表名）/ Executes the query into a DataSet; unlike the other implementations, only the first result set is loaded (a single unnamed table).</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>装载了全部结果集的 DataSet / a DataSet holding every result set.</returns>
        public DataSet ExecuteDataSet(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            var ds = new DataSet();

            using (SqliteCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    // 逐个装载全部结果集 / load every result set in turn
                    do
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        ds.Tables.Add(dt);
                    } while (reader.NextResult());
                }
            }

            return ds;
        }

        /// <summary>异步执行查询并装入 DataSet（逐个装载全部结果集，无表名）/ Asynchronously executes the query into a DataSet (every result set loaded in turn, unnamed tables).</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>装载了全部结果集的 DataSet / a DataSet holding every result set.</returns>
        public async Task<DataSet> ExecuteDataSetAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var ds = new DataSet();

            using (SqliteCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    // 逐个装载全部结果集 / load every result set in turn
                    do
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        ds.Tables.Add(dt);
                    } while (reader.NextResult());
                }
            }

            return ds;
        }

        /// <summary>执行多结果集查询，返回多结果集读取器包装器（包装器持有命令，释放时连带释放命令）/ Executes a multi-result-set query returning a grid-reader wrap (the wrap owns the command and disposes it).</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>多结果集读取器包装器 / the grid-reader wrap.</returns>
        public GridReaderWrap ExecuteGridReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            SqliteCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout);
            var reader = command.ExecuteReader();
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return new GridReaderWrap(wrap);
        }

        /// <summary>异步执行多结果集查询，返回多结果集读取器包装器（包装器持有命令，释放时连带释放命令）/ Asynchronously executes a multi-result-set query returning a grid-reader wrap (the wrap owns the command and disposes it).</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>多结果集读取器包装器 / the grid-reader wrap.</returns>
        public async Task<GridReaderWrap> ExecuteGridReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            SqliteCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return new GridReaderWrap(wrap);
        }

        /// <summary>执行查询，返回 DataReader 包装器（包装器持有命令，释放时连带释放命令与 reader）/ Executes the query returning a DataReader wrap (the wrap owns the command; disposing it releases the reader and command).</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>DataReader 包装器 / the DataReader wrap.</returns>
        public DataReaderWrap ExecuteReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            SqliteCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout);
            var reader = command.ExecuteReader();
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return wrap;
        }

        /// <summary>异步执行查询，返回 DataReader 包装器（包装器持有命令，释放时连带释放命令与 reader）/ Asynchronously executes the query returning a DataReader wrap (the wrap owns the command; disposing it releases the reader and command).</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>DataReader 包装器 / the DataReader wrap.</returns>
        public async Task<DataReaderWrap> ExecuteReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            SqliteCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return wrap;
        }

        /// <summary>执行查询，返回首行首列的值 / Executes the query returning the first column of the first row.</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>首行首列值；无结果时为 null / the first column of the first row; null when there is no result.</returns>
        public object? ExecuteScalar(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (SqliteCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return command.ExecuteScalar();
            }
        }

        /// <summary>异步执行查询，返回首行首列的值 / Asynchronously executes the query returning the first column of the first row.</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>首行首列值；无结果时为 null / the first column of the first row; null when there is no result.</returns>
        public async Task<object?> ExecuteScalarAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (SqliteCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>执行查询并把首行首列值直接转换为 <typeparamref name="T"/>（(T)obj 强转，类型不匹配抛 InvalidCastException；DBNull 不做特殊处理）/ Executes the query casting the first column of the first row directly to <typeparamref name="T"/> ((T)obj; throws InvalidCastException on mismatch; DBNull is not special-cased).</summary>
        /// <typeparam name="T">目标类型 / the target type.</typeparam>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>转换后的标量值；结果为 null 时返回 default(T) / the cast scalar; default(T) when the result is null.</returns>
        public T? ExecuteScalar<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            object? obj;
            using (SqliteCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                obj = command.ExecuteScalar();
            }
            if (obj == null || obj == DBNull.Value)
            {
                return default(T);
            }
            return GenericTool.ConvertScalar<T>(obj);
        }

        /// <summary>异步执行查询并把首行首列值直接转换为 <typeparamref name="T"/>（(T)obj 强转，类型不匹配抛 InvalidCastException）/ Asynchronously executes the query casting the first column of the first row directly to <typeparamref name="T"/> ((T)obj; throws InvalidCastException on mismatch).</summary>
        /// <typeparam name="T">目标类型 / the target type.</typeparam>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>转换后的标量值；结果为 null 时返回 default / the cast scalar; default when the result is null.</returns>
        public async Task<T?> ExecuteScalarAsync<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            object? obj = await ExecuteScalarAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            if (obj == null || obj == DBNull.Value)
            {
                return default(T);
            }
            return GenericTool.ConvertScalar<T>(obj);
        }

        /// <summary>查询全部行，返回 dynamic 列表 / Queries all rows returning a dynamic list.</summary>
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 SqliteConnection / the connection; must be a SqliteConnection.</param>
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
