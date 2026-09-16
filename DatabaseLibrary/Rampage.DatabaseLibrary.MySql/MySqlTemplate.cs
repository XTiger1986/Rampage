using Rampage.Data;
using Rampage.Data.Client;
using System;

namespace Rampage.DatabaseLibrary.MySql
{
    /// <summary>
    /// build sql Template of MySQL database
    /// </summary>
    public class MySqlTemplate : SqlTemplate
    {

        public MySqlTemplate() : base(DatabaseType.MySQL) { }

    }
}
