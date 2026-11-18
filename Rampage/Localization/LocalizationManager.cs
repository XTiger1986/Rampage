using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;

namespace Rampage.Localization
{
	/// <summary>
	/// 本地化语言资源管理器：静态扫描 {应用基目录}/{RootDirectory}/{模块}/{语言}.json 装载各模块资源。
	/// / Localized resource manager: statically scans {base dir}/{RootDirectory}/{module}/{language}.json to load module resources.
	/// </summary>
	/// <remarks>
	/// 每个语言文件以 ConfigurationManager + AddJsonFile(reloadOnChange:true) 装载，文件变更自动生效；
	/// 框架不维护全局“当前语言”，语言由 <see cref="CultureInfoGetter"/> 委托或调用方显式传入的 culture 决定；
	/// 取值回退链：请求语言 → 默认语言（default.json）→ 模块首个资源 → 键名本身（GetText 最终回退）。
	/// </remarks>
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
        /// 扫描资源根目录初始化模块资源：每个一级子目录视为一个模块，目录内每个 *.json（文件名即语言名）
        /// 装载为一个 LocalizationResource；根目录不存在时不加载任何模块。
        /// / Scan the resource root to initialize modules: each first-level subdirectory is a module, and each *.json
        /// inside (file name = language) is loaded as a LocalizationResource; no modules are loaded when the root is missing.
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
        /// 从全局配置的 "LocalizationConfig" 节重读本地化配置，空缺项回填默认值
        /// （_localization_resources / Global / default）。
        /// / Re-read the "LocalizationConfig" section from the global configuration, filling defaults
        /// (_localization_resources / Global / default) for missing values.
        /// </summary>
        public static void ReloadConfig()
        {
            // 全局配置可能晚于本类的静态构造（后台线程早期触达）就绪：null 时直接回退默认值，
            // 不能把 ArgumentNullException 抛进静态构造导致 TypeInitializationException 连环崩溃。
            // / The global configuration may not be ready yet when the static ctor of this class is touched early
            // by a background thread: fall back to defaults on null instead of throwing ArgumentNullException
            // inside the static constructor, which would cascade into a TypeInitializationException crash.
            LocalizationConfig? config = GlobalConfiguration.Configuration?.GetValue<LocalizationConfig>("LocalizationConfig");
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
        /// 当前生效的本地化配置（由 ReloadConfig 设置）。
        /// / The effective localization config (set by ReloadConfig).
        /// </summary>
        public static LocalizationConfig Config
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 模块名 → 模块资源 的映射表。
        /// / Map of module name to its resources.
        /// </summary>
        internal static Dictionary<string, LocalizationModule> ModuleResources
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 自定义获取当前语言的委托函数；未设置或返回 null 时按默认语言（default.json）处理。
        /// / Custom delegate returning the current culture; when unset (or returning null) the default language (default.json) is used.
        /// </summary>
        public static Func<CultureInfo>? CultureInfoGetter
        {
            get;
            set;
        }

		/// <summary>
		/// 获取默认模块（Config.DefaultModuleName）的语言资源。
		/// / Get the language resource of the default module (Config.DefaultModuleName).
		/// </summary>
		/// <param name="culture">目标语言；为 null 时调用 CultureInfoGetter / target culture; CultureInfoGetter is consulted when null</param>
		/// <returns>语言资源；默认模块不存在时返回 null / the resource, or null when the default module is missing</returns>
		public static LocalizationResource? GetResource(CultureInfo? culture = null)
        {
            return GetResource(Config.DefaultModuleName, culture);
        }

		/// <summary>
		/// 获取指定模块的语言资源；culture 为 null 时调用 CultureInfoGetter；模块内语言回退由 LocalizationModule 处理。
		/// / Get the language resource of the given module; CultureInfoGetter is consulted when culture is null;
		/// in-module language fallback is handled by LocalizationModule.
		/// </summary>
		/// <param name="moduleName">模块名 / module name</param>
		/// <param name="culture">目标语言；为 null 时调用 CultureInfoGetter / target culture; CultureInfoGetter is consulted when null</param>
		/// <returns>语言资源；模块不存在时返回 null / the resource, or null when the module does not exist</returns>
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
        /// 获取指定名称的模块。
        /// / Get the module by name.
        /// </summary>
        /// <param name="moduleName">模块名 / module name</param>
        /// <returns>模块；不存在时返回 null / the module, or null when missing</returns>
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
