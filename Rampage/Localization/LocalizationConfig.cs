using System;

namespace Rampage.Localization
{
	/// <summary>
	/// 本地化资源配置（对应全局配置的 "LocalizationConfig" 节）。
	/// / Localization settings (bound to the "LocalizationConfig" section of the global configuration).
	/// </summary>
	public class LocalizationConfig
    {
		/// <summary>
		/// 语言资源根目录路径（相对应用基目录，默认 _localization_resources）。
		/// / Root directory of language resources (relative to the app base directory; default _localization_resources).
		/// </summary>
		public string RootDirectory
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 默认模块名（GetResource 未指定模块时使用，默认 Global）。
        /// / Default module name (used by GetResource when no module is given; default Global).
        /// </summary>
        public string DefaultModuleName
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 默认语言名（回退语言，对应 default.json，默认 default）。
        /// / Default language name (the fallback language mapping to default.json; default "default").
        /// </summary>
        public string DefaultLanguageName
        {
            get;
            set;
        } = default!;

    }
}
