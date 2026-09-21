using Rampage.Localization;
using System;
using System.Globalization;

namespace Rampage.Management.Permission
{
    /// <summary>权限功能信息（由 PermissionLoader 从控制器反射收集） / Permission action info (collected from controllers via reflection by PermissionLoader)</summary>
    public class PermissionActionInfo
    {
        /// <summary>管理员授权功能列表缓存的键前缀 / Cache key prefix for an admin's authorized action list</summary>
        public static readonly string CACHE_KEY_AUTHORIZED_LIST_PFX = "AuthorizedActionListOfAdmin";
        /// <summary>管理员授权功能列表缓存的键模板（{0}=管理员ID） / Cache key template for an admin's authorized action list ({0} = admin ID)</summary>
        public static readonly string CACHE_KEY_AUTHORIZED_LIST_TPL = CACHE_KEY_AUTHORIZED_LIST_PFX + "_{0}";

        /// <summary>功能标识（控制器完整类型名.方法名） / Action identifier (controller full type name + method name)</summary>
        public string? ActionId
        {
            get;
            internal set;
        }

        /// <summary>区域名称 / Area name</summary>
        public string? AreaName
        {
            get;
            internal set;
        }

        private string? _controllerName;
        /// <summary>控制器名称（去除 Controller 后缀） / Controller name (without the Controller suffix)</summary>
        public string? ControllerName
        {
            get { return _controllerName; }
            internal set { _controllerName = value; }
        }

        private string? _actionName;
        /// <summary>视图名称(Action名) / Action name</summary>
        public string? ActionName
        {
            get { return _actionName; }
            internal set { _actionName = value; }
        }

        private string? _resource;
        /// <summary>
        /// 权限资源组标识；未显式赋值时按 区域_控制器_视图 生成
        /// Permission resource group identifier; generated as area_controller_action when not set explicitly
        /// </summary>
        public string Resource
        {
            get
            {
                return _resource ?? $"{AreaName}_{ControllerName}_{ActionName}";
            }
            set
            {
                _resource = value ?? $"{AreaName}_{ControllerName}_{ActionName}";
            }
        }

        /// <summary>权限资源组名称（多语言文本，来自本地化模块） / Permission resource group name (multilingual text from the localization module)</summary>
        public string? ResourceName
        {
            get;
            set;
        }

        /// <summary>功能标题（自定义文本，优先级低于多语言标题） / Action title (custom text, lower priority than the multilingual title)</summary>
        public string? Title
        {
            get;
            set;
        }

        /// <summary>多语言功能标题（键为语言名） / Multilingual action title (keyed by language name)</summary>
        public Dictionary<string, string?> TitleDict
        {
            get; internal set;
        } = default!;

        /// <summary>功能说明 / Action description</summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>排序值（值越小越优先） / Sort order (smaller = higher priority)</summary>
        public int SortOrder
        {
            get;
            set;
        }

        /// <summary>
        /// 获取指定文化的功能标题：多语言标题 → 默认语言标题 → 自定义 Title
        /// Get the action title for the specified culture: multilingual title -> default-language title -> custom Title
        /// </summary>
        /// <param name="culture">目标文化；null 时取当前请求文化，仍无则用默认语言 / The target culture; falls back to the current request culture, then the default language when null</param>
        /// <returns>解析出的标题；无任何可用文本时为 null / The resolved title; null when no text is available</returns>
        public string? GetTitle(CultureInfo? culture = null)
        {
            if (culture == null && LocalizationManager.CultureInfoGetter != null)
            {
                culture = LocalizationManager.CultureInfoGetter();
            }
            string? text = null;
            string cultureName = culture == null ? LocalizationManager.Config.DefaultLanguageName : culture.Name;
            // If the custom text is empty or specifies the use of localized language
            if (TitleDict.ContainsKey(cultureName))
            {
                text = TitleDict[cultureName];
            }

            if (string.IsNullOrEmpty(text) && TitleDict.ContainsKey(LocalizationManager.Config.DefaultLanguageName))
            {
                text = TitleDict[LocalizationManager.Config.DefaultLanguageName];
            }

            if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(Title))
            {
                text = Title;
            }

            return text;
        }

    }



}
