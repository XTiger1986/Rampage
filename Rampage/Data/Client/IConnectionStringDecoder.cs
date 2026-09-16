using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// database connection string decoder interface
    /// </summary>
    public interface IConnectionStringDecoder
    {

        /// <summary>
        /// decode encrypted database connection string
        /// </summary>
        /// <param name="encodedConnectionString"></param>
        /// <returns></returns>
        string Decode(string encodedConnectionString);

    }
}
