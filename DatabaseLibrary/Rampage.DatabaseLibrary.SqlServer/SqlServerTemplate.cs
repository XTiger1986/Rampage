using Rampage.Data;
using Rampage.Data.Client;
using System;

namespace Rampage.DatabaseLibrary.SqlServer
{
    /// <summary>
    /// build sql Template of SqlServer database
    /// </summary>
    public class SqlServerTemplate : SqlTemplate
    {

        public SqlServerTemplate() : base(DatabaseType.SqlServer) { }

    }
}
