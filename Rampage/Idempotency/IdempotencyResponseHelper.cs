using System.Text.Json;
using System.Text.Json.Serialization;
using Rampage.Model;

namespace Rampage.Idempotency
{
    /// <summary>
    /// 幂等响应序列化辅助（与存储/MVC 解耦，便于单测）。
    /// </summary>
    public static class IdempotencyResponseHelper
    {
        /// <summary>历史版本仅缓存 "1" 标记，无完整正文。</summary>
        public const string LegacyMarker = "1";

        /// <summary>并发占位标记：表示同 Key 请求正在处理中，尚无结果。</summary>
        public const string ProcessingMarker = "__RAMPAGE_IDEMPOTENT_PROCESSING__";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null
        };

        /// <summary>
        /// 成功（Code=0）时序列化为与 API 一致的 PascalCase JSON；否则返回 null。
        /// </summary>
        public static string? SerializeSuccess(R? result)
        {
            if (result == null || result.Code != 0)
            {
                return null;
            }

            string json = JsonSerializer.Serialize(result, JsonOptions);
            return string.IsNullOrEmpty(json) ? null : json;
        }

        /// <summary>是否为可回放的缓存值（含旧标记）。</summary>
        public static bool IsReplayable(string? cached)
        {
            return !string.IsNullOrWhiteSpace(cached);
        }

        /// <summary>是否为旧版仅标记、无完整正文的缓存。</summary>
        public static bool IsLegacyMarker(string? cached)
        {
            return cached == LegacyMarker;
        }

        /// <summary>是否为并发占位标记（处理中、无结果）。</summary>
        public static bool IsProcessingMarker(string? cached)
        {
            return cached == ProcessingMarker;
        }
    }
}
