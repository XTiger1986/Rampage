using SkiaSharp;
using System;

namespace Rampage.Drawing
{
    /// <summary>
    /// 图片格式转换工具（PNG ↔ WebP） / Image format conversion utility (PNG ↔ WebP).
    /// </summary>
    /// <remarks>
    /// 两个方向的输入均支持字节数组、流与文件路径；输出支持字节数组、流（*Stream 后缀，已回卷到起始位置）与文件（*File 后缀）。
    /// PNG → WebP 经 PngToWebp* 系列方法（可选有损质量或无损），WebP → PNG 经 WebpToPng* 系列方法（PNG 恒为无损，无质量参数）。
    /// In both directions inputs accept byte arrays, streams and file paths; outputs produce byte arrays, streams
    /// (the *Stream suffix, rewound to the beginning) and files (the *File suffix). PNG → WebP goes through the
    /// PngToWebp* family (lossy quality or lossless); WebP → PNG goes through the WebpToPng* family (PNG is always lossless, no quality argument).
    /// </remarks>
    public class ImageConvert
	{

        /// <summary>
        /// 将 PNG 图片字节转换为 WebP 字节 / Converts PNG image bytes to WebP bytes.
        /// </summary>
        /// <remarks>
        /// 有损模式（lossless=false）按 quality 1~100 编码，数值越大画质越好、体积越大（80~90 为常用平衡点）；
        /// 无损模式（lossless=true）画质与原图一致，quality 退化为压缩努力程度（越大越慢但体积越小）。
        /// PNG 的透明通道会被完整保留。方法不校验输入确为 PNG——任何 SkiaSharp 可解码的格式（JPG/BMP/GIF/WebP 等）均可借此转为 WebP。
        /// Lossy mode (lossless=false) encodes with quality 1-100, where higher means better quality and larger size
        /// (80-90 is the usual sweet spot); in lossless mode (lossless=true) the quality degrades to compression
        /// effort (slower but smaller as it rises). The PNG alpha channel is fully preserved. The method does not
        /// verify the input is actually PNG — any format SkiaSharp can decode (JPG/BMP/GIF/WebP etc.) can be converted to WebP.
        /// </remarks>
        /// <param name="pngBytes">PNG 图片的字节数组 / The raw bytes of the PNG image.</param>
        /// <param name="quality">编码质量（1~100，超出按边界夹取；无损时表示压缩努力程度）/ Encoding quality (1-100, clamped at the bounds; compression effort in lossless mode).</param>
        /// <param name="lossless">是否无损编码 / Whether to encode losslessly.</param>
        /// <returns>WebP 图片的字节数组 / The WebP image bytes.</returns>
        /// <exception cref="Exception">字节流无法解码为图片时抛出 / Thrown when the bytes cannot be decoded as an image.</exception>
        public static byte[] PngToWebp(byte[] pngBytes, int quality = 90, bool lossless = false)
        {
            using var original = SKBitmap.Decode(pngBytes);
            if (original == null)
            {
                throw new Exception("Unable to decode the image.");
            }

            using var image = SKImage.FromBitmap(original);
            using var pixels = image.PeekPixels();
            if (pixels == null)
            {
                throw new Exception("Unable to read the image pixels.");
            }

            // SKWebpEncoderOptions.Quality 为 0~1 的 float（1.0 即 100）/ SKWebpEncoderOptions.Quality is a float in [0,1] (1.0 == 100)
            SKWebpEncoderOptions options = new SKWebpEncoderOptions(
                lossless ? SKWebpEncoderCompression.Lossless : SKWebpEncoderCompression.Lossy,
                Math.Clamp(quality, 1, 100) / 100f);
            using var data = pixels.Encode(options);
            return data.ToArray();
        }

        /// <summary>
        /// 将 PNG 图片流转换为 WebP 字节 / Converts a PNG image stream to WebP bytes.
        /// </summary>
        /// <param name="pngStream">PNG 图片流（可读即可，无需可定位）/ The PNG image stream (readable; seekability not required).</param>
        /// <param name="quality">编码质量（1~100，无损时表示压缩努力程度）/ Encoding quality (1-100; compression effort in lossless mode).</param>
        /// <param name="lossless">是否无损编码 / Whether to encode losslessly.</param>
        /// <returns>WebP 图片的字节数组 / The WebP image bytes.</returns>
        /// <exception cref="Exception">字节流无法解码为图片时抛出 / Thrown when the bytes cannot be decoded as an image.</exception>
        public static byte[] PngToWebp(Stream pngStream, int quality = 90, bool lossless = false)
        {
            using var buffer = new MemoryStream();
            pngStream.CopyTo(buffer);
            return PngToWebp(buffer.ToArray(), quality, lossless);
        }

        /// <summary>
        /// 将 PNG 文件转换为 WebP 字节 / Converts a PNG file to WebP bytes.
        /// </summary>
        /// <param name="pngFilePath">源 PNG 文件路径 / The source PNG file path.</param>
        /// <param name="quality">编码质量（1~100，无损时表示压缩努力程度）/ Encoding quality (1-100; compression effort in lossless mode).</param>
        /// <param name="lossless">是否无损编码 / Whether to encode losslessly.</param>
        /// <returns>WebP 图片的字节数组 / The WebP image bytes.</returns>
        /// <exception cref="Exception">字节流无法解码为图片时抛出 / Thrown when the bytes cannot be decoded as an image.</exception>
        public static byte[] PngToWebp(string pngFilePath, int quality = 90, bool lossless = false)
        {
            return PngToWebp(File.ReadAllBytes(pngFilePath), quality, lossless);
        }

        /// <summary>
        /// 将 PNG 图片字节转换为 WebP 流（已回卷到起始位置）/ Converts PNG image bytes to a WebP stream (rewound to its beginning).
        /// </summary>
        /// <param name="pngBytes">PNG 图片的字节数组 / The raw bytes of the PNG image.</param>
        /// <param name="quality">编码质量（1~100，无损时表示压缩努力程度）/ Encoding quality (1-100; compression effort in lossless mode).</param>
        /// <param name="lossless">是否无损编码 / Whether to encode losslessly.</param>
        /// <returns>已定位到起始位置的 WebP 流 / A WebP stream rewound to its beginning.</returns>
        public static Stream PngToWebpStream(byte[] pngBytes, int quality = 90, bool lossless = false)
        {
            return new MemoryStream(PngToWebp(pngBytes, quality, lossless));
        }

        /// <summary>
        /// 将 PNG 图片流转换为 WebP 流（已回卷到起始位置）/ Converts a PNG image stream to a WebP stream (rewound to its beginning).
        /// </summary>
        /// <param name="pngStream">PNG 图片流（可读即可，无需可定位）/ The PNG image stream (readable; seekability not required).</param>
        /// <param name="quality">编码质量（1~100，无损时表示压缩努力程度）/ Encoding quality (1-100; compression effort in lossless mode).</param>
        /// <param name="lossless">是否无损编码 / Whether to encode losslessly.</param>
        /// <returns>已定位到起始位置的 WebP 流 / A WebP stream rewound to its beginning.</returns>
        public static Stream PngToWebpStream(Stream pngStream, int quality = 90, bool lossless = false)
        {
            return new MemoryStream(PngToWebp(pngStream, quality, lossless));
        }

        /// <summary>
        /// 将 PNG 文件转换为 WebP 流（已回卷到起始位置）/ Converts a PNG file to a WebP stream (rewound to its beginning).
        /// </summary>
        /// <param name="pngFilePath">源 PNG 文件路径 / The source PNG file path.</param>
        /// <param name="quality">编码质量（1~100，无损时表示压缩努力程度）/ Encoding quality (1-100; compression effort in lossless mode).</param>
        /// <param name="lossless">是否无损编码 / Whether to encode losslessly.</param>
        /// <returns>已定位到起始位置的 WebP 流 / A WebP stream rewound to its beginning.</returns>
        public static Stream PngToWebpStream(string pngFilePath, int quality = 90, bool lossless = false)
        {
            return new MemoryStream(PngToWebp(pngFilePath, quality, lossless));
        }

        /// <summary>
        /// 将 PNG 图片字节写入 WebP 文件 / Converts PNG image bytes and writes them to a WebP file.
        /// </summary>
        /// <param name="pngBytes">PNG 图片的字节数组 / The raw bytes of the PNG image.</param>
        /// <param name="webpFilePath">目标 WebP 文件路径 / The destination WebP file path.</param>
        /// <param name="quality">编码质量（1~100，无损时表示压缩努力程度）/ Encoding quality (1-100; compression effort in lossless mode).</param>
        /// <param name="lossless">是否无损编码 / Whether to encode losslessly.</param>
        public static void PngToWebpFile(byte[] pngBytes, string webpFilePath, int quality = 90, bool lossless = false)
        {
            File.WriteAllBytes(webpFilePath, PngToWebp(pngBytes, quality, lossless));
        }

        /// <summary>
        /// 将 PNG 图片流写入 WebP 文件 / Converts a PNG image stream and writes it to a WebP file.
        /// </summary>
        /// <param name="pngStream">PNG 图片流（可读即可，无需可定位）/ The PNG image stream (readable; seekability not required).</param>
        /// <param name="webpFilePath">目标 WebP 文件路径 / The destination WebP file path.</param>
        /// <param name="quality">编码质量（1~100，无损时表示压缩努力程度）/ Encoding quality (1-100; compression effort in lossless mode).</param>
        /// <param name="lossless">是否无损编码 / Whether to encode losslessly.</param>
        public static void PngToWebpFile(Stream pngStream, string webpFilePath, int quality = 90, bool lossless = false)
        {
            File.WriteAllBytes(webpFilePath, PngToWebp(pngStream, quality, lossless));
        }

        /// <summary>
        /// 将 PNG 文件转换为 WebP 文件 / Converts a PNG file to a WebP file.
        /// </summary>
        /// <param name="pngFilePath">源 PNG 文件路径 / The source PNG file path.</param>
        /// <param name="webpFilePath">目标 WebP 文件路径 / The destination WebP file path.</param>
        /// <param name="quality">编码质量（1~100，无损时表示压缩努力程度）/ Encoding quality (1-100; compression effort in lossless mode).</param>
        /// <param name="lossless">是否无损编码 / Whether to encode losslessly.</param>
        public static void PngToWebpFile(string pngFilePath, string webpFilePath, int quality = 90, bool lossless = false)
        {
            File.WriteAllBytes(webpFilePath, PngToWebp(pngFilePath, quality, lossless));
        }


        /// <summary>
        /// 将 WebP 图片字节转换为 PNG 字节 / Converts WebP image bytes to PNG bytes.
        /// </summary>
        /// <remarks>
        /// PNG 恒为无损编码，故无质量参数；WebP 的透明通道会被完整保留。
        /// 方法不校验输入确为 WebP——任何 SkiaSharp 可解码的格式（PNG/JPG/BMP/GIF 等）均可借此转为 PNG。
        /// PNG is always lossless so there is no quality argument; the WebP alpha channel is fully preserved.
        /// The method does not verify the input is actually WebP — any format SkiaSharp can decode (PNG/JPG/BMP/GIF etc.) can be converted to PNG.
        /// </remarks>
        /// <param name="webpBytes">WebP 图片的字节数组 / The raw bytes of the WebP image.</param>
        /// <returns>PNG 图片的字节数组 / The PNG image bytes.</returns>
        /// <exception cref="Exception">字节流无法解码为图片时抛出 / Thrown when the bytes cannot be decoded as an image.</exception>
        public static byte[] WebpToPng(byte[] webpBytes)
        {
            using var original = SKBitmap.Decode(webpBytes);
            if (original == null)
            {
                throw new Exception("Unable to decode the image.");
            }

            using var image = SKImage.FromBitmap(original);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        /// <summary>
        /// 将 WebP 图片流转换为 PNG 字节 / Converts a WebP image stream to PNG bytes.
        /// </summary>
        /// <param name="webpStream">WebP 图片流（可读即可，无需可定位）/ The WebP image stream (readable; seekability not required).</param>
        /// <returns>PNG 图片的字节数组 / The PNG image bytes.</returns>
        /// <exception cref="Exception">字节流无法解码为图片时抛出 / Thrown when the bytes cannot be decoded as an image.</exception>
        public static byte[] WebpToPng(Stream webpStream)
        {
            using var buffer = new MemoryStream();
            webpStream.CopyTo(buffer);
            return WebpToPng(buffer.ToArray());
        }

        /// <summary>
        /// 将 WebP 文件转换为 PNG 字节 / Converts a WebP file to PNG bytes.
        /// </summary>
        /// <param name="webpFilePath">源 WebP 文件路径 / The source WebP file path.</param>
        /// <returns>PNG 图片的字节数组 / The PNG image bytes.</returns>
        /// <exception cref="Exception">字节流无法解码为图片时抛出 / Thrown when the bytes cannot be decoded as an image.</exception>
        public static byte[] WebpToPng(string webpFilePath)
        {
            return WebpToPng(File.ReadAllBytes(webpFilePath));
        }

        /// <summary>
        /// 将 WebP 图片字节转换为 PNG 流（已回卷到起始位置）/ Converts WebP image bytes to a PNG stream (rewound to its beginning).
        /// </summary>
        /// <param name="webpBytes">WebP 图片的字节数组 / The raw bytes of the WebP image.</param>
        /// <returns>已定位到起始位置的 PNG 流 / A PNG stream rewound to its beginning.</returns>
        public static Stream WebpToPngStream(byte[] webpBytes)
        {
            return new MemoryStream(WebpToPng(webpBytes));
        }

        /// <summary>
        /// 将 WebP 图片流转换为 PNG 流（已回卷到起始位置）/ Converts a WebP image stream to a PNG stream (rewound to its beginning).
        /// </summary>
        /// <param name="webpStream">WebP 图片流（可读即可，无需可定位）/ The WebP image stream (readable; seekability not required).</param>
        /// <returns>已定位到起始位置的 PNG 流 / A PNG stream rewound to its beginning.</returns>
        public static Stream WebpToPngStream(Stream webpStream)
        {
            return new MemoryStream(WebpToPng(webpStream));
        }

        /// <summary>
        /// 将 WebP 文件转换为 PNG 流（已回卷到起始位置）/ Converts a WebP file to a PNG stream (rewound to its beginning).
        /// </summary>
        /// <param name="webpFilePath">源 WebP 文件路径 / The source WebP file path.</param>
        /// <returns>已定位到起始位置的 PNG 流 / A PNG stream rewound to its beginning.</returns>
        public static Stream WebpToPngStream(string webpFilePath)
        {
            return new MemoryStream(WebpToPng(webpFilePath));
        }

        /// <summary>
        /// 将 WebP 图片字节写入 PNG 文件 / Converts WebP image bytes and writes them to a PNG file.
        /// </summary>
        /// <param name="webpBytes">WebP 图片的字节数组 / The raw bytes of the WebP image.</param>
        /// <param name="pngFilePath">目标 PNG 文件路径 / The destination PNG file path.</param>
        public static void WebpToPngFile(byte[] webpBytes, string pngFilePath)
        {
            File.WriteAllBytes(pngFilePath, WebpToPng(webpBytes));
        }

        /// <summary>
        /// 将 WebP 图片流写入 PNG 文件 / Converts a WebP image stream and writes it to a PNG file.
        /// </summary>
        /// <param name="webpStream">WebP 图片流（可读即可，无需可定位）/ The WebP image stream (readable; seekability not required).</param>
        /// <param name="pngFilePath">目标 PNG 文件路径 / The destination PNG file path.</param>
        public static void WebpToPngFile(Stream webpStream, string pngFilePath)
        {
            File.WriteAllBytes(pngFilePath, WebpToPng(webpStream));
        }

        /// <summary>
        /// 将 WebP 文件转换为 PNG 文件 / Converts a WebP file to a PNG file.
        /// </summary>
        /// <param name="webpFilePath">源 WebP 文件路径 / The source WebP file path.</param>
        /// <param name="pngFilePath">目标 PNG 文件路径 / The destination PNG file path.</param>
        public static void WebpToPngFile(string webpFilePath, string pngFilePath)
        {
            File.WriteAllBytes(pngFilePath, WebpToPng(webpFilePath));
        }

    }
}
