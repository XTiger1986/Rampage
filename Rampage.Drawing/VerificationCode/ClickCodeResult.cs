using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 点选验证码单个目标（单字或单词）的可点击区域（图片像素坐标系包围盒） /
    /// The clickable region of one target (a character or a word) in a click captcha (bounding box in image-pixel coordinates).
    /// </summary>
    /// <remarks>
    ///  Description:    点选验证码单项目标区域
    ///                  绘制时测量字形包围盒并换算为图片绝对坐标；Contains 按“矩形外扩 tolerance”判定命中，
    ///                  以吸收前端展示缩放与点击抖动带来的偏差。中文点选的目标为单个汉字，英文点选为整个单词。
    ///  Description:    One target region of a click captcha. The glyph bounds measured at draw time are
    ///  converted to absolute image coordinates; Contains inflates the rectangle by tolerance to absorb
    ///  frontend display scaling and click jitter. Chinese targets are single characters; word targets are whole words.
    ///
    /// </remarks>
    public sealed class ClickCodeCharRegion
    {
        /// <summary>
        /// 初始化单项目标区域 / Initializes a target region.
        /// </summary>
        /// <param name="text">该区域对应的目标文本（单字或单词） / The target text this region belongs to (a character or a word).</param>
        /// <param name="bounds">图片像素坐标系的包围盒 / The bounding box in image-pixel coordinates.</param>
        public ClickCodeCharRegion(string text, SKRect bounds)
        {
            this.Text = text;
            this.Bounds = bounds;
        }

        /// <summary>
        /// 该区域对应的目标文本（中文为单个汉字，英文为整个单词） /
        /// The target text of this region (a single Chinese character, or a whole word).
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// 图片像素坐标系的包围盒（左/上/右/下） / The bounding box in image-pixel coordinates (left/top/right/bottom).
        /// </summary>
        public SKRect Bounds { get; }

        /// <summary>
        /// 判断点是否命中本区域（矩形向四周外扩 tolerance 后做包含判定） /
        /// Determines whether the point hits this region (the rectangle is inflated by tolerance on all sides).
        /// </summary>
        /// <param name="x">点的横坐标（图片像素坐标系） / The point's x coordinate (image-pixel space).</param>
        /// <param name="y">点的纵坐标（图片像素坐标系） / The point's y coordinate (image-pixel space).</param>
        /// <param name="tolerance">命中容差，单位像素（0 表示严格按包围盒） / Hit tolerance in pixels (0 = strict bounds).</param>
        /// <returns>命中返回 true / true when the point is inside the inflated bounds.</returns>
        public bool Contains(float x, float y, float tolerance = 0)
        {
            SKRect bounds = this.Bounds;
            return x >= bounds.Left - tolerance
                && x <= bounds.Right + tolerance
                && y >= bounds.Top - tolerance
                && y <= bounds.Bottom + tolerance;
        }
    }

    /// <summary>
    /// 点选验证码的验证结果（目标字串 + 各字包围盒 + 校验辅助） /
    /// The verification result of a click captcha (target code plus per-character regions and a verify helper).
    /// </summary>
    /// <remarks>
    ///  Description:    点选验证码验证结果
    ///                  生成图片时同步产出；使用方将其序列化（ToJson）存入原有验证码存储位（替代普通验证码的
    ///                  字符串答案），校验时反序列化后调用 Verify 与前端提交的点击坐标点数组比对。
    ///  Description:    Produced together with the image. Consumers serialize it (ToJson) into their existing
    ///  captcha store (replacing the plain code string) and, at validation time, deserialize and call Verify
    ///  against the click points submitted by the frontend.
    ///
    /// </remarks>
    public sealed class ClickCodeResult
    {
        /// <summary>
        /// 初始化验证结果（Regions 与 Code 的各项目标顺序一一对应） /
        /// Initializes the result (Regions align one-to-one with the targets of Code, in order).
        /// </summary>
        /// <param name="code">目标串（提示条顺序；中文为连写单字，英文为空格分隔的单词） /
        /// The target code in prompt-bar order (joined characters for Chinese, space-separated words for English).</param>
        /// <param name="regions">各目标项目的可点击区域，顺序与 code 一致 / The clickable regions, ordered to match code.</param>
        public ClickCodeResult(string code, IReadOnlyList<ClickCodeCharRegion> regions)
        {
            this.Code = code;
            this.Regions = regions;
        }

        /// <summary>
        /// 目标串（提示条顺序；中文为连写互异单字，英文为空格分隔互异单词） /
        /// The target code in prompt-bar order (distinct joined characters for Chinese, space-separated distinct words for English).
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// 各目标项目的可点击区域（顺序与 <see cref="Code"/> 一致） /
        /// The clickable regions of the targets (ordered to match <see cref="Code"/>).
        /// </summary>
        public IReadOnlyList<ClickCodeCharRegion> Regions { get; }

        /// <summary>
        /// 校验前端提交的点击坐标点数组：数量须与目标数一致，且第 i 个点（含容差）命中第 i 个目标的区域 /
        /// Verifies the click points submitted by the frontend: the count must match the target count and the
        /// i-th point (with tolerance) must hit the i-th region.
        /// </summary>
        /// <remarks>中文版点击单字区域，英文版点击整词区域，判定逻辑一致 / The containment check is identical for characters and words.</remarks>
        /// <param name="clickedPoints">按点击顺序排列的坐标点数组（图片像素坐标系） /
        /// The click points in click order (image-pixel coordinates).</param>
        /// <param name="tolerance">命中容差，单位像素，默认 4（吸收缩放/抖动偏差，可传 0 严格判定） /
        /// Hit tolerance in pixels, 4 by default (absorbs scaling/jitter; pass 0 for strict bounds).</param>
        /// <returns>全部按序命中返回 true，否则 false / true when every point hits its region in order; otherwise false.</returns>
        public bool Verify(IReadOnlyList<SKPoint>? clickedPoints, float tolerance = 4)
        {
            if (clickedPoints == null || clickedPoints.Count != this.Regions.Count)
            {
                return false;
            }

            for (int i = 0; i < this.Regions.Count; i++)
            {
                if (!this.Regions[i].Contains(clickedPoints[i].X, clickedPoints[i].Y, tolerance))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 序列化为 JSON（供使用方存入验证码存储，替代普通验证码的字符串答案） /
        /// Serializes to JSON for storage in the consumer's captcha store (replacing the plain code string).
        /// </summary>
        /// <returns>JSON 字符串 / The JSON string.</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JsonOptionsLazy.Value);
        }

        /// <summary>
        /// 从 JSON 反序列化（与 <see cref="ToJson"/> 配对） / Deserializes from JSON (paired with <see cref="ToJson"/>).
        /// </summary>
        /// <param name="json">由 ToJson 生成的 JSON 字符串 / The JSON string produced by ToJson.</param>
        /// <returns>反序列化结果；输入无效返回 null / The deserialized result, or null when the input is invalid.</returns>
        public static ClickCodeResult? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ClickCodeResult>(json, JsonOptionsLazy.Value);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// JSON 序列化选项（注册自定义转换器；中文等非 ASCII 字符保持明文，便于存储与排查） /
        /// The JSON serialization options (registers the custom converter; non-ASCII text stays literal for readable storage).
        /// </summary>
        /// <remarks>结果 JSON 仅用于服务端存储与回读，不进入 HTML 上下文，放宽转义无注入风险 /
        /// The JSON lives only in server-side storage and round-trips; it never enters HTML context, so relaxed escaping is safe.</remarks>
        private static readonly Lazy<JsonSerializerOptions> JsonOptionsLazy = new Lazy<JsonSerializerOptions>(() =>
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            options.Converters.Add(new ClickCodeResultJsonConverter());
            return options;
        });
    }

    /// <summary>
    /// ClickCodeResult 的 JSON 转换器（SKRect/SKPoint 为非公开可构造结构，手工映射） /
    /// JSON converter for ClickCodeResult (SKRect/SKPoint need manual mapping for stable shapes).
    /// </summary>
    /// <remarks>
    ///  Description:    点选验证码结果 JSON 转换器
    ///                  输出形如 {"code":"笆崩", "regions":[{"text":"笆","bounds":{"left":..,"top":..,"right":..,"bottom":..}}]}，
    ///                  字段名固定、小写，便于跨语言使用方解析。
    ///  Description:    Emits {"code":"..", "regions":[{"text":"..","bounds":{"left":..,"top":..,"right":..,"bottom":..}}]}
    ///  with fixed lowercase names so non-.NET consumers can parse it easily.
    ///
    /// </remarks>
    public sealed class ClickCodeResultJsonConverter : System.Text.Json.Serialization.JsonConverter<ClickCodeResult>
    {
        /// <summary>
        /// 读取 JSON 并还原 ClickCodeResult / Reads JSON and restores a ClickCodeResult.
        /// </summary>
        /// <param name="reader">JSON 读取器 / The JSON reader.</param>
        /// <param name="objectType">目标类型 / The target type.</param>
        /// <param name="options">序列化选项 / The serializer options.</param>
        /// <returns>还原的实例 / The restored instance.</returns>
        /// <exception cref="JsonException">JSON 结构不符合约定时抛出 / Thrown when the JSON shape is unexpected.</exception>
        public override ClickCodeResult Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement root = document.RootElement;

            string code = root.GetProperty("code").GetString() ?? string.Empty;
            List<ClickCodeCharRegion> regions = new List<ClickCodeCharRegion>();
            foreach (JsonElement element in root.GetProperty("regions").EnumerateArray())
            {
                string text = element.GetProperty("text").GetString() ?? string.Empty;
                JsonElement bounds = element.GetProperty("bounds");
                regions.Add(new ClickCodeCharRegion(
                    text,
                    new SKRect(
                        bounds.GetProperty("left").GetSingle(),
                        bounds.GetProperty("top").GetSingle(),
                        bounds.GetProperty("right").GetSingle(),
                        bounds.GetProperty("bottom").GetSingle())));
            }

            return new ClickCodeResult(code, regions);
        }

        /// <summary>
        /// 写出 ClickCodeResult 为 JSON / Writes a ClickCodeResult as JSON.
        /// </summary>
        /// <param name="writer">JSON 写入器 / The JSON writer.</param>
        /// <param name="value">待写出的实例 / The instance to write.</param>
        /// <param name="options">序列化选项 / The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, ClickCodeResult value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("code", value.Code);
            writer.WriteStartArray("regions");
            foreach (ClickCodeCharRegion region in value.Regions)
            {
                writer.WriteStartObject();
                writer.WriteString("text", region.Text);
                writer.WriteStartObject("bounds");
                writer.WriteNumber("left", region.Bounds.Left);
                writer.WriteNumber("top", region.Bounds.Top);
                writer.WriteNumber("right", region.Bounds.Right);
                writer.WriteNumber("bottom", region.Bounds.Bottom);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
