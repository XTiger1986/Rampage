using Rampage.Hierarchy;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 
    /// </summary>
    public class CurdExecutor
    {

        public static M? Select<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout) where M : class
        {
            return dao.DatabaseClient.SelectOne<M>(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null);
        }

        public static List<M> SelectList<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout) where M : class
        {
            return dao.DatabaseClient.SelectList<M>(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null);
        }

        public static int Execute<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout) where M : class
        {
            return dao.DatabaseClient.Execute(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null);
        }

        public static Task<M?> SelectAsync<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout, CancellationToken cancellationToken = default) where M : class
        {
            return dao.DatabaseClient.SelectOneAsync<M>(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null, cancellationToken);
        }

        public static Task<List<M>> SelectListAsync<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout, CancellationToken cancellationToken = default) where M : class
        {
            return dao.DatabaseClient.SelectListAsync<M>(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null, cancellationToken);
        }

        public static Task<int> ExecuteAsync<M>(SuperDao<M> dao, string sql, Dictionary<string, object>? args, int commandType, int commandTimeout, CancellationToken cancellationToken = default) where M : class
        {
            return dao.DatabaseClient.ExecuteAsync(sql, args, commandType > 0 ? (CommandType)commandType : null, commandTimeout > 0 ? commandTimeout : null, cancellationToken);
        }

    }
}
