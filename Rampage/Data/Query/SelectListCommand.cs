using System;

namespace Rampage.Data.Query
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SelectListCommand : QueryCommand
    {

        public SelectListCommand(string sql) : base(sql) { }

    }
}
