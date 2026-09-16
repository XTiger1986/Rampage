using SkiaSharp;
using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 验证码 - 中文(蓝色)
    /// </summary>
    /// <remarks>
    ///  Description:    验证码 - 中文(蓝色)
    ///                  
    ///  History:        2015/4/28 19:16:04 创建
    ///                  
    /// </remarks>
    public class VerificationCode005 : VerificationCodeBase
    {

        /// <summary>
        /// 
        /// </summary>
        public override string Name
        {
            get { return "中文(蓝色)"; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool DisableIme
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="validataCode"></param>
        /// <returns></returns>
        public override byte[] CreateImage(out string validataCode)
        {
            string strFormat = "丰,王,井,开,夫,天,无,元,专,云,扎,艺,木,五,支,厅,不,太,犬,区,历,尤,友,匹,车,巨,牙,屯,比,互,切,瓦,止,少,日,中,冈,贝,内,水,见,午,牛,手,毛,气,升,长,仁,什,片,仆,化,仇,币,仍,仅,斤,爪,反,介,父,从,今,凶,分,乏,公,仓,月,氏,勿,欠,风,丹,匀,乌,凤,勾,文,六,方,火,为,斗,忆,订,计,户,认,心,尺,引,丑,巴,孔,队,办,以,允,予,劝,双,书,幻,玉,刊,示,末,未,击,打,巧,正,扑,扒,功,扔,去,甘,世,古,节,本,术,可,丙,左,厉,右,石,布,龙,平,灭,轧,东,卡,北,占,业,旧,帅,归,且,旦,奏,春,帮,珍,玻,毒,型,挂,封,持,项,垮,挎,城,挠,政,赴,赵,挡,挺,括,拴,拾,挑,指,垫,挣,挤,拼,挖,按,挥,挪,某,甚,革,荐,巷,带,草,茧,茶,荒,茫,荡,荣,故,胡,南,药,标,枯,柄,栋,相,查,柏,柳,柱,柿,栏,树,要,咸,威,歪,研,砖,厘,厚,砌,砍,面,耐,耍,牵,残,殃,轻,鸦,皆,背,战,点,临,览,竖,省,削,尝,是,盼,眨,哄,显,哑,冒,映,星,昨,畏,趴,胃,贵,界,虹,虾,蚁,思,蚂,虽,品,咽,骂,哗,咱,响,哈,咬,咳,哪,炭,峡,罚,贱,贴,骨,钞,钟,钢,钥,钩,卸,缸,拜,看,矩,怎,牲,选,适,秒,香,种,秋,科,重,复,竿,段,便,俩,贷,顺,修,保,促,侮,俭,俗,俘,信,皇,泉,鬼,侵,追,俊,盾,待,律,很,须,叙,剑,逃,食,盆,胆,胜,胞,胖,脉,勉,狭,狮,独,狡,狱,狠,贸,怨,急,饶,蚀,饺,饼,弯,将,奖,哀,亭,亮,度,迹,庭,疮,疯,疫,疤,姿,亲,音,帝,施,闻,阀,阁,差,养,美,姜,叛,送,类,迷,前,首,逆,总,炼,炸,炮,烂,剃,洁,洪,洒,浇,浊,洞,测,洗,活,派,洽,染,济,洋,洲,浑,浓,津,恒,恢,恰,恼,恨,举,觉,宣,室,宫,宪,突,穿,窃,客,冠,语,扁,袄,祖,神,祝,误,诱,说,诵,垦,退,既,屋,昼,费,陡,眉,孩,除,险,院,娃,姥,姨,姻,娇,怒,架,贺,盈,勇,怠,柔,垒,绑,绒,结,绕,骄,绘,给,络,骆,绝,绞,统";
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
            int width = (int)(((this.ValidataCodeLength * this.ValidataCodeSize) * 1.3) + 10);
            bitmap = new SKBitmap(width, this.ImageHeight);
            CreateImageBmp(ref bitmap, validataCode);
        }

        /// <summary>
        /// 绘制验证码图片/混淆验证码图片
        /// </summary>
        /// <param name="bitMap"></param>
        /// <param name="validateCode"></param>
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
