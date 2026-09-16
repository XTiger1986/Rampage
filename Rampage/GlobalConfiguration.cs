using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Rampage.Caching;
using Rampage.Log;
using System;

namespace Rampage
{
    /// <summary>
    /// 
    /// </summary>
    public class GlobalConfiguration
    {

        private static object _locker = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="environment"></param>
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
        /// 
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
        ///
        /// </summary>
        private static readonly object _reloadCallbackListLock = new object();

        /// <summary>
        /// 
        /// </summary>
        private static IChangeToken? ConfigurationChangeToken
        {
            get;
            set;
        }

        /// <summary>
        /// 环境名称
        /// </summary>
        public static string? EnvironmentName
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public static IConfiguration Configuration
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T? GetConfig<T>()
		{
            return Configuration.Get<T>();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? GetConfig<T>(string key)
        {
            return Configuration.GetValue<T>(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IConfigurationSection GetSection(string key)
		{
            return Configuration.GetSection(key);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? GetSectionConfig<T>(string key)
        {
            IConfigurationSection section = Configuration.GetSection(key);
            return section.Get<T>();
        }

        /// <summary>
        /// 单机内存缓存配置
        /// Memory Cache Configurations
        /// </summary>
        public static MemoryCacheConfig MemoryCacheConfig
		{
			get
			{
                MemoryCacheConfig? config = _memoryCacheConfig;
                if (config == null)
                {
                    // GetValue<T> 只能绑定基元类型，复杂对象必须走 GetSection().Get<T>()，否则配置永远读不到
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

        private static MemoryCacheConfig? _memoryCacheConfig;


        /// <summary>
        /// 
        /// </summary>
        public static List<Action<object?>> ReloadCallbackList
        {
            get;
            private set;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
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
