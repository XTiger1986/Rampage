using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 线条干扰(彩色) / Verification code with colored line interference.
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 线条干扰(彩色)
    ///                  数字字母混合（已剔除易混淆字符）；文字与干扰线均从彩色数组中随机取色，逐字符换色。
    ///
    ///  History:        2015/4/28 19:16:04 创建
    ///
    /// </remarks>
    public class VerificationCode008 : VerificationCodeBase
    {
        /// <summary>
        /// 获取样式名称：线条干扰(彩色) / Gets the style name: line interference (colored).
        /// </summary>
        public override string Name
        {
            get { return "线条干扰(彩色)"; }
        }

        /// <summary>
        /// 生成验证码图片：随机数字字母混合码 + 彩色线条干扰 /
        /// Generate the image: random alphanumeric code plus colored line interference.
        /// </summary>
        /// <remarks>
        /// 字符集为数字 2–9 与大小写字母（剔除 0、O、o、1、l、I、i、J、j、L 等易混淆字符）；
        /// 每个字符独立从 DrawColors 随机取色，每条干扰线独立从 ChaosColors 随机取色。
        /// 返回值为 MemoryStream 的底层缓冲区（GetBuffer），长度通常大于实际 PNG 数据、尾部含冗余字节；PNG 解码器一般可容忍。
        /// The character pool is digits 2–9 plus mixed-case letters with easily confused ones removed; each character
        /// and each interference line picks its color at random from the respective pool.
        /// The returned array is the stream's underlying buffer (GetBuffer), usually longer than the actual PNG payload
        /// with trailing unused bytes; PNG decoders normally tolerate them.
        /// </remarks>
        /// <param name="validataCode">输出：生成的验证码字符串（用户须原样输入） / Output: the generated code the user must enter verbatim.</param>
        /// <returns>PNG 图片字节数组（可能含尾部冗余字节） / The PNG image bytes (may include trailing unused bytes).</returns>
        public override byte[] CreateImage(out string validataCode)
        {
            string strFormat = "2,3,4,5,6,7,8,9,a,b,c,d,e,f,g,h,k,m,n,p,q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,K,M,N,P,Q,R,S,T,U,V,W,X,Y,Z";
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

        int validataCodeSize = 28;
        /// <summary>
        /// 验证码字符的字体大小，单位像素（默认 28） / The font size of the characters in pixels (28 by default).
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

        SKColor[] chaosColors = new SKColor[] { new SKColor(0x6B, 0x42, 0x26), new SKColor(0x4F, 0x2F, 0x4F), new SKColor(0x32, 0x99, 0xcc), new SKColor(0xCD, 0x7F, 0x32), new SKColor(0x23, 0x23, 0x8E), new SKColor(0x70, 0xDB, 0x93), new SKColor(255, 0, 0), new SKColor(0xbc, 0x8f, 0x8E) };
        /// <summary>
        /// 干扰线随机取色数组（默认 8 种彩色） / The color pool the interference lines pick from at random (8 colors by default).
        /// </summary>
        public SKColor[] ChaosColors
        {
            get { return chaosColors; }
            set { chaosColors = value; }
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

        SKColor[] drawColors = new SKColor[] { new SKColor(0x6B, 0x42, 0x26), new SKColor(0x4F, 0x2F, 0x4F), new SKColor(0x32, 0x99, 0xcc), new SKColor(0xCD, 0x7F, 0x32), new SKColor(0x23, 0x23, 0x8E), new SKColor(0x70, 0xDB, 0x93), new SKColor(255, 0, 0), new SKColor(0xbc, 0x8f, 0x8E) };
        /// <summary>
        /// 文字随机取色数组（默认 8 种彩色，逐字符随机取色） /
        /// The color pool the characters pick from at random (8 colors by default, one draw per character).
        /// </summary>
        public SKColor[] DrawColors
        {
            get { return drawColors; }
            set { drawColors = value; }
        }

        string validateCodeFont = "Arial";
        /// <summary>
        /// 字体族名（默认 Arial，须为系统已安装字体；本类不含内嵌字体） /
        /// The font family name (Arial by default; must be installed on the system - no font is embedded).
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
        /// <param name="bitMap">输出：创建好的位图 / Output: the created bitmap.</param>
        /// <param name="validataCode">验证码字符串 / The verification code string.</param>
        void ImageBmp(out SKBitmap bitMap, string validataCode)
        {
            int width = (int)(((this.validataCodeLength * this.validataCodeSize) * 1.2));
            bitMap = new SKBitmap(width, this.ImageHeight);
            CreateImageBmp(ref bitMap, validataCode);
        }

        /// <summary>
        /// 绘制验证码图片/混淆验证码图片（彩色线条 + 逐字符随机彩色文字） /
        /// Draws the code characters and the colored line interference.
        /// </summary>
        /// <remarks>绘制顺序：先画随机直线（位于文字下层，条数为字符数的 2 倍，每条独立取色），
        /// 再逐字符绘制文字（每个字符独立从 DrawColors 随机取色）；文字随机横/纵向偏移规则同 001。</remarks>
        /// <param name="bitmap">目标位图 / The target bitmap.</param>
        /// <param name="validateCode">验证码字符串 / The verification code string.</param>
        void CreateImageBmp(ref SKBitmap bitmap, string validateCode)
        {
            int minValue = (int)Math.Floor(this.ValidataCodeSize / 2M);
            int maxValue = (int)Math.Ceiling(this.ImageHeight - this.ValidataCodeSize / 2M);

            Random random = new Random();

            //字体样式
            SKTypeface typeface = SKTypeface.FromFamilyName(this.VerificationCodeFont, SKFontStyle.Normal);
            SKFont font = new SKFont(typeface, this.ValidataCodeSize);
            SKPaint paint = new SKPaint(font);
            paint.Color = DrawColors[random.Next(DrawColors.Length)];
            paint.IsAntialias = this.FontTextRenderingHint;

            SKPaint linePaint = new SKPaint();
            linePaint.Color = ChaosColors[random.Next(ChaosColors.Length)];
            linePaint.StrokeWidth = 1f;

            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                canvas.DrawColor(BackgroundColor);

                // 先画干扰线条（在文字之下）
                for (int i = 0; i < validateCode.Length * 2; i++)
                {
                    linePaint.Color = ChaosColors[random.Next(ChaosColors.Length)];
                    SKPoint point1 = new SKPoint(random.Next(bitmap.Width), random.Next(bitmap.Height));
                    SKPoint point2 = new SKPoint(random.Next(bitmap.Width), random.Next(bitmap.Height));
                    canvas.DrawLine(point1, point2, linePaint);
                }

                // 再画文字
                for (int i = 0; i < validateCode.Length; i++)
                {
                    paint.Color = DrawColors[random.Next(DrawColors.Length)];
                    SKPoint point = new SKPoint((i * this.ValidataCodeSize) + random.Next(2) + 3, random.Next(minValue, maxValue));
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
