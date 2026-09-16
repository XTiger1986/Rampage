using System;
using System.Text.Json;

namespace Rampage.Json
{
    /// <summary>
    /// 
    /// </summary>
    public class JsonTool
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string Stringify(object? obj)
        {
            if (obj == null) return string.Empty;
            return JsonSerializer.Serialize(obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string Stringify(object? obj, JsonSerializerOptions options)
        {
            if (obj == null) return string.Empty;
            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T? Parse<T>(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }

    }
}
