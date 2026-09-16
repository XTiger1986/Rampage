using System;

namespace Rampage.Enums
{
    /// <summary>
	/// 枚举标记(用来自定义枚举显示文本等等)
    /// Enumeration tag (used to customise the display text of enumerations and so on)
	/// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumFieldAttribute : Attribute
    {

        /// <summary>
        /// 
        /// </summary>
        public EnumFieldAttribute()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="sortOrder"></param>
        public EnumFieldAttribute(string text, int sortOrder = 0)
        {
            this.Text = text;
            this.SortOrder = sortOrder;
        }

        /// <summary>
        /// 自定义枚举默认文本
        /// </summary>
        public string? Text
        {
            get; set;
        }

        /// <summary>
        /// 自定义本地化语言资源键名 (如果定义了 LocalizerKey, 会自动尝试读取多语言资源文本，如果能读取到则优先使用多语言资源文本)
        /// </summary>
        public string? LocalizerKey
        {
            get; set;
        }

        /// <summary>
        /// 排序值, 值越大优先级越高
        /// Sort value, the higher the value, the higher the priority
        /// </summary>
        public int SortOrder
        {
            get; set;
        }

    }
}
