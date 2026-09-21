using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Rampage.Caching;
using Rampage.DependencyInjection;
using Rampage.ExceptionExtensions;
using Rampage.Localization;
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
	/// 抽象控制器基类：提供作用域解析、属性注入、主题视图、本地化、缓存与请求参数读取等能力 / Abstract base controller: provides scope resolution, property injection, themed views, localization, caching and request parameter reading
	/// </summary>
	public abstract class SuperController : Controller
    {

        /// <summary>
        /// 主题视图名含路径分隔符时抛出的 SuperException 错误键 / The SuperException error key thrown when a themed view name contains path separators
        /// </summary>
        public const string EX_KEY_Illegal_ThemeViewName = "Illegal_ThemeViewName";

        /// <summary>
        /// 当前作用域服务提供者（Initialize 后可用）/ The scoped service provider (available after Initialize)
        /// </summary>
        protected virtual IServiceProvider? ServiceProvider
        {
            get;
            set;
        }

        /// <summary>
        /// 当前作用域服务定位器（Initialize 后可用）/ The scoped service locator (available after Initialize)
        /// </summary>
        protected virtual IScopedServiceLocator? ServiceLocator
        {
            get;
            set;
        }

        /// <summary>
        /// 服务器本地当前时间 / The server local current time
        /// </summary>
        protected DateTime LocalNow
        {
            get { return DateTime.Now; }
        }

        /// <summary>
        /// UTC 当前时间 / The current UTC time
        /// </summary>
        protected DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }

        /// <summary>
        /// 当前 Action 的描述符（OnActionExecuting 时填充）/ The current action descriptor (populated in OnActionExecuting)
        /// </summary>
        protected ControllerActionDescriptor ControllerActionDescriptor
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 视图上下文信息（OnActionExecuting 时组装并放入 ViewBag.ViewInfo）/ The view context info (assembled in OnActionExecuting and exposed as ViewBag.ViewInfo)
        /// </summary>
        protected SuperViewInfo ViewInfo
        {
            get;
            private set;
        } = default!;

		/// <summary>
		/// 区域名（无区域为空字符串）/ The area name (empty string when none)
		/// </summary>
		protected string? AreaName
		{
			get;
            private set;
		}

		/// <summary>
		/// 控制器名（不含 Area 与命名空间）/ The controller name (without area or namespace)
		/// </summary>
		protected string ControllerName
		{
			get;
            private set;
        } = default!;

        /// <summary>
        /// Action 名 / The action name
        /// </summary>
        protected string ActionName
		{
			get;
            private set;
        } = default!;

        /// <summary>
        /// 是否 Ajax 请求（按请求头判断）/ Whether the request is an Ajax request (detected from headers)
        /// </summary>
        protected bool IsAjax
		{
			get;
			private set;
		}

        /// <summary>
        /// <see cref="ThemeSetter"/> 的后备字段 / Backing field of <see cref="ThemeSetter"/>
        /// </summary>
        protected Func<string?, WebSkin>? _themeSetter = null;
        /// <summary>
        /// 主题解析委托：返回指定视图对应的 WebSkin（null 表示不启用主题，视图名按原样使用）/ Theme resolver: returns the WebSkin for a view name (null disables theming and uses the view name as-is)
        /// </summary>
        protected virtual Func<string?, WebSkin>? ThemeSetter
        {
            get
            {
                return _themeSetter;
            }
        }

        /// <summary>
        /// 客户端时区（OnActionExecuting 时从请求头解析，可能为 null）/ The client time zone (resolved from the request header in OnActionExecuting; may be null)
        /// </summary>
        protected virtual TimeZoneInfo? ClientTimeZone
        {
            get;
            set;
        }

        /// <summary>
        /// 构造函数：启动页面计时并执行 Initialize / Constructor: starts the page stopwatch and runs Initialize
        /// </summary>
        public SuperController()
        {
            Initialize();
        }

        /// <summary>
        /// 从当前 Rampage 作用域解析 ServiceProvider/ServiceLocator 并执行属性注入 / Resolves ServiceProvider/ServiceLocator from the current Rampage scope and performs property injection
        /// </summary>
        protected virtual void Initialize()
        {
            ContextMonitor.Start();
            RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(GetType().FullName);
            ServiceProvider = handle.ServiceProvider;
            ServiceLocator = handle.Locator;
            AutoInject();
        }

        /// <summary>
        /// 属性注入器（AutoInject 后可用）/ The property injector (available after AutoInject)
        /// </summary>
        protected AutoInjecter? autoInjecter;

        /// <summary>
        /// 在当前 ServiceLocator 上执行属性注入 / Performs property injection against the current ServiceLocator
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
        /// 当前请求的区域文化（OnActionExecuting 时经 CultureInfoGetter 解析）/ The culture of the current request (resolved via CultureInfoGetter in OnActionExecuting)
        /// </summary>
        protected virtual CultureInfo? CurrentCulture
        {
            get;
            set;
        }

        private LocalizationModule? _globalLanguage;
        /// <summary>
        /// 全局默认语言资源模块 / The global default language resource module
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
        /// 请求级缓存（基于 HttpContext.Items），由 [Auto] 注入 / Per-request cache (backed by HttpContext.Items), injected via [Auto]
        /// </summary>
        [Auto]
        protected IContextCache ContextCache
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 进程级内存缓存，由 [Singleton] 注入 / The in-process memory cache, injected via [Singleton]
        /// </summary>
        [Singleton]
        protected IStandAloneCache MemoryCache
        {
            get;
            set;
        } = default!;

        #region QueryString.Get | Form.PostData

        /// <summary>
        /// 用 HttpContext.Request 的 Query/Form 数据填充模型属性 / Renders the model's properties from HttpContext.Request Query|Form data
        /// </summary>
        /// <typeparam name="T">模型类型 / The model type</typeparam>
        /// <param name="model">待填充的模型；null 时新建 / The model to render; a new instance when null</param>
        /// <param name="method">数据来源（默认 Query）/ The data source (Query by default)</param>
        /// <returns>填充后的模型 / The rendered model</returns>
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
		/// 新建模型并用 HttpContext.Request 的 Query/Form 数据填充其属性 / Creates a new model and renders its properties from HttpContext.Request Query|Form data
		/// </summary>
		/// <typeparam name="T">模型类型 / The model type</typeparam>
		/// <param name="method">数据来源（默认 Query）/ The data source (Query by default)</param>
		/// <returns>填充后的模型 / The rendered model</returns>
		[NonAction]
        protected T RenderModel<T>(HttpRequestMethod method = HttpRequestMethod.Get) where T : class, new()
        {
            T model = new T();
            return WebTool.RenderRequestModel<T>(model, method);
        }

		/// <summary>
		/// 读取 HttpContext.Request.Form 中指定名称的字符串值（缺省返回空串）/ Gets the string value of the name from HttpContext.Request.Form (empty string by default)
		/// </summary>
		/// <param name="name">参数名 / The parameter name</param>
		/// <returns>参数值（已 Trim）/ The value (trimmed)</returns>
		[NonAction]
        protected string Post(string name)
        {
            return Get(name, HttpRequestMethod.Post, string.Empty);
        }
		/// <summary>
		/// 读取 HttpContext.Request.Form 中指定名称的字符串值 / Gets the string value of the name from HttpContext.Request.Form
		/// </summary>
		/// <param name="name">参数名 / The parameter name</param>
		/// <param name="defaultValue">缺省值 / The default value</param>
		/// <returns>参数值（已 Trim）/ The value (trimmed)</returns>
		[NonAction]
        protected string Post(string name, string defaultValue)
        {
            return Get(name, HttpRequestMethod.Post, defaultValue);
        }
		/// <summary>
		/// 读取 HttpContext.Request.Query 中指定名称的字符串值（缺省返回空串）/ Gets the string value of the name from HttpContext.Request.Query (empty string by default)
		/// </summary>
		/// <param name="name">参数名 / The parameter name</param>
		/// <returns>参数值（已 Trim）/ The value (trimmed)</returns>
		[NonAction]
        protected string Get(string name)
        {
            return Get(name, HttpRequestMethod.Get, string.Empty);
        }
		/// <summary>
		/// 读取 HttpContext.Request.Query 中指定名称的字符串值 / Gets the string value of the name from HttpContext.Request.Query
		/// </summary>
		/// <param name="name">参数名 / The parameter name</param>
		/// <param name="defaultValue">缺省值 / The default value</param>
		/// <returns>参数值（已 Trim）/ The value (trimmed)</returns>
		[NonAction]
        protected string Get(string name, string defaultValue)
        {
            return Get(name, HttpRequestMethod.Get, defaultValue);
        }
		/// <summary>
		/// 按指定来源读取 HttpContext.Request Query|Form 中指定名称的字符串值（缺省返回空串）/ Gets the string value of the name from HttpContext.Request Query|Form by the given source (empty string by default)
		/// </summary>
		/// <param name="name">参数名 / The parameter name</param>
		/// <param name="method">数据来源 / The data source</param>
		/// <returns>参数值（已 Trim）/ The value (trimmed)</returns>
		[NonAction]
        protected string Get(string name, HttpRequestMethod method)
        {
            return Get(name, method, string.Empty);
        }
		/// <summary>
		/// 按指定来源读取 HttpContext.Request Query|Form 中指定名称的字符串值：值经 Trim，空值返回缺省值 / Gets the string value of the name from HttpContext.Request Query|Form by the given source: the value is trimmed and an empty value yields the default
		/// </summary>
		/// <param name="name">参数名 / The parameter name</param>
		/// <param name="method">数据来源；All 时先 Query 后 Form / The data source; All tries Query then Form</param>
		/// <param name="defaultValue">缺省值 / The default value</param>
		/// <returns>参数值（已 Trim）/ The value (trimmed)</returns>
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
        /// 读取 Form 中指定名称的值并转换为目标类型（缺省返回类型默认值）/ Reads the value of the name from Form converted to the target type (the type's default when absent)
        /// </summary>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <param name="name">参数名 / The parameter name</param>
        /// <returns>转换后的值 / The converted value</returns>
        [NonAction]
        protected T? Post<T>(string name)
        {
            return Get<T>(name, HttpRequestMethod.Post, default(T));
        }
        /// <summary>
        /// 读取 Form 中指定名称的值并转换为目标类型 / Reads the value of the name from Form converted to the target type
        /// </summary>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <param name="name">参数名 / The parameter name</param>
        /// <param name="defaultValue">缺省值 / The default value</param>
        /// <returns>转换后的值 / The converted value</returns>
        [NonAction]
        protected T? Post<T>(string name, T defaultValue)
        {
            return Get<T>(name, HttpRequestMethod.Post, defaultValue);
        }
        /// <summary>
        /// 读取 Query 中指定名称的值并转换为目标类型（缺省返回类型默认值）/ Reads the value of the name from Query converted to the target type (the type's default when absent)
        /// </summary>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <param name="name">参数名 / The parameter name</param>
        /// <returns>转换后的值 / The converted value</returns>
        [NonAction]
        protected T? Get<T>(string name)
        {
            return Get<T>(name, HttpRequestMethod.Get, default(T));
        }
        /// <summary>
        /// 读取 Query 中指定名称的值并转换为目标类型 / Reads the value of the name from Query converted to the target type
        /// </summary>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <param name="name">参数名 / The parameter name</param>
        /// <param name="defaultValue">缺省值 / The default value</param>
        /// <returns>转换后的值 / The converted value</returns>
        [NonAction]
        protected T? Get<T>(string name, T defaultValue)
        {
            return Get<T>(name, HttpRequestMethod.Get, defaultValue);
        }
        /// <summary>
        /// 按指定来源读取值并转换为目标类型（缺省返回类型默认值）/ Reads the value by the given source converted to the target type (the type's default when absent)
        /// </summary>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <param name="name">参数名 / The parameter name</param>
        /// <param name="method">数据来源 / The data source</param>
        /// <returns>转换后的值 / The converted value</returns>
        [NonAction]
        protected T? Get<T>(string name, HttpRequestMethod method)
        {
            return Get<T>(name, method, default(T));
        }
        /// <summary>
        /// 按指定来源读取值并转换为目标类型；空值或转换失败返回缺省值 / Reads the value by the given source converted to the target type; empty or unconvertible values yield the default
        /// </summary>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <param name="name">参数名 / The parameter name</param>
        /// <param name="method">数据来源；All 时先 Query 后 Form / The data source; All tries Query then Form</param>
        /// <param name="defaultValue">缺省值 / The default value</param>
        /// <returns>转换后的值 / The converted value</returns>
        [NonAction]
        protected T? Get<T>(string name, HttpRequestMethod method, T? defaultValue)
        {
            string sValue = Get(name, method, string.Empty);

            T? result = StringTool.TryParse<T>(sValue, defaultValue);

            return result;
        }

        /// <summary>
        /// 读取 Query 中以逗号或空格分隔的值并逐项转换为目标类型 / Reads the comma/space separated value from Query and converts each item to the target type
        /// </summary>
        /// <typeparam name="T">元素类型 / The element type</typeparam>
        /// <param name="name">参数名 / The parameter name</param>
        /// <returns>转换后的列表（空值返回空列表）/ The converted list (empty when the value is absent)</returns>
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
        /// 读取 Form 中以逗号或空格分隔的值并逐项转换为目标类型 / Reads the comma/space separated value from Form and converts each item to the target type
        /// </summary>
        /// <typeparam name="T">元素类型 / The element type</typeparam>
        /// <param name="name">参数名 / The parameter name</param>
        /// <returns>转换后的列表（空值返回空列表）/ The converted list (empty when the value is absent)</returns>
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
        /// 返回视图（应用主题）：ThemeSetter 为 null 时等同于 View(viewName) / Returns a view (theme-aware): equals View(viewName) when ThemeSetter is null
        /// </summary>
        /// <param name="viewName">视图名；null 使用 Action 名 / The view name; null uses the action name</param>
        /// <returns>视图结果 / The view result</returns>
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
        /// 返回携带数据的视图（应用主题，视图名取 Action 名）/ Returns a view with data (theme-aware; the view name defaults to the action name)
        /// </summary>
        /// <param name="data">视图数据 / The view data</param>
        /// <returns>视图结果 / The view result</returns>
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
        /// 返回指定视图名并携带数据的视图（应用主题）/ Returns the named view with data (theme-aware)
        /// </summary>
        /// <param name="viewName">视图名；null 使用 Action 名 / The view name; null uses the action name</param>
        /// <param name="data">视图数据 / The view data</param>
        /// <returns>视图结果 / The view result</returns>
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
        /// 创建成功（Code=0）的 JsonResult / Creates a success (Code=0) JsonResult
        /// </summary>
        /// <param name="data">业务数据 / The payload data</param>
        /// <returns>JSON 结果 / The JSON result</returns>
        [NonAction]
        protected IActionResult Success(object? data)
        {
            return JR(ResultCode.Success, null, data);
        }

		/// <summary>
		/// 创建成功（Code=0）的 JsonResult / Creates a success (Code=0) JsonResult
		/// </summary>
		/// <param name="message">消息文本 / The message</param>
		/// <param name="data">业务数据 / The payload data</param>
		/// <returns>JSON 结果 / The JSON result</returns>
		[NonAction]
        protected IActionResult Success(string? message = null, object? data = null)
        {
            return JR(ResultCode.Success, message, data);
        }

		/// <summary>
		/// 创建失败（Code=10000）的 JsonResult / Creates a failure (Code=10000) JsonResult
		/// </summary>
		/// <param name="message">消息文本 / The message</param>
		/// <param name="data">业务数据 / The payload data</param>
		/// <returns>JSON 结果 / The JSON result</returns>
		[NonAction]
        protected IActionResult Failure(string? message = null, object? data = null)
        {
            return JR(ResultCode.Failure, message, data);
        }

		/// <summary>
		/// 创建指定错误码的失败 JsonResult / Creates a failure JsonResult with the given code
		/// </summary>
		/// <param name="code">错误码 / The result code</param>
		/// <param name="message">消息文本 / The message</param>
		/// <param name="data">业务数据 / The payload data</param>
		/// <returns>JSON 结果 / The JSON result</returns>
		[NonAction]
        protected IActionResult Failure(int code, string? message = null, object? data = null)
        {
            return JR(code, message, data);
        }

		/// <summary>
		/// 创建指定枚举错误码的失败 JsonResult / Creates a failure JsonResult with the given enum code
		/// </summary>
		/// <typeparam name="E">错误码枚举类型 / The result code enum type</typeparam>
		/// <param name="code">错误码枚举值 / The result code value</param>
		/// <param name="message">消息文本 / The message</param>
		/// <param name="data">业务数据 / The payload data</param>
		/// <returns>JSON 结果 / The JSON result</returns>
		[NonAction]
        protected IActionResult Failure<E>(E code, string? message = null, object? data = null) where E : Enum
        {
            return JR(code, message, data);
        }

		/// <summary>
		/// 创建 R 格式的 JsonResult（Code/Message/Data）/ Creates an R-shaped JsonResult (Code/Message/Data)
		/// </summary>
		/// <param name="code">结果码 / The result code</param>
		/// <param name="message">消息文本；null 转为空串 / The message; null becomes an empty string</param>
		/// <param name="data">业务数据 / The payload data</param>
		/// <returns>JSON 结果 / The JSON result</returns>
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
		/// 创建 R 的 JsonResult；R 携带 JsonpId 时输出 JSONP（"id(json)" 文本）/ Creates a JsonResult for R; a JsonpId on R yields JSONP ("id(json)" content) instead
		/// </summary>
		/// <param name="result">统一返回对象 / The unified result object</param>
		/// <returns>JSON 或 JSONP 结果 / The JSON or JSONP result</returns>
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
		/// 创建指定枚举结果码的 R 格式 JsonResult / Creates an R-shaped JsonResult with the given enum result code
		/// </summary>
		/// <typeparam name="E">结果码枚举类型 / The result code enum type</typeparam>
		/// <param name="code">结果码枚举值 / The result code value</param>
		/// <param name="message">消息文本 / The message</param>
		/// <param name="data">业务数据 / The payload data</param>
		/// <returns>JSON 结果 / The JSON result</returns>
		[NonAction]
        protected IActionResult JR<E>(E code, string? message, object? data) where E : Enum
        {
            return JR(Convert.ToInt32(code), message, data);
        }

        #endregion

        /// <summary>
        /// OnActionExecuting 前置钩子：返回 false 短路后续执行 / Hook before OnActionExecuting: returning false short-circuits the rest
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
        /// <returns>true 继续执行 / True to continue</returns>
        protected virtual bool BeforeActionExecuting(ActionExecutingContext context)
        {
            return true;
        }

        /// <summary>
        /// 填充 ControllerActionDescriptor/AreaName/ControllerName/ActionName/IsAjax/ViewInfo/ClientTimeZone/CurrentCulture
        /// 并写入 ViewBag，随后执行 BeforeActionExecuting 钩子。
        /// Populates ControllerActionDescriptor/AreaName/ControllerName/ActionName/IsAjax/ViewInfo/ClientTimeZone/CurrentCulture,
        /// publishes them to ViewBag, then runs the BeforeActionExecuting hook.
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
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

        /// <summary>
        /// OnActionExecuted 前置钩子：返回 false 短路后续执行 / Hook before OnActionExecuted: returning false short-circuits the rest
        /// </summary>
        /// <param name="context">Action 已执行上下文 / The action executed context</param>
        /// <returns>true 继续执行 / True to continue</returns>
        protected virtual bool BeforeActionExecuted(ActionExecutedContext context)
        {
            return true;
        }

        /// <summary>
        /// 执行 BeforeActionExecuted 钩子后调用基类实现 / Runs the BeforeActionExecuted hook and then the base implementation
        /// </summary>
        /// <param name="context">Action 已执行上下文 / The action executed context</param>
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
