using Rampage.Localization;
using System;
using System.Globalization;

namespace Rampage.Management.Permission
{
    public class PermissionResourceInfo
    {

        /// <summary>
        /// Permission Resource Name
        /// 资源名
        /// </summary>
        public string? Resource
        {
            get; 
            internal set;
        }

        /// <summary>
        /// Permission Resource Group Name (Multilingual)
        /// 权限资源组名称(多语言)
        /// </summary>
        public Dictionary<string, string?> ResourceTextDict
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public string? GetResourceText(CultureInfo? culture = null)
        {
            if (culture == null && LocalizationManager.CultureInfoGetter != null)
            {
                culture = LocalizationManager.CultureInfoGetter();
            }
            string? text = null;
            string cultureName = culture == null ? LocalizationManager.Config.DefaultLanguageName : culture.Name;
            if (ResourceTextDict.ContainsKey(cultureName))
            {
                text = ResourceTextDict[cultureName];
            }
            if (string.IsNullOrEmpty(text) && ResourceTextDict.ContainsKey(LocalizationManager.Config.DefaultLanguageName))
            {
                text = ResourceTextDict[LocalizationManager.Config.DefaultLanguageName];
            }
            if (string.IsNullOrEmpty(text))
            {
                text = Resource;
            }

            return text;
        }

    }
}
