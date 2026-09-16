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
    /// Action filter base. Implements <see cref="IFilterFactory"/> with
    /// <see cref="IsReusable"/>=false so AutoInject gets a fresh instance per request.
    /// </summary>
    public abstract class ActionFilterBase : ActionFilterAttribute, IFilterFactory
    {
        public virtual bool IsReusable => false;

        public virtual IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return (IFilterMetadata)EmitTool.CreateModel(GetType())!;
        }

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


        /// <summary>
        /// 
        /// </summary>
        protected ControllerActionDescriptor ControllerActionDescriptor
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        protected string? AreaName
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        protected string ControllerName
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        protected string ActionName
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
            set;
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

        protected virtual void Initialize()
        {
            RampageScope.ScopeHandle handle = RampageScope.RequireCurrent(GetType().FullName);
            ServiceProvider = handle.ServiceProvider;
            ServiceLocator = handle.Locator;
            AutoInject();
        }

        protected virtual bool BeforeActionExecuting(ActionExecutingContext context)
        {
            return true;
        }

        protected virtual void AfterActionExecuting(ActionExecutingContext context)
        {
        }

        protected virtual bool BeforeActionExecuted(ActionExecutedContext context)
        {
            return true;
        }

        protected virtual void AfterActionExecuted(ActionExecutedContext context)
        {
        }

        /// <summary>
        /// 注：ActionFilterAttribute.OnActionExecuting在Controller.OnActionExecuting之后执行
        /// </summary>
        /// <param name="context"></param>
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
