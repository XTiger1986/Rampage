using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Rampage.DependencyInjection;
using Rampage.Log;
using Rampage.Management.BLL;
using Rampage.Management.Core.BLL;
using Rampage.Management.Dto;
using Rampage.Management.Entity;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.Utilities;
using Rampage.Web.Extensions;
using Rampage.Web.Mvc;
using Rampage.Web.Utilities;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Rampage.Management.Mvc
{
    /// <summary>
    /// 标记功能要记录管理员操作日志
    /// The tagging feature should record administrator operation logs
    /// </summary>
    public class RecordOperationFilter : ActionFilterBase
    {

        [Auto]
        protected IAdminService AdminService = default!;
        [Auto]
        protected IAdminOperationLogService AdminOperationLogService = default!;

        /// <summary>
        /// 当前登录的管理员
        /// </summary>
        protected AdminDto? CurrentAdmin
        {
            get;
            private set;
        }

        protected AdminOperationLog CurrentOperationLog;

        /// <summary>
        /// 
        /// </summary>
        public RecordOperationFilter() : base()
        {
            CurrentOperationLog = new AdminOperationLog();
            CurrentOperationLog.CreateTime = DateTime.UtcNow;
        }

        protected override bool BeforeActionExecuting(ActionExecutingContext actionContext)
        {
            try
            {
                R<AdminDto> loginR = AdminService.GetLoginAdmin();
                if (loginR.IsSuccess && loginR.Data != null)
                {
                    CurrentAdmin = loginR.Data;
                    var request = actionContext.HttpContext.Request;
                    var actionDescriptor = (ControllerActionDescriptor)actionContext.ActionDescriptor;
                    bool isAjax = actionContext.HttpContext.Request.IsAjaxRequest();
                    string controllerName = actionDescriptor.ControllerName;
                    string actionName = actionDescriptor.ActionName;
                    string rawUrl = actionContext.HttpContext.Request.RawUrl();
                    var permissionAction = PermissionLoader.GetAction(AreaName, controllerName, actionName);
                    string operationName = string.Empty;
                    if (permissionAction != null)
                    {
                        operationName = permissionAction.GetTitle() ?? permissionAction.ActionName!;
                    }

                    #region 操作日志记录
                    CurrentOperationLog.AreaName = AreaName ?? string.Empty;
                    CurrentOperationLog.ControllerName = ControllerName ?? string.Empty;
                    CurrentOperationLog.ActionName = ActionName ?? string.Empty;
                    CurrentOperationLog.RawUrl = rawUrl;
                    CurrentOperationLog.Referrer = request.UrlReferrer();
                    CurrentOperationLog.UserAgent = request.UserAgent();
                    CurrentOperationLog.AdminId = CurrentAdmin.Id;
                    CurrentOperationLog.OperationIp = WebTool.GetClientIP();
                    CurrentOperationLog.OperationName = operationName;
                    CurrentOperationLog.DataTypeName = string.Empty;
                    if (request.Headers != null && request.Headers.Count > 0)
                    {
                        Dictionary<string, string?> headerData = new Dictionary<string, string?>();
                        foreach (var header in request.Headers)
                        {
                            headerData[header.Key] = IsSensitiveKey(header.Key) ? "***" : header.Value.ToString();
                        }
                        CurrentOperationLog.Headers = TruncateLogData(JsonSerializer.Serialize(headerData));
                    }
                    else
                    {
                        CurrentOperationLog.Headers = string.Empty;
                    }
                    if (request.Cookies != null && request.Cookies.Count > 0)
                    {
                        Dictionary<string, string> cookieData = new Dictionary<string, string>();
                        foreach (var cookie in request.Cookies)
                        {
                            cookieData[cookie.Key] = "***";
                        }
                        CurrentOperationLog.CookieData = TruncateLogData(JsonSerializer.Serialize(cookieData));
                    }
                    else
                    {
                        CurrentOperationLog.CookieData = string.Empty;
                    }
                    if (StringTool.EqualsIgnoreCase(request.Method, "POST") && request.Form != null && request.Form.Count > 0)
                    {
                        Dictionary<string, string?> formData = new Dictionary<string, string?>();
                        foreach (var formItem in request.Form)
                        {
                            formData[formItem.Key] = IsSensitiveKey(formItem.Key) ? "***" : formItem.Value.ToString();
                        }
                        CurrentOperationLog.PostData = TruncateLogData(JsonSerializer.Serialize(formData));
                    }
                    else
                    {
                        CurrentOperationLog.PostData = string.Empty;
                    }
                    IList<ParameterDescriptor> parameterDescriptors = actionDescriptor.Parameters;
                    List<object?>? parameterList = actionContext.ActionArguments.Values != null ? actionContext.ActionArguments.Values.ToList() : null;

                    Dictionary<string, object?> inputData = new Dictionary<string, object?>();
                    if (parameterList != null && parameterDescriptors.Count == parameterList.Count)
                    {
                        for (int p = 0; p < parameterDescriptors.Count; p++)
                        {
                            var parameterDescriptor = parameterDescriptors[p];
                            var parameterObj = parameterList[p];
                            if (!inputData.ContainsKey(parameterDescriptor.Name))
                            {
                                inputData.Add(parameterDescriptor.Name, parameterObj);
                            }
                        }
                    }

                    CurrentOperationLog.DataIn = TruncateLogData(MaskSensitiveJson(JsonSerializer.Serialize(inputData)));
                    #endregion
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }

            return true;
        }

        protected override void AfterActionExecuting(ActionExecutingContext context)
        {
            base.AfterActionExecuting(context);
        }

        protected override bool BeforeActionExecuted(ActionExecutedContext context)
        {
            try
            {
                IActionResult? actionResult = context.Result;
                if (actionResult == null)
                {
                    return true;
                }
                if (CurrentAdmin != null)
                {
                    Type actionResultType = actionResult.GetType();
                    object? data = null;
                    if (actionResultType == typeof(JsonResult))
                    {
                        JsonResult? jr = actionResult as JsonResult;
                        if (jr != null)
                            data = jr.Value;
                    }
                    else if (actionResultType == typeof(ContentResult))
                    {
                        ContentResult? cr = actionResult as ContentResult;
                        if (cr != null)
                            data = cr.Content;
                    }
                    else if (actionResultType == typeof(ViewResult))
                    {
                        ViewResult? vr = actionResult as ViewResult;
                        if (vr != null)
                        {
                            data = vr.Model;
                        }
                    }
                    else if (actionResultType == typeof(ObjectResult))
                    {
                        ObjectResult? vr = actionResult as ObjectResult;
                        if (vr != null)
                        {
                            data = vr.Value;
                        }
                    }

                    if (data != null)
                    {
                        CurrentOperationLog.DataOut = TruncateLogData(MaskSensitiveJson(JsonSerializer.Serialize(data)));
                    }
                    else
                    {
                        CurrentOperationLog.DataOut = string.Empty;
                    }
                    CurrentOperationLog.CreateTime = DateTime.UtcNow;
                    CurrentOperationLog.UpdateTime = DateTime.UtcNow;

                    AdminOperationLogService.AddAdminOperationLog(CurrentOperationLog);
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
            return true;
        }

        private static readonly string[] SensitiveKeyWords = { "password", "passwd", "pwd", "secret", "token", "authorization", "cookie" };

        /// <summary>单字段入库截断长度，避免大响应/大表单拖垮内存与日志表</summary>
        private const int MaxLogDataLength = 8 * 1024;

        private static string TruncateLogData(string data)
        {
            if (data.Length > MaxLogDataLength)
            {
                return string.Concat(data.AsSpan(0, MaxLogDataLength), $"...[truncated, total {data.Length} chars]");
            }
            return data;
        }

        private static bool IsSensitiveKey(string? key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            foreach (string word in SensitiveKeyWords)
            {
                if (key.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 将JSON文本中敏感属性名的值替换为掩码，解析失败时原样返回
        /// </summary>
        private static string MaskSensitiveJson(string json)
        {
            try
            {
                JsonNode? node = JsonNode.Parse(json);
                if (node != null)
                {
                    MaskJsonNode(node);
                    return node.ToJsonString();
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
            return json;
        }

        private static void MaskJsonNode(JsonNode node)
        {
            if (node is JsonObject jsonObject)
            {
                foreach (KeyValuePair<string, JsonNode?> item in jsonObject)
                {
                    if (item.Value is JsonValue or null)
                    {
                        if (IsSensitiveKey(item.Key))
                        {
                            jsonObject[item.Key] = "***";
                        }
                    }
                    else if (item.Value != null)
                    {
                        MaskJsonNode(item.Value);
                    }
                }
            }
            else if (node is JsonArray jsonArray)
            {
                foreach (JsonNode? item in jsonArray)
                {
                    if (item != null)
                    {
                        MaskJsonNode(item);
                    }
                }
            }
        }

    }
}
