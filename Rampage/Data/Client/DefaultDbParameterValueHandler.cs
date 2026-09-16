using System;

namespace Rampage.Data.Client
{
    public class DefaultDbParameterValueHandler : IDbParameterValueHandler
    {

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
