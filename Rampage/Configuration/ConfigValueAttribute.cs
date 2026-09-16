using System;

namespace Rampage.Configuration
{
    /// <summary>
    /// 配置项注入属性标识
    /// configuration node injection Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConfigValueAttribute : Attribute
    {

        public ConfigValueAttribute() { }

        public ConfigValueAttribute(string node)
        {
            Node = node;
        }

        /// <summary>
        /// 配置节点路径
        /// configuration node path, for example: database.connection.timeout
        /// </summary>
        public string? Node {  get; set; }

    }
}
