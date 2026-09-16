using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 线条干扰(彩色)
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 线条干扰(彩色)
    ///                  
    ///  History:        2015/4/28 19:16:04 创建
    ///                  
    /// </remarks>
    public class VerificationCode008 : VerificationCodeBase
    {
        /// <summary>
        /// 
        /// </summary>
        public override string Name
        {
            get { return "线条干扰(彩色)"; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="validataCode"></param>
        /// <returns></returns>
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
        /// 验证码美化
        /// </summary>
        bool FontTextRenderingHint
        {
            get { return fontTextRenderingHint; }
            set { fontTextRenderingHint = value; }
        }

        int validataCodeLength = 4;
        /// <summary>
        /// 验证码长度
        /// </summary>
        public int ValidataCodeLength
        {
            get { return validataCodeLength; }
            set { validataCodeLength = value; }
        }

        int validataCodeSize = 28;
        /// <summary>
        /// 验证码字体大小
        /// </summary>
        public int ValidataCodeSize
        {
            get { return validataCodeSize; }
            set { validataCodeSize = value; }
        }

        int imageHeight = 56;
        /// <summary>
        /// 图片高度
        /// </summary>
        public int ImageHeight
        {
            get { return imageHeight; }
            set { imageHeight = value; }
        }

        SKColor[] chaosColors = new SKColor[] { new SKColor(0x6B, 0x42, 0x26), new SKColor(0x4F, 0x2F, 0x4F), new SKColor(0x32, 0x99, 0xcc), new SKColor(0xCD, 0x7F, 0x32), new SKColor(0x23, 0x23, 0x8E), new SKColor(0x70, 0xDB, 0x93), new SKColor(255, 0, 0), new SKColor(0xbc, 0x8f, 0x8E) };
        /// <summary>
        /// 输出燥点的颜色
        /// </summary>
        public SKColor[] ChaosColors
        {
            get { return chaosColors; }
            set { chaosColors = value; }
        }

        SKColor backgroundColor = new SKColor(255, 255, 255);
        /// <summary>
        /// 自定义背景色(默认白色)
        /// </summary>
        public SKColor BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        SKColor[] drawColors = new SKColor[] { new SKColor(0x6B, 0x42, 0x26), new SKColor(0x4F, 0x2F, 0x4F), new SKColor(0x32, 0x99, 0xcc), new SKColor(0xCD, 0x7F, 0x32), new SKColor(0x23, 0x23, 0x8E), new SKColor(0x70, 0xDB, 0x93), new SKColor(255, 0, 0), new SKColor(0xbc, 0x8f, 0x8E) };
        /// <summary>
        /// 自定义颜色
        /// </summary>
        public SKColor[] DrawColors
        {
            get { return drawColors; }
            set { drawColors = value; }
        }

        string validateCodeFont = "Arial";
        /// <summary>
        /// 自定义字体
        /// </summary>
        public string VerificationCodeFont
        {
            get { return validateCodeFont; }
            set { validateCodeFont = value; }
        }

        #endregion

        #region CreateImage
        void ImageBmp(out SKBitmap bitMap, string validataCode)
        {
            int width = (int)(((this.validataCodeLength * this.validataCodeSize) * 1.2));
            bitMap = new SKBitmap(width, this.ImageHeight);
            CreateImageBmp(ref bitMap, validataCode);
        }

        /// <summary>
        /// 绘制验证码图片/混淆验证码图片
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="validateCode"></param>
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
        /// 获取随机数
        /// </summary>
        /// <param name="formatString"></param>
        /// <param name="len"></param>
        /// <param name="codeString"></param>
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
