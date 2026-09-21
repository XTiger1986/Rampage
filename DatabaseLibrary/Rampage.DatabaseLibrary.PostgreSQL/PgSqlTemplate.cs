using Rampage.Data;
using Rampage.Data.Client;
using System;

namespace Rampage.DatabaseLibrary.PostgreSQL
{
    /// <summary>PostgreSQL 方言 SQL 模板（薄壳：仅以 DatabaseType.PostgreSQL 初始化基类，分页等方言逻辑在核心 <see cref="SqlTemplate"/>）/ PostgreSQL dialect SQL template (a thin shell: merely initializes the base with DatabaseType.PostgreSQL; paging and other dialect logic lives in the core <see cref="SqlTemplate"/>).</summary>
    public class PgSqlTemplate : SqlTemplate
    {

        /// <summary>创建 PostgreSQL 方言模板 / Creates the PostgreSQL dialect template.</summary>
        public PgSqlTemplate() : base(DatabaseType.PostgreSQL) { }

    }
}
