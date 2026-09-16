using Microsoft.Extensions.Configuration;
using System;

namespace Rampage.Configuration
{
    /// <summary>
    /// 配置读取器扩展
    /// configuration extensions
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// 使用路径参数读取指定节点配置值
        /// Read the configuration data of the specified node using the path parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="path">node path, for example: database.connection.timeout</param>
        /// <returns></returns>
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
        /// 使用路径参数读取指定节点配置值
        /// Read the configuration data of the specified node using the path parameter
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="path">node path, for example: database.connection.timeout</param>
        /// <param name="type">return data type</param>
        /// <returns></returns>
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
