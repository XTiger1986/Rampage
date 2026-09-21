using System.Text.Json;
using System.Text.Json.Serialization;
using Rampage.Model;

namespace Rampage.Idempotency
{
    /// <summary>
    /// 幂等响应序列化辅助（与存储/MVC 解耦，便于单测）。
    /// / Serialization helper for idempotent responses (decoupled from the store and MVC for testability).
    /// </summary>
    public static class IdempotencyResponseHelper
    {
        /// <summary>历史版本仅缓存 "1" 标记，无完整正文。/ Legacy versions cached only the "1" marker without a full body.</summary>
        public const string LegacyMarker = "1";

        /// <summary>并发占位标记：表示同 Key 请求正在处理中，尚无结果。/ Concurrency placeholder: a same-key request is in flight with no result yet.</summary>
        public const string ProcessingMarker = "__RAMPAGE_IDEMPOTENT_PROCESSING__";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null
        };

        /// <summary>
        /// 成功（Code=0）时序列化为与 API 一致的 PascalCase JSON；否则返回 null。
        /// / Serializes to PascalCase JSON consistent with the API when Code=0; otherwise returns null.
        /// </summary>
        /// <param name="result">业务结果 / the business result</param>
        /// <returns>响应 JSON；非成功或空结果返回 null / the response JSON; null for non-success or empty results</returns>
        public static string? SerializeSuccess(R? result)
        {
            if (result == null || result.Code != 0)
            {
                return null;
            }

            string json = JsonSerializer.Serialize(result, JsonOptions);
            return string.IsNullOrEmpty(json) ? null : json;
        }

        /// <summary>是否为可回放的缓存值（含旧标记）。/ Whether the cached value is replayable (including the legacy marker).</summary>
        /// <param name="cached">缓存值 / the cached value</param>
        /// <returns>非空白即可回放 / true when the value is not null/whitespace</returns>
        public static bool IsReplayable(string? cached)
        {
            return !string.IsNullOrWhiteSpace(cached);
        }

        /// <summary>是否为旧版仅标记、无完整正文的缓存。/ Whether the cache holds only the legacy marker without a full body.</summary>
        /// <param name="cached">缓存值 / the cached value</param>
        /// <returns>等于 LegacyMarker 时为 true / true when equal to LegacyMarker</returns>
        public static bool IsLegacyMarker(string? cached)
        {
            return cached == LegacyMarker;
        }

        /// <summary>是否为并发占位标记（处理中、无结果）。/ Whether the value is the concurrency placeholder (in flight, no result yet).</summary>
        /// <param name="cached">缓存值 / the cached value</param>
        /// <returns>等于 ProcessingMarker 时为 true / true when equal to ProcessingMarker</returns>
        public static bool IsProcessingMarker(string? cached)
        {
            return cached == ProcessingMarker;
        }
    }
}
