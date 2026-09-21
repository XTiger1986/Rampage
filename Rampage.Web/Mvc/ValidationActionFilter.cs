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
    /// <summary>
    /// 数据验证过滤器：对标注了 [Valid] 的 Action 参数执行模型校验，不通过时短路请求 / Data validation filter: validates action parameters marked with [Valid] and short-circuits the request on failure
    /// </summary>
    /// <remarks>
    /// 校验失败时：Ajax 请求返回携带 ValidationResult 的 R JSON；普通请求把 R 放入 ViewBag.OperationResult 交由视图呈现。
    /// On failure: Ajax requests get an R JSON carrying the ValidationResult; normal requests get R in ViewBag.OperationResult for the view to render.
    /// </remarks>
    public class ValidationActionFilter : ActionFilterBase
    {

        /// <summary>
        /// Action 执行前执行数据校验，失败则短路后续执行 / Runs data validation before the action and short-circuits execution on failure
        /// </summary>
        /// <param name="filterContext">Action 执行上下文 / The action executing context</param>
        /// <returns>true 继续执行 / True to continue</returns>
        protected override bool BeforeActionExecuting(ActionExecutingContext filterContext)
        {
            if (!ValidateData(filterContext))
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// 校验 Action 参数：仅处理带 [Valid] 特性且能取到 ParameterInfo 的参数 / Validates action parameters: only those carrying the [Valid] attribute with a resolvable ParameterInfo
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
        /// <returns>true 表示全部通过 / True when everything passes</returns>
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
