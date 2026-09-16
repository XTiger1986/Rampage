using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 噪点干扰(扭曲)
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 噪点干扰(扭曲)
    ///                  
    ///  History:        2015/4/28 19:16:04 创建
    ///                  
    /// </remarks>
    public class VerificationCode004 : VerificationCodeBase
    {
        /// <summary>
        /// 
        /// </summary>
        public override string Name
        {
            get { return "噪点干扰(扭曲)"; }
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

        int contortRange = 2;
        /// <summary>
        /// 扭曲幅度
        /// </summary>
        public int ContortRange
        {
            get { return contortRange; }
            set { contortRange = value; }
        }

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
        /// 验证码长度(默认4个验证码的长度)
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

        SKColor chaosColor = new SKColor(0xaa, 0xaa, 0x33);
        /// <summary>
        /// 输出燥点的颜色
        /// </summary>
        public SKColor ChaosColor
        {
            get { return chaosColor; }
            set { chaosColor = value; }
        }

        int chaosMode = 1;
        /// <summary>
        /// 燥点模式(1：点式  2：块式  3：线式)
        /// </summary>
        public int ChaosMode
        {
            get { return chaosMode; }
            set { chaosMode = value; }
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

        SKColor drawColor = new SKColor(0x32, 0x99, 0xcc);
        /// <summary>
        /// 自定义颜色
        /// </summary>
        public SKColor DrawColor
        {
            get { return drawColor; }
            set { drawColor = value; }
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

        #region 产生波形滤镜效果

        private const double PI = 3.1415926535897932384626433832795;
        private const double PI2 = 6.283185307179586476925286766559;

        /// <summary>
        /// 正弦曲线Wave扭曲图片
        /// </summary>
        /// <param name="srcBmp">图片路径</param>
        /// <param name="bXDir">如果扭曲则选择为True</param>
        /// <param name="dMultValue">波形的幅度倍数，越大扭曲的程度越高，一般为3</param>
        /// <param name="dPhase">波形的起始相位，取值区间[0-2*PI)</param>
        /// <returns></returns>
        public SKBitmap TwistImage(SKBitmap srcBmp, bool bXDir, double dMultValue, double dPhase)
        {
            SKBitmap destBmp = new SKBitmap(srcBmp.Width, srcBmp.Height);

            using (SKCanvas canvas = new SKCanvas(destBmp))
            {
                canvas.DrawColor(this.BackgroundColor);

                double dBaseAxisLen = bXDir ? (double)destBmp.Height : (double)destBmp.Width;

                for (int i = 0; i < destBmp.Width; i++)
                {
                    for (int j = 0; j < destBmp.Height; j++)
                    {
                        double dx = 0;
                        dx = bXDir ? (PI2 * (double)j) / dBaseAxisLen : (PI2 * (double)i) / dBaseAxisLen;
                        dx += dPhase;
                        double dy = Math.Sin(dx);

                        // 取得当前点的颜色
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
        void ImageBmp(out SKBitmap bitmap, string validataCode)
        {
            int width = (int)(((this.ValidataCodeLength * this.ValidataCodeSize) * 1.3) + 4);
            bitmap = new SKBitmap(width, this.ImageHeight);
            CreateImageBmp(ref bitmap, validataCode);
            bitmap = TwistImage(bitmap, true, this.ContortRange, 6);
        }

        /// <summary>
        /// 绘制验证码图片(随机字符串)/混淆验证码图片
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="validateCode"></param>
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
