using SkiaSharp;
using System;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 旋转着色 + 浅遮罩干扰（人眼友好） / Verification code - rotated, per-character colored, light-mask interference (human friendly).
    /// </summary>
    /// <remarks>
    /// 设计原则 / Design principles：
    ///   1. 先画浅色背景噪点 → 再画清晰文字（抗锯齿）→ 最后少量浅色干扰线条（不与文字同色）；
    ///      draw light background noise first, then crisp anti-aliased text, finally a few pale interference lines
    ///   2. 每个字符独立角度旋转 + 独立颜色，视觉可分段识别；each character gets its own rotation and color so it can be read one by one
    ///   3. 干扰线条使用随机浅色 + 低透明度，不覆盖字符主干；interference uses pale, low-alpha strokes that never dominate the glyphs
    ///   4. 去除易混淆字符 (0/O/o/1/I/l/i/L/j/J 等)；easily confused characters are excluded
    ///
    /// 三层绘制结构：第 1 层浅色网格 + 稀疏圆点（破坏 OCR 二值化分割），
    /// 第 2 层逐字符随机色 + ±15° 旋转 + 纵向抖动的清晰文字，
    /// 第 3 层低透明贝塞尔曲线与极淡短线（破坏 OCR 连通域）。
    /// </remarks>
    public class VerificationCode010 : VerificationCodeBase
    {
        /// <summary>
        /// 获取样式名称：旋转着色噪点干扰(人眼友好) / Gets the style name: rotation + coloring + noise (human friendly).
        /// </summary>
        public override string Name => "旋转着色噪点干扰(人眼友好)";

        #region 属性

        /// <summary>
        /// 验证码字符个数（默认 4） / The number of characters in the code (4 by default).
        /// </summary>
        public int ValidataCodeLength { get; set; } = 4;
        /// <summary>
        /// 验证码字符的字体大小，单位像素（默认 30，粗体） /
        /// The font size of the characters in pixels (30 by default, bold).
        /// </summary>
        public int ValidataCodeSize { get; set; } = 30;
        /// <summary>
        /// 图片高度，单位像素（默认 56）；图片宽度按验证码长度与字号自动推算 /
        /// The image height in pixels (56 by default); the width is derived from code length and font size.
        /// </summary>
        public int ImageHeight { get; set; } = 56;
        /// <summary>
        /// 背景色（默认近白 #F8F8FC） / The background color (near-white #F8F8FC by default).
        /// </summary>
        public SKColor BackgroundColor { get; set; } = new SKColor(248, 248, 252);

        /// <summary>
        /// 字体族名（默认 Arial，须为系统已安装字体；本类不含内嵌字体） /
        /// The font family name (Arial by default; must be installed on the system - no font is embedded).
        /// </summary>
        public string VerificationCodeFont { get; set; } = "Arial";

        #endregion

        // 去除易混淆字符 — 对人眼友好
        private const string SafeChars =
            "2,3,4,5,6,7,8,9,a,b,c,d,e,f,g,h,k,m,n,p,q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,K,M,N,P,Q,R,S,T,U,V,W,X,Y,Z";

        private static readonly SKColor[] TextColors =
        {
            new SKColor(0x1A, 0x56, 0xDB), // 蓝
            new SKColor(0xC6, 0x39, 0x1A), // 红褐
            new SKColor(0x15, 0x73, 0x4D), // 绿
            new SKColor(0x8E, 0x2C, 0x8A), // 紫
            new SKColor(0xBF, 0x6A, 0x02), // 橙
            new SKColor(0x1A, 0x6B, 0x73), // 青
            new SKColor(0x4A, 0x3B, 0x7C), // 深紫
            new SKColor(0x8C, 0x38, 0x21), // 深红
        };

        /// <summary>
        /// 生成验证码图片：数字字母混合码，三层人眼友好绘制 /
        /// Generate the image: an alphanumeric code rendered in three human-friendly layers.
        /// </summary>
        /// <remarks>
        /// 字符集为数字 2–9 与大小写字母（剔除 0、O、o、1、l、I、i、J、j、L 等易混淆字符）。
        /// 与 001–009 不同，本方法返回 ToArray() 的精确副本，数组长度与实际 PNG 数据一致、无尾部冗余字节。
        /// The character pool is digits 2–9 plus mixed-case letters with easily confused ones removed.
        /// Unlike styles 001-009, this method returns an exact ToArray() copy whose length matches the PNG payload - no trailing bytes.
        /// </remarks>
        /// <param name="validataCode">输出：生成的验证码字符串（用户须原样输入） / Output: the generated code the user must enter verbatim.</param>
        /// <returns>PNG 图片字节数组（长度精确） / The PNG image bytes (exact length).</returns>
        public override byte[] CreateImage(out string validataCode)
        {
            GetRandom(SafeChars, ValidataCodeLength, out validataCode);

            int width = (int)((ValidataCodeLength * ValidataCodeSize) * 1.35 + ValidataCodeSize * 0.5);
            var bitmap = new SKBitmap(width, ImageHeight);

            RenderImage(bitmap, validataCode);

            using var stream = new System.IO.MemoryStream();
            using var img = SKImage.FromBitmap(bitmap);
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            data.SaveTo(stream);

            byte[] buff = stream.ToArray();
            bitmap.Dispose();
            return buff;
        }

        /// <summary>
        /// 按三层结构渲染验证码位图 / Renders the bitmap in three layers.
        /// </summary>
        /// <remarks>依次调用：背景噪点层 → 文字层 → 前景干扰层；随机数种子取自 Guid 哈希。</remarks>
        /// <param name="bitmap">目标位图 / The target bitmap.</param>
        /// <param name="code">验证码字符串 / The verification code string.</param>
        private void RenderImage(SKBitmap bitmap, string code)
        {
            using var canvas = new SKCanvas(bitmap);
            var random = new Random(Guid.NewGuid().GetHashCode());

            canvas.DrawColor(BackgroundColor);

            // === 第1层: 浅色背景噪点（在文字之下，不影响文字可读性） ===
            DrawBackgroundNoise(canvas, bitmap.Width, bitmap.Height, random);

            // === 第2层: 清晰文字（主角） ===
            DrawText(canvas, bitmap.Width, code, random);

            // === 第3层: 最浅的前景干扰（在文字之上但低于文字对比度） ===
            DrawForegroundInterference(canvas, bitmap.Width, bitmap.Height, code.Length, random);
        }

        /// <summary>
        /// 第 1 层：浅色网格 + 稀疏噪点，破坏机器 OCR 的二值化分割，但人眼可忽略 /
        /// Layer 1: a pale grid plus sparse dots that defeat OCR binarization yet stay invisible to the eye.
        /// </summary>
        /// <remarks>网格间距随机取 14~19 像素；圆点数量按 w*h/140 密度铺放，半径 0.8~2.4 像素。</remarks>
        /// <param name="canvas">目标画布 / The target canvas.</param>
        /// <param name="w">画布宽度 / The canvas width.</param>
        /// <param name="h">画布高度 / The canvas height.</param>
        /// <param name="rng">随机数发生器 / The random source.</param>
        private static void DrawBackgroundNoise(SKCanvas canvas, int w, int h, Random rng)
        {
            // 细网格线
            using var gridPaint = new SKPaint
            {
                Color = new SKColor(210, 215, 225),
                StrokeWidth = 0.5f,
                IsAntialias = true
            };

            int step = 14 + rng.Next(6); // 14~19px 网格
            for (int x = rng.Next(step); x < w; x += step)
                canvas.DrawLine(x, 0, x, h, gridPaint);
            for (int y = rng.Next(step); y < h; y += step)
                canvas.DrawLine(0, y, w, y, gridPaint);

            // 稀疏背景圆点
            using var dotPaint = new SKPaint
            {
                Color = new SKColor(190, 198, 212),
                IsAntialias = true
            };
            for (int i = 0; i < w * h / 140; i++)
            {
                float x = rng.Next(w), y = rng.Next(h);
                float r = 0.8f + (float)rng.NextDouble() * 1.6f;
                canvas.DrawCircle(x, y, r, dotPaint);
            }
        }

        /// <summary>
        /// 第 2 层：每个字符独立颜色、独立旋转 ±15°、轻微纵向抖动 /
        /// Layer 2: each character gets its own color, a ±15° rotation and a slight vertical jitter.
        /// </summary>
        /// <remarks>字符间距充足（字号 × 0.3），人眼可逐个识别；整体文本在画布内居中，左边距不足 8 像素时强制取 8。
        /// 注意实际旋转角为 (随机数-0.5)×30°，即 ±15°。With generous spacing (font size x 0.3) the characters
        /// read one by one; the text block is centered and the left margin is clamped to at least 8 pixels.
        /// The rotation angle is (random-0.5)x30°, i.e. ±15°.</remarks>
        /// <param name="canvas">目标画布 / The target canvas.</param>
        /// <param name="canvasW">画布宽度，用于居中计算 / The canvas width, used for centering.</param>
        /// <param name="code">验证码字符串 / The verification code string.</param>
        /// <param name="rng">随机数发生器 / The random source.</param>
        private void DrawText(SKCanvas canvas, int canvasW, string code, Random rng)
        {
            var typeface = SKTypeface.FromFamilyName(VerificationCodeFont, SKFontStyle.Bold);
            float effectiveSize = ValidataCodeSize * 1.0f;
            var font = new SKFont(typeface, effectiveSize);

            float charSpacing = ValidataCodeSize * 0.3f;
            float approxCharWidth = effectiveSize * 0.72f;     // 字母/数字大致宽高比
            float totalTextWidth = code.Length * approxCharWidth + charSpacing * (code.Length - 1);

            float startX = (canvasW - totalTextWidth) / 2f;
            if (startX < 8) startX = 8;

            for (int i = 0; i < code.Length; i++)
            {
                var color = TextColors[rng.Next(TextColors.Length)];

                using var paint = new SKPaint(font)
                {
                    Color = color,
                    IsAntialias = true
                };

                float cx = startX + approxCharWidth / 2f;
                float cy = ImageHeight / 2f + effectiveSize / 3f;

                float angle = (float)(rng.NextDouble() - 0.5) * 30f; // ±15°
                float yOffset = (float)(rng.NextDouble() - 0.5) * effectiveSize * 0.35f;

                canvas.Save();
                canvas.RotateDegrees(angle, cx, cy + yOffset);
                canvas.DrawText(code[i].ToString(), startX, cy + effectiveSize * 0.1f + yOffset, paint);
                canvas.Restore();

                startX += approxCharWidth + charSpacing;
            }
        }

        /// <summary>
        /// 第 3 层：少量低对比度干扰路径（颜色接近背景或淡灰），破坏 OCR 连通域 /
        /// Layer 3: a few low-contrast interference paths (near-background or pale gray) that break OCR connected components.
        /// </summary>
        /// <remarks>绘制 2~3 条贯穿上下的半透明三次贝塞尔曲线（Alpha=120），
        /// 以及字符数 2 倍的极淡短线段（Alpha=80，横向漂移 ±20 像素、纵向 ±9 像素）；干扰画在文字之上但对比度远低于文字。
        /// Draws 2-3 semi-transparent cubic Bezier curves crossing the full height (alpha 120) plus twice-the-code-length
        /// ultra-faint short segments (alpha 80); drawn above the text but far lower in contrast.</remarks>
        /// <param name="canvas">目标画布 / The target canvas.</param>
        /// <param name="w">画布宽度 / The canvas width.</param>
        /// <param name="h">画布高度 / The canvas height.</param>
        /// <param name="codeLen">验证码字符数（决定短线数量） / The code length (drives the segment count).</param>
        /// <param name="rng">随机数发生器 / The random source.</param>
        private static void DrawForegroundInterference(SKCanvas canvas, int w, int h, int codeLen, Random rng)
        {
            // 2~3 条贝塞尔曲线，使用极低透明度
            int curveCount = 2 + rng.Next(2);
            for (int c = 0; c < curveCount; c++)
            {
                var curveColor = new SKColor(
                    (byte)(180 + rng.Next(40)),
                    (byte)(185 + rng.Next(35)),
                    (byte)(200 + rng.Next(30)),
                    120 // 半透明
                );

                using var pathPaint = new SKPaint
                {
                    Color = curveColor,
                    StrokeWidth = 0.8f + (float)rng.NextDouble() * 1.2f,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke
                };

                using var path = new SKPath();
                float x0 = rng.Next(w);
                float x3 = rng.Next(w);
                path.MoveTo(x0, -2);
                path.CubicTo(
                    rng.Next(w), h * 0.25f,
                    rng.Next(w), h * 0.65f,
                    x3, h + 2);
                canvas.DrawPath(path, pathPaint);
            }

            // 少量极淡的随机短线段
            int lineCount = codeLen * 2;
            using var linePaint = new SKPaint
            {
                Color = new SKColor(175, 180, 195, 80),
                StrokeWidth = 0.6f,
                IsAntialias = true
            };
            for (int i = 0; i < lineCount; i++)
            {
                float x1 = rng.Next(w);
                float y1 = rng.Next(h);
                float x2 = x1 + (float)(rng.NextDouble() - 0.5) * 40f;
                float y2 = y1 + (float)(rng.NextDouble() - 0.5) * 18f;
                canvas.DrawLine(x1, y1, x2, y2, linePaint);
            }
        }

        #region 工具方法

        /// <summary>
        /// 从逗号分隔的候选字符串中随机抽取指定数量的字符组成验证码 /
        /// Builds the code by picking len characters at random from a comma-separated pool.
        /// </summary>
        /// <remarks>随机种子取自 Guid.NewGuid().GetHashCode()，索引直接用 rnd.Next(pool.Length)，无取模偏差。</remarks>
        /// <param name="formatString">逗号分隔的候选字符集 / The comma-separated character pool.</param>
        /// <param name="len">要抽取的字符个数 / The number of characters to pick.</param>
        /// <param name="codeString">输出：生成的验证码字符串 / Output: the generated code string.</param>
        private static void GetRandom(string formatString, int len, out string codeString)
        {
            codeString = string.Empty;
            string[] pool = formatString.Split(',');
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < len; i++)
            {
                codeString += pool[rnd.Next(pool.Length)];
            }
        }

        #endregion
    }
}
