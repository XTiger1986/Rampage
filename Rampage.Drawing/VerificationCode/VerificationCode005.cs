using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 中文(蓝色) / Verification code with common Chinese characters (blue).
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 中文(蓝色)
    ///                  从内置常用汉字表中随机取字，蓝色文字 + 暗黄噪点干扰。
    ///
    ///  History:        2015/4/28 19:16:04 创建
    ///
    /// </remarks>
    public class VerificationCode005 : VerificationCodeBase
    {

        /// <summary>
        /// 获取样式名称：中文(蓝色) / Gets the style name: Chinese characters (blue).
        /// </summary>
        public override string Name
        {
            get { return "中文(蓝色)"; }
        }

        /// <summary>
        /// 获取是否禁用输入法：本类型返回 false（需允许用户用输入法输入汉字） /
        /// Whether the IME should be disabled: false here so users can type Chinese characters.
        /// </summary>
        public override bool DisableIme
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 生成验证码图片：随机常用汉字 + 方块噪点 / Generate the image: random common Chinese characters plus dot interference.
        /// </summary>
        /// <remarks>
        /// 字符集为内置常用汉字表（约 350 字）。渲染字体不取自 VerificationCodeFont，
        /// 而是通过 SKFontManager.MatchCharacter('时') 自动匹配含中文字形的系统字体，避免乱码。
        /// 返回值为 MemoryStream 的底层缓冲区（GetBuffer），长度通常大于实际 PNG 数据、尾部含冗余字节；PNG 解码器一般可容忍。
        /// The pool is a built-in list of ~350 common Chinese characters. The rendering font is not taken from
        /// VerificationCodeFont; it is matched via SKFontManager.MatchCharacter('时') so a font with CJK glyphs is used.
        /// The returned array is the stream's underlying buffer (GetBuffer), usually longer than the actual PNG payload
        /// with trailing unused bytes; PNG decoders normally tolerate them.
        /// </remarks>
        /// <param name="validataCode">输出：生成的汉字验证码（用户须原样输入） / Output: the generated Chinese code the user must enter verbatim.</param>
        /// <returns>PNG 图片字节数组（可能含尾部冗余字节） / The PNG image bytes (may include trailing unused bytes).</returns>
        public override byte[] CreateImage(out string validataCode)
        {
            string strFormat = VerificationCharPool.CommonChineseChars;
            GetRandom(strFormat, this.ValidataCodeLength, out validataCode);
            SKBitmap bitmap;
            MemoryStream stream = new MemoryStream();

            ImageBmp(out bitmap, validataCode);
            using (SKImage img = SKImage.FromBitmap(bitmap))
            {
                using (SKData p = img.Encode(SKEncodedImageFormat.Png, 100))
                {
                    p.SaveTo(stream);
                }
            }
            bitmap.Dispose();

            byte[] buff = stream.GetBuffer();

            stream.Close();
            stream.Dispose();

            return buff;
        }

        #region 属性

        bool fontTextRenderingHint = false;
        /// <summary>
        /// 文字抗锯齿开关（true 开启抗锯齿，默认 false，对应绘制时的 IsAntialias） /
        /// Toggles text anti-aliasing (true = on; false by default, mapped to IsAntialias when drawing).
        /// </summary>
        bool FontTextRenderingHint
        {
            get { return fontTextRenderingHint; }
            set { fontTextRenderingHint = value; }
        }

        int validataCodeLength = 4;
        /// <summary>
        /// 验证码字符个数（默认 4） / The number of characters in the code (4 by default).
        /// </summary>
        public int ValidataCodeLength
        {
            get { return validataCodeLength; }
            set { validataCodeLength = value; }
        }

        int validataCodeSize = 18;
        /// <summary>
        /// 验证码字符的字体大小，单位像素（默认 18，小于字母类样式） /
        /// The font size of the characters in pixels (18 by default, smaller than the letter styles).
        /// </summary>
        public int ValidataCodeSize
        {
            get { return validataCodeSize; }
            set { validataCodeSize = value; }
        }

        int imageHeight = 56;
        /// <summary>
        /// 图片高度，单位像素（默认 56）；图片宽度按验证码长度与字号自动推算 /
        /// The image height in pixels (56 by default); the width is derived from code length and font size.
        /// </summary>
        public int ImageHeight
        {
            get { return imageHeight; }
            set { imageHeight = value; }
        }

        SKColor chaosColor = new SKColor(0xaa, 0xaa, 0x33);
        /// <summary>
        /// 噪点颜色（默认暗黄 #AAAA33） / The noise dot color (#AAAA33 by default).
        /// </summary>
        public SKColor ChaosColor
        {
            get { return chaosColor; }
            set { chaosColor = value; }
        }

        SKColor backgroundColor = new SKColor(255, 255, 255);
        /// <summary>
        /// 背景色（默认白色） / The background color (white by default).
        /// </summary>
        public SKColor BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        SKColor drawColor = new SKColor(0x32, 0x99, 0xcc);
        /// <summary>
        /// 文字颜色（默认蓝色 #3299CC） / The text color (blue #3299CC by default).
        /// </summary>
        public SKColor DrawColor
        {
            get { return drawColor; }
            set { drawColor = value; }
        }

        string validateCodeFont = "Arial";
        /// <summary>
        /// 字体族名（默认 Arial）。注意：本类型实际渲染不使用该属性，
        /// 而由 SKFontManager.MatchCharacter 自动匹配含中文字形的系统字体 /
        /// The font family name (Arial by default). Note: rendering ignores this property -
        /// a system font with CJK glyphs is matched via SKFontManager.MatchCharacter instead.
        /// </summary>
        public string VerificationCodeFont
        {
            get { return validateCodeFont; }
            set { validateCodeFont = value; }
        }

        #endregion

        #region CreateImage
        /// <summary>
        /// 按验证码长度与字号计算宽度并创建位图，再交由 CreateImageBmp 绘制 /
        /// Creates the bitmap sized from the code length and font size, then draws it via CreateImageBmp.
        /// </summary>
        /// <param name="bitmap">输出：创建好的位图 / Output: the created bitmap.</param>
        /// <param name="validataCode">验证码字符串 / The verification code string.</param>
        void ImageBmp(out SKBitmap bitmap, string validataCode)
        {
            int width = (int)(((this.ValidataCodeLength * this.ValidataCodeSize) * 1.3) + 10);
            bitmap = new SKBitmap(width, this.ImageHeight);
            CreateImageBmp(ref bitmap, validataCode);
        }

        /// <summary>
        /// 绘制验证码图片/混淆验证码图片 /
        /// Draws the Chinese code characters and the dot interference.
        /// </summary>
        /// <remarks>绘制顺序：先画 2×2 方块噪点（位于文字下层，数量为字符数的 10 倍），再逐字符绘制汉字；
        /// 字符纵向偏移区间按字号/1.5 收窄，横向在槽位内随机偏移并加大间距。</remarks>
        /// <param name="bitmap">目标位图 / The target bitmap.</param>
        /// <param name="validateCode">验证码字符串 / The verification code string.</param>
        void CreateImageBmp(ref SKBitmap bitmap, string validateCode)
        {
            int minValue = (int)Math.Floor(this.ValidataCodeSize / 1.5M);
            int maxValue = (int)Math.Ceiling(this.ImageHeight - this.ValidataCodeSize / 1.5M);
            //字体样式
            var fontManager = SKFontManager.Default;
            //SKTypeface typeface = SKTypeface.FromFamilyName(this.VerificationCodeFont, SKFontStyle.Normal);
            SKTypeface typeface = fontManager.MatchCharacter('时'); //解决中文乱码
            SKFont font = new SKFont(typeface, this.ValidataCodeSize);
            SKPaint paint = new SKPaint(font);
            paint.Color = this.DrawColor;
            paint.IsAntialias = this.FontTextRenderingHint;

            SKPaint dotPaint = new SKPaint();
            dotPaint.Color = this.ChaosColor;
            dotPaint.StrokeWidth = 1f;

            Random random = new Random();

            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                canvas.DrawColor(BackgroundColor);

                // 先画噪点（在文字之下）
                for (int i = 0; i < validateCode.Length * 10; i++)
                {
                    int x = random.Next(bitmap.Width);
                    int y = random.Next(bitmap.Height);
                    canvas.DrawRect(x, y, 2, 2, dotPaint);
                }

                // 再画文字
                for (int i = 0; i < validateCode.Length; i++)
                {
                    SKPoint point = new SKPoint((i * this.ValidataCodeSize) + random.Next(4) + 5, random.Next(minValue, maxValue));

                    canvas.DrawText(validateCode[i].ToString(), point, paint);
                }
            }
        }

        /// <summary>
        /// 从逗号分隔的候选字符串中随机抽取指定数量的字符组成验证码 /
        /// Builds the code by picking len characters at random from a comma-separated pool.
        /// </summary>
        /// <remarks>随机索引由 rnd.Next(100000) 对候选数取模得到，存在轻微取模偏差；候选字符集为常规规模时可忽略。</remarks>
        /// <param name="formatString">逗号分隔的候选字符集 / The comma-separated character pool.</param>
        /// <param name="len">要抽取的字符个数 / The number of characters to pick.</param>
        /// <param name="codeString">输出：生成的验证码字符串 / Output: the generated code string.</param>
        private static void GetRandom(string formatString, int len, out string codeString)
        {
            int j1;
            codeString = string.Empty;
            string[] strResult = formatString.Split(new char[] { ',' });//把上面字符存入数组
            Random rnd = new Random();
            for (int i = 0; i < len; i++)
            {
                j1 = rnd.Next(100000) % strResult.Length;
                codeString = codeString + strResult[j1].ToString();
            }
        }
        #endregion
    }
}
