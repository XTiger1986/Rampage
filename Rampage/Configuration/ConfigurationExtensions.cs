using Microsoft.Extensions.Configuration;
using System;

namespace Rampage.Configuration
{
    /// <summary>
    /// 配置读取器扩展。/ Configuration reader extensions.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// 使用路径参数读取指定节点配置值：路径按 "." 分割逐级 GetSection，最后一段 GetValue。
        /// / Read a config node by path: the path is split on "." and walked via GetSection level by level, then GetValue on the last segment.
        /// </summary>
        /// <typeparam name="T">值类型 / value type</typeparam>
        /// <param name="configuration">配置对象 / the configuration</param>
        /// <param name="path">节点路径，如 database.connection.timeout / node path, e.g. database.connection.timeout</param>
        /// <returns>配置值；节点不存在时为 default / the value; default when the node is missing</returns>
        public static T? GetNodeValue<T>(this IConfiguration configuration, string path)
        {
            Type type = typeof(T);
            object? o = GetNodeValue(configuration, path, type);
            if (o == null)
            {
                return default(T);
            }
            return (T)o;
        }

        /// <summary>
        /// 使用路径参数读取指定节点配置值（同 GetNodeValue&lt;T&gt; 重载，以 Type 参数指定返回类型；单段路径直接 GetValue）。
        /// / Read a config node by path (same as the GetNodeValue&lt;T&gt; overload with the target type supplied as a Type; single-segment paths go straight to GetValue).
        /// </summary>
        /// <param name="configuration">配置对象 / the configuration</param>
        /// <param name="path">节点路径，如 database.connection.timeout / node path, e.g. database.connection.timeout</param>
        /// <param name="type">返回值类型 / target type</param>
        /// <returns>配置值；节点不存在时为 null / the value; null when the node is missing</returns>
        public static object? GetNodeValue(this IConfiguration configuration, string path, Type type)
        {
            string[] nodes = path.Split('.');
            if (nodes.Length == 1)
            {
                return configuration.GetValue(type, path);
            }

            object? o = null;
            IConfigurationSection? section = null;
            for (int i = 0; i < nodes.Length; i++)
            {
                string node = nodes[i];
                if (i == nodes.Length - 1)
                {
                    if (section == null)
                    {
                        o = configuration.GetValue(type, node);
                    }
                    else
                    {
                        o = section.GetValue(type, node);
                    }
                }
                else
                {
                    if (section == null)
                    {
                        section = configuration.GetSection(node);
                    }
                    else
                    {
                        section = section.GetSection(node);
                    }
                }
            }

            return o;
        }

    }
}
