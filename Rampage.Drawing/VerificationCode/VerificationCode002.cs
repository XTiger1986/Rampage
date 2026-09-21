using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 噪点干扰(蓝色) / Verification code with blue dot interference.
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 噪点干扰(蓝色)
    ///                  纯字母字符集（不含数字），已剔除 o/i/j/l/O/I/J/L 等易混淆字符；干扰为暗黄色 2×2 方块噪点。
    ///
    ///  History:        2015/4/28 17:38:45 创建
    ///
    /// </remarks>
    public class VerificationCode002 : VerificationCodeBase
    {
        /// <summary>
        /// 获取样式名称：噪点干扰(蓝色) / Gets the style name: dot interference (blue).
        /// </summary>
        public override string Name
        {
            get { return "噪点干扰(蓝色)"; }
        }

        /// <summary>
        /// 生成验证码图片：随机纯字母码 + 方块噪点 / Generate the image: random letters-only code plus dot interference.
        /// </summary>
        /// <remarks>
        /// 字符集仅大小写字母（无数字，剔除 0/O/o/1/l/I/i/J/j/L 等易混淆字符）。
        /// 返回值为 MemoryStream 的底层缓冲区（GetBuffer），长度通常大于实际 PNG 数据、尾部含冗余字节；PNG 解码器一般可容忍。
        /// The pool contains mixed-case letters only (no digits; easily confused ones removed).
        /// The returned array is the stream's underlying buffer (GetBuffer), usually longer than the actual PNG payload
        /// with trailing unused bytes; PNG decoders normally tolerate them.
        /// </remarks>
        /// <param name="validataCode">输出：生成的验证码字符串（用户须原样输入） / Output: the generated code the user must enter verbatim.</param>
        /// <returns>PNG 图片字节数组（可能含尾部冗余字节） / The PNG image bytes (may include trailing unused bytes).</returns>
        public override byte[] CreateImage(out string validataCode)
        {
            string strFormat = "a,b,c,d,e,f,g,h,k,m,n,p,q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,K,M,N,P,Q,R,S,T,U,V,W,X,Y,Z";
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
        /// <param name="bitmap">输出：创建好的位图 / Output: the created bitmap.</param>
        /// <param name="validataCode">验证码字符串 / The verification code string.</param>
        void ImageBmp(out SKBitmap bitmap, string validataCode)
        {
            int width = (int)(((this.ValidataCodeLength * this.ValidataCodeSize) * 1.3) + 4);
            bitmap = new SKBitmap(width, this.ImageHeight);

            CreateImageBmp(ref bitmap, validataCode);
        }

        /// <summary>
        /// 绘制验证码图片(随机字符串)/混淆验证码图片(噪点干扰) /
        /// Draws the code characters and the dot interference.
        /// </summary>
        /// <remarks>绘制顺序：先画 2×2 方块噪点（位于文字下层，数量为字符数的 10 倍），再逐字符绘制文字；
        /// 每个字符在字符宽度槽位内随机横向偏移，并在 [字号/2, 画高-字号/2] 区间随机纵向偏移。</remarks>
        /// <param name="bitmap">目标位图 / The target bitmap.</param>
        /// <param name="validateCode">验证码字符串 / The verification code string.</param>
        void CreateImageBmp(ref SKBitmap bitmap, string validateCode)
        {
            int minValue = (int)Math.Floor(this.ValidataCodeSize / 2M);
            int maxValue = (int)Math.Ceiling(this.ImageHeight - this.ValidataCodeSize / 2M);
            //字体样式
            SKTypeface typeface = SKTypeface.FromFamilyName(this.VerificationCodeFont, SKFontStyle.Normal);
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
