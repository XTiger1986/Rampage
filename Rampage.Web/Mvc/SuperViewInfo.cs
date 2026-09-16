using Microsoft.AspNetCore.Mvc.Controllers;
using Rampage.Localization;
using System;
using System.Globalization;

namespace Rampage.Web.Mvc
{
	/// <summary>
	/// 
	/// </summary>
	public class SuperViewInfo
	{

		/// <summary>
		/// 
		/// </summary>
		public ControllerActionDescriptor ActionDescriptor
		{
			get;
			set;
		} = default!;

		/// <summary>
		/// 
		/// </summary>
		public string? AreaName
		{
			get;
			set;
		}

		/// <summary>
		/// 
		/// </summary>
		public string ControllerName
		{
			get;
			set;
		} = default!;

		/// <summary>
		/// 
		/// </summary>
		public string ActionName
		{
			get;
			set;
		} = default!;

        /// <summary>
        /// 
        /// </summary>
        public string CurrentPath
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        public bool IsAjax
		{
			get;
			set;
		}

		/// <summary>
		/// 
		/// </summary>
		public LocalizationModule GlobalLanguage
		{
			get;
			set;
		} = default!;

		/// <summary>
		/// 
		/// </summary>
		public CultureInfo? CurrentCulture
		{
			get;
			set;
		}

		public TimeZoneInfo? ClientTimeZone
		{
			get;
			set;
		}


    }
}
