using Oracle.ManagedDataAccess.Client;
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

namespace Rampage.DatabaseLibrary.Oracle
{
    /// <summary>Oracle 原生 ADO.NET 数据访问器（ODP.NET 托管驱动），实现 <see cref="IDataAccessor"/> / Native Oracle ADO.NET data accessor (managed ODP.NET driver); implements <see cref="IDataAccessor"/>.</summary>
    /// <remarks>
    /// 行为要点 / Behavior notes:
    /// 1. SQL 参数前缀为 ":"（其余数据库均为 "@"）；参数对象支持 IDictionary&lt;string,object&gt; / IDictionary&lt;string,dynamic&gt; / 匿名对象 / ExpandoObject / 普通 POCO（后三者按属性反射组装，null 属性写入 DBNull）。
    ///    The SQL parameter prefix is ":" (all other databases use "@"); the parameter object may be a dictionary, anonymous type, ExpandoObject or plain POCO (the latter three are assembled from properties via reflection; null properties become DBNull).
    /// 2. RETURNING 子句支持：SQL 同时包含 "RETURNING" 与 ":RETURNING_ID"（均不区分大小写）时，Execute / ExecuteScalar 会自动追加
    ///    名为 RETURNING_ID 的 Decimal OUTPUT 参数，ExecuteScalar 改走 ExecuteNonQuery 并返回该参数值，用于取回自增 / 序列生成的主键。
    ///    RETURNING support: when the SQL contains both "RETURNING" and ":RETURNING_ID" (case-insensitive), Execute / ExecuteScalar
    ///    automatically append a Decimal OUTPUT parameter named RETURNING_ID, and ExecuteScalar switches to ExecuteNonQuery returning
    ///    that parameter's value — used to fetch generated keys.
    /// 3. DateTime 属性值被钳制到 [0001-01-01, 9999-12-31 23:59:59]——Oracle DATE 下限为公元 1 年 1 月 1 日（见 <see cref="GetParameter"/>）。
    ///    DateTime property values are clamped to [0001-01-01, 9999-12-31 23:59:59] because Oracle DATE starts at year 1 AD (see <see cref="GetParameter"/>).
    /// 4. 连接未打开时各方法会先自动打开连接 / every method opens the connection first when it is not open.
    /// 5. ExecuteReader / ExecuteGridReader 返回的包装器持有命令对象，释放包装器会连带释放 reader 与命令。
    ///    The wraps returned by ExecuteReader / ExecuteGridReader own the command; disposing them disposes the reader and the command too.
    /// </remarks>
    public class OracleDataAccessor : IDataAccessor
    {

        /// <summary>RETURNING 子句输出参数名（SQL 中以 :RETURNING_ID 占位）/ the RETURNING-clause output parameter name (referenced as :RETURNING_ID in SQL).</summary>
        private const string ReturningIdParameterName = "RETURNING_ID";

        /// <summary>确保连接已打开（未打开则异步打开）/ Ensures the connection is open (opens it asynchronously when closed).</summary>
        private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>在已打开的连接上创建 OracleCommand 并按参数对象类型组装参数（前缀 ":"）/ Creates an OracleCommand on an open connection and assembles parameters according to the parameter object's type (prefix ":").</summary>
        /// <param name="connection">已打开的连接 / the open connection.</param>
        /// <param name="sql">SQL 语句 / the SQL text.</param>
        /// <param name="param">参数对象 / the parameter object.</param>
        /// <param name="transaction">所属事务 / the owning transaction.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>组装好的命令 / the assembled command.</returns>
        private OracleCommand BuildCommand(OracleConnection connection, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            OracleCommand command = connection.CreateCommand();
            command.CommandText = sql;
            if (transaction != null)
            {
                command.Transaction = (OracleTransaction)transaction;
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

                // 字典：键即参数名（Oracle 前缀 ":"），null 值转 DBNull / dictionary: keys become parameter names (Oracle prefix ":"), null values become DBNull
                if (param is IDictionary<string, object> dictionary)
                {
                    foreach (var kvp in dictionary)
                    {
                        var parameter = new OracleParameter(":" + kvp.Key, kvp.Value ?? DBNull.Value);
                        command.Parameters.Add(parameter);
                    }
                }
                // dynamic 值字典：同上 / dictionary with dynamic values: same as above
                else if (param is IDictionary<string, dynamic> dynamicDictionary)
                {
                    foreach (var kvp in dynamicDictionary)
                    {
                        var parameter = new OracleParameter(":" + kvp.Key, kvp.Value ?? DBNull.Value);
                        command.Parameters.Add(parameter);
                    }
                }
                // 匿名对象 / ExpandoObject / 普通 POCO：按属性组装（两个分支等价）/ anonymous type / ExpandoObject / plain POCO: assembled from properties (both branches are equivalent)
                else if (GenericTool.IsAnonymousType(paramType) || param is System.Dynamic.ExpandoObject)
                {
                    var properties = ReflectionTool.GetPropertyList(paramType);

                    foreach (var property in properties)
                    {
                        var parameter = GetParameter(param, property);
                        command.Parameters.Add(parameter);
                    }
                }
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
        /// <param name="conn">必须为 OracleConnection / must be an OracleConnection.</param>
        /// <returns>组装好的命令 / the assembled command.</returns>
        private OracleCommand CreateCommand(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            OracleConnection connection = (OracleConnection)conn;
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            return BuildCommand(connection, sql, param, transaction, commandType, commandTimeout);
        }

        /// <summary>异步创建命令：转换连接、异步打开连接，再组装命令 / Async command factory: casts the connection, opens it asynchronously when needed, then assembles the command.</summary>
        /// <param name="conn">必须为 OracleConnection / must be an OracleConnection.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>组装好的命令 / the assembled command.</returns>
        private async Task<OracleCommand> CreateCommandAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            OracleConnection connection = (OracleConnection)conn;
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

        /// <summary>检测 SQL 是否需要 RETURNING_ID 输出参数：同时包含 "RETURNING" 与 ":RETURNING_ID"（均不区分大小写）/ Detects whether the SQL needs the RETURNING_ID output parameter: it must contain both "RETURNING" and ":RETURNING_ID" (case-insensitive).</summary>
        /// <param name="sql">SQL 语句 / the SQL text.</param>
        /// <returns>需要时返回 true / true when the parameter is required.</returns>
        private static bool RequiresReturningIdParameter(string sql)
        {
            return sql.Contains("RETURNING", StringComparison.OrdinalIgnoreCase)
                && sql.Contains($":{ReturningIdParameterName}", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>为命令追加 RETURNING_ID 的 Decimal OUTPUT 参数（同名参数已存在则跳过）/ Appends the Decimal OUTPUT parameter RETURNING_ID to the command (skipped when one already exists).</summary>
        /// <param name="command">目标命令 / the target command.</param>
        private static void AddReturningIdParameter(OracleCommand command)
        {
            if (command.Parameters.Contains(ReturningIdParameterName))
            {
                return;
            }

            var returnParam = new OracleParameter(ReturningIdParameterName, OracleDbType.Decimal, ParameterDirection.Output);
            command.Parameters.Add(returnParam);
        }

        /// <summary>按属性生成 OracleParameter：属性名即参数名（前缀 ":"），null 写 DBNull，DateTime 做范围钳制 / Builds an OracleParameter from a property: the property name is the parameter name (prefixed ":"), null becomes DBNull, DateTime is range-clamped.</summary>
        /// <param name="param">参数对象 / the parameter object.</param>
        /// <param name="property">属性信息 / the property info.</param>
        /// <returns>生成好的参数 / the built parameter.</returns>
        private OracleParameter GetParameter(object? param, PropertyInfo property)
        {
            if (param == null)
            {
                return new OracleParameter(":" + property.Name, DBNull.Value);
            }

            var value = EmitTool.GetPropertyValue(param, property.Name);
            if (value == null)
            {
                return new OracleParameter(":" + property.Name, DBNull.Value);
            }
            TypeCode prpTypeCode = Type.GetTypeCode(property.PropertyType);
            object resovleValue;
            switch (prpTypeCode)
            {
                case TypeCode.DateTime:
                    // Oracle DATE 下限为公元 1 年 1 月 1 日，越界值钳制到有效区间 / Oracle DATE starts at year 1 AD; out-of-range values are clamped into the valid range
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
            var parameter = new OracleParameter(":" + property.Name, resovleValue);
            return parameter;
        }

        /// <summary>执行非查询语句，返回受影响行数；检测到 RETURNING :RETURNING_ID 时自动追加输出参数 / Executes a non-query statement and returns the affected row count; appends the output parameter automatically when RETURNING :RETURNING_ID is detected.</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection；未打开时自动打开 / the connection; must be an OracleConnection and is opened automatically when closed.</param>
        /// <param name="param">SQL 参数（前缀 ":"）：匿名对象 / POCO（按属性反射组装）、字典或 ExpandoObject；null 表示无参数 / SQL parameters (prefix ":"): anonymous type / POCO (assembled from properties), dictionary or ExpandoObject; null for none.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型；null 时使用驱动默认（Text）/ the command type; null uses the provider default (Text).</param>
        /// <param name="commandTimeout">命令超时（秒）；null 使用驱动默认 / the command timeout in seconds; null uses the provider default.</param>
        /// <returns>受影响的行数 / the number of affected rows.</returns>
        public int Execute(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (OracleCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                if (RequiresReturningIdParameter(sql))
                {
                    AddReturningIdParameter(command);
                }
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>异步执行非查询语句；检测到 RETURNING :RETURNING_ID 时自动追加输出参数 / Asynchronously executes a non-query statement; appends the output parameter automatically when RETURNING :RETURNING_ID is detected.</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection；未打开时自动打开 / the connection; must be an OracleConnection and is opened automatically when closed.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>受影响的行数 / the number of affected rows.</returns>
        public async Task<int> ExecuteAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (OracleCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                if (RequiresReturningIdParameter(sql))
                {
                    AddReturningIdParameter(command);
                }
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>用 OracleDataAdapter 填充 DataSet（多结果集时由 Fill 依次生成 Table、Table1... 表）/ Fills a DataSet via OracleDataAdapter (multiple result sets become Table, Table1... via Fill).</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>填充后的 DataSet / the filled DataSet.</returns>
        public DataSet ExecuteDataSet(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            DataSet ds = new DataSet();
            using (OracleCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                using (OracleDataAdapter adapter = new OracleDataAdapter(command))
                {
                    adapter.Fill(ds);
                }
            }
            return ds;
        }

        /// <summary>异步执行查询并把全部结果集装入 DataSet（表名 Table1..N，不走 DataAdapter）/ Asynchronously executes the query and loads all result sets into a DataSet (tables named Table1..N, without a DataAdapter).</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>含全部结果集的 DataSet / a DataSet containing all result sets.</returns>
        public async Task<DataSet> ExecuteDataSetAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (OracleCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    return LoadDataSet(reader);
                }
            }
        }

        /// <summary>执行多结果集查询，返回多结果集读取器包装器（包装器持有命令，释放时连带释放命令）/ Executes a multi-result-set query returning a grid-reader wrap (the wrap owns the command and disposes it).</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>多结果集读取器包装器 / the grid-reader wrap.</returns>
        public GridReaderWrap ExecuteGridReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            OracleCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout);
            var reader = command.ExecuteReader();
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return new GridReaderWrap(wrap);
        }

        /// <summary>异步执行多结果集查询，返回多结果集读取器包装器（包装器持有命令，释放时连带释放命令）/ Asynchronously executes a multi-result-set query returning a grid-reader wrap (the wrap owns the command and disposes it).</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>多结果集读取器包装器 / the grid-reader wrap.</returns>
        public async Task<GridReaderWrap> ExecuteGridReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            OracleCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return new GridReaderWrap(wrap);
        }

        /// <summary>执行查询，返回 DataReader 包装器（包装器持有命令，释放时连带释放命令与 reader）/ Executes the query returning a DataReader wrap (the wrap owns the command; disposing it releases the reader and command).</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>DataReader 包装器 / the DataReader wrap.</returns>
        public DataReaderWrap ExecuteReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            OracleCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout);
            var reader = command.ExecuteReader();
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return wrap;
        }

        /// <summary>异步执行查询，返回 DataReader 包装器（包装器持有命令，释放时连带释放命令与 reader）/ Asynchronously executes the query returning a DataReader wrap (the wrap owns the command; disposing it releases the reader and command).</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>DataReader 包装器 / the DataReader wrap.</returns>
        public async Task<DataReaderWrap> ExecuteReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            OracleCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return wrap;
        }

        /// <summary>执行查询返回首行首列值；检测到 RETURNING :RETURNING_ID 时改走 ExecuteNonQuery 并返回 RETURNING_ID 输出参数值（取回生成的主键）/ Executes the query returning the first row's first column; when RETURNING :RETURNING_ID is detected it runs ExecuteNonQuery instead and returns the RETURNING_ID output value (the generated key).</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>首行首列值或 RETURNING_ID 输出参数值；无结果时为 null / the first row's first column or the RETURNING_ID output value; null when there is no result.</returns>
        public object? ExecuteScalar(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (OracleCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout))
            {
                if (RequiresReturningIdParameter(sql))
                {
                    AddReturningIdParameter(command);
                    command.ExecuteNonQuery();
                    return command.Parameters[ReturningIdParameterName].Value;
                }
                return command.ExecuteScalar();
            }
        }

        /// <summary>异步执行查询返回首行首列值；检测到 RETURNING :RETURNING_ID 时改走 ExecuteNonQuery 并返回输出参数值 / Asynchronously executes the query returning the first row's first column; when RETURNING :RETURNING_ID is detected it runs ExecuteNonQuery instead and returns the output value.</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>首行首列值或 RETURNING_ID 输出参数值；无结果时为 null / the first row's first column or the RETURNING_ID output value; null when there is no result.</returns>
        public async Task<object?> ExecuteScalarAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (OracleCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                if (RequiresReturningIdParameter(sql))
                {
                    AddReturningIdParameter(command);
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    return command.Parameters[ReturningIdParameterName].Value;
                }
                return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>执行查询并把首行首列值转换为 <typeparamref name="T"/>（结果为 null 或 DBNull 时返回 default）/ Executes the query converting the first column of the first row to <typeparamref name="T"/> (null or DBNull yields default).</summary>
        /// <typeparam name="T">目标类型 / the target type.</typeparam>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>转换后的标量值；结果为 null / DBNull 时返回 default / the converted scalar; default when the result is null / DBNull.</returns>
        public T? ExecuteScalar<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            object? obj = ExecuteScalar(conn, sql, param, transaction, commandType, commandTimeout);
            if (obj == null || obj == DBNull.Value)
            {
                return default;
            }
            return GenericTool.ConvertScalar<T>(obj);
        }

        /// <summary>异步执行查询并把首行首列值转换为 <typeparamref name="T"/>（结果为 null 或 DBNull 时返回 default）/ Asynchronously executes the query converting the first column of the first row to <typeparamref name="T"/> (null or DBNull yields default).</summary>
        /// <typeparam name="T">目标类型 / the target type.</typeparam>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
        /// <param name="param">SQL 参数对象 / the SQL parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>转换后的标量值；结果为 null / DBNull 时返回 default / the converted scalar; default when the result is null / DBNull.</returns>
        public async Task<T?> ExecuteScalarAsync<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            object? obj = await ExecuteScalarAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            if (obj == null || obj == DBNull.Value)
            {
                return default;
            }
            return GenericTool.ConvertScalar<T>(obj);
        }

        /// <summary>查询全部行，返回 dynamic 列表 / Queries all rows returning a dynamic list.</summary>
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
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
        /// <param name="conn">数据库连接，必须为 OracleConnection / the connection; must be an OracleConnection.</param>
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
