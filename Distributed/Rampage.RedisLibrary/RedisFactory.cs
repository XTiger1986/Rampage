using Microsoft.Extensions.Configuration;
using Rampage.Json;
using Rampage.Log;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// Redis 客户端工厂：读取 RedisFactory:Clients 配置节构建命名 RedisHelper 与连接复用器。
    /// Redis client factory: builds named RedisHelper instances and connection multiplexers from the RedisFactory:Clients section.
    /// 配置读取或初始化失败仅记日志并降级为空集合，不向调用方抛异常。
    /// Configuration or initialization failures are only logged and degraded to empty collections, never thrown to callers.
    /// </summary>
    /// <remarks>
    /// 哨兵/集群无专门 API，直接依赖连接串（如 service,name=xxx）。序列化统一走 JSON。
    /// Sentinel/cluster have no dedicated API; they rely purely on the connection string (e.g. service,name=xxx). Serialization is JSON throughout.
    /// </remarks>
    public class RedisFactory
    {

        private static volatile RedisFactoryConfig? _config;
        /// <summary>
        /// 全局配置；首次访问时自动从配置节加载 / Global configuration; lazily loaded from the configuration section on first access
        /// </summary>
        public static RedisFactoryConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = ReloadConfig();
                }
                return _config;
            }
        }

        /// <summary>
        /// 重新读取 RedisFactory 配置节并整体替换当前配置；已创建的连接与客户端不受影响。
        /// Re-read the RedisFactory configuration section and replace the current config wholesale; already created connections and clients are unaffected.
        /// </summary>
        /// <returns>新构建的配置 / The freshly built configuration</returns>
        public static RedisFactoryConfig ReloadConfig()
        {
            RedisFactoryConfig config = new RedisFactoryConfig();
            IConfigurationSection root = GlobalConfiguration.Configuration.GetSection("RedisFactory");

            IConfigurationSection clientsNode = root.GetSection("Clients");
            List<RedisConfig>? clientList = clientsNode.Get<List<RedisConfig>>();
            if (clientList != null)
            {
                config.Clients = clientList.ToDictionary(c => c.Name!);
            }
            // 完整构建后再发布，避免并发读到半初始化配置
            _config = config;
            return config;
        }

        /// <summary>
        /// 静态构造：加载配置并按 Clients 预创建命名 RedisHelper；任何异常仅记日志并以空客户端字典降级。
        /// Static constructor: loads the config and pre-creates named RedisHelper instances; any exception is only logged and degrades to an empty client dictionary.
        /// </summary>
        static RedisFactory()
        {
            try
            {
                ReloadConfig();

                if (RedisClientDict == null)
                {
                    lock (_redisClientLock)
                    {
                        if (RedisClientDict == null)
                        {
                            RedisClientDict = new Dictionary<string, RedisHelper>();
                            if (Config.Clients != null && Config.Clients.Count > 0)
                            {
                                foreach (var kv in Config.Clients!)
                                {
                                    RedisClientDict.Add(kv.Key, new RedisHelper(kv.Key));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
                RedisClientDict = new Dictionary<string, RedisHelper>();
            }
        }


        /// <summary>
        /// 默认连接复用器（第一个 Clients 配置）/ Default connection multiplexer (the first Clients entry)
        /// </summary>
        /// <remarks>
        /// 双检锁只保护首次创建；断线重连由 StackExchange.Redis 自行处理，绝不在全局锁内同步重连。
        /// Double-checked locking guards only the first creation; reconnection is handled by StackExchange.Redis itself, never synchronously inside the global lock.
        /// </remarks>
        public static IConnectionMultiplexer? ConnMultiplexer
        {
            get;
            private set;
        }

        /// <summary>
        /// 命名连接复用器容器，键为 <see cref="RedisConfig.Name"/>。
        /// Named connection multiplexer container, keyed by <see cref="RedisConfig.Name"/>.
        /// </summary>
        /// <remarks>
        /// 值为 Lazy：连接的建立不在任何全局锁内，单个实例连不上不会阻塞其它实例首连。
        /// Values are Lazy: connections are established outside any global lock, so one unreachable instance never blocks another instance's first connection.
        /// </remarks>
        public static ConcurrentDictionary<string, Lazy<IConnectionMultiplexer>> ConnMultiplexerDict
        {
            get;
        } = new ConcurrentDictionary<string, Lazy<IConnectionMultiplexer>>();

        /// <summary>
        /// 默认连接创建锁 / Lock guarding default multiplexer creation
        /// </summary>
        private static readonly object _multiplexerLocker = new object();

        /// <summary>
        /// 创建连接（不加全局锁：一个实例连不上不再阻塞其它实例首连）。
        /// Create a connection (no global lock: one unreachable instance no longer blocks other instances' first connection).
        /// </summary>
        /// <param name="redisConfig">连接配置；连接串可经 <see cref="RedisFactoryConfig.ConnectionStringDecoder"/> 解密 / Connection config; the connection string may be decrypted via <see cref="RedisFactoryConfig.ConnectionStringDecoder"/></param>
        /// <returns>已注册事件回调的新连接复用器 / A new multiplexer with event callbacks registered</returns>
        private static IConnectionMultiplexer CreateMultiplexer(RedisConfig redisConfig)
        {
            string connectionString;
            if (RedisFactoryConfig.ConnectionStringDecoder != null)
            {
                connectionString = RedisFactoryConfig.ConnectionStringDecoder.DecodeConnectionString(redisConfig.ConnectionString);
            }
            else
            {
                connectionString = redisConfig.ConnectionString;
            }
            IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(connectionString);
            AddRegisterEvent(multiplexer);
            return multiplexer;
        }

        /// <summary>
        /// 尝试获取已存在的命名连接（不触发新连接）/ Try to get an already created named connection (never triggers a new one)
        /// </summary>
        /// <param name="name">客户端名称 / Client name</param>
        /// <returns>已创建的连接；尚未创建返回 null / The existing connection; null when none was created yet</returns>
        private static IConnectionMultiplexer? TryGetConnectionMultiplexer(string name)
        {
            if (ConnMultiplexerDict.TryGetValue(name, out Lazy<IConnectionMultiplexer>? lazy))
            {
                return lazy.Value;
            }
            return null;
        }

        /// <summary>
        /// 获取默认（第一个）客户端配置 / Get the default (first) client configuration
        /// </summary>
        /// <returns>第一个 Clients 配置；未配置任何客户端返回 null / The first Clients entry; null when no client is configured</returns>
        public static RedisConfig? TryGetDefaultRedisConfig()
        {
            if (Config.Clients != null && Config.Clients.Count > 0)
            {
                return Config.Clients.First().Value;
            }
            return null;
        }
        /// <summary>
        /// 获取默认连接复用器（双检锁，仅首次连接加锁）/ Get the default connection multiplexer (double-checked lock; only the first connect takes the lock)
        /// </summary>
        /// <returns>默认连接；未配置客户端返回 null（连接失败会抛出异常）/ The default connection; null when no client is configured (connection failures throw)</returns>
        public static IConnectionMultiplexer? GetConnectionRedisMultiplexer()
        {
            // 已有 multiplexer 时直接返回，由 SE.Redis 自行重连。/ When a multiplexer already exists, return it directly; SE.Redis reconnects on its own.
            // 旧逻辑在 !IsConnected 时于全局 lock 内同步 Connect，会导致全站请求排队数秒。/ The old logic synchronously connected inside the global lock when !IsConnected, queueing all site requests for seconds.
            if (ConnMultiplexer != null)
            {
                return ConnMultiplexer;
            }

            lock (_multiplexerLocker)
            {
                if (ConnMultiplexer == null)
                {
                    RedisConfig? redisConfig = TryGetDefaultRedisConfig();
                    if (redisConfig != null)
                    {
                        string connectionString;
                        if (RedisFactoryConfig.ConnectionStringDecoder != null)
                        {
                            connectionString = RedisFactoryConfig.ConnectionStringDecoder.DecodeConnectionString(redisConfig.ConnectionString);
                        }
                        else
                        {
                            connectionString = redisConfig.ConnectionString;
                        }
                        ConnMultiplexer = ConnectionMultiplexer.Connect(connectionString);
                        AddRegisterEvent(ConnMultiplexer);
                    }
                }
            }

            return ConnMultiplexer;
        }

        /// <summary>
        /// 按名称获取客户端配置 / Get a client configuration by name
        /// </summary>
        /// <param name="name">客户端名称 / Client name</param>
        /// <returns>对应配置；名称未配置或无任何客户端返回 null / The matching config; null when the name is unknown or no client is configured</returns>
        public static RedisConfig? TryGetRedisConfig(string name)
        {
            if (Config.Clients != null && Config.Clients.Count > 0)
            {
                return Config.Clients.ContainsKey(name) ? Config.Clients[name] : null;
            }
            return null;
        }
        /// <summary>
        /// 获取指定名称的连接复用器；name 为空时回退默认连接。
        /// Get the named connection multiplexer; falls back to the default connection when name is empty.
        /// </summary>
        /// <param name="name">RedisConfig.Name / The configured client name</param>
        /// <returns>命名连接（Lazy 保证只建立一次）；名称未配置返回 null / The named connection (Lazy ensures it is created once); null when the name is not configured</returns>
        public static IConnectionMultiplexer? GetConnectionRedisMultiplexer(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return GetConnectionRedisMultiplexer();
            }
            RedisConfig? redisConfig = TryGetRedisConfig(name);
            if (redisConfig == null)
            {
                return null;
            }
            // Lazy 保证同名连接只建立一次，且连接过程不在任何全局锁内 / Lazy ensures the named connection is created once, outside any global lock
            Lazy<IConnectionMultiplexer> lazy = ConnMultiplexerDict.GetOrAdd(name, _ => new Lazy<IConnectionMultiplexer>(() => CreateMultiplexer(redisConfig!)));
            return lazy.Value;
        }


        /// <summary>
        /// 为连接复用器注册全部事件回调（仅 Debug 日志，不抛异常）/ Register all event callbacks on the multiplexer (Debug logging only, never throws)
        /// </summary>
        /// <param name="connMultiplexer">目标连接复用器 / The target multiplexer</param>
        private static void AddRegisterEvent(IConnectionMultiplexer connMultiplexer)
        {
            connMultiplexer.ConnectionRestored += ConnMultiplexer_ConnectionRestored;
            connMultiplexer.ConnectionFailed += ConnMultiplexer_ConnectionFailed;
            connMultiplexer.ErrorMessage += ConnMultiplexer_ErrorMessage;
            connMultiplexer.ConfigurationChanged += ConnMultiplexer_ConfigurationChanged;
            connMultiplexer.HashSlotMoved += ConnMultiplexer_HashSlotMoved;
            connMultiplexer.InternalError += ConnMultiplexer_InternalError;
            connMultiplexer.ConfigurationChangedBroadcast += ConnMultiplexer_ConfigurationChangedBroadcast;
        }

        /// <summary>
        /// 收到重新配置广播时（通常意味着主从同步更改），记 Debug 日志 / On a reconfigure broadcast (usually a master-slave sync change); Debug log
        /// </summary>
        /// <param name="sender">事件源 / Event source</param>
        /// <param name="e">事件参数 / Event arguments</param>
        private static void ConnMultiplexer_ConfigurationChangedBroadcast(object? sender, EndPointEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ConfigurationChangedBroadcast)}: {e.EndPoint}");
        }

        /// <summary>
        /// 发生内部错误时（主要用于调试），记 Debug 日志 / On an internal error (mainly for debugging); Debug log
        /// </summary>
        /// <param name="sender">事件源 / Event source</param>
        /// <param name="e">事件参数 / Event arguments</param>
        private static void ConnMultiplexer_InternalError(object? sender, InternalErrorEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_InternalError)}: {e.Exception}");
        }

        /// <summary>
        /// 集群哈希槽迁移时，记 Debug 日志 / When a cluster hash slot moves; Debug log
        /// </summary>
        /// <param name="sender">事件源 / Event source</param>
        /// <param name="e">事件参数（含新旧节点）/ Event arguments (old and new endpoints)</param>
        private static void ConnMultiplexer_HashSlotMoved(object? sender, HashSlotMovedEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_HashSlotMoved)}: {nameof(e.OldEndPoint)}-{e.OldEndPoint} To {nameof(e.NewEndPoint)}-{e.NewEndPoint}, ");
        }

        /// <summary>
        /// 配置更改时，记 Debug 日志 / When the configuration changes; Debug log
        /// </summary>
        /// <param name="sender">事件源 / Event source</param>
        /// <param name="e">事件参数 / Event arguments</param>
        private static void ConnMultiplexer_ConfigurationChanged(object? sender, EndPointEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ConfigurationChanged)}: {e.EndPoint}");
        }

        /// <summary>
        /// 服务端返回错误消息时，记 Debug 日志 / When the server reports an error message; Debug log
        /// </summary>
        /// <param name="sender">事件源 / Event source</param>
        /// <param name="e">事件参数 / Event arguments</param>
        private static void ConnMultiplexer_ErrorMessage(object? sender, RedisErrorEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ErrorMessage)}: {e.Message}");
        }

        /// <summary>
        /// 物理连接失败时，记 Debug 日志 / When a physical connection fails; Debug log
        /// </summary>
        /// <param name="sender">事件源 / Event source</param>
        /// <param name="e">事件参数 / Event arguments</param>
        private static void ConnMultiplexer_ConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ConnectionFailed)}: {e.Exception}");
        }

        /// <summary>
        /// 物理连接恢复时，记 Debug 日志 / When a physical connection is restored; Debug log
        /// </summary>
        /// <param name="sender">事件源 / Event source</param>
        /// <param name="e">事件参数 / Event arguments</param>
        private static void ConnMultiplexer_ConnectionRestored(object? sender, ConnectionFailedEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ConnectionRestored)}: {e.Exception}");
        }


        /// <summary>
        /// 序列化（JSON）/ Serialize (JSON)
        /// </summary>
        /// <param name="obj">待序列化对象 / The object to serialize</param>
        /// <returns>JSON 字符串；null 输入返回 null / The JSON string; null for a null input</returns>
        public static string? Serialize(object? obj)
        {
            if (obj == null)
                return null;

            //var binaryFormatter = new BinaryFormatter();
            //using (var memoryStream = new MemoryStream())
            //{
            //    binaryFormatter.Serialize(memoryStream, obj);
            //    var data = memoryStream.ToArray();
            //    return data;
            //}

            var data = JsonTool.Stringify(obj);
            return data;
        }

        /// <summary>
        /// 反序列化（JSON）/ Deserialize (JSON)
        /// </summary>
        /// <typeparam name="T">目标类型 / Target type</typeparam>
        /// <param name="data">JSON 字符串 / The JSON string</param>
        /// <returns>反序列化结果；null 输入返回 default(T) / The deserialized object; default(T) for a null input</returns>
        public static T? Deserialize<T>(string data)
        {
            if (data == null)
                return default(T);

            //var binaryFormatter = new BinaryFormatter();
            //using (var memoryStream = new MemoryStream(data))
            //{
            //    var result = (T)binaryFormatter.Deserialize(memoryStream);
            //    return result;
            //}
            var result = JsonTool.Parse<T>(data);
            return result;
        }


        /// <summary>
        /// RedisHelper 字典初始化锁 / Lock guarding RedisHelper dictionary initialization
        /// </summary>
        private static readonly object _redisClientLock = new object();

        /// <summary>
        /// 命名 RedisHelper 客户端字典，静态构造时按配置预创建。
        /// Named RedisHelper clients, pre-created from configuration in the static constructor.
        /// </summary>
        public static Dictionary<string, RedisHelper> RedisClientDict
        {
            get; private set;
        }

        /// <summary>
        /// 获取 Redis 客户端；name 为空返回第一个，未配置名称会因 KeyNotFound 被 catch 捕获。
        /// Get a Redis client; an empty name returns the first one; an unknown name hits KeyNotFound which is caught.
        /// </summary>
        /// <param name="name">RedisFactory:Clients 中的客户端名称 / Client name in RedisFactory:Clients</param>
        /// <returns>客户端实例；无配置或发生异常（含未知名称）时返回 null / The client; null when unconfigured or on error (including unknown names)</returns>
        public static RedisHelper? GetClient(string? name = null)
        {
            try
            {
                if (RedisClientDict.Count > 0)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return RedisClientDict.First().Value;
                    }
                    return RedisClientDict[name];
                }
                return null;
            }
            catch(Exception ex)
            {
                RampageLogger.Error(ex);
                return null;
            }
        }


    }
}
