using SkiaSharp;
using System;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 旋转着色 + 浅遮罩干扰（人眼友好）
    /// 设计原则：
    ///   1. 先画浅色背景噪点 → 再画清晰文字（抗锯齿）→ 最后少量浅色干扰线条（不与文字同色）
    ///   2. 每个字符独立角度旋转 + 独立颜色，视觉可分段识别
    ///   3. 干扰线条使用随机浅色 + 低透明度，不覆盖字符主干
    ///   4. 去除易混淆字符 (0/O/o/1/I/l/i/L/j/J 等)
    /// </summary>
    public class VerificationCode010 : VerificationCodeBase
    {
        public override string Name => "旋转着色噪点干扰(人眼友好)";

        #region 属性

        public int ValidataCodeLength { get; set; } = 4;
        public int ValidataCodeSize { get; set; } = 30;
        public int ImageHeight { get; set; } = 56;
        public SKColor BackgroundColor { get; set; } = new SKColor(248, 248, 252);

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
        /// 浅色网格 + 稀疏噪点，破坏机器OCR的二值化分割，但人眼可忽略
        /// </summary>
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
        /// 每个字符独立颜色、独立旋转 ±18°、轻微纵向抖动
        /// 字符间距充足，人眼可逐个识别
        /// </summary>
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
        /// 少量低对比度干扰路径（颜色接近背景或淡灰），破坏OCR连通域
        /// </summary>
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
