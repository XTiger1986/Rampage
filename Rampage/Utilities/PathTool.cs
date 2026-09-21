using Rampage.Reflection;
using System;
using System.Reflection;
using System.Text;
using System.Web;

namespace Rampage.Utilities
{
    /// <summary>
    /// URL 路径、文件路径等工具类 /
    /// Utilities of url path, file path, etc.
    /// </summary>
    public class PathTool
    {

        /// <summary>
        /// URL 解码（默认使用 UTF8）/ Url decode using UTF8 by default
        /// </summary>
        /// <param name="input">待解码的字符串 / The string to decode</param>
        /// <returns>解码后的字符串；输入为 null 时返回 null / The decoded string; returns null when input is null</returns>
        public static string UrlDecode(string input)
        {
            input = HttpUtility.UrlDecode(input, Encoding.UTF8);
            return input;
        }
        /// <summary>
        /// URL 解码（使用指定编码）/ Url decode using the specified encoding
        /// </summary>
        /// <param name="input">待解码的字符串 / The string to decode</param>
        /// <param name="encoding">解码使用的字符编码 / The encoding used to decode</param>
        /// <returns>解码后的字符串；输入为 null 时返回 null / The decoded string; returns null when input is null</returns>
        public static string UrlDecode(string input, Encoding encoding)
        {
            input = HttpUtility.UrlDecode(input, encoding);
            return input;
        }

        /// <summary>
        /// URL 编码（默认使用 UTF8）/ Url encode using UTF8 by default
        /// </summary>
        /// <param name="input">待编码的字符串 / The string to encode</param>
        /// <returns>编码后的字符串；输入为 null 时返回 null / The encoded string; returns null when input is null</returns>
        public static string UrlEncode(string input)
        {
            input = HttpUtility.UrlEncode(input, Encoding.UTF8);
            return input;
        }

        /// <summary>
        /// URL 编码（使用指定编码）/ Url encode using the specified encoding
        /// </summary>
        /// <param name="input">待编码的字符串 / The string to encode</param>
        /// <param name="encoding">编码使用的字符编码 / The encoding used to encode</param>
        /// <returns>编码后的字符串；输入为 null 时返回 null / The encoded string; returns null when input is null</returns>
        public static string UrlEncode(string input, Encoding encoding)
        {
            input = HttpUtility.UrlEncode(input, encoding);
            return input;
        }

        /// <summary>
        /// 从 URL（绝对或相对）解析查询串为字典 /
        /// Parse the query string of a url (absolute or relative) into a dictionary
        /// </summary>
        /// <param name="url">绝对或相对 URL / An absolute or relative url</param>
        /// <returns>查询参数字典（一键多值）；URL 无法解析或无查询串时返回空字典 / A dictionary of query parameters (one key maps to a list of values); an empty dictionary when the url cannot be parsed or has no query</returns>
        /// <remarks>相对 URL 不以 "/" 开头时自动补齐；无 "=" 或值为空的片段会被忽略，同名键的多个值合并进同一列表。/ A leading "/" is added to relative urls automatically; segments without "=" or with an empty value are ignored, and multiple values of the same key are merged into one list.</remarks>
        public static Dictionary<string, List<string>> GetDictionaryFromUrl(string url)
        {
            string qs;
            Uri? uri = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                if (!url.StartsWith("/"))
                {
                    url = string.Concat("/", url);
                }
                if (!Uri.TryCreate(url, UriKind.Relative, out uri))
                {
                    return new Dictionary<string, List<string>>();
                }
                if (url.IndexOf("?") >= 0)
                {
                    qs = url.Substring(url.IndexOf("?"));
                }
                else
                {
                    qs = string.Empty;
                }
            }
            else
            {
                qs = uri.Query;
            }
            return GetDictionaryFromUrlQuery(qs);
        }

        /// <summary>
        /// 从 URL 查询串解析为字典 /
        /// Parse a url query string into a dictionary
        /// </summary>
        /// <param name="qs">查询串，可带可不带前导 "?" / The query string, with or without a leading "?"</param>
        /// <returns>查询参数字典（一键多值）；解析不到有效键值对时返回空字典 / A dictionary of query parameters (one key maps to a list of values); an empty dictionary when no valid key-value pair exists</returns>
        public static Dictionary<string, List<string>> GetDictionaryFromUrlQuery(string qs)
        {
            qs = qs.TrimStart('?');
            Dictionary<string, List<string>> qsDict = new Dictionary<string, List<string>>();
            string[] qsArr = qs.Split('&', StringSplitOptions.RemoveEmptyEntries);
            if (qsArr.Any())
            {
                foreach (string kvString in qsArr)
                {
                    string[] kvArr = kvString.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    if (kvArr.Length > 1)
                    {
                        string key = kvArr[0];
                        string value = kvArr[1];
                        if (string.IsNullOrEmpty(value))
                        {
                            continue;
                        }
                        if (!qsDict.ContainsKey(key))
                        {
                            qsDict.Add(key, new List<string>());
                        }
                        qsDict[key].Add(value);
                    }
                }
            }

            return qsDict;
        }

        /// <summary>
        /// 由字典生成 URL 查询串（不含前导 "?"）/
        /// Build a url query string from a dictionary (without the leading "?")
        /// </summary>
        /// <param name="dict">查询参数字典（一键多值）/ A dictionary of query parameters (one key maps to a list of values)</param>
        /// <returns>查询串；字典为空时返回空字符串 / The query string; an empty string when the dictionary is empty</returns>
        /// <remarks>一键多值会重复输出为 key=v1&amp;key=v2；值不做 URL 编码。/ Multi-value keys are emitted repeatedly as key=v1&amp;key=v2; values are not url-encoded.</remarks>
        public static string GetUrlQueryFromDictionary(IDictionary<string, List<string>> dict)
        {
            string newQs = string.Empty;
            foreach (var kv in dict)
            {
                foreach(string v in kv.Value)
                {
                    newQs += $"{kv.Key}={v}&";
                }
            }
            if (!string.IsNullOrEmpty(newQs))
            {
                newQs = newQs.TrimEnd('&');
            }
            return newQs;
        }

        /// <summary>
        /// 由对象实例的公开属性生成 URL 查询串（不含前导 "?"）/
        /// Build a url query string from the public properties of an object instance (without the leading "?")
        /// </summary>
        /// <param name="obj">模型实例 / The model instance</param>
        /// <returns>查询串；没有任何可用属性值时返回空字符串 / The query string; an empty string when no property yields a value</returns>
        /// <remarks>值为 null 的属性被跳过；属性值以 ToString() 输出且不做 URL 编码。/ Properties whose value is null are skipped; values are emitted via ToString() without url-encoding.</remarks>
        public static string GetUrlQueryFromModel(object obj)
        {
            Type type = obj.GetType();
            List<PropertyInfo> prpList = ReflectionTool.GetPropertyList(type, false);
            StringBuffer sb = new StringBuffer();
            foreach (PropertyInfo prp in prpList)
            {
                object? v = EmitTool.GetPropertyValue(obj, prp.Name);
                if (v != null)
                {
                    sb += prp.Name;
                    sb += "=";
                    sb += v.ToString()!;
                    sb += "&";
                }
            }
            string qs = sb.ToString();
            qs = qs.TrimEnd('&');
            return qs;
        }

        /// <summary>
        /// 设置 URL 中的指定查询参数 /
        /// Set the specified query item on a url
        /// </summary>
        /// <param name="url">绝对或相对 URL / An absolute or relative url</param>
        /// <param name="name">查询参数名 / The query item name</param>
        /// <param name="value">查询参数值 / The query item value</param>
        /// <param name="encode">是否对值做 URL 编码 / Whether to url-encode the value</param>
        /// <param name="overwrite">参数已存在时是否覆盖原值；false 时追加为新值 / Whether to overwrite existing values; when false the new value is appended</param>
        /// <returns>合并后的 URL；URL 无法解析为绝对/相对地址时原样返回 / The merged url; the original url is returned unchanged when it cannot be parsed</returns>
        /// <remarks>encode 仅对新写入的值生效，已有查询值保持原样。/ Encoding applies only to the newly written value; existing query values are kept as-is.</remarks>
        public static string SetUrlQuery(string url, string name, string value, bool encode = true, bool overwrite = true)
        {
            Uri? uri = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                if (!url.StartsWith("/"))
                {
                    url = string.Concat("/", url);
                }
                if (!Uri.TryCreate(url, UriKind.Relative, out uri))
                {
                    return url;
                }
            }
            if (encode)
            {
                value = HttpUtility.UrlEncode(value);
            }

            string urlPath;
            if (url.IndexOf("?") > 0)
            {
                urlPath = url.Substring(0, url.IndexOf("?"));
            }
            else
            {
                urlPath = url;
            }

            Dictionary<string, List<string>> qsDict = GetDictionaryFromUrl(url);

            if (!qsDict.ContainsKey(name))
            {
                qsDict.Add(name, new List<string>());
            }
            if (overwrite)
            {
                qsDict[name].Clear();
            }
            qsDict[name].Add(value);

            string newQs = GetUrlQueryFromDictionary(qsDict);
            if (!string.IsNullOrEmpty(newQs))
            {
                newQs = "?" + newQs;
                return string.Concat(urlPath, newQs);
            }
            else
            {
                return url;
            }
        }

        /// <summary>
        /// 移除 URL 中的指定查询参数 /
        /// Remove the specified query item from a url
        /// </summary>
        /// <param name="url">绝对 URL / An absolute url</param>
        /// <param name="name">查询参数名 / The query item name</param>
        /// <returns>移除后的 URL / The url after removal</returns>
        /// <remarks>要求 url 为绝对 URL（内部直接 new Uri 解析），相对路径会抛出 UriFormatException；当被移除参数是最后一个查询项时实际返回原 URL，查询串不会被清空。/ The url must be absolute (parsed via new Uri internally), otherwise a UriFormatException is thrown; when the removed item is the last remaining query parameter, the original url is returned and the query string is not cleared.</remarks>
        public static string RemoveUrlQuery(string url, string name)
        {
            Uri uri = new Uri(url);
            string urlPath = uri.GetLeftPart(UriPartial.Path);
            Dictionary<string, List<string>> qsDict = GetDictionaryFromUrl(url);
            if (qsDict.ContainsKey(name))
            {
                qsDict.Remove(name);
            }

            if (qsDict.Any())
            {
                string qs = GetUrlQueryFromDictionary(qsDict);

                if (!string.IsNullOrEmpty(qs))
                {
                    return string.Concat(urlPath, "?", qs);
                }
            }
            return url;
        }

        /// <summary>
        /// 移除 URL 中的多个查询参数 /
        /// Remove multiple query items from a url
        /// </summary>
        /// <param name="url">绝对 URL / An absolute url</param>
        /// <param name="names">要移除的参数名列表 / The names of the query items to remove</param>
        /// <returns>移除后的 URL / The url after removal</returns>
        /// <remarks>要求 url 为绝对 URL（内部直接 new Uri 解析），相对路径会抛出 UriFormatException；当移除后不再剩任何查询项时实际返回原 URL，查询串不会被清空。/ The url must be absolute (parsed via new Uri internally), otherwise a UriFormatException is thrown; when no query item remains after removal, the original url is returned and the query string is not cleared.</remarks>
        public static string RemoveUrlQuerys(string url, params string[] names)
        {
            Uri uri = new Uri(url);
            string urlPath = uri.GetLeftPart(UriPartial.Path);
            Dictionary<string, List<string>> qsDict = GetDictionaryFromUrl(url);
            if (names.Any())
            {
                foreach (string qsName in names)
                {
                    if (qsDict.ContainsKey(qsName))
                    {
                        qsDict.Remove(qsName);
                    }
                }
            }

            if (qsDict.Any())
            {
                string qs = GetUrlQueryFromDictionary(qsDict);

                if (!string.IsNullOrEmpty(qs))
                {
                    return string.Concat(urlPath, "?", qs);
                }
            }
            return url;
        }


        /// <summary>
        /// 获取绝对 URL 的根地址（协议+域名+端口）/
        /// Get the root of an absolute url (scheme + host + port)
        /// </summary>
        /// <param name="url">绝对 URL / An absolute url</param>
        /// <returns>根地址；url 不是合法绝对 URL 时返回空字符串 / The root url; an empty string when the url is not a valid absolute url</returns>
        /// <remarks>仅当端口为 80 时省略端口，其余端口（含 https 默认的 443）都会显式拼接。/ The port is omitted only when it is 80; any other port (including the https default 443) is appended explicitly.</remarks>
        public static string GetUrlRoot(string url)
        {
            Uri? uri = null;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                string s = uri.Scheme;
                string host = uri.Host;
                int p = uri.Port;

                if (p == 80)
                {
                    return string.Concat(s, "://", host);
                }
                else
                {
                    return string.Concat(s, "://", host, ":", p);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 拼接 URL 根地址与多个路径段 /
        /// Merge a url root with path parts
        /// </summary>
        /// <param name="rootPart">URL 根地址 / The root url</param>
        /// <param name="parts">路径段 / The url parts</param>
        /// <returns>拼接后的完整 URL；未提供任何段时返回 rootPart / The merged full url; rootPart when no part is given</returns>
        /// <remarks>rootPart 末尾的 "/" 会被去掉；每个不以 "/" 开头的段自动补 "/" 后按字符串直接拼接（绝对 URL 形式的段同样按普通字符串处理）。/ A trailing "/" of rootPart is trimmed; every part not starting with "/" gets one prepended and parts are concatenated as plain strings (even parts written as absolute urls).</remarks>
        public static string CombileUrl(string rootPart, params string[] parts)
        {
            if (!parts.Any())
            {
                return rootPart;
            }

            if (rootPart.EndsWith("/"))
            {
                rootPart = rootPart.TrimEnd('/');
            }

            string fullPath = rootPart;
            foreach (string part in parts)
            {
                string finalPart = part;
                Uri? tryUri = null;
                if (Uri.TryCreate(finalPart, UriKind.Absolute, out tryUri) && tryUri != null)
                {
                    finalPart = tryUri.PathAndQuery;
                }

                if (!part.StartsWith("/"))
                {
                    finalPart = "/" + part;
                }
                fullPath = string.Concat(fullPath, finalPart);
            }

            return fullPath;
        }

        /// <summary>
        /// 合并 URL（可含查询串）与新的查询串 /
        /// Merge a url (optionally with a query) and a new query string
        /// </summary>
        /// <param name="urlAndQuery">原 URL，可带查询串 / The original url, optionally with a query string</param>
        /// <param name="query">要并入的查询串（可带可不带前导 "?"）/ The query to append (with or without a leading "?")</param>
        /// <param name="overwrite">同名参数是否覆盖原值；false 时追加为新值 / Whether to overwrite existing items of the same name; when false the new values are appended</param>
        /// <returns>合并后的 URL；query 为空白时原样返回 urlAndQuery / The merged url; urlAndQuery unchanged when query is blank</returns>
        public static string CombileUrlQuery(string urlAndQuery, string query, bool overwrite = true)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return urlAndQuery;
            }
            string urlPath;
            if (urlAndQuery.IndexOf("?") > 0)
            {
                urlPath = urlAndQuery.Substring(0, urlAndQuery.IndexOf("?"));
            }
            else
            {
                urlPath = urlAndQuery;
            }

            Dictionary<string, List<string>> dict = GetDictionaryFromUrl(urlAndQuery);
            Dictionary<string, List<string>> appendDict = GetDictionaryFromUrlQuery(query);
            foreach (var kv in appendDict)
            {
                if (!dict.ContainsKey(kv.Key))
                {
                    dict.Add(kv.Key, new List<string>());
                }
                if (overwrite)
                {
                    dict[kv.Key].Clear();
                }
                dict[kv.Key].AddRange(kv.Value);
            }

            string newQs = GetUrlQueryFromDictionary(dict);
            if (!string.IsNullOrEmpty(newQs))
            {
                newQs = "?" + newQs;
                return string.Concat(urlPath, newQs);
            }
            else
            {
                return urlAndQuery;
            }
        }

        /// <summary>
        /// 拼接本地文件路径（Windows 风格）/ Combine local file path parts (Windows style)
        /// </summary>
        /// <param name="parts">路径段 / The path parts</param>
        /// <returns>拼接后的路径；未提供任何段时返回空字符串 / The combined path; an empty string when no part is given</returns>
        /// <remarks>所有 "/" 先替换为 "\\"，除第一段外其余段开头的 "\\" 会被去掉，最后交给 Path.Combine 处理。/ All "/" are replaced with "\\" first; a leading "\\" is trimmed from every part except the first, then the parts are passed to Path.Combine.</remarks>
        public static string CombilePath(params string[] parts)
        {
            if (!parts.Any())
            {
                return string.Empty;
            }

            string[] handledParts = new string[parts.Length];
            for(int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                part = part.Replace("/", "\\");

                if (i > 0)
                {
                    part = part.TrimStart('\\');
                }
                handledParts[i] = part;
            }

            return Path.Combine(handledParts);
        }

        /// <summary>
        /// 获取文件扩展名（不含前导 "."）/
        /// Get the file extension without the leading dot
        /// </summary>
        /// <param name="fileName">文件名 / The file name</param>
        /// <returns>不含 "." 的扩展名；文件名没有扩展名时返回空字符串 / The extension without the dot; an empty string when the file name has no extension</returns>
        public static string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName).TrimStart('.');
        }

    }
}
