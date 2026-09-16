using System;

namespace Rampage.Data.Client
{
    public interface IDbParameterValueHandler
    {

        object? Handle(object? value, TableMapping tableMapping, FieldMapping fieldMapping);

    }
}
