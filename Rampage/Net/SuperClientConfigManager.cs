using Microsoft.Extensions.Configuration;
using System;

namespace Rampage.Net
{
    public class SuperClientConfigManager
    {

        static SuperClientConfigManager()
        {
            ReloadConfigs();
        }

        private static readonly object _containerLock = new object();
        public static Dictionary<string, SuperClientConfig> ConfigContainer
        {
            get;
            private set;
        } = default!;

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
