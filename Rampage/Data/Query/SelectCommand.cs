using System;

namespace Rampage.Data.Query
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SelectCommand : QueryCommand
    {

        public SelectCommand(string sql) : base(sql) { }

    }
}
