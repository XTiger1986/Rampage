using System;

namespace Rampage.Data.Expressions
{
    public class LambdaConditionParameter
    {

        public string? Name { get; set; }

        public object? Value { get; set; }

        public FieldMapping Column { get; set; } = default!;



    }
}
