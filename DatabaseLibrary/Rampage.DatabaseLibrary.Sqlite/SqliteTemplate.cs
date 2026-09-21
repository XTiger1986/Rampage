using Rampage.Data;
using Rampage.Data.Client;
using System;

namespace Rampage.DatabaseLibrary.Sqlite
{
    /// <summary>SQLite 方言 SQL 模板（薄壳：仅以 DatabaseType.Sqlite 初始化基类，分页等方言逻辑在核心 <see cref="SqlTemplate"/>）/ SQLite dialect SQL template (a thin shell: merely initializes the base with DatabaseType.Sqlite; paging and other dialect logic lives in the core <see cref="SqlTemplate"/>).</summary>
    public class SqliteTemplate : SqlTemplate
    {

        /// <summary>创建 SQLite 方言模板 / Creates the SQLite dialect template.</summary>
        public SqliteTemplate() : base(DatabaseType.Sqlite) { }

    }
}
