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
    /// <summary>
    /// Oracle Data Accessor
    /// </summary>
    public class OracleDataAccessor : IDataAccessor
    {

        private const string ReturningIdParameterName = "RETURNING_ID";

        private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
        }

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

                if (param is IDictionary<string, object> dictionary)
                {
                    foreach (var kvp in dictionary)
                    {
                        var parameter = new OracleParameter(":" + kvp.Key, kvp.Value ?? DBNull.Value);
                        command.Parameters.Add(parameter);
                    }
                }
                else if (param is IDictionary<string, dynamic> dynamicDictionary)
                {
                    foreach (var kvp in dynamicDictionary)
                    {
                        var parameter = new OracleParameter(":" + kvp.Key, kvp.Value ?? DBNull.Value);
                        command.Parameters.Add(parameter);
                    }
                }
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

        private OracleCommand CreateCommand(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            OracleConnection connection = (OracleConnection)conn;
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
            return BuildCommand(connection, sql, param, transaction, commandType, commandTimeout);
        }

        private async Task<OracleCommand> CreateCommandAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            OracleConnection connection = (OracleConnection)conn;
            await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            return BuildCommand(connection, sql, param, transaction, commandType, commandTimeout);
        }

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

        private static bool RequiresReturningIdParameter(string sql)
        {
            return sql.Contains("RETURNING", StringComparison.OrdinalIgnoreCase)
                && sql.Contains($":{ReturningIdParameterName}", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddReturningIdParameter(OracleCommand command)
        {
            if (command.Parameters.Contains(ReturningIdParameterName))
            {
                return;
            }

            var returnParam = new OracleParameter(ReturningIdParameterName, OracleDbType.Decimal, ParameterDirection.Output);
            command.Parameters.Add(returnParam);
        }

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

        public GridReaderWrap ExecuteGridReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            OracleCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout);
            var reader = command.ExecuteReader();
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return new GridReaderWrap(wrap);
        }

        public async Task<GridReaderWrap> ExecuteGridReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            OracleCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return new GridReaderWrap(wrap);
        }

        public DataReaderWrap ExecuteReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            OracleCommand command = CreateCommand(conn, sql, param, transaction, commandType, commandTimeout);
            var reader = command.ExecuteReader();
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return wrap;
        }

        public async Task<DataReaderWrap> ExecuteReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            OracleCommand command = await CreateCommandAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var wrap = new DataReaderWrap(reader);
            wrap.SetOwnedCommand(command);
            return wrap;
        }

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

        public T? ExecuteScalar<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            object? obj = ExecuteScalar(conn, sql, param, transaction, commandType, commandTimeout);
            if (obj == null || obj == DBNull.Value)
            {
                return default;
            }
            return GenericTool.ConvertScalar<T>(obj);
        }

        public async Task<T?> ExecuteScalarAsync<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            object? obj = await ExecuteScalarAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false);
            if (obj == null || obj == DBNull.Value)
            {
                return default;
            }
            return GenericTool.ConvertScalar<T>(obj);
        }

        public List<dynamic> SelectList(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (DataReaderWrap xReader = ExecuteReader(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return xReader.SelectList();
            }
        }

        public async Task<List<dynamic>> SelectListAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (DataReaderWrap xReader = await ExecuteReaderAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return xReader.SelectList();
            }
        }

        public List<M> SelectList<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class
        {
            using (DataReaderWrap xReader = ExecuteReader(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return xReader.SelectList<M>();
            }
        }

        public async Task<List<M>> SelectListAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            using (DataReaderWrap xReader = await ExecuteReaderAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return xReader.SelectList<M>();
            }
        }

        public dynamic? SelectOne(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (DataReaderWrap xReader = ExecuteReader(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return xReader.SelectOne();
            }
        }

        public async Task<dynamic?> SelectOneAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (DataReaderWrap xReader = await ExecuteReaderAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return xReader.SelectOne();
            }
        }

        public M? SelectOne<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class
        {
            using (DataReaderWrap xReader = ExecuteReader(conn, sql, param, transaction, commandType, commandTimeout))
            {
                return xReader.SelectOne<M>();
            }
        }

        public async Task<M?> SelectOneAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            using (DataReaderWrap xReader = await ExecuteReaderAsync(conn, sql, param, transaction, commandType, commandTimeout, cancellationToken).ConfigureAwait(false))
            {
                return xReader.SelectOne<M>();
            }
        }
    }
}
