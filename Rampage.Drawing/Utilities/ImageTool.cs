using SkiaSharp;
using System;

namespace Rampage.Drawing
{
    /// <summary>
    /// 图片工具类（缩略图生成、尺寸读取、图片/GIF 判断） / Image utility (thumbnail generation, size probing and image/GIF detection).
    /// </summary>
    public class ImageTool
	{

        /// <summary>
        /// 从原始图片字节生成缩略图，返回 PNG 流 / Generate a thumbnail from the original image bytes and return it as a PNG stream.
        /// </summary>
        /// <remarks>
        /// 缩放策略为等比缩放后居中绘制到目标画布：不裁剪、不拉伸，宽高比不一致时空余区域保持透明。
        /// 仅指定宽或高之一时，另一边按原图宽高比推算。输出为无损 PNG（编码质量参数对 PNG 无效）。
        /// The image is scaled proportionally and centered on the target canvas: never cropped or stretched; when the
        /// aspect ratios differ the padding stays transparent. When only one dimension is given the other is derived
        /// from the source aspect ratio. The output is lossless PNG (the quality argument has no effect on PNG).
        /// </remarks>
        /// <param name="originalImageBytes">原始图片的字节数组 / The raw bytes of the original image.</param>
        /// <param name="thumbnailWidth">缩略图宽度，可为 null / The thumbnail width, or null.</param>
        /// <param name="thumbnailHeight">缩略图高度，可为 null / The thumbnail height, or null.</param>
        /// <returns>已定位到起始位置的 PNG 缩略图流 / A PNG stream rewound to its beginning.</returns>
        /// <exception cref="ArgumentException">宽和高均未指定时抛出 / Thrown when neither dimension is specified.</exception>
        /// <exception cref="Exception">字节流无法解码为图片时抛出 / Thrown when the bytes cannot be decoded as an image.</exception>
        public static Stream GenerateThumbnail(byte[] originalImageBytes, int? thumbnailWidth = null, int? thumbnailHeight = null)
        {
            if (thumbnailWidth == null && thumbnailHeight == null)
            {
                throw new ArgumentException("At least one of thumbnailWidth or thumbnailHeight must be specified");
            }

            // Load the image from the byte array
            using var inputStream = new MemoryStream(originalImageBytes);
            using var originalImage = SKBitmap.Decode(inputStream);

            if (originalImage == null)
            {
                throw new Exception("Unable to decode the image.");
            }

            int width, height;

            // Calculate the new width and height of the thumbnail image
            if (thumbnailWidth.HasValue && thumbnailHeight.HasValue)
            {
                // Both dimensions are specified
                width = thumbnailWidth.Value;
                height = thumbnailHeight.Value;
            }
            else if (thumbnailWidth.HasValue)
            {
                // Only width is specified
                width = thumbnailWidth.Value;
                height = (originalImage.Height * thumbnailWidth.Value) / originalImage.Width;
            }
            else
            {
                // Only height is specified
                height = thumbnailHeight!.Value;
                width = (originalImage.Width * thumbnailHeight.Value) / originalImage.Height;
            }

            // Create a new SKBitmap for the thumbnail with the specified width and height
            var resizedImage = new SKBitmap(width, height, true);

            // Clear bitmap with transparent background
            using (var canvas = new SKCanvas(resizedImage))
            {
                canvas.Clear(SKColors.Transparent);
            }

            // Calculate the drawing rectangle
            float aspectRatio = (float)originalImage.Width / originalImage.Height;
            int drawHeight = height;
            int drawWidth = (int)(height * aspectRatio);

            if (drawWidth > width)
            {
                drawWidth = width;
                drawHeight = (int)(width / aspectRatio);
            }

            int xOffset = (width - drawWidth) / 2;
            int yOffset = (height - drawHeight) / 2;

            // Draw the resized image centered within the specified dimensions
            using (var canvas = new SKCanvas(resizedImage))
            {
                var destRect = new SKRect(xOffset, yOffset, xOffset + drawWidth, yOffset + drawHeight);
                canvas.DrawBitmap(originalImage, destRect);
            }

            // Save the resized image to a stream
            var thumbnailStream = new MemoryStream();
            using (var image = SKImage.FromBitmap(resizedImage))
            {
                image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(thumbnailStream);
            }

            // Reset the stream position to the beginning
            thumbnailStream.Seek(0, SeekOrigin.Begin);

            return thumbnailStream;
        }

        /// <summary>
        /// 从原始图片字节生成缩略图并返回字节数组 / Generate a thumbnail from the original image bytes and return it as a byte array.
        /// </summary>
        /// <remarks>内部调用 <see cref="GenerateThumbnail(byte[], int?, int?)"/> 并将结果流完整读出，缩放规则相同。</remarks>
        /// <param name="originalImageBytes">原始图片的字节数组 / The raw bytes of the original image.</param>
        /// <param name="thumbnailWidth">缩略图宽度，可为 null / The thumbnail width, or null.</param>
        /// <param name="thumbnailHeight">缩略图高度，可为 null / The thumbnail height, or null.</param>
        /// <returns>PNG 缩略图字节数组（长度与实际数据一致） / The thumbnail as PNG bytes (exactly as long as the payload).</returns>
        public static byte[] GenerateThumbnailBytes(byte[] originalImageBytes, int? thumbnailWidth = null, int? thumbnailHeight = null)
        {
            byte[] bytes;
            using (Stream stream = GenerateThumbnail(originalImageBytes, thumbnailWidth, thumbnailHeight))
            {
                bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
            }
            return bytes;
        }

        /// <summary>
        /// 从原始图片字节生成缩略图并写入文件 / Generate a thumbnail from the original image bytes and write it to a file.
        /// </summary>
        /// <param name="originalImageBytes">原始图片的字节数组 / The raw bytes of the original image.</param>
        /// <param name="savePath">缩略图保存路径，已存在则覆盖 / The output file path; an existing file is overwritten.</param>
        /// <param name="thumbnailWidth">缩略图宽度，可为 null / The thumbnail width, or null.</param>
        /// <param name="thumbnailHeight">缩略图高度，可为 null / The thumbnail height, or null.</param>
        public static void GenerateThumbnail(byte[] originalImageBytes, string savePath, int? thumbnailWidth = null, int? thumbnailHeight = null)
        {
            using var thumbnailStream = GenerateThumbnail(originalImageBytes, thumbnailWidth, thumbnailHeight);

            using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
            thumbnailStream.CopyTo(fileStream);
        }

        /// <summary>
        /// 读取图片的像素尺寸 / Get the pixel size of an image.
        /// </summary>
        /// <remarks>仅解码图片头部元数据，不加载完整像素数据；无法解码时返回 0×0。读取前会将流位置重置到 0。</remarks>
        /// <param name="imgStream">图片流 / The stream containing the image.</param>
        /// <returns>图片宽高；解码失败时宽高均为 0 / The image size, or 0×0 when the data cannot be decoded.</returns>
        public static ImageSize GetImageSize(Stream imgStream)
        {
            imgStream.Position = 0;
            ImageSize s = new ImageSize();

            SKImageInfo info = SKBitmap.DecodeBounds(imgStream);
            if (info != SKImageInfo.Empty)
            {
                s.Width = info.Width;
                s.Height = info.Height;
            }

            return s;
        }

        /// <summary>
        /// 判断是否为动图（多帧图像） / Determine whether the stream is an animated (multi-frame) image.
        /// </summary>
        /// <remarks>判定依据是解码器帧数大于 1，不校验具体编码格式：动画 GIF/WebP 均返回 true，而单帧静态 GIF 会返回 false。</remarks>
        /// <param name="fileStream">待检测的流 / The stream to inspect.</param>
        /// <returns>帧数大于 1 返回 true，否则 false / true when the stream contains more than one frame; otherwise false.</returns>
        public static bool IsGif(Stream fileStream)
        {
            bool result = false;

            using (SKCodec skCodec = SKCodec.Create(fileStream))
            {
                result = skCodec.FrameCount > 1;
            }

            return result;
        }

        /// <summary>
        /// 判断流是否为可解码的图片 / Determine whether the stream is a decodable image.
        /// </summary>
        /// <param name="fileStream">待检测的流 / The stream to inspect.</param>
        /// <returns>可解码为图片返回 true / true when the data decodes as an image.</returns>
        public static bool IsImage(Stream fileStream)
        {
            SKImageInfo info = SKBitmap.DecodeBounds(fileStream);
            return info != SKImageInfo.Empty;
        }

        /// <summary>
        /// 判断字节数组是否为可解码的图片 / Determine whether the byte array is a decodable image.
        /// </summary>
        /// <param name="fileBytes">待检测的字节数组 / The bytes to inspect.</param>
        /// <returns>可解码为图片返回 true / true when the data decodes as an image.</returns>
        public static bool IsImage(byte[] fileBytes)
        {
            SKImageInfo info = SKBitmap.DecodeBounds(fileBytes);
            return info != SKImageInfo.Empty;
        }

        /// <summary>
        /// 判断文件是否为可解码的图片 / Determine whether the file is a decodable image.
        /// </summary>
        /// <param name="path">文件路径 / The file path to inspect.</param>
        /// <returns>文件存在且可解码为图片返回 true；文件不存在直接返回 false / true when the file exists and decodes as an image; false outright when it does not exist.</returns>
        public static bool IsImage(string path)
        {
            if (!File.Exists(path))
                return false;

            bool result = false;
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                SKImageInfo info = SKBitmap.DecodeBounds(fs);
                result = info != SKImageInfo.Empty;
            }

            return result;
        }


    }
}
