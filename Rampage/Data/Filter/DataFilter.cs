using Rampage.Reflection;
using System;

namespace Rampage.Data.Filter
{

    /// <summary>
    /// 数据筛选器基类：以属性 + <see cref="FilterItemAttribute"/> 声明各筛选项，
    /// 非空属性值参与条件构建（见 <see cref="FilterParser"/>）
    /// Base class of data filters: each filter item is declared by a property with <see cref="FilterItemAttribute"/>;
    /// non-null property values take part in condition building (see <see cref="FilterParser"/>)
    /// </summary>
    public abstract class DataFilter 
    {

        /// <summary>
        /// 检查是否有任一筛选项已被赋值（任一属性值非 null 即视为已设置）
        /// Check whether any filter item has been set (any non-null property value counts as set)
        /// </summary>
        /// <returns>已设置返回 true / True when any item is set</returns>
        public bool CheckIsSet()
        {
            Type type = this.GetType();

            FilterSchema? schema = FilterSchemaStore.GetFilterSchema(type);
            if (schema != null)
            {
                foreach (var item in schema.ItemList!)
                {
                    var prp = item.Property;
                    object? prpValue = EmitTool.GetPropertyValue(this, prp!.Name);
                    if (prpValue != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }

}
