namespace Rampage.Drawing
{
    /// <summary>
    /// 图片探测结果（尺寸 / 编码格式 / 帧数） / Image probe result (dimensions, encoded format and frame count).
    /// </summary>
    /// <remarks>
    /// 由 <see cref="PhotoProcessor.ProbeImage(byte[])"/> 产出：仅读取图片头部元数据，不加载完整像素数据。
    /// 无法解码时 <see cref="IsDecodable"/> 为 false 且宽高均为 0。
    /// Produced by <see cref="PhotoProcessor.ProbeImage(byte[])"/>: reads only header metadata without loading
    /// full pixel data. When the data cannot be decoded, <see cref="IsDecodable"/> is false and both dimensions are 0.
    /// </remarks>
    public sealed class ImageProbeResult
    {
        /// <summary>
        /// 宽度（像素）；解码失败时为 0 / The width in pixels; 0 when decoding failed.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 高度（像素）；解码失败时为 0 / The height in pixels; 0 when decoding failed.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 编码格式的小写规范名（jpeg / png / webp / gif / bmp / ico / heif / avif 等；无法识别为 "unknown"）
        /// / The lowercase canonical name of the encoded format (jpeg / png / webp / gif / bmp / ico / heif / avif etc.; "unknown" when unrecognized).
        /// </summary>
        public string Format { get; set; } = "unknown";

        /// <summary>
        /// 帧数；解码失败时为 0 / The number of frames; 0 when decoding failed.
        /// </summary>
        public int FrameCount { get; set; }

        /// <summary>
        /// 是否为可解码的图片（宽高均大于 0） / Whether the data decodes as an image (both dimensions greater than 0).
        /// </summary>
        public bool IsDecodable => Width > 0 && Height > 0;

        /// <summary>
        /// 是否为动图（帧数大于 1：动画 GIF/WebP 等均返回 true） / Whether the image is animated (more than one frame: animated GIF/WebP etc. return true).
        /// </summary>
        public bool IsAnimated => FrameCount > 1;
    }
}
