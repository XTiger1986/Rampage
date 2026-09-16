using Rampage.Localization;
using System;
using System.Globalization;

namespace Rampage.Management.Permission
{
    public class PermissionActionInfo
    {
        public static readonly string CACHE_KEY_AUTHORIZED_LIST_PFX = "AuthorizedActionListOfAdmin";
        public static readonly string CACHE_KEY_AUTHORIZED_LIST_TPL = CACHE_KEY_AUTHORIZED_LIST_PFX + "_{0}";

        /// <summary>
        /// 
        /// </summary>
        public string? ActionId
        {
            get;
            internal set;
        }

        /// <summary>
        /// 区域名称
        /// </summary>
        public string? AreaName
        {
            get;
            internal set;
        }

        private string? _controllerName;
        /// <summary>
        /// 控制器名称
        /// </summary>
        public string? ControllerName
        {
            get { return _controllerName; }
            internal set { _controllerName = value; }
        }

        private string? _actionName;
        /// <summary>
        /// 视图名称
        /// </summary>
        public string? ActionName
        {
            get { return _actionName; }
            internal set { _actionName = value; }
        }

        private string? _resource;
        /// <summary>
        /// 权限资源组标识
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

        /// <summary>
        /// 权限资源组名称
        /// </summary>
        public string? ResourceName
        {
            get;
            set;
        }

        /// <summary>
        /// 功能标题
        /// </summary>
        public string? Title
        {
            get;
            set;
        }

        /// <summary>
        /// Multiple Language Title
        /// </summary>
        public Dictionary<string, string?> TitleDict
        {
            get; internal set;
        } = default!;

        /// <summary>
        /// 功能说明
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// 排序值（值越小，越优先）
        /// </summary>
        public int SortOrder
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
