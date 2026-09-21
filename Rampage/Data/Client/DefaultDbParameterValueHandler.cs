using System;

namespace Rampage.Data.Client
{
    /// <summary>
    /// 默认数据库参数值处理器 / Default database parameter value handler
    /// </summary>
    /// <remarks>
    /// 转换规则：null 原样返回；枚举类型按字段映射的 EnumString 配置转为字符串（true）或 32 位整数（false）；
    /// 其余类型不经转换直接返回。作为 <see cref="DatabaseFactory.DbParameterValueHandler"/> 的缺省实现懒加载创建。
    /// Conversion rules: null passes through; enums are converted to string (when the field mapping's
    /// EnumString is true) or to a 32-bit integer; all other types pass through unchanged.
    /// Lazily created as the default implementation of <see cref="DatabaseFactory.DbParameterValueHandler"/>.
    /// </remarks>
    public class DefaultDbParameterValueHandler : IDbParameterValueHandler
    {

        /// <summary>
        /// 处理参数值：null 原样返回；枚举按 <paramref name="fieldMapping"/> 的 EnumString 配置转为字符串或
        /// 32 位整数（供数据库以数值形式存储枚举）；其余类型原样返回
        /// Handle a parameter value: null is returned as-is; enums are converted to string or a 32-bit integer
        /// per the field mapping's EnumString flag; other types pass through unchanged
        /// </summary>
        /// <param name="value">原始属性值，可能为 null / The raw property value, possibly null</param>
        /// <param name="tableMapping">所属模型的表映射（本实现未使用） / The table mapping of the owning model (unused by this implementation)</param>
        /// <param name="fieldMapping">目标字段映射，提供枚举配置 / The field mapping of the target column, providing enum settings</param>
        /// <returns>转换后的参数值 / The converted parameter value</returns>
        public virtual object? Handle(object? value, TableMapping tableMapping, FieldMapping fieldMapping)
        {
            if (value == null)
            {
                return value;
            }
            if (fieldMapping.PropertyType.IsEnum)
            {
                if (fieldMapping.EnumString)
                {
                    return Convert.ToString(value);
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            else
            {
                return value;
            }
        }

    }
}
