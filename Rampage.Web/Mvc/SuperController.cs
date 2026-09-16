using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Rampage;
using Rampage.Caching;
using Rampage.DependencyInjection;
using Rampage.ExceptionExtensions;
using Rampage.Localization;
using Rampage.Log;
using Rampage.Model;
using Rampage.Utilities;
using Rampage.Web.Caching;
using Rampage.Web.Extensions;
using Rampage.Web.Theme;
using Rampage.Web.TimeZone;
using Rampage.Web.Utilities;
using System;
using System.Globalization;
using System.Text.Json;

namespace Rampage.Web.Mvc
{
	/// <summary>
	/// abstract Base Controller
	/// </summary>
	public abstract class SuperController : Controller
    {

        public const string EX_KEY_Illegal_ThemeViewName = "Illegal_ThemeViewName";

        protected virtual IServiceProvider? ServiceProvider
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual IScopedServiceLocator? ServiceLocator
        {
            get;
            set;
        }

        protected DateTime LocalNow
        {
            get { return DateTime.Now; }
        }

        protected DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }

        /// <summary>
        /// 
        /// </summary>
        protected ControllerActionDescriptor ControllerActionDescriptor
        {
            get;
            private set;
        } = default!;

        protected SuperViewInfo ViewInfo
        {
            get;
            private set;
        } = default!;

		/// <summary>
		/// 
		/// </summary>
		protected string? AreaName
		{
			get;
            private set;
		}

		/// <summary>
		/// 
		/// </summary>
		protected string ControllerName
		{
			get;
            private set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        protected string ActionName
		{
			get;
            private set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        protected bool IsAjax
		{
			get;
			private set;
		}

        protected Func<string?, WebSkin>? _themeSetter = null;
        protected virtual Func<string?, WebSkin>? ThemeSetter
        {
            get
            {
                return _themeSetter;
            }
        }

        protected virtual TimeZoneInfo? ClientTimeZone
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public SuperController()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            ContextMonitor.Start();
            RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(GetType().FullName);
            ServiceProvider = handle.ServiceProvider;
            ServiceLocator = handle.Locator;
            AutoInject();
        }

        /// <summary>
        /// 
        /// </summary>
        protected AutoInjecter? autoInjecter;

        /// <summary>
        /// 
        /// </summary>
        [NonAction]
        protected virtual void AutoInject()
        {
            if (ServiceLocator == null)
            {
                throw new InvalidOperationException($"{GetType().FullName}: ServiceLocator is null before AutoInject.");
            }
            autoInjecter = new AutoInjecter(ServiceLocator);
            autoInjecter.Build(this);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual CultureInfo? CurrentCulture
        {
            get;
            set;
        }

        private LocalizationModule? _globalLanguage;
        /// <summary>
        /// Global Default Language Resources
        /// </summary>
		protected virtual LocalizationModule GlobalLanguage
		{
			get
			{
				if (_globalLanguage == null)
				{
                    _globalLanguage = LocalizationManager.GetModule(LocalizationManager.Config.DefaultModuleName);
				}
				return _globalLanguage!;
			}
		}

        /// <summary>
        /// 
        /// </summary>
        [Auto]
        protected IContextCache ContextCache
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        [Singleton]
        protected IStandAloneCache MemoryCache
        {
            get;
            set;
        } = default!;

        #region QueryString.Get | Form.PostData

        /// <summary>
        /// render model data form HttpContext.Request Query|Form
        /// </summary>
        /// <param name="model">model to render</param>
        /// <returns></returns>
        [NonAction]
		protected T RenderModel<T>(T? model, HttpRequestMethod method = HttpRequestMethod.Get) where T : class, new()
		{
			if (model == null)
            {
                model = new T();
            }
			return WebTool.RenderRequestModel<T>(model, method);
		}

		/// <summary>
		/// render new model data form HttpContext.Request Query|Form
		/// </summary>
		/// <returns></returns>
		[NonAction]
        protected T RenderModel<T>(HttpRequestMethod method = HttpRequestMethod.Get) where T : class, new()
        {
            T model = new T();
            return WebTool.RenderRequestModel<T>(model, method);
        }

		/// <summary>
		/// get string value of name in HttpContext.Request.Form
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[NonAction]
        protected string Post(string name)
        {
            return Get(name, HttpRequestMethod.Post, string.Empty);
        }
		/// <summary>
		/// get string value of name in HttpContext.Request.Form
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		[NonAction]
        protected string Post(string name, string defaultValue)
        {
            return Get(name, HttpRequestMethod.Post, defaultValue);
        }
		/// <summary>
		/// get string value of name in HttpContext.Request.Query
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[NonAction]
        protected string Get(string name)
        {
            return Get(name, HttpRequestMethod.Get, string.Empty);
        }
		/// <summary>
		/// get string value of name in HttpContext.Request.Query
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		[NonAction]
        protected string Get(string name, string defaultValue)
        {
            return Get(name, HttpRequestMethod.Get, defaultValue);
        }
		/// <summary>
		/// get string value of name in HttpContext.Request.Query|Form
		/// </summary>
		/// <param name="name"></param>
		/// <param name="method"></param>
		/// <returns></returns>
		[NonAction]
        protected string Get(string name, HttpRequestMethod method)
        {
            return Get(name, method, string.Empty);
        }
		/// <summary>
		/// get string value of name in HttpContext.Request.Query|Form
		/// </summary>
		/// <param name="name"></param>
		/// <param name="method"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		[NonAction]
        protected string Get(string name, HttpRequestMethod method, string defaultValue)
        {
            string? sValue;
            switch (method)
            {
                case HttpRequestMethod.Get:
                    sValue = Request.Query[name];
                    break;
                case HttpRequestMethod.Post:
                    sValue = Request.GetForm(name);
                    break;
                case HttpRequestMethod.All:
                default:
                    if (Request.Query.ContainsKey(name))
                        sValue = Request.Query[name];
                    else if (Request.Form.ContainsKey(name))
                        sValue = Request.GetForm(name);
                    else
                        sValue = null;
                    break;
            }
            if (string.IsNullOrEmpty(sValue))
            {
                return defaultValue;
            }
            sValue = sValue.Trim();
            return sValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        [NonAction]
        protected T? Post<T>(string name)
        {
            return Get<T>(name, HttpRequestMethod.Post, default(T));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [NonAction]
        protected T? Post<T>(string name, T defaultValue)
        {
            return Get<T>(name, HttpRequestMethod.Post, defaultValue);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        [NonAction]
        protected T? Get<T>(string name)
        {
            return Get<T>(name, HttpRequestMethod.Get, default(T));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [NonAction]
        protected T? Get<T>(string name, T defaultValue)
        {
            return Get<T>(name, HttpRequestMethod.Get, defaultValue);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        [NonAction]
        protected T? Get<T>(string name, HttpRequestMethod method)
        {
            return Get<T>(name, method, default(T));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [NonAction]
        protected T? Get<T>(string name, HttpRequestMethod method, T? defaultValue)
        {
            string sValue = Get(name, method, string.Empty);

            T? result = StringTool.TryParse<T>(sValue, defaultValue);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        [NonAction]
        protected IEnumerable<T> GetArray<T>(string name)
        {
            string s = Get(name, HttpRequestMethod.Get, string.Empty);
            if (string.IsNullOrEmpty(s))
            {
                return new List<T>();
            }
            string[] arr = s.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<T> result = arr.Select(a => StringTool.TryParse<T>(a)!);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        [NonAction]
        protected IEnumerable<T> PostArray<T>(string name)
        {
            string s = Get(name, HttpRequestMethod.Post, string.Empty);
            if (string.IsNullOrEmpty(s))
            {
                return new List<T>();
            }
            string[] arr = s.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<T> result = arr.Select(a => StringTool.TryParse<T>(a)!);
            return result;
        }

        #endregion

        #region View

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        [NonAction]
        protected IActionResult VW(string? viewName)
        {
            string? finalViewName = GetThemeViewName(viewName);
            if (string.IsNullOrEmpty(finalViewName))
            {
                return View();
            }
            else
            {
                return View(finalViewName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [NonAction]
        protected IActionResult VW(object? data = null)
        {
            string? finalViewName = GetThemeViewName(null);
            if (string.IsNullOrEmpty(finalViewName))
            {
                return View(data);
            }
            else
            {
                return View(finalViewName, data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [NonAction]
        protected IActionResult VW(string? viewName, object? data)
        {
            string? finalViewName = GetThemeViewName(viewName);
            if (string.IsNullOrEmpty(finalViewName))
            {
                return View(data);
            }
            else
            {
                return View(finalViewName, data);
            }
        }

        [NonAction]
        private string? GetThemeViewName(string? viewName)
        {
            string? finalViewName;
            if (ThemeSetter != null)
            {
                if (!string.IsNullOrEmpty(viewName) && (viewName.Contains("/") || viewName.Contains("\\")))
                {
                    throw new SuperException(EX_KEY_Illegal_ThemeViewName, viewName);
                }
                WebSkin skin = ThemeSetter(viewName);
                ViewBag.CurrentTheme = skin;
                string skinCode = skin.Code!;
                finalViewName = $"/Themes/{skinCode}/{ControllerName}/{viewName ?? ActionName}.cshtml";
            }
            else
            {
                finalViewName = viewName;
            }
            return finalViewName;
        }

        #endregion

        #region JsonResult

        /// <summary>
        /// create JsonResult of Success（Code=0）
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [NonAction]
        protected IActionResult Success(object? data)
        {
            return JR(ResultCode.Success, null, data);
        }

		/// <summary>
		/// create JsonResult of Success（Code=0）
		/// </summary>
		/// <param name="data"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		[NonAction]
        protected IActionResult Success(string? message = null, object? data = null)
        {
            return JR(ResultCode.Success, message, data);
        }

		/// <summary>
		/// create JsonResult of Failure（Code=10000）
		/// </summary>
		/// <param name="data"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		[NonAction]
        protected IActionResult Failure(string? message = null, object? data = null)
        {
            return JR(ResultCode.Failure, message, data);
        }

		/// <summary>
		/// create JsonResult of Failure
		/// </summary>
		/// <param name="code"></param>
		/// <param name="data"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		[NonAction]
        protected IActionResult Failure(int code, string? message = null, object? data = null)
        {
            return JR(code, message, data);
        }

		/// <summary>
		/// create JsonResult of Failure
		/// </summary>
		/// <param name="code"></param>
		/// <param name="data"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		[NonAction]
        protected IActionResult Failure<E>(E code, string? message = null, object? data = null) where E : Enum
        {
            return JR(code, message, data);
        }

		/// <summary>
		/// create JsonResult
		/// </summary>
		/// <param name="code"></param>
		/// <param name="message"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[NonAction]
        protected IActionResult JR(int code, string? message, object? data)
        {
            if (message == null)
            {
                message = string.Empty;
            }
            R opResult = new R();
            opResult.Code = code;
            opResult.Message = message;
            opResult.Data = data;

            IActionResult jsonResult = JR(opResult);
            return jsonResult;
        }

		/// <summary>
		/// create JsonResult
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		[NonAction]
        protected IActionResult JR(R result)
        {
            if (!string.IsNullOrEmpty(result.JsonpId))
            {
                var jsonStr = JsonSerializer.Serialize(result);
                var res = result.JsonpId + "(" + jsonStr + ")";
                return Content(res);

            }
            else
            {
                JsonResult jsonResult = base.Json(result);
                return jsonResult;
            }
        }

		/// <summary>
		/// create JsonResult
		/// </summary>
		/// <param name="code"></param>
		/// <param name="message"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[NonAction]
        protected IActionResult JR<E>(E code, string? message, object? data) where E : Enum
        {
            return JR(Convert.ToInt32(code), message, data);
        }

        #endregion

        protected virtual bool BeforeActionExecuting(ActionExecutingContext context)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public sealed override void OnActionExecuting(ActionExecutingContext context)
        {
            ControllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            IsAjax = context.HttpContext.Request.IsAjaxRequest();
            AreaName = context.RouteData.Values["area"] == null ? string.Empty : context.RouteData.Values["area"]!.ToString();
            if (string.IsNullOrEmpty(AreaName))
            {
                AreaName = context.RouteData.DataTokens["area"] == null ? string.Empty : context.RouteData.DataTokens["area"]!.ToString();
            }
            ControllerName = ControllerActionDescriptor.ControllerName;
            ActionName = ControllerActionDescriptor.ActionName;
            string path = context.HttpContext.Request.Path;
            ViewBag.ControllerActionDescriptor = ControllerActionDescriptor;
            ViewBag.IsAjax = IsAjax;
            ViewBag.AreaName = AreaName;
            ViewBag.ControllerName = ControllerName;
            ViewBag.ActionName = ActionName;

            ClientTimeZone = TimeZoneContext.GetClientTimeZone();

            CurrentCulture = LocalizationManager.CultureInfoGetter != null ? LocalizationManager.CultureInfoGetter() : null;
            ViewInfo = new SuperViewInfo
            {
                ActionDescriptor = ControllerActionDescriptor,
                AreaName = AreaName,
                ControllerName = ControllerName,
                ActionName = ActionName,
                CurrentPath = path,
                IsAjax = IsAjax,
                GlobalLanguage = GlobalLanguage,
                CurrentCulture = CurrentCulture,
                ClientTimeZone = ClientTimeZone
            };
            ViewBag.ViewInfo = ViewInfo;

            bool isContinue = BeforeActionExecuting(context);
            if (!isContinue)
            {
                return;
            }
            
            base.OnActionExecuting(context);
        }

        protected virtual bool BeforeActionExecuted(ActionExecutedContext context)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public sealed override void OnActionExecuted(ActionExecutedContext context)
        {
            bool isContinue = BeforeActionExecuted(context);
            if (!isContinue)
            {
                return;
            }
            base.OnActionExecuted(context);
        }
    }
}
