using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Rampage.Localization;
using Rampage.Model;
using Rampage.Reflection;
using Rampage.Validation;
using System;
using System.Reflection;

namespace Rampage.Web.Mvc
{
    public class ValidationActionFilter : ActionFilterBase
    {

        protected override bool BeforeActionExecuting(ActionExecutingContext filterContext)
        {
            if (!ValidateData(filterContext))
            {
                return false;
            }
            return true;
        }


        protected virtual bool ValidateData(ActionExecutingContext context)
        {
            ActionDescriptor actionDescriptor = context.ActionDescriptor;
            IList<ParameterDescriptor> parameterDescriptors = actionDescriptor.Parameters;
            List<object?>? parameterList = context.ActionArguments.Values != null ? context.ActionArguments.Values.ToList() : null;
            if (parameterDescriptors != null && parameterDescriptors.Count > 0 && parameterList != null && parameterList.Count == parameterDescriptors.Count)
            {
                for (int p = 0; p < parameterDescriptors.Count; p++)
                {
                    var parameter = parameterList[p];
                    ParameterDescriptor parameterDescriptor = parameterDescriptors[p];
                    ParameterInfo? parameterInfo = EmitTool.GetPropertyValue(parameterDescriptor, "ParameterInfo") as ParameterInfo;
                    if (parameterInfo == null)
                    {
                        continue;
                    }

                    var cas = parameterInfo.CustomAttributes;
                    if (cas == null)
                    {
                        continue;
                    }

                    if (cas.Any(a => a.AttributeType == typeof(ValidAttribute)))
                    {
                        ValidationResult validationResult = Validator.Validate(parameter, LocalizationManager.CultureInfoGetter != null ? LocalizationManager.CultureInfoGetter() : null);
                        if (validationResult.Success == false)
                        {
                            R operationResult = R.Create(ResultCode.DataVerificationFailed);
                            operationResult.ValidationResult = validationResult;
                            if (IsAjax)
                            {
                                context.Result = new JsonResult(operationResult);
                                break;
                            }
                            else
                            {
                                if (context.Controller is Controller controller)
                                {
                                    controller.ViewBag.OperationResult = operationResult;
                                }
                            }
                            return false;
                        }
                    }
                }
            }

            return true;
        }

    }
}
