using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 2年级算术(中文蓝色)
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 2年级算术(中文蓝色)
    ///                  
    ///  History:        2015/4/28 19:16:04 创建
    ///                  
    /// </remarks>
    public class VerificationCode006 : VerificationCodeBase
    {
        /// <summary>
        /// 
        /// </summary>
        public override string Name
        {
            get { return "2年级算术(中文蓝色)"; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override string Tip
        {
            get
            {
                return "输入计算结果";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resultCode"></param>
        /// <returns></returns>
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
        /// 验证码美化
        /// </summary>
        bool FontTextRenderingHint
        {
            get { return fontTextRenderingHint; }
            set { fontTextRenderingHint = value; }
        }

        int validataCodeSize = 18;
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

        #endregion

        #region CreateImage
        void ImageBmp(out SKBitmap bitmap, string validataCode)
        {
            int width = (int)(((validataCode.Length * this.ValidataCodeSize) * 1.3) + 10);
            bitmap = new SKBitmap(width, this.ImageHeight);
            CreateImageBmp(ref bitmap, validataCode);
        }

        /// <summary>
        /// 绘制验证码图片/混淆验证码图片
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="validateCode"></param>
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
        /// 
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="codeString"></param>
        /// <param name="resultCode"></param>
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
                if (num2 < num1)
                {
                    int temp = num1;
                    num1 = num2;
                    num2 = temp;
                }

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
