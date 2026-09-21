using Rampage.Data;
using Rampage.Data.Client;
using System;

namespace Rampage.DatabaseLibrary.MySql
{
    /// <summary>MySQL 方言 SQL 模板（薄壳：仅以 DatabaseType.MySQL 初始化基类，分页等方言逻辑在核心 <see cref="SqlTemplate"/>）/ MySQL dialect SQL template (a thin shell: merely initializes the base with DatabaseType.MySQL; paging and other dialect logic lives in the core <see cref="SqlTemplate"/>).</summary>
    public class MySqlTemplate : SqlTemplate
    {

        /// <summary>创建 MySQL 方言模板 / Creates the MySQL dialect template.</summary>
        public MySqlTemplate() : base(DatabaseType.MySQL) { }

    }
}
