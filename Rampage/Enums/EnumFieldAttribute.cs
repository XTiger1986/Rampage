using System;

namespace Rampage.Enums
{
    /// <summary>
    /// 枚举字段标记：自定义枚举显示文本、本地化键与排序 / Enum field attribute: customizes display text, localization key and sort order of an enum field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumFieldAttribute : Attribute
    {

        /// <summary>
        /// 创建默认标记 / Creates a default attribute
        /// </summary>
        public EnumFieldAttribute()
        {
        }

        /// <summary>
        /// 以显示文本与排序值创建标记 / Creates the attribute with display text and sort order
        /// </summary>
        /// <param name="text">自定义显示文本 / Custom display text</param>
        /// <param name="sortOrder">排序值，默认 0（表示按声明倒序参与排序） / Sort order, 0 by default (falls back to reverse declaration order)</param>
        public EnumFieldAttribute(string text, int sortOrder = 0)
        {
            this.Text = text;
            this.SortOrder = sortOrder;
        }

        /// <summary>
        /// 自定义枚举默认文本 / Custom default display text of the enum field
        /// </summary>
        public string? Text
        {
            get; set;
        }

        /// <summary>
        /// 自定义本地化资源键名；定义后会优先尝试读取多语言资源文本，读取到则优先使用 / Custom localization resource key; when set, localized resource text is preferred whenever it can be resolved
        /// </summary>
        public string? LocalizerKey
        {
            get; set;
        }

        /// <summary>
        /// 排序值，值越大优先级越高（0 表示未指定，按声明倒序处理） / Sort order, higher values come first (0 means unspecified and is treated as reverse declaration order)
        /// </summary>
        public int SortOrder
        {
            get; set;
        }

    }
}
