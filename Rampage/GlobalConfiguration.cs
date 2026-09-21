using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Rampage.Caching;
using Rampage.Log;
using System;

namespace Rampage
{
    /// <summary>
    /// 全局配置入口：加载 config/app.json 与环境覆盖配置，并支持热重载回调链 / Global configuration entry: loads config/app.json with environment overrides and supports a hot-reload callback chain
    /// </summary>
    /// <remarks>
    /// Initialize 使用双重检查锁定，仅首次调用生效；配置文件热重载时会依次执行 ReloadCallbackList 中的回调、
    /// 将 _memoryCacheConfig 置空以失效缓存，随后重新挂接变更监听以感知后续重载。
    /// Initialize uses double-checked locking and takes effect only on the first call; on any configuration hot reload the callbacks
    /// in ReloadCallbackList run, _memoryCacheConfig is nulled to invalidate the cache, and the change watcher is re-registered for later reloads.
    /// </remarks>
    public class GlobalConfiguration
    {

        private static object _locker = new object();

        /// <summary>
        /// 初始化全局配置（仅首次调用生效） / Initializes the global configuration (effective on the first call only)
        /// </summary>
        /// <remarks>
        /// 加载 config/app.json，若存在 config/app.{环境名}.json 则追加为环境覆盖配置（两者均支持热重载）。
        /// Loads config/app.json and appends config/app.{EnvironmentName}.json as an environment override when it exists (both are reloadable).
        /// </remarks>
        /// <param name="environment">环境名称 / Environment name</param>
        public static void Initialize(string environment)
        {
            if (Configuration == null)
            {
                lock (_locker)
                {
                    if (Configuration == null)
                    {
                        EnvironmentName = environment;
                        var configurationManager = new ConfigurationManager();
                        configurationManager.AddJsonFile("config/app.json", true, true);
                        if (!string.IsNullOrEmpty(EnvironmentName))
                        {
                            string envConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config\\app.{EnvironmentName}.json");
                            if (File.Exists(envConfigFilePath))
                            {
                                configurationManager.AddJsonFile($"config\\app.{EnvironmentName}.json", true, true);
                            }
                        }

                        Configuration = configurationManager;
                    }

                    RegsterChangeCallbacks();
                }
            }
        }

        /// <summary>
        /// 注册配置变更回调：触发回调链并失效内存缓存配置，随后重新挂接监听 / Registers the change callback that runs the callback chain, invalidates the memory cache config, then re-arms the watcher
        /// </summary>
        private static void RegsterChangeCallbacks()
        {
            try
            {
                ConfigurationChangeToken = Configuration.GetReloadToken();
                ConfigurationChangeToken.RegisterChangeCallback((o) =>
                {
                    _memoryCacheConfig = null;
                    Action<object?>[] callbacks;
                    lock (_reloadCallbackListLock)
                    {
                        callbacks = ReloadCallbackList.ToArray();
                    }
                    foreach (var callback in callbacks)
                    {
                        callback(o);
                    }
                    RegsterChangeCallbacks();
                }, Configuration);
            }
            catch(Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }

        static GlobalConfiguration()
        {
            ReloadCallbackList = new List<Action<object?>>();
        }

        /// <summary>
        /// 重载回调列表的同步锁 / Lock guarding the reload callback list
        /// </summary>
        private static readonly object _reloadCallbackListLock = new object();

        /// <summary>
        /// 当前配置变更令牌，每次触发后重新挂接 / The current configuration change token, re-registered after each fire
        /// </summary>
        private static IChangeToken? ConfigurationChangeToken
        {
            get;
            set;
        }

        /// <summary>
        /// 当前环境名称 / Current environment name
        /// </summary>
        public static string? EnvironmentName
        {
            get;
            private set;
        }

        /// <summary>
        /// 全局配置根对象（ConfigurationManager，支持热重载） / The root configuration object (a reloadable ConfigurationManager)
        /// </summary>
        public static IConfiguration Configuration
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 将整个配置绑定到 T 类型实例 / Binds the whole configuration to an instance of T
        /// </summary>
        /// <typeparam name="T">配置模型类型 / Configuration model type</typeparam>
        /// <returns>绑定结果 / The bound value</returns>
        public static T? GetConfig<T>()
		{
            return Configuration.Get<T>();
		}

        /// <summary>
        /// 按配置键读取标量值 / Reads a scalar value by configuration key
        /// </summary>
        /// <typeparam name="T">值类型 / Value type</typeparam>
        /// <param name="key">配置键 / Configuration key</param>
        /// <returns>键不存在时返回类型默认值 / The value, or the default of T when the key is absent</returns>
        public static T? GetConfig<T>(string key)
        {
            return Configuration.GetValue<T>(key);
        }

        /// <summary>
        /// 获取指定键的配置节 / Gets the configuration section at the given key
        /// </summary>
        /// <param name="key">配置键 / Configuration key</param>
        /// <returns>配置节（键不存在时为空节） / The section (an empty section when the key is absent)</returns>
        public static IConfigurationSection GetSection(string key)
		{
            return Configuration.GetSection(key);
		}

        /// <summary>
        /// 获取指定键的配置节并绑定到 T / Gets the configuration section at the given key and binds it to T
        /// </summary>
        /// <typeparam name="T">配置模型类型 / Configuration model type</typeparam>
        /// <param name="key">配置键 / Configuration key</param>
        /// <returns>绑定结果，节为空时可能为 null / The bound value, possibly null for an empty section</returns>
        public static T? GetSectionConfig<T>(string key)
        {
            IConfigurationSection section = Configuration.GetSection(key);
            return section.Get<T>();
        }

        /// <summary>
        /// 单机内存缓存配置（首次访问缓存，热重载时失效；CacheKeyPrefix 缺省为 "Rampage_"） / In-process memory cache configuration (cached on first access, invalidated on reload; CacheKeyPrefix defaults to "Rampage_")
        /// </summary>
        public static MemoryCacheConfig MemoryCacheConfig
		{
			get
			{
                MemoryCacheConfig? config = _memoryCacheConfig;
                if (config == null)
                {
                    // GetValue<T> 只能绑定基元类型，复杂对象必须走 GetSection().Get<T>()，否则配置永远读不到 / GetValue<T> binds primitives only; complex objects must use GetSection().Get<T>() or the config is never read
                    config = Configuration.GetSection("MemoryCache").Get<MemoryCacheConfig>() ?? new MemoryCacheConfig();
                    if (string.IsNullOrEmpty(config.CacheKeyPrefix))
                    {
                        config.CacheKeyPrefix = "Rampage_";
                    }
                    _memoryCacheConfig = config;
                }
                return config;
			}
		}

        /// <summary>
        /// MemoryCacheConfig 的缓存实例，配置热重载时置空以失效 / Cached MemoryCacheConfig instance, nulled on configuration reload to invalidate it
        /// </summary>
        private static MemoryCacheConfig? _memoryCacheConfig;


        /// <summary>
        /// 配置热重载回调列表 / List of configuration reload callbacks
        /// </summary>
        public static List<Action<object?>> ReloadCallbackList
        {
            get;
            private set;
        }

        /// <summary>
        /// 注册配置热重载回调（同一委托重复注册会被忽略） / Registers a configuration reload callback (registering the same delegate twice is ignored)
        /// </summary>
        /// <param name="action">配置重载时执行的回调 / Callback executed on configuration reload</param>
        public static void RegisterReloadCallback(Action<object?> action)
        {
            lock (_reloadCallbackListLock)
            {
                if (!ReloadCallbackList.Contains(action))
                {
                    ReloadCallbackList.Add(action);
                }
            }
        }

    }
}
