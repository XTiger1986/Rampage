using Rampage.Reflection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Rampage.Utilities
{
    /// <summary>
    /// CSV 编解码工具（RFC4180 转义；泛型读写基于 EmitTool 编译委托，无逐次反射） /
    /// CSV encode/decode utility (RFC 4180 escaping; generic read/write uses EmitTool compiled delegates instead of per-call reflection)
    /// </summary>
    public static class CsvTool
    {
        /// <summary>
        /// 编码字符串二维表为 CSV 文本（CRLF 行分隔；含逗号/引号/换行的字段按 RFC4180 引号包裹转义） /
        /// Encode a table of string rows into CSV text (CRLF line separators; fields containing commas/quotes/line breaks are quoted-escaped per RFC 4180)
        /// </summary>
        /// <param name="rows">行集合，每行为一个字段数组 / The rows; each row is an array of fields</param>
        /// <returns>CSV 文本 / The CSV text</returns>
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
        /// 将字符串二维表写入文本写入器（CRLF 行分隔；RFC4180 引号转义） /
        /// Write a table of string rows to a text writer (CRLF line separators; RFC 4180 quote escaping)
        /// </summary>
        /// <param name="writer">目标写入器 / The target writer</param>
        /// <param name="rows">行集合，每行为一个字段数组 / The rows; each row is an array of fields</param>
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
        /// 编码对象集合为 CSV 文本（默认含表头，表头取可读写属性名；值以 InvariantCulture 转字符串；
        /// null 项整行跳过，null 字段输出空串） /
        /// Encode a sequence of objects into CSV text (a header of read/write property names is included by default;
        /// values are converted with InvariantCulture; null items skip the row and null fields render empty)
        /// </summary>
        /// <typeparam name="T">元素类型 / The element type</typeparam>
        /// <param name="items">对象集合 / The objects to encode</param>
        /// <param name="includeHeader">是否输出表头行，默认 true / Whether to emit the header row, true by default</param>
        /// <returns>CSV 文本 / The CSV text</returns>
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
        /// 解析 CSV 文本为字符串二维表（自动处理引号包裹、双引号转义与字段内换行；完全空白的行被丢弃） /
        /// Parse CSV text into a table (handles quoted fields, doubled-quote escaping and line breaks inside fields; entirely blank rows are dropped)
        /// </summary>
        /// <param name="csv">CSV 文本 / The CSV text</param>
        /// <returns>行集合 / The parsed rows</returns>
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
                        // 跳过 \r，行结束以 \n 为准（兼容 \r\n 与 \n）/ skip \r; lines are terminated by \n (accepts both \r\n and \n)
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
        /// 解析 CSV 文本为对象集合（有表头时按忽略大小写的同名属性映射，无表头时按列位置映射；
        /// 不匹配的列与空单元格被跳过不赋值） /
        /// Parse CSV text into objects (with a header, columns map to same-named properties case-insensitively;
        /// without a header, columns map by position; unmatched columns and empty cells are skipped)
        /// </summary>
        /// <remarks>单元格值经 InvariantCulture 的 Convert.ChangeType 转换为属性类型，转换失败会抛异常。Cell values are converted to the property type via InvariantCulture Convert.ChangeType; a failed conversion throws.</remarks>
        /// <typeparam name="T">目标类型，须有无参构造 / The target type, must have a parameterless constructor</typeparam>
        /// <param name="csv">CSV 文本 / The CSV text</param>
        /// <param name="hasHeader">首行是否为表头，默认 true / Whether the first row is a header, true by default</param>
        /// <returns>对象集合 / The parsed objects</returns>
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
                    // 复用 EmitTool 的编译委托缓存 / reuse EmitTool's compiled-delegate cache
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
