using System;

namespace Rampage.Configuration
{
    /// <summary>
    /// 配置注入属性标识
    /// configuration injection Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SuperConfigAttribute : Attribute
    {


        public SuperConfigAttribute() { }

        public SuperConfigAttribute(string path)
        {
            Path = path;
        }

        /// <summary>
        /// 配置文件路径
        /// configuration file path
        /// </summary>
        public string? Path
        {
            get; set;
        }

    }
}
