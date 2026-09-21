using Rampage.Data;
using System;
using System.Collections;

namespace Rampage.Data
{
    /// <summary>
    /// 待执行的 SQL 语句及其参数的封装
    /// Encapsulates a SQL statement together with its parameters
    /// </summary>
    public class SqlInfo
    {

        /// <summary>
        /// 待执行的 SQL 文本
        /// The SQL text to execute
        /// </summary>
        public string? Sql
        {
            get;
            set;
        }

        /// <summary>
        /// SQL 参数字典（调用 InitializeParameters 前为 null）
        /// The SQL parameter dictionary (null until InitializeParameters is called)
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

        /// <summary>
        /// 获取当前参数字典
        /// Get the current parameter dictionary
        /// </summary>
        /// <returns>参数字典；未初始化时为 null / the dictionary, or null before initialization</returns>
        public IDictionary<string, object?>? GetParameters()
        {
            return Parameters;
        }

        /// <summary>
        /// 参数字典是否已初始化
        /// Whether the parameter dictionary has been initialized
        /// </summary>
        public bool ParametersInitialized
        {
            get
            {
                return Parameters != null;
            }
        }

        /// <summary>
        /// 初始化一个空的参数字典（已有参数会被清空）
        /// Initialize an empty parameter dictionary (existing parameters are discarded)
        /// </summary>
        public void InitializeParameters()
        {
            Parameters = new Dictionary<string, object?>();
        }

        /// <summary>
        /// 添加一个 SQL 参数（参数字典未初始化时自动先初始化）
        /// Add one SQL parameter, initializing the dictionary on demand
        /// </summary>
        /// <param name="name">参数名 / parameter name</param>
        /// <param name="value">参数值 / parameter value</param>
        public void AddParameter(string name, object? value)
        {
            if (!ParametersInitialized)
            {
                InitializeParameters();
            }
            Parameters!.Add(name, value);
        }

        /// <summary>
        /// 为 IN 子句批量添加参数，并返回经格式化后以逗号连接的参数引用串
        /// Add parameters for an IN clause and return the formatted parameter names joined by comma
        /// </summary>
        /// <param name="values">IN 值集合 / the values of the IN clause</param>
        /// <param name="formatParameter">参数引用的格式化函数（如添加数据库参数前缀符）/ formats a parameter reference (e.g. adding a parameter prefix marker)</param>
        /// <param name="paramPrefix">生成的参数名前缀 / prefix for generated parameter names</param>
        /// <returns>逗号连接的参数引用串；集合为空时返回空字符串 / comma-joined parameter references, or an empty string for an empty collection</returns>
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
                // 跳过已占用的参数名，避免同一 SqlInfo 上多次调用 AddInParameters 时键冲突
                // Skip parameter names already taken to avoid key collisions across repeated AddInParameters calls
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
