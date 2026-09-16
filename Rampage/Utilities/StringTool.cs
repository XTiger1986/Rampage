using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Rampage.Json;

namespace Rampage.Utilities
{
    /// <summary>
    /// String Utility
    /// </summary>
    public class StringTool
    {


		#region Comparison

		/// <summary>
		/// Case Sensitive, Compare strings using ordinal (binary) sort rules.
		/// </summary>
		/// <param name="s1">string 1</param>
		/// <param name="s2">string 2</param>
		/// <returns></returns>
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
		/// case insensitive, Compare strings using ordinal (binary) sort rules.
		/// </summary>
		/// <param name="s1">string 1</param>
		/// <param name="s2">string 2</param>
		/// <returns></returns>
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
		/// Case Sensitive, check does a text start with a specified character
		/// </summary>
		/// <param name="text">text to check</param>
		/// <param name="lookfor">char to look for</param>
		/// <returns></returns>
		public static bool StartsWith(string? text, char? lookfor)
        {
            if (string.IsNullOrEmpty(text) || lookfor == null)
            {
                return false;
            }
            return (text.Length > 0 && text[0] == lookfor);
        }

		/// <summary>
		/// Case Sensitive, check does a text start with a specified string, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="target">text to check</param>
		/// <param name="lookfor">string to look for</param>
		/// <returns></returns>
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
		/// case insensitive, check does a text start with a specified string, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="text">text to check</param>
		/// <param name="lookfor">string to look for</param>
		/// <returns></returns>
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
		/// Case Sensitive, check does a text end with a specified character
		/// </summary>
		/// <param name="text">text to check</param>
		/// <param name="lookfor">char to look for</param>
		/// <returns></returns>
		public static bool EndsWith(string? text, char? lookfor)
        {
            if (text == null || lookfor == null)
            {
                return false;
            }
            return (text.Length > 0 && text[text.Length - 1] == lookfor);
        }

		/// <summary>
		/// Case Sensitive, check does a text end with a specified string, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="text">text to check</param>
		/// <param name="lookfor">string to look for</param>
		/// <returns></returns>
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
		/// case insensitive, check does a text end with a specified string, using ordinal (binary) sort rules
		/// </summary>
		/// <param name="text">text to check</param>
		/// <param name="lookfor">string to look for</param>
		/// <returns></returns>
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
        /// try convert string to a enum or guid or value
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type">enum type | Guid | value type</param>
        /// <param name="result"></param>
        /// <returns></returns>
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
		/// try convert string to a enum or guid or value
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type">enum type | Guid | value type</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
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
		/// try convert string to a enum or guid or value
		/// </summary>
		/// <typeparam name="T">enum type | Guid | value type</typeparam>
		/// <param name="input"></param>
		/// <param name="result"></param>
		/// <returns></returns>
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
		/// try convert string to a enum or guid or value
		/// </summary>
		/// <typeparam name="T">enum type | Guid | value type</typeparam>
		/// <param name="input"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
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
		/// try convert string to a enum or guid or value
		/// </summary>
		/// <typeparam name="T">enum type | Guid | value type</typeparam>
		/// <param name="input"></param>
		/// <returns></returns>
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
        /// Separate a string into a list using the specified symbol (Omit array elements that contain an empty string from the result.)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
        public static List<T> Explode<T>(string text, params string[] separators)
        {
            return Explode<T>(text, StringSplitOptions.RemoveEmptyEntries, true, separators);
        }
        /// <summary>
        /// Separate a string into a list using the specified symbol (Omit array elements that contain an empty string from the result.)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
        public static List<T> Explode<T>(string text, bool trim, params string[] separators)
        {
            return Explode<T>(text, StringSplitOptions.RemoveEmptyEntries, trim, separators);
        }
        /// <summary>
        /// Separate a string into a list using the specified symbol 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <param name="options"></param>
        /// <param name="separators"></param>
        /// <returns></returns>
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
                T? item = StringTool.TryParse<T>(str);
                if (item != null)
                    list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// Combine collections into strings using comma separator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string Implode<T>(IEnumerable<T> list)
        {
            return Implode<T>(list, ",", "");
        }
        /// <summary>
        /// Combine collections into strings using specified separators
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string Implode<T>(IEnumerable<T> list, string separator)
        {
            return Implode<T>(list, separator, "");
        }
        /// <summary>
        /// Combine collections into strings using specified separators
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <param name="quote">mark added before and after each item</param>
        /// <returns></returns>
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
        /// Combine collections into strings using comma separator
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string Implode(IEnumerable list)
        {
            return Implode(list, ",", "");
        }
        /// <summary>
        /// Combine collections into strings using specified separators
        /// </summary>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string Implode(IEnumerable list, string separator)
        {
            return Implode(list, separator, "");
        }
        /// <summary>
        /// Combine collections into strings using specified separators
        /// </summary>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <param name="quote">mark added before and after each item</param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
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
        /// build integer list json string from start to end
        /// </summary>
        /// <param name="start">start number (inclusive)</param>
        /// <param name="end">end number (exclusive)</param>
        /// <returns></returns>
        public static string BuildNumberList(int start, int end, int step = 1)
        {
            if (start == end)
            {
                return "[]";
            }
            List<int> list = MathTool.BuildNumberList(start, end);
            return JsonTool.Stringify(list);
        }

        /// <summary>
        /// Supplement the left side of the entire text with a specified string
        /// </summary>
        /// <param name="text"></param>
        /// <param name="length"></param>
        /// <param name="padStr"></param>
        /// <returns></returns>
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
        /// Supplement the right side of the entire text with a specified string
        /// </summary>
        /// <param name="text"></param>
        /// <param name="length"></param>
        /// <param name="padStr"></param>
        /// <returns></returns>
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
        /// Check if it is purely numerical (include int, double, single, decimal...)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsPureNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            bool decimalPointFound = false;

            foreach (char c in input)
            {
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

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check if it is pure Chinese characters
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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
        /// Check if it is Chinese character
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool IsChineseCharacter(char c)
        {
            return (c >= '\u4E00' && c <= '\u9FA5') || // Commonly used Chinese characters
                   (c >= '\u3400' && c <= '\u4DBF') || // CJK Unified Ideographs Extension A
                   (c >= '\uF900' && c <= '\uFAD9');   // Compatible with Chinese characters
        }
        /// <summary>
        /// Check if it is Chinese CJK Unified Ideographs Extension 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
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
        /// check if it is a common mobile string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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
        /// check if it is a email string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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
        /// check if it is a valid url string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsValidUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }
            return Regex.IsMatch(input, _urlPattern);
        }

        /// <summary>
        /// Generate a string of random numbers of a specified length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
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
        /// Generate a string of random letters of specified length
        /// </summary>
        /// <param name="length"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
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
        /// Generate a string of random letters or numbers of specified length
        /// </summary>
        /// <param name="length"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string? IfBlank(string? src, string? defaultValue)
        {
            if (string.IsNullOrWhiteSpace(src))
            {
                return defaultValue;
            }
            return src;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string? IfEmpty(string? src, string? defaultValue)
        {
            if (string.IsNullOrEmpty(src))
            {
                return defaultValue;
            }
            return src;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        /// <param name="yes"></param>
        /// <param name="no"></param>
        /// <returns></returns>
        public static string? If(bool b, string? yes, string? no = "")
        {
            return b ? yes : no;
        }

    }
}
