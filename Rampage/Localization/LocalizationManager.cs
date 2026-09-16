using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;

namespace Rampage.Localization
{
	/// <summary>
	/// Localized Language Resource Manager
	/// </summary>
	public class LocalizationManager
    {
        private static object _locker = new object();

        static LocalizationManager()
        {
            ReloadConfig();

            if (ModuleResources == null)
            {
                lock (_locker)
                {
                    if (ModuleResources == null)
                    {
                        Initialize();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void Initialize()
        {
            ModuleResources = new Dictionary<string, LocalizationModule>();

            DirectoryInfo rootDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.RootDirectory));
            if (!rootDirectory.Exists)
            {
                return;
            }

            DirectoryInfo[] moduleDirectories = rootDirectory.GetDirectories();
            if (moduleDirectories.Length == 0)
            {
                return;
            }

            foreach(DirectoryInfo moduleDirectory in moduleDirectories)
            {
                FileInfo[] resourceFiles = moduleDirectory.GetFiles("*.json");
                if (resourceFiles.Length == 0)
                {
                    continue;
                }
                string moduleName = moduleDirectory.Name;
                LocalizationModule localizationModule = new LocalizationModule();
                localizationModule.ModuleName = moduleName;
                localizationModule.Resources = new Dictionary<string, LocalizationResource>();

                foreach(FileInfo resourceFile in resourceFiles)
                {
                    string langName = Path.GetFileNameWithoutExtension(resourceFile.Name);

                    LocalizationResource resource = new LocalizationResource();
                    resource.ModuleName = moduleName;
                    resource.CultureName = langName;
                    ConfigurationManager resManager = new ConfigurationManager();
                    resManager.AddJsonFile($"{Config.RootDirectory}\\{moduleName}\\{langName}.json", true, true);
                    resource.Resource = resManager;

                    localizationModule.Resources.Add(langName, resource);
                }

                ModuleResources.Add(moduleName, localizationModule);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ReloadConfig()
        {
            LocalizationConfig? config = GlobalConfiguration.Configuration.GetValue<LocalizationConfig>("LocalizationConfig");
            if (config == null)
            {
                config = new LocalizationConfig();
            }
            if (string.IsNullOrEmpty(config.RootDirectory))
                config.RootDirectory = "_localization_resources";
            if (string.IsNullOrEmpty(config.DefaultModuleName))
                config.DefaultModuleName = "Global";
            if (string.IsNullOrEmpty(config.DefaultLanguageName))
                config.DefaultLanguageName = "default";
            Config = config;
        }

        /// <summary>
        /// 
        /// </summary>
        public static LocalizationConfig Config
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        internal static Dictionary<string, LocalizationModule> ModuleResources
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 自定义获取当前语言的委托函数
        /// </summary>
        public static Func<CultureInfo>? CultureInfoGetter
        {
            get;
            set;
        }

		/// <summary>
		/// get Localized Language Resource
		/// </summary>
		/// <param name="culture"></param>
		/// <returns></returns>
		public static LocalizationResource? GetResource(CultureInfo? culture = null)
        {
            return GetResource(Config.DefaultModuleName, culture);
        }

		/// <summary>
		/// get Localized Language Resource
		/// </summary>
		/// <param name="moduleName"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public static LocalizationResource? GetResource(string moduleName, CultureInfo? culture = null)
        {
            if (culture == null)
            {
                culture = CultureInfoGetter != null ? CultureInfoGetter() : null;
            }
            if (ModuleResources.ContainsKey(moduleName))
            {
                LocalizationModule localizationModule = ModuleResources[moduleName];
                return localizationModule.GetResource(culture);
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public static LocalizationModule? GetModule(string moduleName)
        {
            if (ModuleResources.ContainsKey(moduleName))
            {
                LocalizationModule localizationModule = ModuleResources[moduleName];
                return localizationModule;
            }
            return null;
        }

    }
}
