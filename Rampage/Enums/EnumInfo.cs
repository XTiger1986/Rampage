using System;

namespace Rampage.Enums
{
    public class EnumInfo
    {

        /// <summary>
        /// 自定义枚举默认文本
        /// </summary>
        public string? Text
        {
            get; set;
        }

        /// <summary>
        /// 多语言文本字典(key:CultureName)
        /// </summary>
        public Dictionary<string, string?> TextDict
        {
            get; internal set;
        } = default!;

        /// <summary>
        /// 枚举排序值, 值越大优先级越高
        /// </summary>
        public int SortOrder
        {
            get; set;
        }

        /// <summary>
        /// 枚举 String 值
        /// </summary>
        public string? StringValue
        {
            get;
            internal set;
        }

        /// <summary>
        /// 枚举 Int 值
        /// </summary>
        public int IntValue
        {
            get;
            internal set;
        }


    }
}
