using Rampage.Reflection;
using System;

namespace Rampage.Data.Filter
{

    /// <summary>
    /// 
    /// </summary>
    public abstract class DataFilter 
    {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
