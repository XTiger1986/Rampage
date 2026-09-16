using Rampage.Data;
using Rampage.Data.Client;

namespace Rampage.DatabaseLibrary.Oracle
{
    /// <summary>
    /// build sql Template of Oracle database
    /// </summary>
    public class OracleTemplate : SqlTemplate
    {

        public OracleTemplate() : base(DatabaseType.Oracle) { }

    }
}
