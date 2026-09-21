using System;

namespace Rampage.Configuration
{
    /// <summary>
    /// 配置注入属性标识（标注配置类，配合 SuperConfigManager.RegisterConfigs 使用）。/ Configuration injection attribute (marks a configuration class for SuperConfigManager.RegisterConfigs).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SuperConfigAttribute : Attribute
    {
        /// <summary>
        /// 无参构造：Path 留空，注册时按默认路径 config/app.json 处理。
        /// / Parameterless ctor: Path stays null and defaults to config/app.json at registration.
        /// </summary>
        public SuperConfigAttribute() { }

        /// <summary>
        /// 以配置文件路径构造。/ Construct with the configuration file path.
        /// </summary>
        /// <param name="path">配置文件路径（相对应用基目录）/ config file path (relative to the app base directory)</param>
        public SuperConfigAttribute(string path)
        {
            Path = path;
        }

        /// <summary>
        /// 配置文件路径；为空时使用默认值 config/app.json。/ Configuration file path; defaults to config/app.json when null.
        /// </summary>
        public string? Path
        {
            get; set;
        }

    }
}
