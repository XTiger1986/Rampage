using System;

namespace Rampage.Configuration
{
    /// <summary>
    /// 配置项注入属性标识（标注配置类中需要绑定配置值的 public static 属性/字段）。/ Configuration node injection attribute (marks the public static properties/fields of a config class to bind).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConfigValueAttribute : Attribute
    {
        /// <summary>
        /// 无参构造：Node 留空，需另行指定。/ Parameterless ctor: Node stays null and must be set separately.
        /// </summary>
        public ConfigValueAttribute() { }

        /// <summary>
        /// 以配置节点路径构造。/ Construct with the config node path.
        /// </summary>
        /// <param name="node">配置节点路径 / configuration node path</param>
        public ConfigValueAttribute(string node)
        {
            Node = node;
        }

        /// <summary>
        /// 配置节点路径，按 "." 分割逐级读取（如 database.connection.timeout）。/ Configuration node path resolved level by level, split on "." (e.g. database.connection.timeout).
        /// </summary>
        public string? Node {  get; set; }

    }
}
