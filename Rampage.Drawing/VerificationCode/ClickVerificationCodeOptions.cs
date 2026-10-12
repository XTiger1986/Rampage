using System;
using System.IO;

namespace Rampage.Drawing.VerificationCode
{
    /// <summary>
    /// 点选验证码的部署级静态配置（背景图库来源） /
    /// Deployment-level static options for the click captcha (background image sources).
    /// </summary>
    /// <remarks>
    ///  Description:    点选验证码静态配置
    ///                  背景图按“Provider 委托 → BackgroundImageDirectory 目录 → 算法绘制 → 噪点兜底”的顺序
    ///                  自动降级取用：Provider 返回非空字节或目录内存在可用图片时优先使用真实图片做背景，
    ///                  均未配置或取图失败时退化为算法随机绘制的背景，再退化为噪点+干扰线背景。
    ///  Description:    Backgrounds fall back automatically in the order: the Provider delegate, then the
    ///  BackgroundImageDirectory, then an algorithmically drawn background, then dot/line interference.
    ///  A real image is used when the provider returns bytes or the directory holds a usable file.
    ///
    /// </remarks>
    public static class ClickVerificationCodeOptions
    {
        /// <summary>
        /// 背景图提供委托（优先级最高）：返回图片字节（JPG/PNG/WebP 等可解码格式）即用作背景；返回 null 视为未提供 /
        /// The background provider delegate (highest priority): return decodable image bytes (JPG/PNG/WebP etc.) to
        /// use as the background; returning null counts as "not provided" and falls through.
        /// </summary>
        public static Func<byte[]?>? BackgroundImageProvider { get; set; }

        /// <summary>
        /// 背景图库目录（优先级次之）：每次生成时从目录内随机取一张图片做背景；目录不存在或无可用图片时降级 /
        /// The background image directory (second priority): one image is picked at random per generation;
        /// a missing directory or no usable image falls through.
        /// </summary>
        /// <remarks>支持 jpg/jpeg/png/webp/bmp 扩展名（大小写不敏感）；目录需对运行进程可读 /
        /// Extensions jpg/jpeg/png/webp/bmp are recognized (case-insensitive); the directory must be readable by the process.</remarks>
        public static string? BackgroundImageDirectory { get; set; }

        /// <summary>
        /// 英文单词池覆盖（供 WordClickVerificationCode 取词）：非空时使用使用方词表，null（默认）用内置常用词表 /
        /// The English word pool override for WordClickVerificationCode: when set it wins, null (default) uses the built-in pool.
        /// </summary>
        /// <remarks>词表内容建议全小写；实现内部会做空白过滤、转小写与去重后再取词 /
        /// Entries should be lowercase; the implementation trims, lowercases and deduplicates before picking.</remarks>
        public static string[]? EnglishWordPool { get; set; }

        /// <summary>
        /// 从配置的背景来源取一张背景图字节（供生成器调用的统一入口，内部已按优先级降级） /
        /// Fetches background image bytes from the configured sources (single entry used by the generator; priority-based fallback applied).
        /// </summary>
        /// <param name="random">随机源 / The random source.</param>
        /// <returns>背景图字节；所有来源均未命中时返回 null（由生成器继续降级） /
        /// The background bytes, or null when no source matched (the generator keeps falling back).</returns>
        internal static byte[]? TryGetBackgroundImageBytes(Random random)
        {
            // 1. 委托来源 / Delegate source.
            Func<byte[]?>? provider = BackgroundImageProvider;
            if (provider != null)
            {
                try
                {
                    byte[]? bytes = provider();
                    if (bytes != null && bytes.Length > 0)
                    {
                        return bytes;
                    }
                }
                catch
                {
                    // 委托异常视同未提供，继续降级 / A throwing delegate counts as "not provided"; keep falling back.
                }
            }

            // 2. 目录来源 / Directory source.
            string? directory = BackgroundImageDirectory;
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                try
                {
                    string[] files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);
                    // 先过滤出可用图片再随机取，避免命中不可用文件后误降级 / Filter first, then pick, so one bad file does not trigger a fallback.
                    string[] images = Array.FindAll(files, IsSupportedImageFile);
                    if (images.Length > 0)
                    {
                        string picked = images[random.Next(images.Length)];
                        return File.ReadAllBytes(picked);
                    }
                }
                catch
                {
                    // 目录读取异常视同未配置，继续降级 / A read failure counts as "not configured"; keep falling back.
                }
            }

            return null;
        }

        /// <summary>
        /// 判断文件是否为支持的背景图扩展名 / Determines whether the file has a supported background image extension.
        /// </summary>
        /// <param name="path">文件路径 / The file path.</param>
        /// <returns>支持返回 true / true when the extension is supported.</returns>
        private static bool IsSupportedImageFile(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".webp":
                case ".bmp":
                    return true;
                default:
                    return false;
            }
        }
    }
}
