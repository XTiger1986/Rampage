using Microsoft.Extensions.Configuration;
using System;

namespace Rampage.Net
{
    /// <summary>
    /// HTTP 客户端配置管理器：从配置节 SuperHttpClients:Clients 读取 SuperClientConfig 列表并按 Name 建立索引
    /// HTTP client configuration manager: reads the SuperClientConfig list from the SuperHttpClients:Clients section and indexes it by Name
    /// </summary>
    public class SuperClientConfigManager
    {
        /// <summary>静态构造：首次访问本类时自动加载配置 / Static constructor: loads configs automatically on first access to this class</summary>
        static SuperClientConfigManager()
        {
            ReloadConfigs();
        }

        private static readonly object _containerLock = new object();
        /// <summary>配置容器：以配置 Name 为键 / The configuration container keyed by config Name</summary>
        public static Dictionary<string, SuperClientConfig> ConfigContainer
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 重新读取配置节并整体重建配置容器（重复 Name 以首次出现为准）
        /// Reloads the configuration section and rebuilds the container as a whole (duplicate names keep the first occurrence)
        /// </summary>
        /// <remarks>重建过程在锁内完成，线程安全 / The rebuild runs inside a lock, hence thread-safe</remarks>
        public static void ReloadConfigs()
        {
            lock (_containerLock)
            {
                ConfigContainer = new Dictionary<string, SuperClientConfig>();

                IConfigurationSection configSection = GlobalConfiguration.Configuration.GetSection("SuperHttpClients");
                IConfigurationSection clientsNode = configSection.GetSection("Clients");
                List<SuperClientConfig>? configList = clientsNode.Get<List<SuperClientConfig>>();
                if (configList != null && configList.Count > 0)
                {
                    foreach (SuperClientConfig config in configList)
                    {
                        string configName = config.Name!;
                        if (!ConfigContainer.ContainsKey(configName))
                        {
                            ConfigContainer.Add(configName, config);
                        }
                    }
                }
            }
        }

        /// <summary>按名称获取客户端配置 / Gets a client config by name</summary>
        /// <param name="name">配置名称 / The config name</param>
        /// <returns>匹配的配置；不存在时返回 null / The matching config, or null when it does not exist</returns>
        public static SuperClientConfig? GetConfig(string name)
        {
            if (ConfigContainer.ContainsKey(name))
            {
                return ConfigContainer.GetValueOrDefault(name);
            }
            return null;
        }

    }
}
