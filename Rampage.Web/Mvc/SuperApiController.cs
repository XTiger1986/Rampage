using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Rampage.Caching;
using Rampage.DependencyInjection;
using Rampage;
using Rampage.Localization;
using Rampage.Log;
using Rampage.Utilities;
using Rampage.Web.Caching;
using Rampage.Web.Extensions;
using Rampage.Web.TimeZone;
using Rampage.Web.Utilities;
using System;
using System.Globalization;

namespace Rampage.Web.Mvc
{
	/// <summary>
	/// API 控制器基类：提供作用域解析、属性注入、本地化、客户端时区与请求参数读取等能力 / Base class for API controllers: provides scope resolution, property injection, localization, client time zone and request parameter reading
	/// </summary>
	[ApiController]
	public abstract class SuperApiController : ControllerBase
	{

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
        /// <see cref="DefaultLanguage"/> 的后备字段 / Backing field of <see cref="DefaultLanguage"/>
        /// </summary>
        protected LocalizationResource? _defaultLanguage;
		/// <summary>
		/// 默认语言资源 / The default language resources
		/// </summary>
		protected virtual LocalizationResource DefaultLanguage
		{
			get
			{
				if (_defaultLanguage == null)
				{
					_defaultLanguage = LocalizationManager.GetResource(null);
				}
				return _defaultLanguage!;
			}
		}

        private CultureInfo? _currentCulture;
        /// <summary>
        /// 当前请求的区域文化：经 LocalizationManager.CultureInfoGetter 解析，可显式赋值覆盖 / The culture of the current request: resolved via LocalizationManager.CultureInfoGetter, overridable by assignment
        /// </summary>
        protected CultureInfo? CurrentCulture
        {
            get
            {
                if (_currentCulture == null)
                {
                    _currentCulture = LocalizationManager.CultureInfoGetter != null ? LocalizationManager.CultureInfoGetter() : null;
                }
                return _currentCulture;
            }
            set { _currentCulture = value; }
        }



        /// <summary>
        /// 宿主环境信息，由 [Auto] 注入 / The host environment, injected via [Auto]
        /// </summary>
        [Auto]
        protected IWebHostEnvironment WebHostEnvironment
        {
            get;
            set;
        } = default!;

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

        /// <summary>
        /// 是否 Ajax 请求（按请求头判断）/ Whether the request is an Ajax request (detected from headers)
        /// </summary>
        protected bool IsAjax
        {
            get;
            private set;
        }

        private TimeZoneInfo? _clientTimeZone;
        /// <summary>
        /// 客户端时区：优先取 TimeZoneContext 从请求头解析的结果，解析失败回退服务器本地时区 / Client time zone: resolved by TimeZoneContext from the request header, falling back to the server local time zone
        /// </summary>
        protected virtual TimeZoneInfo ClientTimeZone
        {
            get
            {
                if (_clientTimeZone == null)
                {
                    _clientTimeZone = TimeZoneContext.GetClientTimeZone();
                }
                if (_clientTimeZone == null)
                {
                    _clientTimeZone = TimeZoneInfo.Local;
                }
                return _clientTimeZone;
            }
            set
            {
                _clientTimeZone = value;
            }
        }

        /// <summary>
        /// 构造函数：执行 Initialize / Constructor: runs Initialize
        /// </summary>
        public SuperApiController()
        {
            Initialize();
        }

        /// <summary>
        /// 从当前 Rampage 作用域解析 ServiceProvider/ServiceLocator 并执行属性注入 / Resolves ServiceProvider/ServiceLocator from the current Rampage scope and performs property injection
        /// </summary>
        protected virtual void Initialize()
        {
            RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(GetType().FullName);
            ServiceProvider = handle.ServiceProvider;
            ServiceLocator = handle.Locator;
            AutoInject();
        }

        /// <summary>
        /// 属性注入器（AutoInject 后可用）/ The property injector (available after AutoInject)
        /// </summary>
        protected AutoInjecter? Injecter;

        /// <summary>
        /// 在当前 ServiceLocator 上执行属性注入 / Performs property injection against the current ServiceLocator
        /// </summary>
        protected virtual void AutoInject()
        {
            if (ServiceLocator == null)
            {
                throw new InvalidOperationException($"{GetType().FullName}: ServiceLocator is null before AutoInject.");
            }
            Injecter = new AutoInjecter(ServiceLocator);
            Injecter.Build(this);
        }

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

    }
}
