using System.Collections.Generic;

namespace Rampage.Drawing
{
    /// <summary>
    /// 照片一站式处理结果（原图探测信息 + WebP 缩略图字节 + EXIF 白名单键值）
    /// / One-call photo processing result (original probe info, WebP thumbnail bytes and the EXIF whitelist map).
    /// </summary>
    /// <remarks>
    /// 由 <see cref="PhotoProcessor.ProcessPhoto(byte[], int, int)"/> 产出。EXIF 白名单仅含出现的键，
    /// 绝不包含 GPS、序列号与厂商私有注释（详见该方法的策略说明）。
    /// Produced by <see cref="PhotoProcessor.ProcessPhoto(byte[], int, int)"/>. The EXIF whitelist contains only
    /// the keys actually present and never carries GPS, serial numbers or maker notes (see that method for the policy).
    /// </remarks>
    public sealed class PhotoProcessResult
    {
        /// <summary>
        /// 原图探测信息（尺寸 / 格式 / 帧数） / Probe info of the original image (dimensions / format / frame count).
        /// </summary>
        public ImageProbeResult Probe { get; set; } = new ImageProbeResult();

        /// <summary>
        /// WebP 缩略图字节（等比缩放、从不放大） / The WebP thumbnail bytes (aspect-preserving, never upscaled).
        /// </summary>
        public byte[] ThumbnailWebpBytes { get; set; } = System.Array.Empty<byte>();

        /// <summary>
        /// EXIF 白名单键值（仅含原图中出现的白名单键） / The EXIF whitelist map (only keys present in the original image).
        /// </summary>
        public IReadOnlyDictionary<string, object?> Exif { get; set; } = new Dictionary<string, object?>();
    }
}
