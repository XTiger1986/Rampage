using System;

namespace Rampage.Net
{
    /// <summary>
    /// HTTP 客户端配置项：对应配置节 SuperHttpClients:Clients 列表中的一条配置
    /// HTTP client configuration entry: one item of the list in the SuperHttpClients:Clients configuration section
    /// </summary>
    public class SuperClientConfig
    {
        /// <summary>配置名称：作为 <see cref="SuperClientConfigManager"/> 容器的键，供客户端按名匹配 / The config name: the key in the <see cref="SuperClientConfigManager"/> container used to match a client by name</summary>
        public string? Name
        {
            get; set;
        }

        /// <summary>API 根地址（scheme + host[:port]），优先级低于 [SuperClient] 特性的 Root / The API root (scheme + host[:port]), taking lower priority than the [SuperClient] attribute's Root</summary>
        public string? Root
        {
            get; set;
        }

    }
}
