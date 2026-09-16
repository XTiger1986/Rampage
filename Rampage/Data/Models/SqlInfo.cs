using Rampage.Data;
using System;
using System.Collections;

namespace Rampage.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlInfo
    {

        /// <summary>
        /// 
        /// </summary>
        public string? Sql
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        protected IDictionary<string, object?>? Parameters
        {
            get;
            set;
        }

        /// <summary>
        /// 生成的SQL语句是否在末尾拼接了返回自增长键值的SQL语句
        /// Does the generated SQL statement append a SQL statement at the end to return the auto-increment key value
        /// </summary>
        public bool IsAutoIncrement
        {
            get;
            set;
        }

        /// <summary>
        /// 自增长字段信息
        /// Auto-increment field information
        /// </summary>
        public FieldMapping? AutoIncrementInfo
        {
            get;
            set;
        }

        public IDictionary<string, object?>? GetParameters()
        {
            return Parameters;
        }

        public bool ParametersInitialized
        {
            get
            {
                return Parameters != null;
            }
        }

        public void InitializeParameters()
        {
            Parameters = new Dictionary<string, object?>();
        }

        public void AddParameter(string name, object? value)
        {
            if (!ParametersInitialized)
            {
                InitializeParameters();
            }
            Parameters!.Add(name, value);
        }

        /// <summary>
        /// Add parameters for an IN clause and return formatted parameter names joined by comma.
        /// </summary>
        public string AddInParameters(IEnumerable values, Func<string, string> formatParameter, string paramPrefix)
        {
            if (!ParametersInitialized)
            {
                InitializeParameters();
            }

            List<string> paramRefs = new List<string>();
            int index = 0;
            foreach (object? value in values)
            {
                string key = $"{paramPrefix}_{index}";
                while (Parameters!.ContainsKey(key))
                {
                    index++;
                    key = $"{paramPrefix}_{index}";
                }
                AddParameter(key, value);
                paramRefs.Add(formatParameter(key));
                index++;
            }
            return paramRefs.Count > 0 ? string.Join(",", paramRefs) : string.Empty;
        }

    }
}
