using System;

namespace Rampage.Enums
{
    /// <summary>
    /// 枚举成员描述信息（文本、多语言文本、排序与取值） / Describes an enum member (text, localized texts, sort order and values)
    /// </summary>
    public class EnumInfo
    {

        /// <summary>
        /// 枚举默认显示文本（来自 EnumFieldAttribute.Text） / Default display text of the enum member (from EnumFieldAttribute.Text)
        /// </summary>
        public string? Text
        {
            get; set;
        }

        /// <summary>
        /// 多语言文本字典（键为 CultureName） / Localized text dictionary keyed by culture name
        /// </summary>
        public Dictionary<string, string?> TextDict
        {
            get; internal set;
        } = default!;

        /// <summary>
        /// 排序值，值越大优先级越高 / Sort order; higher values come first
        /// </summary>
        public int SortOrder
        {
            get; set;
        }

        /// <summary>
        /// 枚举字段名字符串值 / String (field name) value of the enum member
        /// </summary>
        public string? StringValue
        {
            get;
            internal set;
        }

        /// <summary>
        /// 枚举成员的整数值 / Integer value of the enum member
        /// </summary>
        public int IntValue
        {
            get;
            internal set;
        }


    }
}
