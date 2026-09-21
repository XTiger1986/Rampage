using Rampage.Data;
using Rampage.Data.Client;
using System;

namespace Rampage.DatabaseLibrary.SqlServer
{
    /// <summary>SQL Server 方言 SQL 模板（薄壳：仅以 DatabaseType.SqlServer 初始化基类，分页等方言逻辑在核心 <see cref="SqlTemplate"/>）/ SQL Server dialect SQL template (a thin shell: merely initializes the base with DatabaseType.SqlServer; paging and other dialect logic lives in the core <see cref="SqlTemplate"/>).</summary>
    public class SqlServerTemplate : SqlTemplate
    {

        /// <summary>创建 SQL Server 方言模板 / Creates the SQL Server dialect template.</summary>
        public SqlServerTemplate() : base(DatabaseType.SqlServer) { }

    }
}
