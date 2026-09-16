using Rampage.Reflection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Rampage.Utilities
{
    /// <summary>
    /// CSV 编解码工具（RFC4180 转义；泛型读写基于 EmitTool 编译委托，无逐次反射）
    /// </summary>
    public static class CsvTool
    {
        /// <summary>
        /// 编码字符串二维表为 CSV 文本
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static string Encode(IEnumerable<string[]> rows)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string[] row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }
                    AppendEscaped(sb, row[i]);
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 写入 CSV 到文本写入器
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="rows"></param>
        public static void Write(TextWriter writer, IEnumerable<string[]> rows)
        {
            foreach (string[] row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(',');
                    }
                    writer.Write(Escape(row[i]));
                }
                writer.Write("\r\n");
            }
        }

        /// <summary>
        /// 编码对象集合为 CSV 文本（默认含表头，取属性名）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="includeHeader"></param>
        /// <returns></returns>
        public static string Encode<T>(IEnumerable<T> items, bool includeHeader = true)
        {
            List<PropertyAccessor> accessors = GetAccessors<T>();
            StringBuilder sb = new StringBuilder();
            if (includeHeader)
            {
                for (int i = 0; i < accessors.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }
                    AppendEscaped(sb, accessors[i].Name);
                }
                sb.Append("\r\n");
            }
            foreach (T item in items)
            {
                if (item == null)
                {
                    continue;
                }
                for (int i = 0; i < accessors.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }
                    object? value = accessors[i].Get(item);
                    AppendEscaped(sb, value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture));
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 解析 CSV 文本为字符串二维表（自动处理引号转义与换行）
        /// </summary>
        /// <param name="csv"></param>
        /// <returns></returns>
        public static List<string[]> Parse(string csv)
        {
            List<string[]> rows = new List<string[]>();
            List<string> currentRow = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;
            bool fieldStarted = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else
                {
                    if (c == '"' && !fieldStarted)
                    {
                        inQuotes = true;
                        fieldStarted = true;
                    }
                    else if (c == ',')
                    {
                        currentRow.Add(field.ToString());
                        field.Clear();
                        fieldStarted = false;
                    }
                    else if (c == '\r')
                    {
                        // 等待 \n（若是行尾独立 \r 也按行结束处理）
                    }
                    else if (c == '\n')
                    {
                        currentRow.Add(field.ToString());
                        field.Clear();
                        fieldStarted = false;
                        if (currentRow.Count > 1 || currentRow[0].Length > 0)
                        {
                            rows.Add(currentRow.ToArray());
                        }
                        currentRow.Clear();
                    }
                    else
                    {
                        field.Append(c);
                        fieldStarted = true;
                    }
                }
            }
            if (field.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(field.ToString());
                if (currentRow.Count > 1 || currentRow[0].Length > 0)
                {
                    rows.Add(currentRow.ToArray());
                }
            }
            return rows;
        }

        /// <summary>
        /// 解析 CSV 文本为对象集合（表头映射同名属性，忽略不匹配列）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="csv"></param>
        /// <param name="hasHeader"></param>
        /// <returns></returns>
        public static List<T> Parse<T>(string csv, bool hasHeader = true) where T : new()
        {
            List<string[]> rows = Parse(csv);
            List<T> result = new List<T>();
            if (rows.Count == 0)
            {
                return result;
            }

            List<PropertyAccessor> accessors = GetAccessors<T>();
            List<int> columnMap = new List<int>();
            for (int c = 0; c < rows[0].Length; c++)
            {
                string? columnName = hasHeader ? rows[0][c] : null;
                int targetIndex = -1;
                for (int a = 0; a < accessors.Count; a++)
                {
                    if (hasHeader ? string.Equals(accessors[a].Name, columnName, StringComparison.OrdinalIgnoreCase) : a == c)
                    {
                        targetIndex = a;
                        break;
                    }
                }
                columnMap.Add(targetIndex);
            }

            int startRow = hasHeader ? 1 : 0;
            for (int r = startRow; r < rows.Count; r++)
            {
                T item = new T();
                string[] row = rows[r];
                for (int c = 0; c < row.Length && c < columnMap.Count; c++)
                {
                    int targetIndex = columnMap[c];
                    if (targetIndex < 0 || string.IsNullOrEmpty(row[c]))
                    {
                        continue;
                    }
                    accessors[targetIndex].Set(item, row[c]);
                }
                result.Add(item);
            }
            return result;
        }

        private static void AppendEscaped(StringBuilder sb, string? value)
        {
            if (value == null)
            {
                return;
            }
            if (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0)
            {
                sb.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
            }
            else
            {
                sb.Append(value);
            }
        }

        private static string Escape(string? value)
        {
            if (value != null && (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value ?? string.Empty;
        }

        private sealed class PropertyAccessor
        {
            public string Name = string.Empty;
            public Func<object, object?> Get = _ => null;
            public Action<object, string> Set = (_, _) => { };
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, List<PropertyAccessor>> AccessorCache = new();

        private static List<PropertyAccessor> GetAccessors<T>()
        {
            return AccessorCache.GetOrAdd(typeof(T), type =>
            {
                List<PropertyAccessor> list = new List<PropertyAccessor>();
                foreach (System.Reflection.PropertyInfo property in type.GetProperties())
                {
                    if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }
                    PropertyAccessor accessor = new PropertyAccessor { Name = property.Name };
                    // 复用 EmitTool 的编译委托缓存
                    accessor.Get = obj => EmitTool.GetPropertyValue(obj, property.Name);
                    accessor.Set = (obj, value) =>
                    {
                        object? converted = null;
                        if (!string.IsNullOrEmpty(value))
                        {
                            converted = Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType, CultureInfo.InvariantCulture);
                        }
                        EmitTool.SetPropertyValue(obj, property.Name, converted);
                    };
                    list.Add(accessor);
                }
                return list;
            });
        }
    }
}
