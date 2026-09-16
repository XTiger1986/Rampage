using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Rampage;
using Rampage.DependencyInjection;
using Rampage.Localization;
using Rampage.Log;
using Rampage.Model;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Web.Mvc
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class SuperMiddleware 
    {

        /// <summary>
        /// 
        /// </summary>
        protected readonly RequestDelegate _next;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        public SuperMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Custom middleware operation, return whether to execute the original request operation
        /// </summary>
        protected virtual Func<HttpContext, bool>? BeforeInvoke
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual HttpContext? HttpContext
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual IScopedServiceLocator? ServiceLocator
        {
            get;
            protected set;
        }

        /// <summary>
        /// 
        /// </summary>
        protected AutoInjecter? autoInjecter;

        /// <summary>
        /// 
        /// </summary>
        protected virtual void AutoInject()
        {
            if (ServiceLocator != null)
            {
                try
                {
                    autoInjecter = new AutoInjecter(ServiceLocator);
                    autoInjecter.Build(this);
                }
                catch (Exception ex)
                {
                    RampageLogger.Error("AutoInject error", ex);
                }
            }
        }

        private LocalizationResource? _defaultLanguage;
        /// <summary>
        /// Global Default Language Resources
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public virtual async Task InvokeAsync(HttpContext httpContext)
        {
            IScopedServiceLocator? locator = httpContext.RequestServices.GetService<IScopedServiceLocator>();
            if (locator == null)
            {
                throw new InvalidOperationException(
                    "CommonMiddleware: IScopedServiceLocator is not registered in RequestServices.");
            }

            using (RampageScope.Use(httpContext.RequestServices, locator))
            {
                try
                {
                    bool isNext = true;
                    if (BeforeInvoke != null)
                    {
                        isNext = BeforeInvoke(httpContext);
                    }
                    if (!httpContext.Response.Headers.ContainsKey(WebGlobals.HEADER_KEY_POWERBY))
                    {
                        httpContext.Response.Headers[WebGlobals.HEADER_KEY_POWERBY] = Globals.FRAMEWORK_NAME;
                    }

                    if (isNext)
                    {
                        await _next(httpContext);
                    }
                }
                catch (Exception ex)
                {
                    await HandleExceptionAsync(httpContext, ex);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected abstract Task HandleExceptionAsync(HttpContext context, Exception exception);

    }
}
