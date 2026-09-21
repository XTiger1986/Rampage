using System;
using System.Globalization;

namespace Rampage.Localization
{
	/// <summary>
	/// 本地化资源模块：对应资源根目录下的一个子目录（目录名即模块名）。
	/// / A localization module corresponding to one subdirectory of the resource root (directory name = module name).
	/// </summary>
	public class LocalizationModule
    {

        /// <summary>
        /// 模块名（即目录名）。
        /// / Module name (the directory name).
        /// </summary>
        public string ModuleName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 语言名 → 语言资源 的映射。
        /// / Map of language name to language resource.
        /// </summary>
        public Dictionary<string, LocalizationResource>? Resources
        {
            get;
            internal set;
        }

        /// <summary>
        /// 按语言获取资源；culture 为 null 时取默认语言资源。
        /// / Get the resource for the culture; the default language resource is used when culture is null.
        /// </summary>
        /// <param name="culture">目标语言 / target culture</param>
        /// <returns>语言资源；模块无任何资源时返回 null / the resource, or null when the module has none</returns>
        public LocalizationResource? GetResource(CultureInfo? culture)
        {
            string resourceKey;
            if (culture == null)
            {
                resourceKey = LocalizationManager.Config.DefaultLanguageName;
            }
            else
            {
                resourceKey = culture.Name;
            }
            return GetResource(resourceKey);
        }

        /// <summary>
        /// 按语言名获取资源，回退链：指定语言 → 默认语言（default.json）→ 模块首个资源。
        /// / Get the resource by language name with fallback: requested language → default language (default.json) → the module's first resource.
        /// </summary>
        /// <param name="resourceKey">语言名（如 zh-CN、default）/ language name (e.g. zh-CN, default)</param>
        /// <returns>语言资源；模块无任何资源时返回 null / the resource, or null when the module has none</returns>
        public LocalizationResource? GetResource(string resourceKey)
        {
            if (Resources != null && Resources.Count > 0)
            {
                if (Resources.ContainsKey(resourceKey))
                {
                    return Resources[resourceKey];
                }
                else if (Resources.ContainsKey(LocalizationManager.Config.DefaultLanguageName))
                {
                    return Resources[LocalizationManager.Config.DefaultLanguageName];
                }
                else
                {
                    return Resources.First().Value;
                }
            }
            return null;
        }

        /// <summary>
        /// 以 <see cref="LocalizationManager.CultureInfoGetter"/> 决定的语言读取键值。
        /// / Read the key's value using the culture resolved from <see cref="LocalizationManager.CultureInfoGetter"/>.
        /// </summary>
        /// <param name="key">资源键 / resource key</param>
        /// <param name="defaultText">未命中时返回的默认文本，可为 null / text returned on miss, may be null</param>
        /// <returns>键值或 defaultText / the value, or defaultText on miss</returns>
        public string? Get(string key, string? defaultText = null)
        {
            CultureInfo? culture = LocalizationManager.CultureInfoGetter != null ? LocalizationManager.CultureInfoGetter() : null;
            return Get(key, culture, defaultText);
        }

        /// <summary>
        /// 以指定语言读取键值；目标语言未命中时回退默认语言（default.json）。
        /// / Read the key's value for the given culture, falling back to the default language (default.json) on miss.
        /// </summary>
        /// <param name="key">资源键 / resource key</param>
        /// <param name="culture">目标语言；为 null 时取默认语言 / target culture; the default language when null</param>
        /// <param name="defaultText">未命中时返回的默认文本，可为 null / text returned on miss, may be null</param>
        /// <returns>键值或 defaultText / the value, or defaultText on miss</returns>
        public string? Get(string key, CultureInfo? culture, string? defaultText = null)
        {
            string? text = null;
            string cultureName = culture == null ? LocalizationManager.Config.DefaultLanguageName : culture.Name;
            LocalizationResource? resource = GetResource(cultureName);
            if (resource != null)
            {
                text = resource.Get(key, defaultText);
            }

            if (string.IsNullOrEmpty(text))
            {
                resource = GetResource(LocalizationManager.Config.DefaultLanguageName);
                if (resource != null)
                {
                    text = resource.Get(key, defaultText);
                }
            }

            return text;
        }

        /// <summary>
        /// 以 <see cref="LocalizationManager.CultureInfoGetter"/> 决定的语言取文本并用 string.Format 格式化。
        /// / Get the text for the culture from <see cref="LocalizationManager.CultureInfoGetter"/> and format it via string.Format.
        /// </summary>
        /// <param name="key">资源键 / resource key</param>
        /// <param name="args">格式化参数 / format arguments</param>
        /// <returns>格式化后的文本；未命中时返回键名本身 / the formatted text; the key itself on miss</returns>
        public string GetText(string key, params object?[] args)
        {
            CultureInfo? culture = LocalizationManager.CultureInfoGetter != null ? LocalizationManager.CultureInfoGetter() : null;
            return GetText(key, culture, args);
        }

        /// <summary>
        /// 以指定语言取文本并用 string.Format 格式化；回退链：目标语言 → 默认语言 → 键名本身。
        /// / Get and format text for the given culture; fallback chain: requested culture → default language → the key itself.
        /// </summary>
        /// <param name="key">资源键 / resource key</param>
        /// <param name="culture">目标语言；为 null 时取默认语言 / target culture; the default language when null</param>
        /// <param name="args">格式化参数 / format arguments</param>
        /// <returns>格式化后的文本；全部未命中时返回键名本身 / the formatted text; the key itself when everything misses</returns>
        public string GetText(string key, CultureInfo? culture, params object?[] args)
        {
            string? text = null;
            string cultureName = culture == null ? LocalizationManager.Config.DefaultLanguageName : culture.Name;
            LocalizationResource? resource = GetResource(cultureName);
            if (resource != null)
            {
                text = resource.GetText(key, args);
            }

            if (string.IsNullOrEmpty(text))
            {
                resource = GetResource(LocalizationManager.Config.DefaultLanguageName);
                if (resource != null)
                {
                    text = resource.GetText(key, args);
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                text = string.IsNullOrEmpty(text) ? key : string.Empty;
            }

            return text;
        }



    }
}
