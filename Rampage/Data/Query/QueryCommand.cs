using System;
using System.Data;

namespace Rampage.Data.Query
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class QueryCommand : Attribute
    {

        /// <summary>
        /// 
        /// </summary>
        public string Sql
        {
            get;
            set;
        } = default!;

        public CommandType? CommandType
        {
            get;
            set;
        }

        public int? CommandTimeout
        {
            get;
            set;
        }


        private QueryCommand() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        public QueryCommand(string sql)
        {
            Sql = sql;
        }

    }
}
