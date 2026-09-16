using Dapper;
using Rampage.Data;
using Rampage.Data.Client;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Dapper
{
    /// <summary>
    /// a DataAccessor using Dapper
    /// </summary>
    public class DapperDataAccessor : IDataAccessor
    {
        static DapperDataAccessor()
        {
            // DateOnly（PG date）与 TimeSpan（PG time / Npgsql 10 TimeOnly）TypeHandler
            NpgsqlDateCompat.EnsureRegistered();
            // write_root → WriteRoot（SELECT * 或无别名时）
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private DataSet ConvertDataReaderToDataSet(IDataReader data)
        {
            DataSet ds = new DataSet();
            int i = 0;
            while (!data.IsClosed)
            {
                ds.Tables.Add("Table" + (i + 1));
                ds.EnforceConstraints = false;
                ds.Tables[i].Load(data);
                i++;
            }
            return ds;
        }

        private static CommandDefinition CreateCommandDefinition(string sql, object? param, IDbTransaction? transaction, CommandType? commandType, int? commandTimeout, CancellationToken cancellationToken)
        {
            return new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        }

        public int Execute(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.Execute(sql, param, transaction, commandTimeout, commandType);
        }

        public Task<int> ExecuteAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return conn.ExecuteAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken));
        }

        public DataSet ExecuteDataSet(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (IDataReader reader = conn.ExecuteReader(sql, param, transaction, commandTimeout, commandType))
            {
                return ConvertDataReaderToDataSet(reader);
            }
        }

        public async Task<DataSet> ExecuteDataSetAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (IDataReader reader = await conn.ExecuteReaderAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false))
            {
                return ConvertDataReaderToDataSet(reader);
            }
        }

        public GridReaderWrap ExecuteGridReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            SqlMapper.GridReader gridReader = conn.QueryMultiple(sql, param, transaction, commandTimeout, commandType);
            return new GridReaderWrap(new DapperGridReader(gridReader));
        }

        public async Task<GridReaderWrap> ExecuteGridReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            SqlMapper.GridReader gridReader = await conn.QueryMultipleAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false);
            return new GridReaderWrap(new DapperGridReader(gridReader));
        }

        public DataReaderWrap ExecuteReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            IDataReader reader = conn.ExecuteReader(sql, param, transaction, commandTimeout, commandType);
            return new DataReaderWrap(reader);
        }

        public async Task<DataReaderWrap> ExecuteReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            IDataReader reader = await conn.ExecuteReaderAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false);
            return new DataReaderWrap(reader);
        }

        public object ExecuteScalar(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.ExecuteScalar(sql, param, transaction, commandTimeout, commandType);
        }

        public Task<object?> ExecuteScalarAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return conn.ExecuteScalarAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken));
        }

        public T ExecuteScalar<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.ExecuteScalar<T>(sql, param, transaction, commandTimeout, commandType);
        }

        public Task<T?> ExecuteScalarAsync<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return conn.ExecuteScalarAsync<T>(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken))!;
        }

        public List<dynamic> SelectList(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.Query<dynamic>(sql, param, transaction, true, commandTimeout, commandType).ToList();
        }

        public async Task<List<dynamic>> SelectListAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var result = await conn.QueryAsync<dynamic>(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false);
            return result.ToList();
        }

        public List<M> SelectList<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class
        {
            return conn.Query<M>(sql, param, transaction, true, commandTimeout, commandType).ToList();
        }

        public async Task<List<M>> SelectListAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            var result = await conn.QueryAsync<M>(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false);
            return result.ToList();
        }

        public dynamic? SelectOne(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.QueryFirstOrDefault(sql, param, transaction, commandTimeout, commandType);
        }

        public Task<dynamic?> SelectOneAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return conn.QueryFirstOrDefaultAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken));
        }

        public M? SelectOne<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class
        {
            return conn.QueryFirstOrDefault<M>(sql, param, transaction, commandTimeout, commandType);
        }

        public Task<M?> SelectOneAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            return conn.QueryFirstOrDefaultAsync<M>(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken))!;
        }
    }
}
