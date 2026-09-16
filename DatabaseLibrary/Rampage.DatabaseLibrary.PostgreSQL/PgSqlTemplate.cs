using Rampage.Data;
using Rampage.Data.Client;
using System;

namespace Rampage.DatabaseLibrary.PostgreSQL
{
    /// <summary>
    /// build sql Template of PostgreSQL database
    /// </summary>
    public class PgSqlTemplate : SqlTemplate
    {

        public PgSqlTemplate() : base(DatabaseType.PostgreSQL) { }

    }
}
