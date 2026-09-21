using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Rampage.Json;

namespace Rampage.Utilities
{
    /// <summary>
    /// 字符串工具类（比较、解析、拆合、中英文校验、随机串生成等）/
    /// String utilities (comparison, parsing, explode/implode, Chinese/English validation, random string generation, etc.)
    /// </summary>
    public class StringTool
    {


		#region Comparison

		/// <summary>
		/// 区分大小写地比较两个字符串是否相等（按序数/二进制规则）/
		/// Check whether two strings are equal, case sensitively, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="s1">字符串 1 / String 1</param>
		/// <param name="s2">字符串 2 / String 2</param>
		/// <returns>相等返回 true / true when the strings are equal</returns>
		/// <remarks>null 与空字符串视为相等：双方均为 null/空串时返回 true，仅一方为 null/空串时返回 false。/ null and an empty string are treated as equal: true when both sides are null/empty, false when only one side is.</remarks>
		public static bool Equals(string? s1, string? s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
            {
                return true;
            }
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            {
                return false;
            }

            if (s2.Length != s1.Length)
            {
                return false;
            }

            return (0 == string.Compare(s1, 0, s2, 0, s2.Length, StringComparison.Ordinal));
        }

		/// <summary>
		/// 忽略大小写比较两个字符串是否相等（按序数/二进制规则）/
		/// Check whether two strings are equal, ignoring case, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="s1">字符串 1 / String 1</param>
		/// <param name="s2">字符串 2 / String 2</param>
		/// <returns>忽略大小写后相等返回 true / true when the strings are equal ignoring case</returns>
		/// <remarks>null 与空字符串视为相等：双方均为 null/空串时返回 true，仅一方为 null/空串时返回 false。/ null and an empty string are treated as equal: true when both sides are null/empty, false when only one side is.</remarks>
		public static bool EqualsIgnoreCase(string? s1, string? s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
            {
                return true;
            }
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            {
                return false;
            }

            if (s2.Length != s1.Length)
            {
                return false;
            }

            return (0 == string.Compare(s1, 0, s2, 0, s2.Length, StringComparison.OrdinalIgnoreCase));
        }

		/// <summary>
		/// 区分大小写地判断文本是否以指定字符开头 /
		/// Check whether the text starts with the specified character, case sensitively
		/// </summary>
		/// <param name="text">待检查文本 / The text to check</param>
		/// <param name="lookfor">要查找的字符 / The character to look for</param>
		/// <returns>匹配返回 true；文本为空或字符为 null 时返回 false / true on match; false when the text is null/empty or the character is null</returns>
		public static bool StartsWith(string? text, char? lookfor)
        {
            if (string.IsNullOrEmpty(text) || lookfor == null)
            {
                return false;
            }
            return (text.Length > 0 && text[0] == lookfor);
        }

		/// <summary>
		/// 区分大小写地判断文本是否以指定字符串开头（按序数/二进制规则）/
		/// Check whether the text starts with the specified string, case sensitively, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="target">待检查文本 / The text to check</param>
		/// <param name="lookfor">要查找的字符串 / The string to look for</param>
		/// <returns>匹配返回 true；任一参数为 null/空串或待查找串更长时返回 false / true on match; false when either side is null/empty or the looked-for string is longer</returns>
		public static bool StartsWith(string? target, string? lookfor)
        {
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(lookfor))
            {
                return false;
            }

            if (lookfor.Length > target.Length)
            {
                return false;
            }

            return (0 == string.Compare(target, 0, lookfor, 0, lookfor.Length, StringComparison.Ordinal));
        }

		/// <summary>
		/// 忽略大小写判断文本是否以指定字符串开头（按序数/二进制规则）/
		/// Check whether the text starts with the specified string, ignoring case, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="text">待检查文本 / The text to check</param>
		/// <param name="lookfor">要查找的字符串 / The string to look for</param>
		/// <returns>忽略大小写后匹配返回 true / true on match when case is ignored</returns>
		public static bool StartsWithIgnoreCase(string? text, string? lookfor)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(lookfor))
            {
                return false;
            }

            if (lookfor.Length > text.Length)
            {
                return false;
            }
            return (0 == string.Compare(text, 0, lookfor, 0, lookfor.Length, StringComparison.OrdinalIgnoreCase));
        }


		/// <summary>
		/// 区分大小写地判断文本是否以指定字符结尾 /
		/// Check whether the text ends with the specified character, case sensitively
		/// </summary>
		/// <param name="text">待检查文本 / The text to check</param>
		/// <param name="lookfor">要查找的字符 / The character to look for</param>
		/// <returns>匹配返回 true；文本或字符为 null 时返回 false / true on match; false when the text or the character is null</returns>
		public static bool EndsWith(string? text, char? lookfor)
        {
            if (text == null || lookfor == null)
            {
                return false;
            }
            return (text.Length > 0 && text[text.Length - 1] == lookfor);
        }

		/// <summary>
		/// 区分大小写地判断文本是否以指定字符串结尾（按序数/二进制规则）/
		/// Check whether the text ends with the specified string, case sensitively, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="text">待检查文本 / The text to check</param>
		/// <param name="lookfor">要查找的字符串 / The string to look for</param>
		/// <returns>匹配返回 true；文本为 null、待查找串为 null/空串或待查找串更长时返回 false / true on match; false when the text is null, the looked-for string is null/empty, or it is longer than the text</returns>
		public static bool EndsWith(string? text, string? lookfor)
        {
            if (text == null || string.IsNullOrEmpty(lookfor))
            {
                return false;
            }
            int indexA = text.Length - lookfor.Length;

            if (indexA < 0)
            {
                return false;
            }

            return (0 == string.Compare(text, indexA, lookfor, 0, lookfor.Length, StringComparison.Ordinal));
        }

		/// <summary>
		/// 忽略大小写判断文本是否以指定字符串结尾（按序数/二进制规则）/
		/// Check whether the text ends with the specified string, ignoring case, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="text">待检查文本 / The text to check</param>
		/// <param name="lookfor">要查找的字符串 / The string to look for</param>
		/// <returns>忽略大小写后匹配返回 true / true on match when case is ignored</returns>
		public static bool EndsWithIgnoreCase(string? text, string? lookfor)
        {
            if (text == null || string.IsNullOrEmpty(lookfor))
            {
                return false;
            }
            int indexA = text.Length - lookfor.Length;

            if (indexA < 0)
            {
                return false;
            }

            return (0 == string.Compare(text, indexA, lookfor, 0, lookfor.Length, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Parse

        /// <summary>
        /// 尝试将字符串转换为枚举、Guid 或基础类型值 /
        /// Try to convert a string to an enum, a Guid or a value-type value
        /// </summary>
        /// <param name="input">输入字符串；为 null 时直接失败 / The input string; fails immediately when null</param>
        /// <param name="type">目标类型：枚举、Guid、基础值类型或其 Nullable 包装 / The target type: enum, Guid, a value type, or their nullable wrapper</param>
        /// <param name="result">转换结果；失败时为 null / The converted value; null on failure</param>
        /// <returns>转换成功返回 true / true when the conversion succeeds</returns>
        /// <remarks>枚举同时接受数值与名称（名称忽略大小写）；可空类型先解包为底层类型再转换；不支持的目标类型返回 false。/ Enums accept both numeric values and names (case-insensitive); nullable types are unwrapped first; unsupported target types return false.</remarks>
        public static bool TryParse(string input, Type type, out object? result)
        {
            result = null;

            if (input == null)
            {
                return false;
            }

            bool succeed = false;
            object? parsedValue = null;

            bool isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable)
            {
                type = type.GetGenericArguments()[0];
            }

            if (type.IsEnum)
            {
                int enumInt;

                if (int.TryParse(input, out enumInt))
                {
                    parsedValue = Enum.ToObject(type, enumInt);
                    succeed = true;
                }
                else
                {
                    string enumString = input;
                    foreach (string name in Enum.GetNames(type))
                    {
                        if (EqualsIgnoreCase(name, enumString))
                        {
                            parsedValue = Enum.Parse(type, enumString, true);
                            succeed = true;
                        }
                    }
                }

                if (parsedValue == null)
                {
                    return false;
                }
            }
            else if (type == typeof(Guid))
            {
                Guid temp;
                if (Guid.TryParse(input, out temp))
                {
                    parsedValue = temp;
                    succeed = true;
                }
                else
                    return false;
            }
            else
            {
                TypeCode typeCode = Type.GetTypeCode(type);

                switch (typeCode)
                {
                    case TypeCode.String:
                        parsedValue = input;
                        succeed = true;
                        break;
                    case TypeCode.Boolean:
                        {
                            Boolean temp;
                            succeed = Boolean.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.Byte:
                        {
                            Byte temp;
                            succeed = Byte.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.Decimal:
                        {
                            Decimal temp;
                            succeed = Decimal.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.Double:
                        {
                            Double temp;
                            succeed = Double.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.Int16:
                        {
                            Int16 temp;
                            succeed = Int16.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.Int32:
                        {
                            Int32 temp;
                            succeed = Int32.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.Int64:
                        {
                            Int64 temp;
                            succeed = Int64.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.SByte:
                        {
                            SByte temp;
                            succeed = SByte.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.Single:
                        {
                            Single temp;
                            succeed = Single.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.UInt16:
                        {
                            UInt16 temp;
                            succeed = UInt16.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.UInt32:
                        {
                            UInt32 temp;
                            succeed = UInt32.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.UInt64:
                        {
                            UInt64 temp;
                            succeed = UInt64.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    case TypeCode.DateTime:
                        {
                            DateTime temp;
                            succeed = DateTime.TryParse(input, out temp);

                            if (succeed)
                                parsedValue = temp;
                            else
                                return false;
                        }
                        break;
                    default:
                        return false;
                }
            }

            result = parsedValue;

            return succeed;
        }

		/// <summary>
		/// 尝试将字符串转换为枚举、Guid 或基础类型值，失败时返回默认值 /
		/// Try to convert a string to an enum, a Guid or a value-type value, returning the default value on failure
		/// </summary>
		/// <param name="input">输入字符串 / The input string</param>
		/// <param name="type">目标类型：枚举、Guid、基础值类型或其 Nullable 包装 / The target type: enum, Guid, a value type, or their nullable wrapper</param>
		/// <param name="defaultValue">转换失败时返回的值 / The value returned on failure</param>
		/// <returns>转换结果；失败时返回 defaultValue / The converted value, or defaultValue on failure</returns>
		public static object? TryParse(string input, Type type, object? defaultValue)
        {
            object? result;
            bool success = TryParse(input, type, out result);
            if (success)
            {
                return result;
            }
            return defaultValue;
        }

		/// <summary>
		/// 尝试将字符串转换为泛型目标类型的值 /
		/// Try to convert a string to the generic target type
		/// </summary>
		/// <typeparam name="T">目标类型：枚举、Guid、基础值类型或其 Nullable 包装 / The target type: enum, Guid, a value type, or their nullable wrapper</typeparam>
		/// <param name="input">输入字符串 / The input string</param>
		/// <param name="result">转换结果；失败时为 default(T) / The converted value; default(T) on failure</param>
		/// <returns>转换成功返回 true / true when the conversion succeeds</returns>
		public static bool TryParse<T>(string input, out T? result)
        {
            result = default(T);
            Type type = typeof(T);
            object? obj;
            bool success = TryParse(input, type, out obj);
            if (success)
            {
                result = (T?)obj;
            }
            return success;
        }

		/// <summary>
		/// 尝试将字符串转换为泛型目标类型的值，失败时返回默认值 /
		/// Try to convert a string to the generic target type, returning the default value on failure
		/// </summary>
		/// <typeparam name="T">目标类型：枚举、Guid、基础值类型或其 Nullable 包装 / The target type: enum, Guid, a value type, or their nullable wrapper</typeparam>
		/// <param name="input">输入字符串 / The input string</param>
		/// <param name="defaultValue">转换失败时返回的值 / The value returned on failure</param>
		/// <returns>转换结果；失败时返回 defaultValue / The converted value, or defaultValue on failure</returns>
		public static T? TryParse<T>(string input, T? defaultValue)
        {
            Type type = typeof(T);
            object? result;
            bool success = TryParse(input, type, out result);
            if (success)
            {
                return (T?)result;
            }
            return defaultValue;
        }

		/// <summary>
		/// 尝试将字符串转换为泛型目标类型的值，失败时返回 default(T) /
		/// Try to convert a string to the generic target type, returning default(T) on failure
		/// </summary>
		/// <typeparam name="T">目标类型：枚举、Guid、基础值类型或其 Nullable 包装 / The target type: enum, Guid, a value type, or their nullable wrapper</typeparam>
		/// <param name="input">输入字符串 / The input string</param>
		/// <returns>转换结果；失败时返回 default(T) / The converted value, or default(T) on failure</returns>
		public static T? TryParse<T>(string input)
        {
            Type type = typeof(T);
            object? result;
            bool success = TryParse(input, type, out result);
            if (success)
            {
                return (T?)result;
            }
            return default(T);
        }

        #endregion

        #region Explode / Implode

        /// <summary>
        /// 用指定分隔符将字符串拆分为目标类型列表（省略空字符串元素）/
        /// Separate a string into a list of the target type using the specified separators (empty entries omitted)
        /// </summary>
        /// <typeparam name="T">元素目标类型 / The target element type</typeparam>
        /// <param name="text">待拆分字符串 / The string to split</param>
        /// <param name="separators">分隔符数组；为 null 或空时默认使用 "," / The separators; defaults to "," when null or empty</param>
        /// <returns>转换后的元素列表；无法转换的元素被跳过 / The converted list; items that fail to convert are skipped</returns>
        public static List<T> Explode<T>(string text, params string[] separators)
        {
            return Explode<T>(text, StringSplitOptions.RemoveEmptyEntries, true, separators);
        }
        /// <summary>
        /// 用指定分隔符将字符串拆分为目标类型列表（省略空字符串元素）/
        /// Separate a string into a list of the target type using the specified separators (empty entries omitted)
        /// </summary>
        /// <typeparam name="T">元素目标类型 / The target element type</typeparam>
        /// <param name="text">待拆分字符串 / The string to split</param>
        /// <param name="trim">是否对每个分段做 Trim / Whether to trim each segment</param>
        /// <param name="separators">分隔符数组；为 null 或空时默认使用 "," / The separators; defaults to "," when null or empty</param>
        /// <returns>转换后的元素列表；无法转换的元素被跳过 / The converted list; items that fail to convert are skipped</returns>
        public static List<T> Explode<T>(string text, bool trim, params string[] separators)
        {
            return Explode<T>(text, StringSplitOptions.RemoveEmptyEntries, trim, separators);
        }
        /// <summary>
        /// 用指定分隔符与拆分选项将字符串拆分为目标类型列表 /
        /// Separate a string into a list of the target type using the specified separators and split options
        /// </summary>
        /// <typeparam name="T">元素目标类型 / The target element type</typeparam>
        /// <param name="text">待拆分字符串 / The string to split</param>
        /// <param name="options">拆分选项（如是否移除空元素）/ Split options (e.g. whether empty entries are removed)</param>
        /// <param name="trim">是否对每个分段做 Trim / Whether to trim each segment</param>
        /// <param name="separators">分隔符数组；为 null 或空时默认使用 "," / The separators; defaults to "," when null or empty</param>
        /// <returns>转换后的元素列表；无法转换的元素被跳过 / The converted list; items that fail to convert are skipped</returns>
        public static List<T> Explode<T>(string text, StringSplitOptions options, bool trim, params string[] separators)
        {
            if (separators == null || separators.Length == 0)
            {
                separators = new string[] { "," };
            }
            string[] strArr = text.Split(separators, options);
            List<T> list = new List<T>();
            foreach (string str in strArr)
            {
                string segment = trim ? str.Trim() : str;
                T? item = StringTool.TryParse<T>(segment);
                if (item != null)
                    list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// 用逗号将集合拼接为字符串 /
        /// Combine a collection into a string using a comma separator
        /// </summary>
        /// <typeparam name="T">元素类型 / The element type</typeparam>
        /// <param name="list">集合；为 null 时返回空字符串 / The collection; an empty string when null</param>
        /// <returns>拼接结果；末尾不带分隔符 / The joined string; without a trailing separator</returns>
        public static string Implode<T>(IEnumerable<T> list)
        {
            return Implode<T>(list, ",", "");
        }
        /// <summary>
        /// 用指定分隔符将集合拼接为字符串 /
        /// Combine a collection into a string using the specified separator
        /// </summary>
        /// <typeparam name="T">元素类型 / The element type</typeparam>
        /// <param name="list">集合；为 null 时返回空字符串 / The collection; an empty string when null</param>
        /// <param name="separator">分隔符；为 null 时退回使用 "," / The separator; falls back to "," when null</param>
        /// <returns>拼接结果；末尾不带分隔符 / The joined string; without a trailing separator</returns>
        public static string Implode<T>(IEnumerable<T> list, string separator)
        {
            return Implode<T>(list, separator, "");
        }
        /// <summary>
        /// 用指定分隔符将集合拼接为字符串，可为每个元素加包裹符 /
        /// Combine a collection into a string using the specified separator, optionally wrapping each item
        /// </summary>
        /// <typeparam name="T">元素类型 / The element type</typeparam>
        /// <param name="list">集合；为 null 时返回空字符串 / The collection; an empty string when null</param>
        /// <param name="separator">分隔符；为 null 时退回使用 "," / The separator; falls back to "," when null</param>
        /// <param name="quote">添加在每个元素前后的包裹符（如引号），为空时不包裹 / The mark added before and after each item (e.g. quotes); no wrapping when empty</param>
        /// <returns>拼接结果；末尾不带分隔符，null 元素输出为空字符串 / The joined string; without a trailing separator, null items are emitted as empty strings</returns>
        public static string Implode<T>(IEnumerable<T> list, string separator, string quote)
        {
            if (separator == null)
            {
                separator = ",";
            }

            if (list != null)
            {
                StringBuilder strBuff = new StringBuilder(string.Empty);
                foreach (T item in list)
                {
                    if (!string.IsNullOrEmpty(quote))
                    {
                        strBuff.Append(string.Concat(quote, item != null ? item.ToString() : string.Empty, quote, separator));
                    }
                    else
                    {
                        strBuff.Append(string.Concat(item != null ? item.ToString() : string.Empty, separator));
                    }
                }

                string str = strBuff.ToString();
                if (str.Length > separator.Length)
                {
                    str = str.Substring(0, str.Length - separator.Length);
                }
                return str;
            }

            return string.Empty;
        }

        /// <summary>
        /// 用逗号将非泛型集合拼接为字符串 /
        /// Combine a non-generic collection into a string using a comma separator
        /// </summary>
        /// <param name="list">集合；为 null 时返回空字符串 / The collection; an empty string when null</param>
        /// <returns>拼接结果；末尾不带分隔符 / The joined string; without a trailing separator</returns>
        public static string Implode(IEnumerable list)
        {
            return Implode(list, ",", "");
        }
        /// <summary>
        /// 用指定分隔符将非泛型集合拼接为字符串 /
        /// Combine a non-generic collection into a string using the specified separator
        /// </summary>
        /// <param name="list">集合；为 null 时返回空字符串 / The collection; an empty string when null</param>
        /// <param name="separator">分隔符；为 null 时退回使用 "," / The separator; falls back to "," when null</param>
        /// <returns>拼接结果；末尾不带分隔符 / The joined string; without a trailing separator</returns>
        public static string Implode(IEnumerable list, string separator)
        {
            return Implode(list, separator, "");
        }
        /// <summary>
        /// 用指定分隔符将非泛型集合拼接为字符串，可为每个元素加包裹符 /
        /// Combine a non-generic collection into a string using the specified separator, optionally wrapping each item
        /// </summary>
        /// <param name="list">集合；为 null 时返回空字符串 / The collection; an empty string when null</param>
        /// <param name="separator">分隔符；为 null 时退回使用 "," / The separator; falls back to "," when null</param>
        /// <param name="quote">添加在每个元素前后的包裹符（如引号），为空时不包裹 / The mark added before and after each item (e.g. quotes); no wrapping when empty</param>
        /// <returns>拼接结果；末尾不带分隔符 / The joined string; without a trailing separator</returns>
        public static string Implode(IEnumerable list, string separator, string quote)
        {
            if (separator == null)
            {
                separator = ",";
            }
            if (list != null)
            {
                StringBuilder strBuff = new StringBuilder();
                IEnumerator iet = list.GetEnumerator();
                while (iet.MoveNext())
                {
                    object item = iet.Current;
                    if (!string.IsNullOrEmpty(quote))
                    {
                        strBuff.Append(string.Concat(quote, item.ToString(), quote, separator));
                    }
                    else
                    {
                        strBuff.Append(string.Concat(item.ToString(), separator));
                    }
                }

                string str = strBuff.ToString();
                if (!string.IsNullOrEmpty(str))
                {
                    str = str.Substring(0, str.Length - separator.Length);
                }
                return str;
            }

            return string.Empty;
        }

        #endregion

        /// <summary>
        /// 将文本重复拼接指定次数 /
        /// Repeat and concatenate the text the given number of times
        /// </summary>
        /// <param name="text">要重复的文本 / The text to repeat</param>
        /// <param name="loop">重复次数；小于等于 0 时返回空字符串 / The repeat count; an empty string when it is zero or negative</param>
        /// <returns>重复拼接后的字符串 / The repeated string</returns>
        public static string LoopString(string text, int loop)
        {
            StringBuffer sb = new StringBuffer();
            for(int i = 0; i < loop; i++)
            {
                sb += text;
            }
            return sb.ToString();
        }

        /// <summary>
        /// 生成从 start 到 end 的整数列表 JSON 字符串 /
        /// Build a JSON array string of integers from start to end
        /// </summary>
        /// <param name="start">起始数（含）/ The start number (inclusive)</param>
        /// <param name="end">结束数（不含）/ The end number (exclusive)</param>
        /// <param name="step">步长，小于等于 0 时按 1 处理 / The step; values at or below 0 are treated as 1</param>
        /// <returns>形如 [1,2,3] 的 JSON 数组字符串；start 等于 end 时返回 "[]" / A JSON array string such as [1,2,3]; "[]" when start equals end</returns>
        /// <remarks>实际委托 MathTool.BuildNumberList 生成；end 小于 start 时按降序生成。/ Delegates to MathTool.BuildNumberList; a descending list is produced when end is less than start.</remarks>
        public static string BuildNumberList(int start, int end, int step = 1)
        {
            if (start == end)
            {
                return "[]";
            }
            List<int> list = MathTool.BuildNumberList(start, end, step);
            return JsonTool.Stringify(list);
        }

        /// <summary>
        /// 用指定字符串在文本左侧补齐到目标长度 /
        /// Pad the left side of the text with a specified string up to the target length
        /// </summary>
        /// <param name="text">原始文本，不允许为 null 或空串 / The original text; must not be null or empty</param>
        /// <param name="length">目标总长度，须非负 / The target total length; must be non-negative</param>
        /// <param name="padStr">补齐用的字符串；为空时使用单个空格 / The padding string; a single space when null or empty</param>
        /// <returns>补齐后的文本；原文本已达到或超过目标长度时原样返回 / The padded text; returned as-is when it already meets or exceeds the target length</returns>
        /// <remarks>padStr 重复拼接后可能被截断，以满足精确长度。/ The repeated padStr may be truncated to reach the exact length.</remarks>
        /// <exception cref="ArgumentException">text 为 null 或空串时抛出 / Thrown when text is null or empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">length 为负数时抛出 / Thrown when length is negative</exception>
        public static string PadLeft(string text, int length, string? padStr = null)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentException("text cannot be empty", nameof(text));
            if (string.IsNullOrEmpty(padStr))
            {
                padStr = " ";
            }
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");

            if (text.Length >= length)
            {
                return text;
            }

            int paddingNeeded = length - text.Length;
            string padding = string.Empty;

            while (padding.Length < paddingNeeded)
            {
                padding += padStr;
            }

            padding = padding.Substring(0, paddingNeeded);
            return padding + text;
        }

        /// <summary>
        /// 用指定字符串在文本右侧补齐到目标长度 /
        /// Pad the right side of the text with a specified string up to the target length
        /// </summary>
        /// <param name="text">原始文本，不允许为 null 或空串 / The original text; must not be null or empty</param>
        /// <param name="length">目标总长度，须非负 / The target total length; must be non-negative</param>
        /// <param name="padStr">补齐用的字符串；为空时使用单个空格 / The padding string; a single space when null or empty</param>
        /// <returns>补齐后的文本；原文本已达到或超过目标长度时原样返回 / The padded text; returned as-is when it already meets or exceeds the target length</returns>
        /// <remarks>padStr 重复拼接后可能被截断，以满足精确长度。/ The repeated padStr may be truncated to reach the exact length.</remarks>
        /// <exception cref="ArgumentException">text 为 null 或空串时抛出 / Thrown when text is null or empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">length 为负数时抛出 / Thrown when length is negative</exception>
        public static string PadRight(string text, int length, string? padStr = null)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentException("text cannot be empty", nameof(text));
            if (string.IsNullOrEmpty(padStr))
            {
                padStr = " ";
            }
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");

            if (text.Length >= length)
            {
                return text;
            }

            int paddingNeeded = length - text.Length;
            string padding = string.Empty;

            while (padding.Length < paddingNeeded)
            {
                padding += padStr;
            }

            padding = padding.Substring(0, paddingNeeded);
            return text + padding;
        }

        /// <summary>
        /// 判断字符串是否为纯数字（含 int、double、single、decimal 等形式：可选前导负号、至多一个小数点） /
        /// Check whether the string is purely numerical (int, double, single, decimal forms: an optional leading minus and at most one decimal point)
        /// </summary>
        /// <param name="input">输入字符串 / The input string</param>
        /// <returns>纯数字返回 true；空串或仅负号返回 false / true for purely numerical input; false for an empty string or a lone minus sign</returns>
        public static bool IsPureNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            int start = 0;
            // 可选前导负号 / optional leading minus sign
            if (input[0] == '-')
            {
                start = 1;
                if (input.Length == 1)
                {
                    return false;
                }
            }

            bool decimalPointFound = false;

            for (int i = start; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsDigit(c))
                {
                    continue;
                }
                else if (c == '.')
                {
                    if (decimalPointFound)
                    {
                        return false;
                    }
                    decimalPointFound = true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断字符串是否全为中文字符 /
        /// Check whether the string consists purely of Chinese characters
        /// </summary>
        /// <param name="input">输入字符串 / The input string</param>
        /// <returns>全部为中文返回 true；空串返回 false / true when every character is Chinese; false for an empty string</returns>
        public static bool IsPureChinese(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            foreach (char c in input)
            {
                if (!IsChineseCharacter(c.ToString()))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 判断单个字符是否为汉字（基本区、扩展A区或兼容区）/
        /// Check whether the character is a Chinese character (basic block, Extension A, or compatibility block)
        /// </summary>
        /// <param name="c">待判断字符 / The character to check</param>
        /// <returns>属于汉字区块返回 true / true when the character falls in a Chinese character block</returns>
        private static bool IsChineseCharacter(char c)
        {
            return (c >= '\u4E00' && c <= '\u9FA5') || // Commonly used Chinese characters
                   (c >= '\u3400' && c <= '\u4DBF') || // CJK Unified Ideographs Extension A
                   (c >= '\uF900' && c <= '\uFAD9');   // Compatible with Chinese characters
        }
        /// <summary>
        /// 判断字符串是否全为汉字 /
        /// Check whether the string consists entirely of Chinese characters
        /// </summary>
        /// <param name="str">输入字符串 / The input string</param>
        /// <returns>全部为汉字返回 true；空串返回 true / true when every character is Chinese; an empty string returns true</returns>
        /// <remarks>含代理项字符（如 CJK 扩展 B 及之后的增补平面汉字）时直接返回 false。/ Returns false when the string contains surrogate pairs (e.g. CJK Extension B and beyond).</remarks>
        public static bool IsChineseCharacter(string str)
        {
            foreach (char c in str)
            {
                if (char.IsHighSurrogate(c) || char.IsLowSurrogate(c))
                {
                    return false;
                }
            }
            foreach (char c in str)
            {
                if (!IsChineseCharacter(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static readonly string _mobilePattern = @"^(?:\+?(\d{1,3}))?[-.\s]?(\(?\d{1,4}?\)?[-.\s]?[\d-.\s]{3,7})$";
        /// <summary>
        /// 判断是否为常见手机号格式的字符串 /
        /// Check whether the string is a common mobile number format
        /// </summary>
        /// <param name="input">输入字符串 / The input string</param>
        /// <returns>匹配常见手机号格式返回 true；空白串返回 false / true when it matches a common mobile format; false for null or whitespace</returns>
        public static bool IsCommonMobile(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            return Regex.IsMatch(input, _mobilePattern);
        }

        private static readonly string _emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        /// <summary>
        /// 判断是否为电子邮件格式的字符串 /
        /// Check whether the string is an email format
        /// </summary>
        /// <param name="input">输入字符串 / The input string</param>
        /// <returns>匹配电子邮件格式返回 true；空白串返回 false / true when it matches an email format; false for null or whitespace</returns>
        public static bool IsEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            return Regex.IsMatch(input, _emailPattern);
        }


        private static readonly string _urlPattern = @"^(?<protocol>[a-zA-Z][a-zA-Z\d+\-.]*):\/\/(?<host>[^\s\/$.?#].[^\s]*)";
        /// <summary>
        /// 判断是否为合法的 URL 字符串 /
        /// Check whether the string is a valid url
        /// </summary>
        /// <param name="input">输入字符串 / The input string</param>
        /// <returns>匹配协议://主机 形式返回 true；空白串返回 false / true when it matches a scheme://host form; false for null or whitespace</returns>
        public static bool IsValidUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }
            return Regex.IsMatch(input, _urlPattern);
        }

        /// <summary>
        /// 生成指定长度的随机数字字符串 /
        /// Generate a string of random digits of the specified length
        /// </summary>
        /// <param name="length">长度；小于等于 0 时返回空字符串 / The length; an empty string when it is zero or negative</param>
        /// <returns>仅含数字 0-9 的随机字符串 / A random string containing only digits 0-9</returns>
        /// <remarks>使用加密安全的 RandomNumberGenerator 生成随机字节。/ Random bytes come from the cryptographically secure RandomNumberGenerator.</remarks>
        public static string GenerateRandomNumberString(int length)
        {
            if (length <= 0)
            {
                return string.Empty;
            }

            byte[] randomNumbers = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumbers);
            }

            char[] digits = new char[length];
            for (int i = 0; i < length; i++)
            {
                digits[i] = (char)('0' + (randomNumbers[i] % 10));
            }

            return new string(digits);
        }

        private static readonly char[] _caseSensitiveLetters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly char[] _caseInsensitiveLetters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

        /// <summary>
        /// 生成指定长度的随机字母字符串 /
        /// Generate a string of random letters of the specified length
        /// </summary>
        /// <param name="length">长度；小于等于 0 时返回空字符串 / The length; an empty string when it is zero or negative</param>
        /// <param name="caseSensitive">是否包含大小写混合字母；false 时仅小写 / Whether to mix upper and lower case; lowercase only when false</param>
        /// <returns>仅含字母的随机字符串 / A random string containing only letters</returns>
        public static string GenerateRandomLetterString(int length, bool caseSensitive = true)
        {
            if (length <= 0)
            {
                return string.Empty;
            }

            Random random = new Random(Guid.NewGuid().GetHashCode());
            var stringBuilder = new StringBuilder(length);
            char[] letters = caseSensitive ? _caseSensitiveLetters : _caseInsensitiveLetters;

            for (int i = 0; i < length; i++)
            {
                char randomChar = letters[random.Next(letters.Length)];
                stringBuilder.Append(randomChar);
            }

            return stringBuilder.ToString();
        }
        
        /// <summary>
        /// 生成指定长度的随机字母数字字符串 /
        /// Generate a string of random letters and digits of the specified length
        /// </summary>
        /// <param name="length">长度；小于等于 0 时返回空字符串 / The length; an empty string when it is zero or negative</param>
        /// <param name="caseSensitive">字母部分是否包含大小写混合；false 时仅小写 / Whether letters mix upper and lower case; lowercase only when false</param>
        /// <returns>由字母与数字 0-9 组成的随机字符串 / A random string of letters and digits 0-9</returns>
        public static string GenerateRandomString(int length, bool caseSensitive = true)
        {
            if (length <= 0)
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder(length);
            char[] letters = caseSensitive ? _caseSensitiveLetters : _caseInsensitiveLetters;

            char[] combinedChars = new char[letters.Length + 10];
            Array.Copy(letters, combinedChars, letters.Length);
            for (int i = 0; i < 10; i++)
            {
                combinedChars[letters.Length + i] = (char)('0' + i);
            }

            Random random = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < length; i++)
            {
                char randomChar = combinedChars[random.Next(combinedChars.Length)];
                stringBuilder.Append(randomChar);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 源字符串为空白（null、空串或全空白字符）时返回默认值，否则返回原值 /
        /// Return the default value when the source is blank (null, empty or whitespace), otherwise the source itself
        /// </summary>
        /// <param name="src">源字符串 / The source string</param>
        /// <param name="defaultValue">默认值 / The default value</param>
        /// <returns>src 非空白时返回 src，否则返回 defaultValue / src when it is not blank, otherwise defaultValue</returns>
        public static string? IfBlank(string? src, string? defaultValue)
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                return defaultValue;
            }
            return src;
        }

        /// <summary>
        /// 源字符串为 null 或空串时返回默认值，否则返回原值 /
        /// Return the default value when the source is null or empty, otherwise the source itself
        /// </summary>
        /// <param name="src">源字符串 / The source string</param>
        /// <param name="defaultValue">默认值 / The default value</param>
        /// <returns>src 非空时返回 src，否则返回 defaultValue / src when it is not empty, otherwise defaultValue</returns>
        public static string? IfEmpty(string? src, string? defaultValue)
        {
            if (string.IsNullOrEmpty(src))
            {
                return defaultValue;
            }
            return src;
        }

        /// <summary>
        /// 按布尔条件二选一返回字符串 /
        /// Return one of two strings depending on the boolean value
        /// </summary>
        /// <param name="b">条件 / The condition</param>
        /// <param name="yes">条件为 true 时返回的字符串 / The string returned when true</param>
        /// <param name="no">条件为 false 时返回的字符串，默认为空串 / The string returned when false; defaults to an empty string</param>
        /// <returns>yes 或 no / Either yes or no</returns>
        public static string? If(bool b, string? yes, string? no = "")
        {
            return b ? yes : no;
        }

    }
}
