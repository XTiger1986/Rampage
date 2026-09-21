using Rampage.Data;
using Rampage.Data.Client;

namespace Rampage.DatabaseLibrary.Oracle
{
    /// <summary>Oracle 方言 SQL 模板（薄壳：仅以 DatabaseType.Oracle 初始化基类，分页等方言逻辑在核心 <see cref="SqlTemplate"/>）/ Oracle dialect SQL template (a thin shell: merely initializes the base with DatabaseType.Oracle; paging and other dialect logic lives in the core <see cref="SqlTemplate"/>).</summary>
    public class OracleTemplate : SqlTemplate
    {

        /// <summary>创建 Oracle 方言模板 / Creates the Oracle dialect template.</summary>
        public OracleTemplate() : base(DatabaseType.Oracle) { }

    }
}
