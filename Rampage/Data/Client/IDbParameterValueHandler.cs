using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 数据库参数值处理器接口 / Database parameter value handler interface
    /// </summary>
    /// <remarks>
    /// 在模型属性值写入 SQL 参数之前做统一转换（如枚举按映射配置转为底层数值或字符串），
    /// 由 <see cref="Rampage.Data.SqlTemplate"/> 在生成 INSERT/UPDATE/查询条件参数时调用。
    /// 默认实现为 <see cref="DefaultDbParameterValueHandler"/>，可通过
    /// <see cref="DatabaseFactory.DbParameterValueHandler"/> 全局替换为自定义实现（注意 SqlTemplate 在构造时会快照当前处理器）。
    /// Converts model property values just before they are written into SQL parameters
    /// (e.g. enums to their underlying number or string per the mapping). Called by
    /// <see cref="Rampage.Data.SqlTemplate"/> when generating INSERT/UPDATE/condition parameters.
    /// The default implementation is <see cref="DefaultDbParameterValueHandler"/>; replace it globally via
    /// <see cref="DatabaseFactory.DbParameterValueHandler"/> (note that SqlTemplate snapshots the handler at construction).
    /// </remarks>
    public interface IDbParameterValueHandler
    {

        /// <summary>
        /// 处理即将作为 SQL 参数写入数据库的值 / Handle a value about to be written as a SQL parameter
        /// </summary>
        /// <param name="value">原始属性值，可能为 null / The raw property value, possibly null</param>
        /// <param name="tableMapping">所属模型的表映射 / The table mapping of the owning model</param>
        /// <param name="fieldMapping">目标字段映射 / The field mapping of the target column</param>
        /// <returns>转换后的参数值 / The converted parameter value</returns>
        object? Handle(object? value, TableMapping tableMapping, FieldMapping fieldMapping);

    }
}
