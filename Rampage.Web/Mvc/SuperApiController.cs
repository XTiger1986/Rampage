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
	[ApiController]
	public abstract class SuperApiController : ControllerBase
	{

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


        protected LocalizationResource? _defaultLanguage;
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
        /// 
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



        [Auto]
        protected IWebHostEnvironment WebHostEnvironment
        {
            get;
            set;
        } = default!;

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

        /// <summary>
        /// 
        /// </summary>
        protected bool IsAjax
        {
            get;
            private set;
        }

        private TimeZoneInfo? _clientTimeZone;
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
        /// 
        /// </summary>
        public SuperApiController()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(GetType().FullName);
            ServiceProvider = handle.ServiceProvider;
            ServiceLocator = handle.Locator;
            AutoInject();
        }

        /// <summary>
        /// 
        /// </summary>
        protected AutoInjecter? Injecter;

        /// <summary>
        /// 
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

    }
}
