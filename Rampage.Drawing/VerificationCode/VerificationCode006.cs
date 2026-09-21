using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 2年级算术(中文蓝色) / Verification code as primary-school (grade 2) arithmetic (Chinese, blue).
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 2年级算术(中文蓝色)
    ///                  图片展示形如“45加23”或“57减30”的算式，resultCode 为计算结果；
    ///                  算式用汉字“加/减”表示运算符，难度控制在二年级口算范围（不出现负数与退位）。
    ///
    ///  History:        2015/4/28 19:16:04 创建
    ///
    /// </remarks>
    public class VerificationCode006 : VerificationCodeBase
    {
        /// <summary>
        /// 获取样式名称：2年级算术(中文蓝色) / Gets the style name: grade-2 arithmetic (Chinese, blue).
        /// </summary>
        public override string Name
        {
            get { return "2年级算术(中文蓝色)"; }
        }

        /// <summary>
        /// 获取输入提示：输入计算结果 / Gets the input hint: enter the arithmetic result.
        /// </summary>
        public override string Tip
        {
            get
            {
                return "输入计算结果";
            }
        }

        /// <summary>
        /// 生成验证码图片：随机生成一道加减算式并绘制，答案作为验证码 /
        /// Generate the image: a random addition/subtraction expression whose result is the code.
        /// </summary>
        /// <remarks>
        /// 渲染字体不取自 VerificationCodeFont，而是通过 SKFontManager.MatchCharacter('时') 自动匹配含中文字形的系统字体。
        /// 返回值为 MemoryStream 的底层缓冲区（GetBuffer），长度通常大于实际 PNG 数据、尾部含冗余字节；PNG 解码器一般可容忍。
        /// The rendering font is matched via SKFontManager.MatchCharacter('时') rather than VerificationCodeFont.
        /// The returned array is the stream's underlying buffer (GetBuffer), usually longer than the actual PNG payload
        /// with trailing unused bytes; PNG decoders normally tolerate them.
        /// </remarks>
        /// <param name="resultCode">输出：算式的计算结果（用户须输入该结果） / Output: the arithmetic result the user must enter.</param>
        /// <returns>PNG 图片字节数组（可能含尾部冗余字节） / The PNG image bytes (may include trailing unused bytes).</returns>
        public override byte[] CreateImage(out string resultCode)
        {

            string validataCode;
            GetRandom(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, out validataCode, out resultCode);
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

        int validataCodeSize = 18;
        /// <summary>
        /// 算式文字的字体大小，单位像素（默认 18）；本类型无验证码长度属性，宽度按算式实际字符数推算 /
        /// The font size of the expression in pixels (18 by default); there is no length property - the width
        /// is derived from the actual number of characters in the expression.
        /// </summary>
        public int ValidataCodeSize
        {
            get { return validataCodeSize; }
            set { validataCodeSize = value; }
        }

        int imageHeight = 56;
        /// <summary>
        /// 图片高度，单位像素（默认 56） / The image height in pixels (56 by default).
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
        /// 按算式字符数与字号计算宽度并创建位图，再交由 CreateImageBmp 绘制 /
        /// Creates the bitmap sized from the expression length and font size, then draws it via CreateImageBmp.
        /// </summary>
        /// <param name="bitmap">输出：创建好的位图 / Output: the created bitmap.</param>
        /// <param name="validataCode">算式文本（如“45加23”） / The expression text (e.g. "45加23").</param>
        void ImageBmp(out SKBitmap bitmap, string validataCode)
        {
            int width = (int)(((validataCode.Length * this.ValidataCodeSize) * 1.3) + 10);
            bitmap = new SKBitmap(width, this.ImageHeight);
            CreateImageBmp(ref bitmap, validataCode);
        }

        /// <summary>
        /// 绘制验证码图片/混淆验证码图片 /
        /// Draws the arithmetic expression and the dot interference.
        /// </summary>
        /// <remarks>绘制顺序：先画 2×2 方块噪点（位于文字下层，数量为算式字符数的 10 倍），再逐字符绘制算式；
        /// 字符按“字号 + 每字符 5 像素”的步进横向排列并随机纵向偏移。</remarks>
        /// <param name="bitmap">目标位图 / The target bitmap.</param>
        /// <param name="validateCode">算式文本 / The expression text.</param>
        void CreateImageBmp(ref SKBitmap bitmap, string validateCode)
        {
            int minValue = (int)Math.Floor(this.ValidataCodeSize / 1.5M);
            int maxValue = (int)Math.Ceiling(this.ImageHeight - this.ValidataCodeSize / 3M);
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
                    SKPoint point = new SKPoint((i * this.ValidataCodeSize) + i * 5, random.Next(minValue, maxValue));

                    canvas.DrawText(validateCode[i].ToString(), point, paint);
                }
            }
        }

        /// <summary>
        /// 随机生成一道二年级加减算式及其答案 / Generates a random grade-2 addition/subtraction expression and its answer.
        /// </summary>
        /// <remarks>
        /// <para>操作数构造：num1 为三个随机数字按 0/10/20 权重累加（首位非 0），恒落在 31~57；num2 为三个随机数字之和（首位非 0），恒落在 1~27。</para>
        /// <para>约 50% 概率出加法“num1加num2”；否则出减法：因 num1 恒大于 num2，先交换保证大数减小数（结果非负），
        /// 再在个位被减数偏大时把 num1 的个位压到 num2 个位的一半左右，使减法永不退位——契合二年级口算难度。</para>
        /// <para>Operands: num1 sums three random digits weighted 0/10/20 (leading digit non-zero), always 31-57;
        /// num2 is the plain sum of three digits, always 1-27. Roughly half the time an addition "num1加num2" is emitted;
        /// otherwise a subtraction is emitted with the operands swapped so the larger is minuend, and num1's ones digit
        /// clamped below num2's ones digit, so the subtraction never requires borrowing.</para>
        /// </remarks>
        /// <param name="numbers">候选数字数组（0–9） / The candidate digits (0-9).</param>
        /// <param name="codeString">输出：算式文本，如“45加23” / Output: the expression text, e.g. "45加23".</param>
        /// <param name="resultCode">输出：算式答案 / Output: the answer.</param>
        private static void GetRandom(int[] numbers, out string codeString, out string resultCode)
        {
            Random rnd = new Random();
            int num1 = 0, num2 = 0;


            for (int i = 0; i < 3; i++)
            {
                int j1 = rnd.Next(numbers.Length);
                if (i == 0 && numbers[j1] == 0)
                {
                    i--;
                    continue;
                }
                num1 += i * 10 + numbers[j1];
            }
            for (int i = 0; i < 3; i++)
            {
                int j1 = rnd.Next(numbers.Length);
                if (i == 0 && numbers[j1] == 0)
                {
                    i--;
                    continue;
                }
                num2 += numbers[j1];
            }

            if (rnd.Next(100) % 2 == 1)
            {
                codeString = num1 + "加" + num2;
                resultCode = (num1 + num2).ToString();
            }
            else
            {
                // 交换保证大数减小数，结果非负 / Swap so the larger number is the minuend and the result stays non-negative
                if (num2 < num1)
                {
                    int temp = num1;
                    num1 = num2;
                    num2 = temp;
                }

                // 把 num1 的个位压到 num2 个位的一半左右，避免退位 / Clamp num1's ones digit to about half of num2's to avoid borrowing
                int g1, g2;
                g1 = num1 % 10;
                g2 = num2 % 10;

                if (g1 > g2)
                {
                    num1 -= g1;
                    g1 = g2 / 2;
                    num1 += g1;
                }

                codeString = num2 + "减" + num1;
                resultCode = (Math.Abs(num2 - num1)).ToString();
            }
        }
        #endregion
    }
}
