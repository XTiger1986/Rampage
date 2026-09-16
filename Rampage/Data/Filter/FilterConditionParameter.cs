using System;

namespace Rampage.Data.Filter
{
    public class FilterConditionParameter
    {

        public string? Name { get; set; }

        public object? Value { get; set; }

        public FieldMapping Column { get; set; } = default!;

    }
}
