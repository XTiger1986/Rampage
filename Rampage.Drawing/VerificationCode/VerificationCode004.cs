using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 噪点干扰(扭曲) / Verification code with dot interference and sine-wave distortion.
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 噪点干扰(扭曲)
    ///                  唯一应用波形扭曲的类型：先绘制噪点与文字，再经 TwistImage 做正弦扭曲；
    ///                  数字字母混合（已剔除易混淆字符），噪点支持点/块/线三种模式。
    ///
    ///  History:        2015/4/28 19:16:04 创建
    ///
    /// </remarks>
    public class VerificationCode004 : VerificationCodeBase
    {
        /// <summary>
        /// 获取样式名称：噪点干扰(扭曲) / Gets the style name: dot interference (distorted).
        /// </summary>
        public override string Name
        {
            get { return "噪点干扰(扭曲)"; }
        }

        /// <summary>
        /// 生成验证码图片：随机数字字母混合码 + 噪点干扰 + 正弦扭曲 /
        /// Generate the image: random alphanumeric code, dot interference, then sine-wave distortion.
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

        int contortRange = 2;
        /// <summary>
        /// 扭曲幅度（正弦波像素偏移的倍数，越大扭曲越强，默认 2） /
        /// The distortion amplitude (multiplier of the sine pixel offset; larger = stronger twist, default 2).
        /// </summary>
        public int ContortRange
        {
            get { return contortRange; }
            set { contortRange = value; }
        }

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
        /// 噪点颜色（默认暗黄 #AAAA33） / The noise color (#AAAA33 by default).
        /// </summary>
        public SKColor ChaosColor
        {
            get { return chaosColor; }
            set { chaosColor = value; }
        }

        int chaosMode = 1;
        /// <summary>
        /// 噪点模式：1 点式（默认，3×3 小方块）2 块式（3×3 方块、粗笔刷）3 线式（随机短线）；其他值按点式处理 /
        /// Noise mode: 1 dots (default, 3×3 rects), 2 blocks (3×3 rects with a thick stroke), 3 lines (random short lines);
        /// any other value falls back to dots.
        /// </summary>
        public int ChaosMode
        {
            get { return chaosMode; }
            set { chaosMode = value; }
        }

        SKColor backgroundColor = new SKColor(255, 255, 255);
        /// <summary>
        /// 背景色（默认白色），亦作为扭曲后未被映射像素的填充色 /
        /// The background color (white by default); also fills pixels left unmapped after distortion.
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

        #region 产生波形滤镜效果

        private const double PI = 3.1415926535897932384626433832795;
        private const double PI2 = 6.283185307179586476925286766559;

        /// <summary>
        /// 正弦曲线 Wave 扭曲图片，返回新位图（源图不被修改） /
        /// Applies a sine-wave distortion and returns a new bitmap (the source is left untouched).
        /// </summary>
        /// <remarks>
        /// 逐像素重采样：按正弦值把每个源像素偏移后写入目标位图；偏移出画布的像素被丢弃，
        /// 未被覆盖的目标点保持背景色。实现基于 GetPixel/SetPixel，大图下性能较差。
        /// Per-pixel resampling: each source pixel is written to the destination offset by a sine value;
        /// pixels shifted off the canvas are dropped and uncovered destination points keep the background color.
        /// Built on GetPixel/SetPixel, which is slow for large images.
        /// </remarks>
        /// <param name="srcBmp">待扭曲的源位图 / The source bitmap to distort.</param>
        /// <param name="bXDir">波形方向：true 时沿纵向传播、像素随行横向偏移；false 时沿横向传播、像素随列纵向偏移 /
        /// The wave direction: true propagates vertically, shifting pixels horizontally per row; false propagates horizontally, shifting pixels vertically per column.</param>
        /// <param name="dMultValue">波形幅度倍数，越大扭曲越强（本类默认经 ContortRange 取 2） /
        /// The amplitude multiplier; larger means stronger distortion (this class passes ContortRange, default 2).</param>
        /// <param name="dPhase">波形起始相位，取值区间 [0, 2π) / The wave start phase, in [0, 2*PI).</param>
        /// <returns>扭曲后的新位图 / The distorted bitmap.</returns>
        public SKBitmap TwistImage(SKBitmap srcBmp, bool bXDir, double dMultValue, double dPhase)
        {
            SKBitmap destBmp = new SKBitmap(srcBmp.Width, srcBmp.Height);

            using (SKCanvas canvas = new SKCanvas(destBmp))
            {
                canvas.DrawColor(this.BackgroundColor);

                // 波形基轴长度：bXDir 为 true 时按高度归一化（波沿行传播），否则按宽度 / Base axis used to normalize the phase
                double dBaseAxisLen = bXDir ? (double)destBmp.Height : (double)destBmp.Width;

                for (int i = 0; i < destBmp.Width; i++)
                {
                    for (int j = 0; j < destBmp.Height; j++)
                    {
                        double dx = 0;
                        dx = bXDir ? (PI2 * (double)j) / dBaseAxisLen : (PI2 * (double)i) / dBaseAxisLen;
                        dx += dPhase;
                        double dy = Math.Sin(dx);

                        // 按正弦值计算偏移后的目标坐标 / Compute the destination coordinates shifted by the sine value
                        int nOldX = 0, nOldY = 0;
                        nOldX = bXDir ? i + (int)(dy * dMultValue) : i;
                        nOldY = bXDir ? j : j + (int)(dy * dMultValue);

                        SKColor color = srcBmp.GetPixel(i, j);
                        if (nOldX >= 0 && nOldX < destBmp.Width
                         && nOldY >= 0 && nOldY < destBmp.Height)
                        {
                            destBmp.SetPixel(nOldX, nOldY, color);
                        }
                    }
                }
            }

            return destBmp;
        }

        #endregion

        #endregion

        #region CreateImage
        /// <summary>
        /// 按验证码长度与字号计算宽度并创建位图，绘制后沿纵向做正弦扭曲 /
        /// Creates the bitmap sized from the code length and font size, draws it, then applies a vertical sine-wave distortion.
        /// </summary>
        /// <remarks>扭曲参数：方向为纵向（bXDir=true）、幅度取 <see cref="ContortRange"/>、起始相位固定为 6 弧度。</remarks>
        /// <param name="bitmap">输出：创建并扭曲后的位图 / Output: the created and distorted bitmap.</param>
        /// <param name="validataCode">验证码字符串 / The verification code string.</param>
        void ImageBmp(out SKBitmap bitmap, string validataCode)
        {
            int width = (int)(((this.ValidataCodeLength * this.ValidataCodeSize) * 1.3) + 4);
            bitmap = new SKBitmap(width, this.ImageHeight);
            CreateImageBmp(ref bitmap, validataCode);
            bitmap = TwistImage(bitmap, true, this.ContortRange, 6);
        }

        /// <summary>
        /// 绘制验证码图片(随机字符串)/混淆验证码图片 /
        /// Draws the code characters and the noise interference.
        /// </summary>
        /// <remarks>绘制顺序：先按 <see cref="ChaosMode"/> 画噪点（点式/块式为字符数 20 倍的 3×3 方块，线式为字符数 5 倍的随机短线，
        /// 均在文字之下、扭曲之前），再逐字符绘制文字；文字随机横/纵向偏移规则同 001。</remarks>
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

                // 先画噪点干扰（在文字之下，扭曲前）
                switch (this.ChaosMode)
                {
                    case 1: //点式
                    default:
                        for (int i = 0; i < validateCode.Length * 20; i++)
                        {
                            int x = random.Next(bitmap.Width);
                            int y = random.Next(bitmap.Height);
                            canvas.DrawRect(x, y, 3, 3, dotPaint);
                        }
                        break;
                    case 2: //块式
                        dotPaint.StrokeWidth = 4f;
                        for (int i = 0; i < validateCode.Length * 20; i++)
                        {
                            int x = random.Next(bitmap.Width);
                            int y = random.Next(bitmap.Height);
                            canvas.DrawRect(x, y, 3, 3, dotPaint);
                        }
                        break;
                    case 3: //线式
                        dotPaint.StrokeWidth = 1f;
                        for (int i = 0; i < validateCode.Length * 5; i++)
                        {
                            SKPoint point1 = new SKPoint(random.Next(bitmap.Width), random.Next(bitmap.Height));
                            SKPoint point2 = new SKPoint(random.Next(bitmap.Width), random.Next(bitmap.Height));

                            canvas.DrawLine(point1, point2, dotPaint);
                        }
                        break;
                }

                // 再画文字（噪点之上）
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
