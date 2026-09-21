using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Rampage;
using Rampage.DependencyInjection;
using Rampage.Localization;
using Rampage.Log;
using Rampage.Reflection;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Web.Mvc
{
    /// <summary>
    /// Action 过滤器基类：实现 <see cref="IFilterFactory"/> 且 <see cref="IsReusable"/>=false，
    /// 使 AutoInject 在每个请求得到全新实例。基类封装作用域初始化、属性注入与前后置钩子。
    /// Action filter base: implements <see cref="IFilterFactory"/> with <see cref="IsReusable"/>=false
    /// so AutoInject gets a fresh instance per request; encapsulates scope init, property injection and before/after hooks.
    /// </summary>
    public abstract class ActionFilterBase : ActionFilterAttribute, IFilterFactory
    {
        /// <summary>
        /// 是否复用过滤器实例（恒为 false，确保每次请求重新创建并注入）/ Whether the filter instance is reused (always false so each request gets a freshly injected instance)
        /// </summary>
        public virtual bool IsReusable => false;

        /// <summary>
        /// 创建过滤器实例（经 EmitTool 织入属性注入逻辑）/ Creates the filter instance (with property injection woven in via EmitTool)
        /// </summary>
        /// <param name="serviceProvider">服务提供者 / The service provider</param>
        /// <returns>新过滤器实例 / The new filter instance</returns>
        public virtual IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return (IFilterMetadata)EmitTool.CreateModel(GetType())!;
        }

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
        /// 当前 Action 的描述符（OnActionExecuting 时填充）/ The current action descriptor (populated in OnActionExecuting)
        /// </summary>
        protected ControllerActionDescriptor ControllerActionDescriptor
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 区域名（无区域为空字符串）/ The area name (empty string when none)
        /// </summary>
        protected string? AreaName
        {
            get;
            set;
        }

        /// <summary>
        /// 控制器名（不含 Area 与命名空间）/ The controller name (without area or namespace)
        /// </summary>
        protected string ControllerName
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// Action 名 / The action name
        /// </summary>
        protected string ActionName
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
            set;
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
        /// OnActionExecuting 前置钩子：返回 false 短路后续执行 / Hook before OnActionExecuting: returning false short-circuits the rest
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
        /// <returns>true 继续执行 / True to continue</returns>
        protected virtual bool BeforeActionExecuting(ActionExecutingContext context)
        {
            return true;
        }

        /// <summary>
        /// OnActionExecuting 后置钩子（无论是否短路都会执行）/ Hook after OnActionExecuting (runs even when short-circuited)
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
        protected virtual void AfterActionExecuting(ActionExecutingContext context)
        {
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
        /// OnActionExecuted 后置钩子（无论是否短路都会执行）/ Hook after OnActionExecuted (runs even when short-circuited)
        /// </summary>
        /// <param name="context">Action 已执行上下文 / The action executed context</param>
        protected virtual void AfterActionExecuted(ActionExecutedContext context)
        {
        }

        /// <summary>
        /// 初始化上下文并执行前置/后置钩子。注：ActionFilterAttribute.OnActionExecuting 在 Controller.OnActionExecuting 之后执行。
        /// Initializes the context and runs the before/after hooks. Note: ActionFilterAttribute.OnActionExecuting runs after Controller.OnActionExecuting.
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
        public sealed override void OnActionExecuting(ActionExecutingContext context)
        {
            Initialize();

            ControllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            IsAjax = context.HttpContext.Request.IsAjaxRequest();
            AreaName = context.RouteData.Values["area"] == null ? "" : context.RouteData.Values["area"]!.ToString();
            if (string.IsNullOrEmpty(AreaName))
            {
                AreaName = context.RouteData.DataTokens["area"] == null ? "" : context.RouteData.DataTokens["area"]!.ToString();
            }
            ControllerName = ControllerActionDescriptor.ControllerName;
            ActionName = ControllerActionDescriptor.ActionName;

            bool isContinue = BeforeActionExecuting(context);

            if (isContinue)
            {
                base.OnActionExecuting(context);
            }

            AfterActionExecuting(context);
		}

        /// <summary>
        /// 执行 OnActionExecuted 的前置/后置钩子 / Runs the before/after hooks around OnActionExecuted
        /// </summary>
        /// <param name="context">Action 已执行上下文 / The action executed context</param>
        public sealed override void OnActionExecuted(ActionExecutedContext context)
        {
            bool isContinue = BeforeActionExecuted(context);
            if (isContinue)
            {
                base.OnActionExecuted(context);
            }

            AfterActionExecuted(context);
        }

    }
}
