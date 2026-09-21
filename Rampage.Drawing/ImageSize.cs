using System;

namespace Rampage.Drawing
{
    /// <summary>
    /// 图片尺寸 / Image size (width and height in pixels).
    /// </summary>
    /// <remarks>仅包含宽、高两个可读写属性的可变结构体 / A mutable struct carrying just Width and Height.</remarks>
    public struct ImageSize
    {
        /// <summary>
        /// 宽度（像素） / The width in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 高度（像素） / The height in pixels.
        /// </summary>
        public int Height { get; set; }

    }
}
