using Microsoft.AspNetCore.Mvc.Controllers;
using Rampage.Localization;
using System;
using System.Globalization;

namespace Rampage.Web.Mvc
{
	/// <summary>
	/// 视图上下文信息：由 SuperController.OnActionExecuting 组装并放入 ViewBag.ViewInfo，供视图布局页统一读取当前 Action、区域、语言与时区等 / View context info: assembled by SuperController.OnActionExecuting and exposed as ViewBag.ViewInfo so layouts can read the current action, area, language and time zone
	/// </summary>
	public class SuperViewInfo
	{

		/// <summary>
		/// 当前 Action 的描述符 / The descriptor of the current action
		/// </summary>
		public ControllerActionDescriptor ActionDescriptor
		{
			get;
			set;
		} = default!;

		/// <summary>
		/// 区域名（无区域为空字符串）/ The area name (empty string when none)
		/// </summary>
		public string? AreaName
		{
			get;
			set;
		}

		/// <summary>
		/// 控制器名（不含 Area 与命名空间）/ The controller name (without area or namespace)
		/// </summary>
		public string ControllerName
		{
			get;
			set;
		} = default!;

		/// <summary>
		/// Action 名 / The action name
		/// </summary>
		public string ActionName
		{
			get;
			set;
		} = default!;

        /// <summary>
        /// 当前请求路径 / The current request path
        /// </summary>
        public string CurrentPath
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 是否 Ajax 请求（按请求头判断）/ Whether the request is an Ajax request (detected from headers)
        /// </summary>
        public bool IsAjax
		{
			get;
			set;
		}

		/// <summary>
		/// 全局默认语言资源模块 / The global default language resource module
		/// </summary>
		public LocalizationModule GlobalLanguage
		{
			get;
			set;
		} = default!;

		/// <summary>
		/// 当前请求的区域文化（未解析时为 null）/ The culture of the current request (null when unresolved)
		/// </summary>
		public CultureInfo? CurrentCulture
		{
			get;
			set;
		}

		/// <summary>
		/// 客户端时区（请求头未提供时为 null）/ The client time zone (null when the request header is absent)
		/// </summary>
		public TimeZoneInfo? ClientTimeZone
		{
			get;
			set;
		}


    }
}
