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
    /// 配置管理器
    /// Configuration Manager
    /// </summary>
    public class SuperConfigManager
    {
        public const string EX_KEY_ConfigurationFile_NotExists = "ConfigurationFile_NotExists";

        private static readonly object _containerLock = new object();

        public static ConcurrentDictionary<string, IConfiguration> ConfigurationContainer
        {
            get;
        } = new ConcurrentDictionary<string, IConfiguration>();

        public static ConcurrentDictionary<string, List<Action<string>>> ReloadCallbackListContainer
        {
            get;
        } = new ConcurrentDictionary<string, List<Action<string>>>();

        private static readonly Type _configAttrType = typeof(SuperConfigAttribute);
        private static readonly Type _configValueType = typeof(ConfigValueAttribute);

        /// <summary>
        /// 注册配置类
        /// Register configuration class
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="environment"></param>
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
        /// 获取指定环境的配置路径
        /// Get the configuration path for the specified environment
        /// </summary>
        /// <param name="path">default configuration path</param>
        /// <param name="environment"></param>
        /// <returns></returns>
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
        /// 注册配置类
        /// Register configuration class
        /// </summary>
        /// <param name="path"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
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
        /// 注册配置变更回调函数
        /// Register configuration change callback functions
        /// </summary>
        /// <param name="path"></param>
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
        /// 注册配置变更回调函数
        /// Register configuration change callback function
        /// </summary>
        /// <param name="path"></param>
        /// <param name="action"></param>
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
