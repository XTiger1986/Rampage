using Microsoft.Extensions.Configuration;
using Rampage.Json;
using Rampage.Log;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;

namespace Rampage.RedisLibrary
{
    public class RedisFactory
    {

        private static volatile RedisFactoryConfig? _config;
        /// <summary>
        ///
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
        ///
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
        /// Default Redis Connection Multiplexer
        /// </summary>
        public static IConnectionMultiplexer? ConnMultiplexer
        {
            get;
            private set;
        }

        /// <summary>
        /// Redis Connection Multiplexer Container
        /// key: RedisConfig.Name
        /// </summary>
        public static ConcurrentDictionary<string, Lazy<IConnectionMultiplexer>> ConnMultiplexerDict
        {
            get;
        } = new ConcurrentDictionary<string, Lazy<IConnectionMultiplexer>>();

        /// <summary>
        ///
        /// </summary>
        private static readonly object _multiplexerLocker = new object();

        /// <summary>
        /// 创建连接（不加全局锁：一个实例连不上不再阻塞其它实例首连）
        /// </summary>
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
        /// Try Get Connection Multiplexer
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static IConnectionMultiplexer? TryGetConnectionMultiplexer(string name)
        {
            if (ConnMultiplexerDict.TryGetValue(name, out Lazy<IConnectionMultiplexer>? lazy))
            {
                return lazy.Value;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static RedisConfig? TryGetDefaultRedisConfig()
        {
            if (Config.Clients != null && Config.Clients.Count > 0)
            {
                return Config.Clients.First().Value;
            }
            return null;
        }
        /// <summary>
        /// Try Get Default Connection Multiplexer
        /// </summary>
        /// <returns></returns>
        public static IConnectionMultiplexer? GetConnectionRedisMultiplexer()
        {
            // 已有 multiplexer 时直接返回，由 SE.Redis 自行重连。
            // 旧逻辑在 !IsConnected 时于全局 lock 内同步 Connect，会导致全站请求排队数秒。
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
        /// 
        /// </summary>
        /// <returns></returns>
        public static RedisConfig? TryGetRedisConfig(string name)
        {
            if (Config.Clients != null && Config.Clients.Count > 0)
            {
                return Config.Clients.ContainsKey(name) ? Config.Clients[name] : null;
            }
            return null;
        }
        /// <summary>
        /// Try Get Connection Multiplexer
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
            // Lazy 保证同名连接只建立一次，且连接过程不在任何全局锁内
            Lazy<IConnectionMultiplexer> lazy = ConnMultiplexerDict.GetOrAdd(name, _ => new Lazy<IConnectionMultiplexer>(() => CreateMultiplexer(redisConfig!)));
            return lazy.Value;
        }


        /// <summary>
        /// Add RegisterEvent for ConnectionMultiplexer
        /// </summary>
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
        /// 重新配置广播时（通常意味着主从同步更改）
        /// When reconfiguring the broadcast (usually implying changes in master-slave synchronization)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ConfigurationChangedBroadcast(object? sender, EndPointEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ConfigurationChangedBroadcast)}: {e.EndPoint}");
        }

        /// <summary>
        /// 发生内部错误时（主要用于调试）
        /// An internal error occurred (mainly for debugging)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_InternalError(object? sender, InternalErrorEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_InternalError)}: {e.Exception}");
        }

        /// <summary>
        /// 更改集群时
        /// When changing the cluster
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_HashSlotMoved(object? sender, HashSlotMovedEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_HashSlotMoved)}: {nameof(e.OldEndPoint)}-{e.OldEndPoint} To {nameof(e.NewEndPoint)}-{e.NewEndPoint}, ");
        }

        /// <summary>
        /// 配置更改时
        /// When the configuration changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ConfigurationChanged(object? sender, EndPointEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ConfigurationChanged)}: {e.EndPoint}");
        }

        /// <summary>
        /// 发生错误时
        /// When an error occurs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ErrorMessage(object? sender, RedisErrorEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ErrorMessage)}: {e.Message}");
        }

        /// <summary>
        /// 物理连接失败时
        /// When the physical connection fails
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ConnectionFailed)}: {e.Exception}");
        }

        /// <summary>
        /// 建立物理连接时
        /// When establishing a physical connection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnMultiplexer_ConnectionRestored(object? sender, ConnectionFailedEventArgs e)
        {
            RampageLogger.Debug($"{nameof(ConnMultiplexer_ConnectionRestored)}: {e.Exception}");
        }


        /// <summary>
        /// 序列化
        /// Serialize
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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
        /// 反序列化
        /// Deserialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        private static readonly object _redisClientLock = new object();

        public static Dictionary<string, RedisHelper> RedisClientDict
        {
            get; private set;
        }

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
