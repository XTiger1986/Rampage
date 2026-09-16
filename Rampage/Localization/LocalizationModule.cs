using System;
using System.Globalization;

namespace Rampage.Localization
{
	/// <summary>
	/// Module to which localized language resources belong
	/// </summary>
	public class LocalizationModule
    {

        /// <summary>
        /// 
        /// </summary>
        public string ModuleName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, LocalizationResource>? Resources
        {
            get;
            internal set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="resourceKey"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultText"></param>
        /// <returns></returns>
        public string? Get(string key, string? defaultText = null)
        {
            CultureInfo? culture = LocalizationManager.CultureInfoGetter != null ? LocalizationManager.CultureInfoGetter() : null;
            return Get(key, culture, defaultText);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultText"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public string GetText(string key, params object?[] args)
        {
            CultureInfo? culture = LocalizationManager.CultureInfoGetter != null ? LocalizationManager.CultureInfoGetter() : null;
            return GetText(key, culture, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="args"></param>
        /// <returns></returns>
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
