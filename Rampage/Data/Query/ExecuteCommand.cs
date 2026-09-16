using System;

namespace Rampage.Data.Query
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExecuteCommand : QueryCommand
    {

        public ExecuteCommand(string sql) : base(sql) { }

    }
}
