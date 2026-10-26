using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Rampage.Drawing
{
    /// <summary>
    /// 照片处理管线（WebP 缩略图衍生、尺寸/格式探测、EXIF 白名单抽取）
    /// / Photo processing pipeline (WebP thumbnail derivation, size/format probing and EXIF whitelist extraction).
    /// </summary>
    /// <remarks>
    /// 面向「上传一张静态照片 → 落库/落盘」场景的一组纯函数式工具：输入均为原图字节，不触碰文件系统与数据库。
    /// 缩略图恒输出 WebP（等比缩放、从不放大）；探测只读头部元数据；EXIF 仅抽取摄影参数白名单。
    /// 与 <see cref="ImageTool"/>（通用缩略图 PNG 输出）和 <see cref="ImageConvert"/>（PNG ↔ WebP 互转）互补：
    /// 本类聚合了照片上传处理所需的完整一步（<see cref="ProcessPhoto(byte[], int, int)"/>）。
    /// A set of pure-function tools for the "upload one static photo → persist to disk/database" scenario: inputs are
    /// the original image bytes; nothing touches the file system or database. Thumbnails are always WebP
    /// (aspect-preserving, never upscaled); probing reads only header metadata; EXIF extraction is restricted to a
    /// photography-parameter whitelist. Complements <see cref="ImageTool"/> (generic PNG thumbnails) and
    /// <see cref="ImageConvert"/> (PNG ↔ WebP conversion): this class aggregates the full one-call step needed by a
    /// photo upload pipeline (<see cref="ProcessPhoto(byte[], int, int)"/>).
    /// </remarks>
    public static class PhotoProcessor
    {
        /// <summary>
        /// 默认 WebP 编码质量（1~100） / The default WebP encoding quality (1-100).
        /// </summary>
        public const int DEFAULT_WEBP_QUALITY = 80;

        /// <summary>
        /// 从原始图片字节生成 WebP 缩略图 / Generate a WebP thumbnail from the original image bytes.
        /// </summary>
        /// <remarks>
        /// 缩放策略：按宽度等比缩放（高度按原图宽高比推算），从不裁剪、不拉伸；原图宽度不超过目标宽度时不放大，
        /// 直接以原尺寸重编码为 WebP。输出恒为有损 WebP，透明通道按 WebP 规范保留。
        /// The image is scaled proportionally to the target width (the height is derived from the source aspect
        /// ratio), never cropped or stretched; when the source is already no wider than the target it is never
        /// upscaled and is simply re-encoded as WebP at its original size. The output is always lossy WebP; the alpha
        /// channel is preserved per the WebP specification.
        /// </remarks>
        /// <param name="originalBytes">原始图片的字节数组 / The raw bytes of the original image.</param>
        /// <param name="targetWidth">缩略图目标宽度（像素，须大于 0；原图更窄时按原宽输出）/ The target thumbnail width in pixels (must be positive; a narrower source keeps its own width).</param>
        /// <param name="quality">WebP 编码质量（1~100，超出按边界夹取）/ The WebP encoding quality (1-100, clamped at the bounds).</param>
        /// <returns>WebP 缩略图字节数组 / The WebP thumbnail bytes.</returns>
        /// <exception cref="ArgumentOutOfRangeException">目标宽度不大于 0 时抛出 / Thrown when the target width is not positive.</exception>
        /// <exception cref="Exception">字节流无法解码为图片时抛出 / Thrown when the bytes cannot be decoded as an image.</exception>
        public static byte[] GenerateWebpThumbnail(byte[] originalBytes, int targetWidth, int quality = DEFAULT_WEBP_QUALITY)
        {
            if (targetWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetWidth), "The target width must be positive.");
            }

            using var original = SKBitmap.Decode(originalBytes);
            if (original == null)
            {
                throw new Exception("Unable to decode the image.");
            }

            // 从不放大：原图宽度不超过目标宽度时按原尺寸重编码 / Never upscale: re-encode at the original size when already narrow enough
            int width = Math.Min(targetWidth, original.Width);
            int height = Math.Max(1, (int)Math.Round(original.Height * (double)width / original.Width));

            SKBitmap source = original;
            SKBitmap? resized = null;
            try
            {
                if (width != original.Width)
                {
                    // 高质量过滤：缩小画质与速度的常用平衡点 / High-quality filtering: the usual quality/speed sweet spot for downscaling
                    resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
                    if (resized == null)
                    {
                        throw new Exception("Unable to resize the image.");
                    }
                    source = resized;
                }

                using var image = SKImage.FromBitmap(source);
                using var data = image.Encode(SKEncodedImageFormat.Webp, Math.Clamp(quality, 1, 100));
                return data.ToArray();
            }
            finally
            {
                resized?.Dispose();
            }
        }

        /// <summary>
        /// 探测图片的尺寸、编码格式与帧数 / Probe the dimensions, encoded format and frame count of an image.
        /// </summary>
        /// <remarks>
        /// 仅解码头部元数据，不加载完整像素数据；损坏或无法识别的数据返回 <see cref="ImageProbeResult.IsDecodable"/>
        /// 为 false 的空结果（宽高 0、格式 unknown），不抛异常。动图判定与 <see cref="ImageTool.IsGif(Stream)"/> 一致（帧数大于 1）。
        /// Only header metadata is decoded; full pixel data is never loaded. Corrupt or unrecognized data yields an
        /// empty result with <see cref="ImageProbeResult.IsDecodable"/> == false (0×0, format "unknown") instead of
        /// throwing. Animation detection matches <see cref="ImageTool.IsGif(Stream)"/> (more than one frame).
        /// </remarks>
        /// <param name="bytes">待探测的字节数组 / The bytes to probe.</param>
        /// <returns>探测结果（永不为 null） / The probe result (never null).</returns>
        public static ImageProbeResult ProbeImage(byte[] bytes)
        {
            ImageProbeResult result = new ImageProbeResult();
            if (bytes == null || bytes.Length == 0)
            {
                return result;
            }

            try
            {
                using var stream = new MemoryStream(bytes, writable: false);
                using SKCodec? codec = SKCodec.Create(stream);
                if (codec == null)
                {
                    return result;
                }
                result.Width = codec.Info.Width;
                result.Height = codec.Info.Height;
                result.FrameCount = codec.FrameCount;
                result.Format = MapFormat(codec.EncodedFormat);
            }
            catch
            {
                // 损坏数据按不可解码处理：探测是校验入口，不向外抛解析异常 / Corrupt data is reported as undecodable: probing is a validation entry point and must not throw
                return new ImageProbeResult();
            }
            return result;
        }

        /// <summary>
        /// 从原始图片字节抽取 EXIF 摄影参数白名单 / Extract the EXIF photography-parameter whitelist from the original image bytes.
        /// </summary>
        /// <remarks>
        /// <para>白名单策略（隐私红线）：只输出下列键且在原图中确实出现时才有对应项——
        /// Make（机身品牌）、Model（机身型号）、LensModel（镜头型号）、FNumber（光圈值，数值）、
        /// ExposureTime（快门速度，「1/250」风格分数字符串，由有理数化约而来）、ISOSpeedRatings（ISO 感光度，整数）、
        /// FocalLength（焦距 mm，数值）、FocalLengthIn35mmFormat（35mm 等效焦距，整数）、
        /// DateTimeOriginal（拍摄时间，ISO「yyyy-MM-ddTHH:mm:ss」字符串；EXIF 时间不含时区，按原样输出朴素值，不附加 Z 后缀）、
        /// Orientation（方向，整数 1~8）。
        /// 绝不输出 GPS（任何 GpsDirectory 字段）、机身/镜头序列号、软件信息与厂商私有注释（MakerNote）——
        /// 本方法只按标签精确读取 IFD0 与 SubIFD 的白名单字段，从根本上不接触上述目录。
        /// 调用方若需对外投影，应在本白名单基础上再做展示格式化。</para>
        /// <para>解析是尽力而为的：单字段读取失败（脏数据）仅跳过该字段，整体解析失败返回空字典，不抛异常。</para>
        /// <para>Whitelist policy (privacy red line): only the following keys are emitted, and only when actually present
        /// in the file — Make, Model, LensModel, FNumber (numeric), ExposureTime (a "1/250"-style fraction string
        /// reduced from the rational), ISOSpeedRatings (integer), FocalLength (numeric, mm),
        /// FocalLengthIn35mmFormat (integer), DateTimeOriginal (an ISO "yyyy-MM-ddTHH:mm:ss" string; EXIF times carry
        /// no timezone so the naive value is emitted as-is, without a Z suffix), Orientation (integer 1-8).
        /// GPS (any GpsDirectory field), body/lens serial numbers, software strings and maker notes are never emitted —
        /// the method reads only the whitelisted tags from IFD0 and SubIFD by exact tag id and never touches those
        /// directories at all. Callers projecting to public output should apply display formatting on top of this
        /// whitelist. Parsing is best-effort: a failing field is skipped, a wholesale parse failure yields an empty
        /// dictionary; nothing throws.</para>
        /// </remarks>
        /// <param name="bytes">原始图片的字节数组 / The raw bytes of the original image.</param>
        /// <returns>白名单键值字典（只读；无可用信息时为空字典） / The whitelist map (read-only; empty when nothing usable was found).</returns>
        public static IReadOnlyDictionary<string, object?> ExtractExifWhitelist(byte[] bytes)
        {
            Dictionary<string, object?> result = new Dictionary<string, object?>();
            if (bytes == null || bytes.Length == 0)
            {
                return result;
            }

            try
            {
                IReadOnlyList<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(new MemoryStream(bytes, writable: false));
                ExifIfd0Directory? ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                ExifSubIfdDirectory? subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                // IFD0：机身品牌/型号与方向 / IFD0: camera make/model and orientation
                if (ifd0 != null)
                {
                    PutString(result, "Make", SafeGetString(ifd0, ExifDirectoryBase.TagMake));
                    PutString(result, "Model", SafeGetString(ifd0, ExifDirectoryBase.TagModel));
                    if (ifd0.TryGetInt32(ExifDirectoryBase.TagOrientation, out int orientation))
                    {
                        result["Orientation"] = orientation;
                    }
                }

                // SubIFD：镜头与曝光参数 / SubIFD: lens and exposure parameters
                if (subIfd != null)
                {
                    PutString(result, "LensModel", SafeGetString(subIfd, ExifDirectoryBase.TagLensModel));
                    if (subIfd.TryGetRational(ExifDirectoryBase.TagFNumber, out Rational fNumber) && fNumber.Denominator != 0)
                    {
                        result["FNumber"] = fNumber.ToDouble();
                    }
                    if (subIfd.TryGetRational(ExifDirectoryBase.TagExposureTime, out Rational exposureTime))
                    {
                        string? formatted = FormatExposureTime(exposureTime);
                        if (formatted != null)
                        {
                            result["ExposureTime"] = formatted;
                        }
                    }
                    if (subIfd.TryGetInt32(ExifDirectoryBase.TagIsoEquivalent, out int iso))
                    {
                        result["ISOSpeedRatings"] = iso;
                    }
                    if (subIfd.TryGetRational(ExifDirectoryBase.TagFocalLength, out Rational focalLength) && focalLength.Denominator != 0)
                    {
                        result["FocalLength"] = focalLength.ToDouble();
                    }
                    if (subIfd.TryGetInt32(ExifDirectoryBase.Tag35MMFilmEquivFocalLength, out int focal35))
                    {
                        result["FocalLengthIn35mmFormat"] = focal35;
                    }
                    if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime takenTime))
                    {
                        // EXIF 时间不含时区：输出朴素 ISO 串（无 Z 后缀），时区语义由存储层约定 / EXIF times are timezone-less: emit a naive ISO string (no Z suffix); timezone semantics are the storage layer's convention
                        result["DateTimeOriginal"] = takenTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                }
            }
            catch
            {
                // 解析失败返回已收集的部分（通常为空）：EXIF 是附加信息，不应让脏数据阻断主流程 / Return whatever was collected (usually empty): EXIF is auxiliary and corrupt metadata must not break the main flow
            }
            return result;
        }

        /// <summary>
        /// 一站式照片处理：探测原图 + 生成 WebP 缩略图 + 抽取 EXIF 白名单 / One-call photo processing: probe the original, generate the WebP thumbnail and extract the EXIF whitelist.
        /// </summary>
        /// <remarks>内部依次调用 <see cref="ProbeImage(byte[])"/>、<see cref="GenerateWebpThumbnail(byte[], int, int)"/> 与 <see cref="ExtractExifWhitelist(byte[])"/>；
        /// 原图无法解码时抛出异常（此时不产生任何结果）。EXIF 抽取失败不阻断（结果中为空字典）。
        /// Internally calls <see cref="ProbeImage(byte[])"/>, <see cref="GenerateWebpThumbnail(byte[], int, int)"/> and
        /// <see cref="ExtractExifWhitelist(byte[])"/> in order; an undecodable original throws (no partial result is
        /// produced). EXIF extraction failures never abort the pipeline (the map is simply empty).</remarks>
        /// <param name="originalBytes">原始图片的字节数组 / The raw bytes of the original image.</param>
        /// <param name="thumbWidth">缩略图目标宽度（像素）/ The target thumbnail width in pixels.</param>
        /// <param name="webpQuality">WebP 编码质量（1~100）/ The WebP encoding quality (1-100).</param>
        /// <returns>聚合处理结果 / The aggregated processing result.</returns>
        /// <exception cref="ArgumentOutOfRangeException">缩略图目标宽度不大于 0 时抛出 / Thrown when the thumbnail target width is not positive.</exception>
        /// <exception cref="Exception">字节流无法解码为图片时抛出 / Thrown when the bytes cannot be decoded as an image.</exception>
        public static PhotoProcessResult ProcessPhoto(byte[] originalBytes, int thumbWidth, int webpQuality = DEFAULT_WEBP_QUALITY)
        {
            ImageProbeResult probe = ProbeImage(originalBytes);
            if (!probe.IsDecodable)
            {
                throw new Exception("Unable to decode the image.");
            }
            return new PhotoProcessResult
            {
                Probe = probe,
                ThumbnailWebpBytes = GenerateWebpThumbnail(originalBytes, thumbWidth, webpQuality),
                Exif = ExtractExifWhitelist(originalBytes)
            };
        }

        /// <summary>
        /// 安全读取字符串标签：读取失败（类型不符/脏数据）时返回 null / Safely read a string tag; null when reading fails (type mismatch or corrupt data).
        /// </summary>
        /// <param name="directory">EXIF 目录 / The EXIF directory.</param>
        /// <param name="tagId">标签 Id / The tag id.</param>
        /// <returns>去首尾空白的字符串；缺失或读取失败返回 null / The trimmed string; null when missing or unreadable.</returns>
        private static string? SafeGetString(MetadataExtractor.Directory directory, int tagId)
        {
            try
            {
                return directory.GetString(tagId)?.Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 写入字符串键：仅当值非空白时写入 / Put a string key only when the value is non-blank.
        /// </summary>
        /// <param name="result">目标字典 / The target dictionary.</param>
        /// <param name="key">白名单键名 / The whitelist key.</param>
        /// <param name="value">字符串值（可为 null）/ The string value (may be null).</param>
        private static void PutString(Dictionary<string, object?> result, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                result[key] = value;
            }
        }

        /// <summary>
        /// 快门速度有理数格式化：化约分数后输出「1/250」风格字符串；分子分母非正时返回 null
        /// / Format an exposure-time rational as a reduced "1/250"-style fraction string; null when numerator/denominator is not positive.
        /// </summary>
        /// <param name="rational">EXIF 有理数（秒）/ The EXIF rational (seconds).</param>
        /// <returns>分数字符串；无法格式化返回 null / The fraction string; null when it cannot be formatted.</returns>
        private static string? FormatExposureTime(Rational rational)
        {
            long numerator = rational.Numerator;
            long denominator = rational.Denominator;
            if (numerator <= 0 || denominator <= 0)
            {
                return null;
            }
            // 化约分数（EXIF 常见 10/400 → 1/40）/ Reduce the fraction (EXIF often carries e.g. 10/400 → 1/40)
            long gcd = GreatestCommonDivisor(numerator, denominator);
            return $"{numerator / gcd}/{denominator / gcd}";
        }

        /// <summary>
        /// 最大公约数（辗转相除）/ The greatest common divisor (Euclidean algorithm).
        /// </summary>
        /// <param name="a">操作数 / An operand.</param>
        /// <param name="b">操作数 / An operand.</param>
        /// <returns>最大公约数 / The greatest common divisor.</returns>
        private static long GreatestCommonDivisor(long a, long b)
        {
            while (b != 0)
            {
                long temp = a % b;
                a = b;
                b = temp;
            }
            return Math.Abs(a);
        }

        /// <summary>
        /// SkiaSharp 编码格式 → 小写规范名 / Map a SkiaSharp encoded format to its lowercase canonical name.
        /// </summary>
        /// <param name="format">SkiaSharp 编码格式 / The SkiaSharp encoded format.</param>
        /// <returns>小写规范名；未识别返回 "unknown" / The lowercase canonical name; "unknown" when unrecognized.</returns>
        private static string MapFormat(SKEncodedImageFormat format)
        {
            return format switch
            {
                SKEncodedImageFormat.Jpeg => "jpeg",
                SKEncodedImageFormat.Png => "png",
                SKEncodedImageFormat.Webp => "webp",
                SKEncodedImageFormat.Gif => "gif",
                SKEncodedImageFormat.Bmp => "bmp",
                SKEncodedImageFormat.Ico => "ico",
                SKEncodedImageFormat.Wbmp => "wbmp",
                SKEncodedImageFormat.Heif => "heif",
                SKEncodedImageFormat.Avif => "avif",
                SKEncodedImageFormat.Dng => "dng",
                _ => "unknown"
            };
        }
    }
}
