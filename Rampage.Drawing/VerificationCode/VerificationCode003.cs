using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 线条噪点干扰(蓝色) / Verification code with blue line and dot interference.
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 线条噪点干扰(蓝色)
    ///                  001 与 002 的组合：数字字母混合（已剔除易混淆字符），同时叠加方块噪点与随机直线干扰。
    ///
    ///  History:        2015/4/28 19:16:04 创建
    ///
    /// </remarks>
    public class VerificationCode003 : VerificationCodeBase
    {
        /// <summary>
        /// 获取样式名称：线条噪点干扰(蓝色) / Gets the style name: line + dot interference (blue).
        /// </summary>
        public override string Name
        {
            get { return "线条噪点干扰(蓝色)"; }
        }
        /// <summary>
        /// 生成验证码图片：随机数字字母混合码 + 噪点与线条双重干扰 /
        /// Generate the image: random alphanumeric code plus both dot and line interference.
        /// </summary>
        /// <remarks>
        /// 字符集为数字 2–9 与大小写字母（剔除 0、O、o、1、l、I、i、J、j、L 等易混淆字符）。
        /// 返回值为 MemoryStream 的底层缓冲区（GetBuffer），长度通常大于实际 PNG 数据、尾部含冗余字节；PNG 解码器一般可容忍。
        /// The character pool is digits 2–9 plus mixed-case letters with easily confused ones removed.
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

        SKColor chaosColor = new SKColor(0xaa, 0xaa, 0x33);
        /// <summary>
        /// 噪点颜色（默认暗黄 #AAAA33） / The noise dot color (#AAAA33 by default).
        /// </summary>
        public SKColor ChaosColor
        {
            get { return chaosColor; }
            set { chaosColor = value; }
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
        /// 绘制验证码图片(随机字符串)/混淆验证码图片(干扰线条噪点) /
        /// Draws the code characters with combined dot and line interference.
        /// </summary>
        /// <remarks>绘制顺序：先画 2×2 方块噪点（数量为字符数的 6 倍）与随机直线（条数为字符数的 2 倍），
        /// 均位于文字下层，再逐字符绘制文字；文字随机横/纵向偏移规则同 001。</remarks>
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

            SKPaint linePaint = new SKPaint();
            linePaint.Color = new SKColor(0x22, 0x77, 0xaa);
            linePaint.StrokeWidth = 0.6f;
            linePaint.IsAntialias = true;

            Random random = new Random();

            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                canvas.DrawColor(BackgroundColor);

                // 先画背景干扰（在文字之下）：噪点 + 线条
                for (int i = 0; i < validateCode.Length * 6; i++)
                {
                    int x = random.Next(bitmap.Width);
                    int y = random.Next(bitmap.Height);
                    canvas.DrawRect(x, y, 2, 2, dotPaint);
                }

                for (int i = 0; i < validateCode.Length * 2; i++)
                {
                    SKPoint point1 = new SKPoint(random.Next(bitmap.Width), random.Next(bitmap.Height));
                    SKPoint point2 = new SKPoint(random.Next(bitmap.Width), random.Next(bitmap.Height));
                    canvas.DrawLine(point1, point2, linePaint);
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
