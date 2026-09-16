using Rampage.Reflection;
using System;
using System.Reflection;
using System.Text;
using System.Web;

namespace Rampage.Utilities
{
    /// <summary>
    /// Utilities of url path、file path, etc...
    /// </summary>
    public class PathTool
    {

        /// <summary>
        /// Url Decode (use UTF8 default)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UrlDecode(string input)
        {
            input = HttpUtility.UrlDecode(input, Encoding.UTF8);
            return input;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string UrlDecode(string input, Encoding encoding)
        {
            input = HttpUtility.UrlDecode(input, encoding);
            return input;
        }

        /// <summary>
        /// Url Encode (use UTF8 default)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UrlEncode(string input)
        {
            input = HttpUtility.UrlEncode(input, Encoding.UTF8);
            return input;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string UrlEncode(string input, Encoding encoding)
        {
            input = HttpUtility.UrlEncode(input, encoding);
            return input;
        }

        /// <summary>
        /// get query string Dictionary from url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
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
        /// get query string Dictionary from url query string
        /// </summary>
        /// <param name="qs">query string</param>
        /// <returns></returns>
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
        /// get url query string from a Dictionary Data (exclude question mark(?))
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
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
        /// get url query string from a class instance (exclude question mark(?))
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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
        /// set url query item
        /// </summary>
        /// <param name="url">absolute url or relative url</param>
        /// <param name="name">querystring name</param>
        /// <param name="value">querystring value</param>
        /// <param name="encode">is to urlencode value</param>
        /// <param name="overwrite">overwrite value if exists</param>
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
        /// remove query string item from url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="name"></param>
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
        /// remove query string item from url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="names"></param>
        /// <returns></returns>
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
        /// get root url from Absolute url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
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
        /// merge some url parts
        /// </summary>
        /// <param name="rootPart">root url</param>
        /// <param name="parts">url parts</param>
        /// <returns></returns>
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
        /// merge url with query and new query string
        /// </summary>
        /// <param name="urlAndQuery"></param>
        /// <param name="query"></param>
        /// <returns></returns>
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
        /// get file extension without dot prefix
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName).TrimStart('.');
        }

    }
}
