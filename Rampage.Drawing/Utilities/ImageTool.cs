using SkiaSharp;
using System;

namespace Rampage.Drawing
{
    /// <summary>
    /// Image Utility
    /// </summary>
    public class ImageTool
	{

        /// <summary>
        /// generate thumbnail image from original image bytes
        /// </summary>
        /// <param name="originalImageBytes"></param>
        /// <param name="thumbnailWidth"></param>
        /// <param name="thumbnailHeight"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="originalImageBytes"></param>
        /// <param name="thumbnailWidth"></param>
        /// <param name="thumbnailHeight"></param>
        /// <returns></returns>
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
        /// generate thumbnail image from original image bytes
        /// </summary>
        /// <param name="originalImageBytes"></param>
        /// <param name="savePath"></param>
        /// <param name="thumbnailWidth"></param>
        /// <param name="thumbnailHeight"></param>
        public static void GenerateThumbnail(byte[] originalImageBytes, string savePath, int? thumbnailWidth = null, int? thumbnailHeight = null)
        {
            using var thumbnailStream = GenerateThumbnail(originalImageBytes, thumbnailWidth, thumbnailHeight);

            using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
            thumbnailStream.CopyTo(fileStream);
        }

        /// <summary>
        /// get image size
        /// </summary>
        /// <returns></returns>
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
        /// check is gif image
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns></returns>
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
        /// check is image
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public static bool IsImage(Stream fileStream)
        {
            SKImageInfo info = SKBitmap.DecodeBounds(fileStream);
            return info != SKImageInfo.Empty;
        }

        /// <summary>
        /// check is image
        /// </summary>
        /// <param name="fileBytes"></param>
        /// <returns></returns>
        public static bool IsImage(byte[] fileBytes)
        {
            SKImageInfo info = SKBitmap.DecodeBounds(fileBytes);
            return info != SKImageInfo.Empty;
        }

        /// <summary>
        /// check is image
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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
