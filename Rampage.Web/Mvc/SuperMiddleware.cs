using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Rampage.AppLog;
using Rampage.DependencyInjection;
using Rampage.Localization;
using System;

namespace Rampage.Web.Mvc
{
    /// <summary>
    /// 自定义中间件基类：在请求作用域内执行属性注入，并把异常交给 HandleExceptionAsync 统一处理 / Base class for custom middleware: performs property injection within the request scope and funnels exceptions into HandleExceptionAsync
    /// </summary>
    public abstract class SuperMiddleware 
    {

        /// <summary>
        /// 管道中的下一个委托 / The next delegate in the pipeline
        /// </summary>
        protected readonly RequestDelegate _next;

        /// <summary>
        /// 构造函数 / Constructor
        /// </summary>
        /// <param name="next">管道中的下一个委托 / The next delegate in the pipeline</param>
        public SuperMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// 自定义中间件操作：返回是否继续执行原始请求；null（默认）表示继续 / Custom middleware operation: return whether to continue the original request; null (default) continues
        /// </summary>
        protected virtual Func<HttpContext, bool>? BeforeInvoke
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// 当前请求的 HttpContext（InvokeAsync 执行期间有效）/ The HttpContext of the current request (valid during InvokeAsync)
        /// </summary>
        protected virtual HttpContext? HttpContext
        {
            get;
            private set;
        }

        /// <summary>
        /// 请求作用域服务定位器（请求级依赖由此解析）/ The request-scoped service locator (resolves request-scoped dependencies)
        /// </summary>
        public virtual IScopedServiceLocator? ServiceLocator
        {
            get;
            protected set;
        }

        /// <summary>
        /// 属性注入器（AutoInject 后可用）/ The property injector (available after AutoInject)
        /// </summary>
        protected AutoInjecter? autoInjecter;

        /// <summary>
        /// 在当前 ServiceLocator 上执行属性注入；失败仅记录日志不中断请求 / Performs property injection against the current ServiceLocator; failures are logged without breaking the request
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
        /// 全局默认语言资源 / Global default language resources
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
        /// 处理请求：建立 Rampage 请求作用域 → BeforeInvoke 短路判断 → 补写 powered-by 响应头 → 调用后续管道；异常统一交给 <see cref="HandleExceptionAsync"/> / Handles the request: opens a Rampage request scope → BeforeInvoke short-circuit → writes the powered-by response header → invokes the rest of the pipeline; exceptions go to <see cref="HandleExceptionAsync"/>
        /// </summary>
        /// <param name="httpContext">当前 HTTP 上下文 / The current HTTP context</param>
        /// <returns>异步任务 / A task representing the operation</returns>
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
        /// 处理管道中未捕获的异常（由派生类实现响应策略） / Handles exceptions uncaught by the pipeline (derived classes implement the response strategy)
        /// </summary>
        /// <param name="context">当前 HTTP 上下文 / The current HTTP context</param>
        /// <param name="exception">捕获的异常 / The caught exception</param>
        /// <returns>异步任务 / A task representing the operation</returns>
        protected abstract Task HandleExceptionAsync(HttpContext context, Exception exception);

    }
}
