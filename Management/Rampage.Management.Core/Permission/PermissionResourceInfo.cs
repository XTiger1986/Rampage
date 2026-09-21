using Rampage.Localization;
using System;
using System.Globalization;

namespace Rampage.Management.Permission
{
    /// <summary>权限资源组信息（聚合同一 Resource 标识下的多个功能） / Permission resource info (aggregates the actions sharing one Resource identifier)</summary>
    public class PermissionResourceInfo
    {

        /// <summary>资源名（资源组标识） / Resource name (resource group identifier)</summary>
        public string? Resource
        {
            get; 
            internal set;
        }

        /// <summary>权限资源组名称（多语言，键为语言名） / Permission resource group name (multilingual, keyed by language name)</summary>
        public Dictionary<string, string?> ResourceTextDict
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 获取指定文化的资源组名称：当前文化文本 → 默认语言文本 → 资源名兜底
        /// Get the resource group name for the specified culture: current-culture text -> default-language text -> the resource name as fallback
        /// </summary>
        /// <param name="culture">目标文化；null 时取当前请求文化，仍无则用默认语言 / The target culture; falls back to the current request culture, then the default language when null</param>
        /// <returns>解析出的资源组名称；无可用文本时为资源名 / The resolved resource group name; the resource name when no text is available</returns>
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
