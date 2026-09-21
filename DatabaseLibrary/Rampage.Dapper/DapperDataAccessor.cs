using Dapper;
using Rampage.Data;
using Rampage.Data.Client;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Dapper
{
    /// <summary>基于 Dapper 的 <see cref="IDataAccessor"/> 实现，可经 DatabaseFactory.GetDataAccessType(name) 按配置替换默认 ADO 访问器 / A Dapper-backed <see cref="IDataAccessor"/>; can replace the default ADO accessor per configuration via DatabaseFactory.GetDataAccessType(name).</summary>
    /// <remarks>
    /// 静态构造中完成两项全局初始化（进程内仅一次）：
    /// 1. 注册 DateOnly / TimeSpan 的 Dapper TypeHandler（Npgsql 日期时间兼容，见 <see cref="NpgsqlDateCompat"/>）；
    /// 2. 开启 DefaultTypeMap.MatchNamesWithUnderscores，使下划线列名（write_root）自动映射到驼峰属性（WriteRoot），仅对 SELECT * 或无别名列生效。
    /// 同步方法直接调用 Dapper 扩展（不携带 CancellationToken）；异步方法通过 CommandDefinition 传递取消令牌。
    /// Two process-wide initializations happen in the static ctor: registering the DateOnly / TimeSpan Dapper TypeHandlers
    /// (Npgsql date/time compatibility, see <see cref="NpgsqlDateCompat"/>) and enabling DefaultTypeMap.MatchNamesWithUnderscores
    /// so underscore column names (write_root) map to Pascal-case properties (WriteRoot); effective for SELECT * / unaliased columns.
    /// Sync methods call the Dapper extensions directly (no CancellationToken); async methods forward the token via CommandDefinition.
    /// </remarks>
    public class DapperDataAccessor : IDataAccessor
    {
        static DapperDataAccessor()
        {
            // DateOnly（PG date）与 TimeSpan（PG time / Npgsql 10 TimeOnly）TypeHandler
            NpgsqlDateCompat.EnsureRegistered();
            // write_root → WriteRoot（SELECT * 或无别名时）
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        /// <summary>把 DataReader 的全部结果集装入 DataSet / Loads every result set of a DataReader into a DataSet.</summary>
        /// <param name="data">数据读取器 / the data reader.</param>
        /// <returns>含全部结果集的 DataSet，表依次命名为 Table1..N / a DataSet holding all result sets, tables named Table1..N in order.</returns>
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

        /// <summary>组装 Dapper CommandDefinition，异步方法借它传递 CancellationToken / Builds the Dapper CommandDefinition used by async methods to carry the CancellationToken.</summary>
        private static CommandDefinition CreateCommandDefinition(string sql, object? param, IDbTransaction? transaction, CommandType? commandType, int? commandTimeout, CancellationToken cancellationToken)
        {
            return new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        }

        /// <summary>执行非查询语句，返回受影响行数 / Executes a non-query statement and returns the affected row count.</summary>
        /// <param name="conn">数据库连接（任意 ADO.NET 提供程序连接）/ the database connection (any ADO.NET provider connection).</param>
        /// <param name="param">Dapper 参数对象：匿名对象、POCO、字典或 DynamicParameters / the Dapper parameter object: anonymous type, POCO, dictionary or DynamicParameters.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>受影响的行数 / the number of affected rows.</returns>
        public int Execute(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.Execute(sql, param, transaction, commandTimeout, commandType);
        }

        /// <summary>异步执行非查询语句，返回受影响行数 / Asynchronously executes a non-query statement and returns the affected row count.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>受影响的行数 / the number of affected rows.</returns>
        public Task<int> ExecuteAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return conn.ExecuteAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken));
        }

        /// <summary>执行查询并把全部结果集装入 DataSet（表名 Table1..N）/ Executes the query and loads all result sets into a DataSet (tables named Table1..N).</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>含全部结果集的 DataSet / a DataSet containing all result sets.</returns>
        public DataSet ExecuteDataSet(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (IDataReader reader = conn.ExecuteReader(sql, param, transaction, commandTimeout, commandType))
            {
                return ConvertDataReaderToDataSet(reader);
            }
        }

        /// <summary>异步执行查询并把全部结果集装入 DataSet（表名 Table1..N）/ Asynchronously executes the query and loads all result sets into a DataSet (tables named Table1..N).</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>含全部结果集的 DataSet / a DataSet containing all result sets.</returns>
        public async Task<DataSet> ExecuteDataSetAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using (IDataReader reader = await conn.ExecuteReaderAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false))
            {
                return ConvertDataReaderToDataSet(reader);
            }
        }

        /// <summary>执行多结果集查询，返回由 <see cref="DapperGridReader"/>（包裹 SqlMapper.GridReader）支撑的包装器 / Executes a multi-result-set query returning a wrap backed by <see cref="DapperGridReader"/> (which wraps SqlMapper.GridReader).</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>多结果集读取器包装器 / the grid-reader wrap.</returns>
        public GridReaderWrap ExecuteGridReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            SqlMapper.GridReader gridReader = conn.QueryMultiple(sql, param, transaction, commandTimeout, commandType);
            return new GridReaderWrap(new DapperGridReader(gridReader));
        }

        /// <summary>异步执行多结果集查询，返回由 <see cref="DapperGridReader"/> 支撑的包装器 / Asynchronously executes a multi-result-set query returning a wrap backed by <see cref="DapperGridReader"/>.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>多结果集读取器包装器 / the grid-reader wrap.</returns>
        public async Task<GridReaderWrap> ExecuteGridReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            SqlMapper.GridReader gridReader = await conn.QueryMultipleAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false);
            return new GridReaderWrap(new DapperGridReader(gridReader));
        }

        /// <summary>执行查询，返回单结果集的 DataReader 包装器 / Executes the query returning a single-result-set DataReader wrap.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>DataReader 包装器 / the DataReader wrap.</returns>
        public DataReaderWrap ExecuteReader(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            IDataReader reader = conn.ExecuteReader(sql, param, transaction, commandTimeout, commandType);
            return new DataReaderWrap(reader);
        }

        /// <summary>异步执行查询，返回单结果集的 DataReader 包装器 / Asynchronously executes the query returning a single-result-set DataReader wrap.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>DataReader 包装器 / the DataReader wrap.</returns>
        public async Task<DataReaderWrap> ExecuteReaderAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            IDataReader reader = await conn.ExecuteReaderAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false);
            return new DataReaderWrap(reader);
        }

        /// <summary>执行查询，返回首行首列的值 / Executes the query returning the first column of the first row.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>首行首列值；无结果时为 null / the first column of the first row; null when there is no result.</returns>
        public object ExecuteScalar(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.ExecuteScalar(sql, param, transaction, commandTimeout, commandType);
        }

        /// <summary>异步执行查询，返回首行首列的值 / Asynchronously executes the query returning the first column of the first row.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>首行首列值；无结果时为 null / the first column of the first row; null when there is no result.</returns>
        public Task<object?> ExecuteScalarAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return conn.ExecuteScalarAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken));
        }

        /// <summary>执行查询并把首行首列值转换为 <typeparamref name="T"/> / Executes the query converting the first column of the first row to <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">目标类型 / the target type.</typeparam>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>转换后的标量值；无结果时为 default(T) / the converted scalar; default(T) when there is no result.</returns>
        public T ExecuteScalar<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.ExecuteScalar<T>(sql, param, transaction, commandTimeout, commandType);
        }

        /// <summary>异步执行查询并把首行首列值转换为 <typeparamref name="T"/> / Asynchronously executes the query converting the first column of the first row to <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">目标类型 / the target type.</typeparam>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>转换后的标量值；无结果时为 default / the converted scalar; default when there is no result.</returns>
        public Task<T?> ExecuteScalarAsync<T>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return conn.ExecuteScalarAsync<T>(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken))!;
        }

        /// <summary>查询全部行，返回 dynamic（DapperRow）列表 / Queries all rows returning a dynamic (DapperRow) list.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>行列表（缓冲加载，无行为空列表）/ the buffered row list; an empty list when there are no rows.</returns>
        public List<dynamic> SelectList(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.Query<dynamic>(sql, param, transaction, true, commandTimeout, commandType).ToList();
        }

        /// <summary>异步查询全部行，返回 dynamic 列表 / Asynchronously queries all rows returning a dynamic list.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>行列表（缓冲加载，无行为空列表）/ the buffered row list; an empty list when there are no rows.</returns>
        public async Task<List<dynamic>> SelectListAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var result = await conn.QueryAsync<dynamic>(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false);
            return result.ToList();
        }

        /// <summary>查询全部行并映射为 <typeparamref name="M"/> 列表 / Queries all rows mapping them to a list of <typeparamref name="M"/>.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>映射结果列表（缓冲加载）/ the buffered mapped list.</returns>
        public List<M> SelectList<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class
        {
            return conn.Query<M>(sql, param, transaction, true, commandTimeout, commandType).ToList();
        }

        /// <summary>异步查询全部行并映射为 <typeparamref name="M"/> 列表 / Asynchronously queries all rows mapping them to a list of <typeparamref name="M"/>.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>映射结果列表（缓冲加载）/ the buffered mapped list.</returns>
        public async Task<List<M>> SelectListAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            var result = await conn.QueryAsync<M>(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken)).ConfigureAwait(false);
            return result.ToList();
        }

        /// <summary>查询首行，无行时返回 null / Queries the first row; null when there is no row.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>首行动态行对象；无行时为 null / the first row as a dynamic object; null when there is no row.</returns>
        public dynamic? SelectOne(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null)
        {
            return conn.QueryFirstOrDefault(sql, param, transaction, commandTimeout, commandType);
        }

        /// <summary>异步查询首行，无行时返回 null / Asynchronously queries the first row; null when there is no row.</summary>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>首行动态行对象；无行时为 null / the first row as a dynamic object; null when there is no row.</returns>
        public Task<dynamic?> SelectOneAsync(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return conn.QueryFirstOrDefaultAsync(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken));
        }

        /// <summary>查询首行并映射为 <typeparamref name="M"/>，无行时返回 null / Queries the first row mapping it to <typeparamref name="M"/>; null when there is no row.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <returns>映射后的首行；无行时为 null / the mapped first row; null when there is no row.</returns>
        public M? SelectOne<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null) where M : class
        {
            return conn.QueryFirstOrDefault<M>(sql, param, transaction, commandTimeout, commandType);
        }

        /// <summary>异步查询首行并映射为 <typeparamref name="M"/>，无行时返回 null / Asynchronously queries the first row mapping it to <typeparamref name="M"/>; null when there is no row.</summary>
        /// <typeparam name="M">目标实体类型 / the target entity type.</typeparam>
        /// <param name="conn">数据库连接 / the database connection.</param>
        /// <param name="param">Dapper 参数对象 / the Dapper parameter object.</param>
        /// <param name="transaction">要加入的事务；null 表示不使用事务 / the transaction to enlist in; null for none.</param>
        /// <param name="commandType">命令类型 / the command type.</param>
        /// <param name="commandTimeout">命令超时（秒）/ the command timeout in seconds.</param>
        /// <param name="cancellationToken">取消令牌 / the cancellation token.</param>
        /// <returns>映射后的首行；无行时为 null / the mapped first row; null when there is no row.</returns>
        public Task<M?> SelectOneAsync<M>(IDbConnection conn, string sql, object? param = null, IDbTransaction? transaction = null, CommandType? commandType = CommandType.Text, int? commandTimeout = null, CancellationToken cancellationToken = default) where M : class
        {
            return conn.QueryFirstOrDefaultAsync<M>(CreateCommandDefinition(sql, param, transaction, commandType, commandTimeout, cancellationToken))!;
        }
    }
}
