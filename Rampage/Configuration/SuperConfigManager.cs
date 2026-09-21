using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Rampage.ExceptionExtensions;
using Rampage.Log;
using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Rampage.Configuration
{
    /// <summary>
    /// 配置管理器：按路径注册/缓存 JSON 配置，支持环境文件覆盖、reloadOnChange 热更新与变更回调。/ Configuration manager: registers/caches JSON configs by path, with environment overlays, reloadOnChange and change callbacks.
    /// </summary>
    /// <remarks>
    /// RegisterConfigs 仅在启动时把 [ConfigValue] 成员绑定一次；文件热更新只触发 RegisterReloadCallback 注册的用户回调，
    /// 已绑定的静态属性/字段不会自动重绑。/ RegisterConfigs binds [ConfigValue] members once at startup; hot reloads
    /// fire only user callbacks registered via RegisterReloadCallback — bound static members are NOT rebound automatically.
    /// </remarks>
    public class SuperConfigManager
    {
        /// <summary>
        /// 配置文件不存在时抛出的 SuperException 的 Key。/ The SuperException key thrown when a config file does not exist.
        /// </summary>
        public const string EX_KEY_ConfigurationFile_NotExists = "ConfigurationFile_NotExists";

        private static readonly object _containerLock = new object();

        /// <summary>
        /// 已注册配置容器（配置路径 → IConfiguration）。/ Container of registered configs (path → IConfiguration).
        /// </summary>
        public static ConcurrentDictionary<string, IConfiguration> ConfigurationContainer
        {
            get;
        } = new ConcurrentDictionary<string, IConfiguration>();

        /// <summary>
        /// 配置变更回调容器（配置路径 → 回调列表）。/ Container of reload callbacks (path → callback list).
        /// </summary>
        public static ConcurrentDictionary<string, List<Action<string>>> ReloadCallbackListContainer
        {
            get;
        } = new ConcurrentDictionary<string, List<Action<string>>>();

        private static readonly Type _configAttrType = typeof(SuperConfigAttribute);
        private static readonly Type _configValueType = typeof(ConfigValueAttribute);

        /// <summary>
        /// 注册配置类：扫描程序集中所有带 [SuperConfig] 的类型，注册其配置文件（Path 为空时用 config/app.json），
        /// 并把各类型中带 [ConfigValue] 的 public static 可读写属性/字段一次性绑定到配置值。
        /// 仅在启动时执行；之后配置文件热更新不会自动重绑这些静态成员。
        /// / Register config classes: scans the assembly for [SuperConfig] types, registers their config files
        /// (defaulting to config/app.json), and one-shot binds their [ConfigValue]-marked public static writable
        /// properties/fields to config values. Runs once at startup; later hot reloads do NOT rebind the static members.
        /// </summary>
        /// <param name="assembly">待扫描的程序集 / assembly to scan</param>
        /// <param name="environment">环境名；非空且存在 xxx.{env}.json 时以其覆盖默认配置 / environment name; when set and xxx.{env}.json exists, it overlays the base config</param>
        public static void RegisterConfigs(Assembly assembly, string? environment = null)
        {
            Type[] types = assembly.GetTypes();

            List<Type> configTypeList = new List<Type>();
            foreach (Type type in types)
            {
                if (!type.IsDefined(_configAttrType, true))
                {
                    continue;
                }

                SuperConfigAttribute configAttr = ReflectionTool.GetCustomAttribute<SuperConfigAttribute>(type)!;
                string? path = configAttr.Path;
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = "config/app.json";
                }

                Register(path, environment);
                configTypeList.Add(type);
            }

            if (configTypeList.Any())
            {
                foreach (Type configType in configTypeList)
                {
                    SuperConfigAttribute configAttr = ReflectionTool.GetCustomAttribute<SuperConfigAttribute>(configType)!;
                    string? path = configAttr.Path;
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        path = "config/app.json";
                    }
                    if (!ConfigurationContainer.ContainsKey(path))
                    {
                        continue;
                    }

                    IConfiguration configuration = ConfigurationContainer[path];

                    List<PropertyInfo> propertyList = ReflectionTool.GetPropertyList(configType);
                    List<FieldInfo> fieldList = ReflectionTool.GetFieldList(configType);

                    List<PropertyInfo> configPropertyList = propertyList.FindAll(p =>
                    {
                        if (!p.CanRead || !p.CanWrite)
                        {
                            return false;
                        }
                        if (!p.SetMethod!.IsStatic || !p.GetMethod!.IsStatic || !p.SetMethod!.IsPublic || !p.GetMethod!.IsPublic)
                        {
                            return false;
                        }
                        if (p.IsDefined(_configValueType, true))
                        {
                            return true;
                        }
                        return false;
                    });

                    List<FieldInfo> configFieldList = fieldList.FindAll(f =>
                    {
                        if (!f.IsPublic || !f.IsStatic)
                        {
                            return false;
                        }
                        if (f.IsDefined(_configValueType, true))
                        {
                            return true;
                        }
                        return false;
                    });

                    foreach (PropertyInfo configProperty in configPropertyList)
                    {
                        Type configValueType = configProperty.PropertyType;
                        ConfigValueAttribute configValueAttr = ReflectionTool.GetCustomAttribute<ConfigValueAttribute>(configProperty)!;
                        string node = configValueAttr.Node!;

                        object? configValue = configuration.GetNodeValue(node, configValueType);
                        if (configValue != null)
                        {
                            configProperty.SetValue(null, configValue);
                        }
                    }

                    foreach (FieldInfo configField in configFieldList)
                    {
                        Type configValueType = configField.FieldType;
                        ConfigValueAttribute configValueAttr = ReflectionTool.GetCustomAttribute<ConfigValueAttribute>(configField)!;
                        string node = configValueAttr.Node!;

                        object? configValue = configuration.GetNodeValue(node, configValueType);
                        if (configValue != null)
                        {
                            configField.SetValue(null, configValue);
                        }
                    }

                }
            }
        }

        /// <summary>
        /// 获取指定环境的配置路径：将 path 转换为同目录下的 xxx.{environment}.json。
        /// / Get the environment-specific config path: xxx.{environment}.json in the same directory as path.
        /// </summary>
        /// <param name="path">默认配置文件路径 / base configuration file path</param>
        /// <param name="environment">环境名 / environment name</param>
        /// <returns>环境配置文件的完整路径 / the full path of the environment config file</returns>
        public static string GetEnvConfigPath(string path, string environment)
        {
            string fullPath = PathTool.CombilePath(AppDomain.CurrentDomain.BaseDirectory, path);

            string? dirPath = Path.GetDirectoryName(fullPath);
            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            string fileExt = Path.GetExtension(fullPath);

            string envConfigFilePath = PathTool.CombilePath(dirPath!, $"{fileName}.{environment}{fileExt}");
            return envConfigFilePath;
        }

        /// <summary>
        /// 注册配置文件并缓存（双检锁防重复构建）：主文件不存在时抛出 Key 为 EX_KEY_ConfigurationFile_NotExists 的 SuperException；
        /// 以 AddJsonFile(optional:true, reloadOnChange:true) 添加主文件，指定环境且环境文件存在时追加 xxx.{env}.json 覆盖。
        /// / Register and cache a config file (double-checked locking): throws SuperException with key
        /// EX_KEY_ConfigurationFile_NotExists when the base file is missing; adds the JSON file with optional=true,
        /// reloadOnChange=true, and appends xxx.{env}.json as an overlay when the environment file exists.
        /// </summary>
        /// <param name="path">配置文件路径（相对应用基目录）/ config file path (relative to the app base directory)</param>
        /// <param name="environment">环境名 / environment name</param>
        /// <returns>该路径对应的 IConfiguration（已注册时直接返回缓存实例）/ the IConfiguration for the path (the cached instance when already registered)</returns>
        public static IConfiguration Register(string path, string? environment = null)
        {
            IConfiguration? configuration;
            if (!ConfigurationContainer.TryGetValue(path, out configuration))
            {
                lock (_containerLock)
                {
                    if (!ConfigurationContainer.TryGetValue(path, out configuration))
                    {
                        string fullPath = PathTool.CombilePath(AppDomain.CurrentDomain.BaseDirectory, path);
                        if (!File.Exists(fullPath))
                        {
                            throw new SuperException(EX_KEY_ConfigurationFile_NotExists, path);
                        }
                        var configurationManager = new ConfigurationManager();
                        configurationManager.AddJsonFile(fullPath, true, true);
                        if (!string.IsNullOrEmpty(environment))
                        {
                            string envConfigFilePath = GetEnvConfigPath(path, environment);
                            if (File.Exists(envConfigFilePath))
                            {
                                configurationManager.AddJsonFile(envConfigFilePath, true, true);
                            }
                        }

                        configuration = configurationManager;

                        ConfigurationContainer.TryAdd(path, configuration);
                        ReloadCallbackListContainer.TryRemove(path, out _);

                        RegsterChangeCallbacks(path);
                    }
                }
            }
            configuration = ConfigurationContainer[path];

            return configuration;
        }

        /// <summary>
        /// 注册配置变更回调函数：监听 GetReloadToken，文件变更时依次触发用户注册的回调并重新注册监听。
        /// / Register the change callback for a config path: watches GetReloadToken, fires user callbacks in turn on
        /// change, then re-registers the watch.
        /// </summary>
        /// <param name="path">配置文件路径 / config file path</param>
        private static void RegsterChangeCallbacks(string path)
        {
            try
            {
                IConfiguration? configuration;
                if (!ConfigurationContainer.TryGetValue(path, out configuration))
                {
                    return;
                }
                IChangeToken changeToken = configuration.GetReloadToken();
                changeToken.RegisterChangeCallback((o) =>
                {
                    if (o == null)
                    {
                        return;
                    }
                    string changedPath = (string)o;

                    if (ReloadCallbackListContainer.TryGetValue(changedPath, out List<Action<string>>? actionList))
                    {
                        Action<string>[] callbacks;
                        lock (_containerLock)
                        {
                            callbacks = actionList.ToArray();
                        }
                        foreach (var callback in callbacks)
                        {
                            callback(changedPath);
                        }
                    }
                    RegsterChangeCallbacks(changedPath);
                }, path);
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }

        /// <summary>
        /// 注册配置文件变更回调函数（同一回调不重复添加）。文件变更时仅触发此处注册的回调；
        /// RegisterConfigs 绑定的静态成员不会自动重绑。
        /// / Register a reload callback for a config path (duplicates are ignored). Only callbacks registered here run
        /// on change; static members bound by RegisterConfigs are not rebound automatically.
        /// </summary>
        /// <param name="path">配置文件路径 / config file path</param>
        /// <param name="action">回调（参数为发生变更的配置路径）/ the callback (receives the changed config path)</param>
        public static void RegisterReloadCallback(string path, Action<string> action)
        {
            List<Action<string>> actionList = ReloadCallbackListContainer.GetOrAdd(path, _ => new List<Action<string>>());
            lock (_containerLock)
            {
                if (!actionList.Contains(action))
                {
                    actionList.Add(action);
                }
            }
        }


    }
}
