using System;
using System.Text.Json;

namespace Rampage.Json
{
    /// <summary>
    /// JSON 工具：System.Text.Json 的薄封装。
    /// / JSON helper: a thin wrapper over System.Text.Json.
    /// </summary>
    public class JsonTool
    {

        /// <summary>
        /// 序列化为 JSON 字符串；obj 为 null 时返回 string.Empty。
        /// / Serialize to a JSON string; returns string.Empty when obj is null.
        /// </summary>
        /// <param name="obj">待序列化对象 / object to serialize</param>
        /// <returns>JSON 字符串；obj 为 null 时为空串 / the JSON string; empty when obj is null</returns>
        public static string Stringify(object? obj)
        {
            if (obj == null) return string.Empty;
            return JsonSerializer.Serialize(obj);
        }

        /// <summary>
        /// 按自定义序列化选项序列化为 JSON 字符串；obj 为 null 时返回 string.Empty。
        /// / Serialize to a JSON string with custom options; returns string.Empty when obj is null.
        /// </summary>
        /// <param name="obj">待序列化对象 / object to serialize</param>
        /// <param name="options">序列化选项 / serializer options</param>
        /// <returns>JSON 字符串；obj 为 null 时为空串 / the JSON string; empty when obj is null</returns>
        public static string Stringify(object? obj, JsonSerializerOptions options)
        {
            if (obj == null) return string.Empty;
            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// 反序列化：json 为 null/空串直接返回 default；任何解析异常被吞掉并同样返回 default。
        /// / Deserialize: null/empty input returns default immediately; any parse exception is swallowed and also returns default.
        /// </summary>
        /// <remarks>
        /// 解析失败被静默吞掉且返回 default(T)——调用方无法区分「JSON 合法但值就是 null/默认值」与「JSON 格式错误」，
        /// 对正确性敏感的数据请勿依赖本方法判断有效性。
        /// / Failures are silently swallowed into default(T): callers cannot distinguish "valid JSON whose value is
        /// null/default" from "malformed JSON"; do not rely on this method to validate critical data.
        /// </remarks>
        /// <typeparam name="T">目标类型 / target type</typeparam>
        /// <param name="json">JSON 字符串 / JSON string</param>
        /// <returns>反序列化结果；空输入或解析失败时为 default / deserialized value; default on empty input or parse failure</returns>
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
