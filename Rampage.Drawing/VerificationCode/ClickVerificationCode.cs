using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 点选验证码 - 依次点击目标（默认中文单字，可派生英文单词等） /
    /// Click captcha: click the targets in the prompted order (Chinese characters by default; derive for words etc.).
    /// </summary>
    /// <remarks>
    ///  Description:    点选验证码（中文单字默认实现）
    ///                  从内置常用汉字表不重复抽取目标字与干扰字，背景按“图库 → 算法绘制（渐变+曲线+
    ///                  噪点干扰）”降级合成，目标与干扰项经矩形碰撞检测散布在提示条下方，提示条
    ///                  “请依次点击：X X X”直接绘制在图片顶部，整图编码为单个 PNG。
    ///                  取材（PickSeeds）与提示前缀（PromptPrefix）为虚点：英文单词模式由
    ///                  WordClickVerificationCode 派生实现；验证结果为各目标项目的可点击坐标范围
    ///                  （ClickCodeResult），前端按点击顺序提交坐标点数组，由 ClickCodeResult.Verify 比对。
    ///  Description:    Distinct targets and distractors are drawn from the built-in common Chinese pool. The
    ///  background falls back from a configured image library to an algorithmic draw (gradient + curves +
    ///  dot/line interference). Targets and distractors are scattered below the prompt bar after rectangle
    ///  collision checks, and the prompt bar is drawn into the top of the same PNG. Seeding (PickSeeds) and
    ///  the prompt prefix (PromptPrefix) are virtual: WordClickVerificationCode derives the English word mode.
    ///  The result carries each target's clickable region (ClickCodeResult); consumers store it serialized and
    ///  match frontend click points in order via ClickCodeResult.Verify.
    ///
    /// </remarks>
    public class ClickVerificationCode : ClickVerificationCodeBase
    {

        /// <summary>
        /// 获取样式名称：点选文字 / Gets the style name: click-the-characters.
        /// </summary>
        public override string Name
        {
            get { return "点选文字"; }
        }

        /// <summary>
        /// 生成点选验证码图片与坐标范围验证结果 /
        /// Generates the click captcha image together with its region-based verification result.
        /// </summary>
        /// <remarks>
        /// 流程：可选随机词数组合 → 不重复取材（目标 + 干扰）→ 字号自适应矩形碰撞排版 → 背景降级合成 →
        /// 阴影+彩色绘制文字 → 顶部提示条 → 记录验证结果 → PNG 编码（精确长度字节）。
        /// 字体经 SKFontManager.MatchCharacter 匹配含中文字形的系统字体；测量与绘制共用同一
        /// SKPaint（Typeface + TextSize），保证包围盒与实际字形一致。
        /// Flow: optional random count mix → pick distinct targets and distractors → adaptive-size collision-checked
        /// layout → compose the background with fallbacks → draw glyphs (shadow + color) → prompt bar → record the
        /// result → PNG encode (exact-length bytes). A CJK-capable system font is matched via
        /// SKFontManager.MatchCharacter; measuring and drawing share one SKPaint so bounds match the rendered glyphs.
        /// </remarks>
        /// <param name="result">输出：验证结果（目标串 + 各目标项目可点击区域，按提示顺序） /
        /// Output: the verification result (the target code plus each target's clickable region, in prompt order).</param>
        /// <returns>PNG 图片字节数组（精确长度，无冗余字节） / The PNG image bytes (exact length, no trailing slack).</returns>
        /// <exception cref="ArgumentOutOfRangeException">目标数小于 1 或干扰数小于 0 时抛出 /
        /// Thrown when TargetCount is below 1 or DistractorCount is below 0.</exception>
        /// <exception cref="InvalidOperationException">画布过小、降至最小字号仍无法无重叠排布全部目标时抛出 /
        /// Thrown when the canvas is too small to lay all targets out without overlap even at the minimum font size.</exception>
        public override byte[] CreateImage(out ClickCodeResult result)
        {
            if (this.TargetCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(this.TargetCount), this.TargetCount, "目标数必须至少为 1 / TargetCount must be at least 1.");
            }
            if (this.DistractorCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(this.DistractorCount), this.DistractorCount, "干扰数不能为负 / DistractorCount must not be negative.");
            }

            Random random = Random.Shared;

            // 0. 随机目标/干扰组合（默认关闭；英文词模式开启，随机 2+2 或 3+1，总数恒为 4） /
            //    Optional random count mix (off by default; the word mode enables it): 2+2 or 3+1, four items total.
            int targetCount = this.TargetCount;
            int distractorCount = this.DistractorCount;
            if (this.RandomizeCounts)
            {
                if (random.Next(2) == 0)
                {
                    targetCount = 2;
                    distractorCount = 2;
                }
                else
                {
                    targetCount = 3;
                    distractorCount = 1;
                }
            }

            // 1. 不重复取材：目标与干扰互异，避免点击歧义 /
            //    Distinct targets and distractors avoid click ambiguity (virtual: characters by default, words in the word mode).
            List<string> targets = this.PickSeeds(targetCount, new HashSet<string>(StringComparer.Ordinal), random);
            HashSet<string> used = new HashSet<string>(targets, StringComparer.Ordinal);
            List<string> distractors = distractorCount > 0
                ? this.PickSeeds(distractorCount, used, random)
                : new List<string>();

            using SKTypeface typeface = SKFontManager.Default.MatchCharacter('时');

            // 2. 字号自适应排版：从设定字号逐级降档重试，首轮成功则字号不变（默认参数恒首轮成功） /
            //    Adaptive-size layout: step down on failure; the first success keeps the configured size
            //    (default parameters always succeed on the first round).
            int minFontSize = Math.Min(MinimumLayoutFontSize, this.FontSize);
            List<PlacedChar> placed;
            int layoutFontSize = this.FontSize;
            while (true)
            {
                using (SKPaint layoutPaint = this.NewGlyphPaint(typeface, layoutFontSize))
                {
                    try
                    {
                        placed = this.LayoutChars(targets, distractors, layoutPaint, random);
                        break;
                    }
                    catch (InvalidOperationException) when (layoutFontSize > minFontSize)
                    {
                        layoutFontSize = Math.Max(minFontSize, layoutFontSize - FontSizeStep);
                    }
                }
            }

            using (SKBitmap bitmap = new SKBitmap(this.ImageWidth, this.ImageHeight))
            {
                using (SKCanvas canvas = new SKCanvas(bitmap))
                {
                    // 3. 背景：图库 → 算法绘制自动降级 / Background: image library, then algorithmic fallback.
                    this.DrawBackground(canvas, random);

                    using (SKPaint textPaint = this.NewGlyphPaint(typeface, layoutFontSize))
                    {
                        // 4. 绘制目标与干扰项：先深色偏移阴影保证任意背景可读，再画彩色主体 /
                        //    Draw targets and distractors: a dark offset shadow first for readability on any
                        //    background, then the colored glyph.
                        foreach (PlacedChar item in placed)
                        {
                            textPaint.Color = this.ShadowColor;
                            canvas.DrawText(item.Text, item.Origin.X + 2, item.Origin.Y + 2, textPaint);
                            textPaint.Color = this.DrawColors[random.Next(this.DrawColors.Length)];
                            canvas.DrawText(item.Text, item.Origin.X, item.Origin.Y, textPaint);
                        }
                    }

                    // 5. 顶部提示条（覆盖在背景之上） / The prompt bar drawn over the background's top strip.
                    this.DrawHeader(canvas, targets, typeface);

                    canvas.Flush();
                }

                // 6. 记录验证结果并编码 PNG / Record the result and encode the PNG.
                List<ClickCodeCharRegion> regions = new List<ClickCodeCharRegion>(targets.Count);
                for (int i = 0; i < targets.Count; i++)
                {
                    regions.Add(new ClickCodeCharRegion(targets[i], placed[i].Bounds));
                }
                result = new ClickCodeResult(string.Concat(targets), regions);

                using (SKImage image = SKImage.FromBitmap(bitmap))
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
            }
        }

        /// <summary>
        /// 字号自适应降档的最小字号（默认 16；设定字号更小时以设定字号为下限） /
        /// The floor the adaptive layout steps down to (16 by default; a smaller configured size wins).
        /// </summary>
        protected const int MinimumLayoutFontSize = 16;

        /// <summary>
        /// 字号自适应降档的步长（默认 4） / The step the adaptive layout shrinks by (4 by default).
        /// </summary>
        protected const int FontSizeStep = 4;

        /// <summary>
        /// 创建文字绘制/测量共用的画笔（同一 paint 状态保证包围盒与实际字形一致） /
        /// Creates the paint shared by glyph measuring and drawing (one state keeps bounds aligned with rendering).
        /// </summary>
        /// <param name="typeface">含中文字形的字体面 / The typeface holding CJK glyphs.</param>
        /// <param name="fontSize">字号，单位像素 / The font size in pixels.</param>
        /// <returns>配置好的画笔 / The configured paint.</returns>
        private SKPaint NewGlyphPaint(SKTypeface typeface, int fontSize)
        {
            return new SKPaint
            {
                Typeface = typeface,
                TextSize = fontSize,
                IsAntialias = true
            };
        }

        /// <summary>
        /// 图内提示条的前缀文案（默认“请依次点击：”，后接以空格分隔的目标；使用方可按需改写，如英文词模式） /
        /// The prompt-bar prefix (default "请依次点击：", followed by the space-joined targets; settable per instance,
        /// e.g. by the English word mode).
        /// </summary>
        public virtual string PromptPrefix
        {
            get { return promptPrefix; }
            set { promptPrefix = value ?? string.Empty; }
        }

        /// <summary>
        /// 提示前缀的后备存储 / The backing store of the prompt prefix.
        /// </summary>
        string promptPrefix = "请依次点击：";

        /// <summary>
        /// 从目标池不重复抽取指定个数的目标（可排除已用项；默认实现抽取单个汉字） /
        /// Picks the requested number of distinct targets from the pool (already-used ones can be excluded;
        /// the default implementation picks single Chinese characters).
        /// </summary>
        /// <param name="count">要抽取的个数（调用方保证 ≥ 1） / How many to pick (the caller guarantees ≥ 1).</param>
        /// <param name="excluded">须排除的已用项集合 / The set of items to exclude.</param>
        /// <param name="random">随机源 / The random source.</param>
        /// <returns>抽中的目标列表 / The picked targets.</returns>
        /// <exception cref="InvalidOperationException">剩余可用目标不足时抛出 / Thrown when the pool has too few usable targets left.</exception>
        protected virtual List<string> PickSeeds(int count, HashSet<string> excluded, Random random)
        {
            string[] candidates = VerificationCharPool.CommonChineseChars.Split(',');
            List<string> picked = new List<string>(count);
            int attempts = 0;
            int maxAttempts = candidates.Length * 20;

            while (picked.Count < count)
            {
                if (attempts++ > maxAttempts)
                {
                    throw new InvalidOperationException(
                        $"剩余可用目标不足以抽取 {count} 个不重复项 / Not enough distinct targets left to pick {count}.");
                }

                string candidate = candidates[random.Next(candidates.Length)];
                if (excluded.Contains(candidate) || picked.Contains(candidate))
                {
                    continue;
                }
                picked.Add(candidate);
            }

            return picked;
        }

        #region 属性

        int targetCount = 3;
        /// <summary>
        /// 目标个数（提示条与校验目标，默认 3，须至少为 1；中文语境为目标字数，英文词模式为目标词数） /
        /// The number of targets (the prompt and the verification targets; 3 by default, at least 1;
        /// character count for Chinese, word count for the word mode).
        /// </summary>
        public int TargetCount
        {
            get { return targetCount; }
            set { targetCount = value; }
        }

        int distractorCount = 2;
        /// <summary>
        /// 干扰项个数（不参与校验、纯迷惑，默认 2，可为 0 关闭） /
        /// The number of distractors (decoys excluded from verification; 2 by default, 0 disables them).
        /// </summary>
        public int DistractorCount
        {
            get { return distractorCount; }
            set { distractorCount = value; }
        }

        bool randomizeCounts = false;
        /// <summary>
        /// 是否随机化目标/干扰组合：开启后每次生成在“2 目标+2 干扰”与“3 目标+1 干扰”间随机（总数恒为 4），
        /// 覆盖 TargetCount/DistractorCount 设置（默认 false，保持固定配置） /
        /// Whether to randomize the target/distractor mix: when on, each generation randomly uses
        /// "2 targets + 2 distractors" or "3 targets + 1 distractor" (four items total), overriding
        /// TargetCount/DistractorCount (false by default, fixed configuration kept).
        /// </summary>
        public bool RandomizeCounts
        {
            get { return randomizeCounts; }
            set { randomizeCounts = value; }
        }

        int imageWidth = 320;
        /// <summary>
        /// 图片宽度，单位像素（默认 320） / The image width in pixels (320 by default).
        /// </summary>
        public int ImageWidth
        {
            get { return imageWidth; }
            set { imageWidth = value; }
        }

        int imageHeight = 160;
        /// <summary>
        /// 图片高度，单位像素（默认 160） / The image height in pixels (160 by default).
        /// </summary>
        public int ImageHeight
        {
            get { return imageHeight; }
            set { imageHeight = value; }
        }

        int headerHeight = 36;
        /// <summary>
        /// 顶部提示条高度，单位像素（默认 36） / The prompt bar height in pixels (36 by default).
        /// </summary>
        public int HeaderHeight
        {
            get { return headerHeight; }
            set { headerHeight = value; }
        }

        int fontSize = 34;
        /// <summary>
        /// 目标与干扰项的字号，单位像素（默认 34；放不下时自适应降档，下限见 MinimumLayoutFontSize） /
        /// The glyph font size in pixels (34 by default; shrunk adaptively when items do not fit,
        /// floored at MinimumLayoutFontSize).
        /// </summary>
        public int FontSize
        {
            get { return fontSize; }
            set { fontSize = value; }
        }

        int headerFontSize = 16;
        /// <summary>
        /// 提示条文字字号，单位像素（默认 16，文字过宽时自动缩小） /
        /// The prompt bar font size in pixels (16 by default; shrunk automatically when the text overflows).
        /// </summary>
        public int HeaderFontSize
        {
            get { return headerFontSize; }
            set { headerFontSize = value; }
        }

        SKColor headerBackgroundColor = new SKColor(255, 255, 255);
        /// <summary>
        /// 提示条背景色（默认白色） / The prompt bar background color (white by default).
        /// </summary>
        public SKColor HeaderBackgroundColor
        {
            get { return headerBackgroundColor; }
            set { headerBackgroundColor = value; }
        }

        SKColor headerTextColor = new SKColor(0x33, 0x33, 0x33);
        /// <summary>
        /// 提示条文字颜色（默认深灰 #333333） / The prompt bar text color (dark gray #333333 by default).
        /// </summary>
        public SKColor HeaderTextColor
        {
            get { return headerTextColor; }
            set { headerTextColor = value; }
        }

        SKColor shadowColor = new SKColor(0x20, 0x20, 0x20, 0xC8);
        /// <summary>
        /// 文字阴影色（默认半透明深灰，向右下偏移 2 像素，保证任意背景可读） /
        /// The glyph shadow color (semi-transparent dark gray by default, offset 2px down-right for readability on any background).
        /// </summary>
        public SKColor ShadowColor
        {
            get { return shadowColor; }
            set { shadowColor = value; }
        }

        SKColor[] drawColors = new SKColor[]
        {
            new SKColor(0xFF, 0xA5, 0x00), // 橙 / orange
            new SKColor(0xFF, 0xD7, 0x00), // 金黄 / gold
            new SKColor(0x00, 0xCE, 0xD1), // 青 / dark cyan
            new SKColor(0xFF, 0xFF, 0xFF), // 白 / white
            new SKColor(0xAD, 0xFF, 0x2F), // 草绿 / green yellow
            new SKColor(0xFF, 0x69, 0xB4)  // 粉 / hot pink
        };
        /// <summary>
        /// 目标与干扰项的随机取色数组（默认 6 种亮色，逐项随机） /
        /// The color pool for targets and distractors (6 bright colors by default, one draw per item).
        /// </summary>
        public SKColor[] DrawColors
        {
            get { return drawColors; }
            set { drawColors = value; }
        }

        #endregion

        #region 背景

        /// <summary>
        /// 绘制背景：优先取配置图库的真实图片，取不到或解码失败时降级为算法随机绘制 /
        /// Draws the background: a real image from the configured library first, degrading to an algorithmic draw
        /// when unavailable or undecodable.
        /// </summary>
        /// <param name="canvas">目标画布 / The target canvas.</param>
        /// <param name="random">随机源 / The random source.</param>
        private void DrawBackground(SKCanvas canvas, Random random)
        {
            byte[]? backgroundBytes = ClickVerificationCodeOptions.TryGetBackgroundImageBytes(random);
            if (backgroundBytes != null && this.TryDrawCoverBackground(canvas, backgroundBytes))
            {
                return;
            }

            this.DrawAlgorithmicBackground(canvas, random);
        }

        /// <summary>
        /// 将背景图按 cover 方式（等比缩放、居中裁剪）铺满画布 /
        /// Draws the background image covering the canvas (scaled proportionally, center-cropped).
        /// </summary>
        /// <param name="canvas">目标画布 / The target canvas.</param>
        /// <param name="imageBytes">背景图字节 / The background image bytes.</param>
        /// <returns>解码并绘制成功返回 true；字节无法解码返回 false（调用方继续降级） /
        /// true when decoded and drawn; false when the bytes cannot be decoded (the caller keeps falling back).</returns>
        private bool TryDrawCoverBackground(SKCanvas canvas, byte[] imageBytes)
        {
            using SKBitmap? source = TryDecodeBackground(imageBytes);
            if (source == null || source.Width == 0 || source.Height == 0)
            {
                return false;
            }

            float scale = Math.Max((float)this.ImageWidth / source.Width, (float)this.ImageHeight / source.Height);
            float drawWidth = source.Width * scale;
            float drawHeight = source.Height * scale;
            float left = (this.ImageWidth - drawWidth) / 2f;
            float top = (this.ImageHeight - drawHeight) / 2f;

            canvas.DrawBitmap(source, new SKRect(left, top, left + drawWidth, top + drawHeight));
            return true;
        }

        /// <summary>
        /// 安全解码背景图字节（无效字节在 SkiaSharp 中会以异常而非 null 呈现） /
        /// Safely decodes background bytes (invalid bytes surface as an exception rather than null in SkiaSharp).
        /// </summary>
        /// <param name="imageBytes">背景图字节 / The background image bytes.</param>
        /// <returns>解码成功的位图；失败返回 null / The decoded bitmap, or null on failure.</returns>
        private static SKBitmap? TryDecodeBackground(byte[] imageBytes)
        {
            try
            {
                return SKBitmap.Decode(imageBytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 算法随机绘制背景：双色渐变打底 + 半透明随机曲线，再叠加既有噪点与干扰线机制 /
        /// Draws an algorithmic background: a two-color gradient base plus translucent random curves,
        /// topped with the existing dot and line interference.
        /// </summary>
        /// <remarks>渐变色取自随机高亮度 HSL 颜色；噪点为 2×2 方块（密度随画布面积），
        /// 干扰线为少量随机直线，与 VerificationCode003 的混淆机制同源。</remarks>
        /// <param name="canvas">目标画布 / The target canvas.</param>
        /// <param name="random">随机源 / The random source.</param>
        private void DrawAlgorithmicBackground(SKCanvas canvas, Random random)
        {
            // 1. 双色渐变打底 / The two-color gradient base.
            SKColor first = RandomLightColor(random);
            SKColor second = RandomLightColor(random);
            SKPoint start = new SKPoint(0, 0);
            SKPoint end = random.Next(2) == 0
                ? new SKPoint(this.ImageWidth, this.ImageHeight)
                : new SKPoint(this.ImageWidth * 0.4f, this.ImageHeight);

            using (SKPaint gradientPaint = new SKPaint())
            {
                using (SKShader shader = SKShader.CreateLinearGradient(
                    start, end, new SKColor[] { first, second }, null, SKShaderTileMode.Clamp))
                {
                    gradientPaint.Shader = shader;
                    canvas.DrawRect(0, 0, this.ImageWidth, this.ImageHeight, gradientPaint);
                }
            }

            // 2. 半透明随机曲线 / Translucent random curves.
            using (SKPaint curvePaint = new SKPaint())
            {
                curvePaint.IsAntialias = true;
                curvePaint.Style = SKPaintStyle.Stroke;
                curvePaint.StrokeWidth = 2f;
                int curveCount = 4 + random.Next(3);
                for (int i = 0; i < curveCount; i++)
                {
                    curvePaint.Color = RandomTranslucentColor(random);
                    using SKPath path = new SKPath();
                    path.MoveTo(random.Next(this.ImageWidth), random.Next(this.ImageHeight));
                    path.CubicTo(
                        random.Next(this.ImageWidth), random.Next(this.ImageHeight),
                        random.Next(this.ImageWidth), random.Next(this.ImageHeight),
                        random.Next(this.ImageWidth), random.Next(this.ImageHeight));
                    canvas.DrawPath(path, curvePaint);
                }
            }

            // 3. 噪点（2×2 方块，密度随画布面积）+ 干扰线，与 VerificationCode003 的混淆机制同源 /
            //    Noise dots (2×2, area-scaled density) plus interference lines, same lineage as VerificationCode003.
            using (SKPaint dotPaint = new SKPaint())
            {
                dotPaint.Color = new SKColor(0x88, 0x88, 0x88, 0x66);
                int dotCount = this.ImageWidth * this.ImageHeight / 400;
                for (int i = 0; i < dotCount; i++)
                {
                    canvas.DrawRect(random.Next(this.ImageWidth), random.Next(this.ImageHeight), 2, 2, dotPaint);
                }
            }

            using (SKPaint linePaint = new SKPaint())
            {
                linePaint.Color = new SKColor(0x22, 0x77, 0xaa, 0x55);
                linePaint.StrokeWidth = 0.8f;
                linePaint.IsAntialias = true;
                for (int i = 0; i < 6; i++)
                {
                    canvas.DrawLine(
                        random.Next(this.ImageWidth), random.Next(this.ImageHeight),
                        random.Next(this.ImageWidth), random.Next(this.ImageHeight),
                        linePaint);
                }
            }
        }

        /// <summary>
        /// 生成随机高亮度颜色（作渐变底色，保证文字可读） /
        /// Creates a random light color for the gradient base (keeps glyphs readable).
        /// </summary>
        /// <param name="random">随机源 / The random source.</param>
        /// <returns>高亮度随机色 / The random light color.</returns>
        private static SKColor RandomLightColor(Random random)
        {
            return SKColor.FromHsl(random.Next(360), 30 + random.Next(30), 82 + random.Next(10));
        }

        /// <summary>
        /// 生成随机半透明颜色（作曲线干扰色） / Creates a random translucent color for the curve interference.
        /// </summary>
        /// <param name="random">随机源 / The random source.</param>
        /// <returns>随机半透明色 / The random translucent color.</returns>
        private static SKColor RandomTranslucentColor(Random random)
        {
            return SKColor.FromHsl(random.Next(360), 40 + random.Next(40), 40 + random.Next(40), (byte)(60 + random.Next(60)));
        }

        #endregion

        #region 排版与提示条

        /// <summary>
        /// 已排布的单项（绘制原点 + 绝对坐标包围盒） / One laid-out item (draw origin plus the absolute bounds).
        /// </summary>
        private sealed class PlacedChar
        {
            /// <summary>
            /// 该位置对应的目标文本（单字或单词） / The target text at this position (a character or a word).
            /// </summary>
            public string Text = string.Empty;

            /// <summary>
            /// DrawText 的绘制原点（左缘 + 基线） / The DrawText origin (left edge plus baseline).
            /// </summary>
            public SKPoint Origin;

            /// <summary>
            /// 图片像素坐标系的字形包围盒 / The glyph bounds in image-pixel coordinates.
            /// </summary>
            public SKRect Bounds;
        }

        /// <summary>
        /// 在提示条下方区域为目标与干扰项做矩形碰撞排版（目标在前，顺序即提示顺序） /
        /// Lays targets and distractors out below the prompt bar with rectangle collision checks
        /// (targets first, in prompt order).
        /// </summary>
        /// <param name="targets">目标列表 / The targets.</param>
        /// <param name="distractors">干扰项列表 / The distractors.</param>
        /// <param name="paint">测量用画笔（Typeface + TextSize 已就绪） / The measuring paint (typeface and size set).</param>
        /// <param name="random">随机源 / The random source.</param>
        /// <returns>排布结果（顺序：目标 → 干扰） / The placement (targets then distractors).</returns>
        private List<PlacedChar> LayoutChars(List<string> targets, List<string> distractors, SKPaint paint, Random random)
        {
            int fontSize = (int)paint.TextSize;
            int margin = Math.Max(4, fontSize / 4);
            SKRect area = new SKRect(
                margin,
                this.HeaderHeight + margin,
                this.ImageWidth - margin,
                this.ImageHeight - margin);

            // 基准重叠间距随字号缩放，失败后逐级放宽至 0 /
            // The base overlap gap scales with the font size and relaxes stepwise to zero on failure.
            int baseGap = Math.Max(4, fontSize / 4);
            int[] gapLevels = new int[] { baseGap, baseGap / 2, 0 };
            List<PlacedChar> placed = new List<PlacedChar>(targets.Count + distractors.Count);

            foreach (string text in targets)
            {
                placed.Add(PlaceChar(text, area, placed, paint, random, gapLevels));
            }
            foreach (string text in distractors)
            {
                placed.Add(PlaceChar(text, area, placed, paint, random, gapLevels));
            }

            return placed;
        }

        /// <summary>
        /// 为单个目标寻找不与已排项重叠的位置 /
        /// Finds a non-overlapping position for one target.
        /// </summary>
        /// <remarks>随机选取绘制原点，字形盒（绝对坐标 = 原点 + MeasureText 相对盒）经间距外扩后
        /// 不得与已排项相交：每级间距重试 60 次，逐级放宽，全部失败说明画布相对字号过小。
        /// MeasureText 的包围盒以绘制原点（左缘 + 基线）为参照，与 DrawText 语义一致。</remarks>
        /// <param name="text">待排布的目标文本 / The target text to place.</param>
        /// <param name="area">可用的排布区域 / The usable layout area.</param>
        /// <param name="placed">已排布项（碰撞检测对象） / The already-placed items (collision targets).</param>
        /// <param name="paint">测量用画笔 / The measuring paint.</param>
        /// <param name="random">随机源 / The random source.</param>
        /// <param name="gapLevels">逐级放宽的重叠间距（像素） / The gap levels tried in order, relaxing stepwise.</param>
        /// <returns>排布结果 / The placement.</returns>
        /// <exception cref="InvalidOperationException">各级间距重试后仍无法放下时抛出 / Thrown when no level yields a free spot.</exception>
        private PlacedChar PlaceChar(
            string text,
            SKRect area,
            IReadOnlyList<PlacedChar> placed,
            SKPaint paint,
            Random random,
            int[] gapLevels)
        {
            // 字形盒相对绘制原点（基线）测量；绝对盒 = 原点 + 相对盒 /
            // Glyph bounds are measured relative to the draw origin (baseline); the absolute box = origin + relative box.
            SKRect relativeBounds = SKRect.Empty;
            paint.MeasureText(text, ref relativeBounds);

            float minX = area.Left - relativeBounds.Left;
            float maxX = area.Right - relativeBounds.Right;
            float minY = area.Top - relativeBounds.Top;
            float maxY = area.Bottom - relativeBounds.Bottom;

            foreach (int gap in gapLevels)
            {
                for (int attempt = 0; attempt < 60; attempt++)
                {
                    if (minX > maxX || minY > maxY)
                    {
                        break; // 画布放不下该项，逐级放宽也无济于事 / The item never fits; relaxing gaps cannot help.
                    }

                    SKPoint origin = new SKPoint(
                        minX + (float)random.NextDouble() * (maxX - minX),
                        minY + (float)random.NextDouble() * (maxY - minY));
                    SKRect bounds = new SKRect(
                        origin.X + relativeBounds.Left,
                        origin.Y + relativeBounds.Top,
                        origin.X + relativeBounds.Right,
                        origin.Y + relativeBounds.Bottom);

                    SKRect test = bounds;
                    test.Inflate(gap, gap);
                    bool overlaps = false;
                    for (int i = 0; i < placed.Count; i++)
                    {
                        if (test.IntersectsWith(placed[i].Bounds))
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (!overlaps)
                    {
                        return new PlacedChar { Text = text, Origin = origin, Bounds = bounds };
                    }
                }
            }

            throw new InvalidOperationException(
                $"画布 {this.ImageWidth}x{this.ImageHeight} 相对字号 {this.FontSize} 过小，无法无重叠排布全部目标 / "
                + $"Canvas {this.ImageWidth}x{this.ImageHeight} is too small for font size {this.FontSize} to lay out without overlap.");
        }

        /// <summary>
        /// 绘制顶部提示条：底色块 + 前缀 + 以空格分隔的目标（文字过宽时自动缩小字号） /
        /// Draws the prompt bar: the background strip, the prefix, and the space-joined targets (shrunk when too wide).
        /// </summary>
        /// <param name="canvas">目标画布 / The target canvas.</param>
        /// <param name="targets">目标列表（按提示顺序） / The targets (in prompt order).</param>
        /// <param name="typeface">含中文字形的字体面 / The typeface holding CJK glyphs.</param>
        private void DrawHeader(SKCanvas canvas, List<string> targets, SKTypeface typeface)
        {
            using (SKPaint stripPaint = new SKPaint())
            {
                stripPaint.Color = this.HeaderBackgroundColor;
                canvas.DrawRect(0, 0, this.ImageWidth, this.HeaderHeight, stripPaint);
            }

            string prompt = this.PromptPrefix + string.Join(" ", targets);

            using (SKPaint textPaint = new SKPaint())
            {
                textPaint.Typeface = typeface;
                textPaint.IsAntialias = true;
                textPaint.Color = this.HeaderTextColor;
                textPaint.TextSize = Math.Min(this.HeaderFontSize, this.HeaderHeight - 6);

                // 文字过宽时逐号缩小，保证不溢出提示条 / Step down until the prompt fits the strip.
                while (textPaint.TextSize > 10 && textPaint.MeasureText(prompt) > this.ImageWidth - 16)
                {
                    textPaint.TextSize -= 1;
                }

                SKRect promptBounds = SKRect.Empty;
                textPaint.MeasureText(prompt, ref promptBounds);
                float baseline = (this.HeaderHeight - (promptBounds.Top + promptBounds.Bottom)) / 2f;
                canvas.DrawText(prompt, 8, baseline, textPaint);
            }
        }

        #endregion
    }
}
