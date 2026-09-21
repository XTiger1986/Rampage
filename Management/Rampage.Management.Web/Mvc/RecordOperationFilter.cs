using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Rampage.AppLog;
using Rampage.DependencyInjection;
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
    /// Marks a feature whose administrator operations should be written to the operation log
    /// </summary>
    public class RecordOperationFilter : ActionFilterBase
    {

        /// <summary>
        /// 管理员服务，由 [Auto] 属性注入 / The admin service, injected via [Auto]
        /// </summary>
        [Auto]
        protected IAdminService AdminService = default!;
        /// <summary>
        /// 管理员操作日志服务，由 [Auto] 属性注入 / The admin operation log service, injected via [Auto]
        /// </summary>
        [Auto]
        protected IAdminOperationLogService AdminOperationLogService = default!;

        /// <summary>
        /// 当前登录的管理员 / The current logged-in admin
        /// </summary>
        protected AdminDto? CurrentAdmin
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前请求累积的操作日志实体：前置阶段采集请求上下文与入参，后置阶段补充出参并落库
        /// The operation log entity accumulated for the current request: the context and input are captured up front, the output is appended afterwards, then it is persisted
        /// </summary>
        protected AdminOperationLog CurrentOperationLog;

        /// <summary>
        /// 构造函数：初始化操作日志实体并记录创建时间（UTC）
        /// Constructor: initializes the operation log entity and stamps its creation time (UTC)
        /// </summary>
        public RecordOperationFilter() : base()
        {
            CurrentOperationLog = new AdminOperationLog();
            CurrentOperationLog.CreateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 前置处理：登录有效时采集请求上下文（Area/Controller/Action、RawUrl、Referrer、UserAgent、客户端IP、操作名）与入参数据，敏感项掩码、超长截断后暂存到 <see cref="CurrentOperationLog"/>
        /// Pre-processing: when logged in, captures the request context (area/controller/action, raw url, referrer, user agent, client IP, operation name) and the action arguments into <see cref="CurrentOperationLog"/> with sensitive values masked and oversized data truncated
        /// </summary>
        /// <param name="actionContext">Action 执行上下文 / The action executing context</param>
        /// <returns>恒为 true（采集失败仅记日志，不阻断请求）/ Always true (capture failures are logged and never block the request)</returns>
        /// <remarks>Cookie 值整体以 *** 记录；Header、表单与入参 JSON 中命中敏感关键字的值替换为 ***；操作名取 PermissionLoader 收录的功能标题（无则用 Action 名）。</remarks>
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
                    // 参数描述符与实参按位置一一对应时才逐项具名记录，避免错位 / Arguments are recorded by name only when descriptors and values align positionally
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

        /// <summary>
        /// 后置处理（仅调用基类空实现，预留扩展点）/ Post-processing (merely calls the empty base hook; reserved for extension)
        /// </summary>
        /// <param name="context">Action 执行上下文 / The action executing context</param>
        protected override void AfterActionExecuting(ActionExecutingContext context)
        {
            base.AfterActionExecuting(context);
        }

        /// <summary>
        /// 已执行处理：从 ActionResult（Json/Content/View/Object）提取返回数据，掩码截断后写入 <see cref="CurrentOperationLog"/> 并落库
        /// Post-execution processing: extracts the returned data from the ActionResult (Json/Content/View/Object), then persists <see cref="CurrentOperationLog"/> with masked and truncated payload
        /// </summary>
        /// <param name="context">Action 已执行上下文 / The action executed context</param>
        /// <returns>恒为 true（落库失败仅记日志，不阻断请求）/ Always true (persistence failures are logged and never block the request)</returns>
        /// <remarks>未登录（CurrentAdmin 为 null）或结果为 null 时不落库。</remarks>
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

        /// <summary>
        /// 敏感关键字列表（名称命中即掩码）/ Sensitive keywords (a name hit triggers masking)
        /// </summary>
        private static readonly string[] SensitiveKeyWords = { "password", "passwd", "pwd", "secret", "token", "authorization", "cookie" };

        /// <summary>单字段入库截断长度，避免大响应/大表单拖垮内存与日志表 / Per-field truncation length kept in storage so large responses/forms cannot bloat memory or the log table</summary>
        private const int MaxLogDataLength = 8 * 1024;

        /// <summary>
        /// 超长数据截断到入库上限并附截断标记 / Truncates oversized data to the storage cap and appends a truncation marker
        /// </summary>
        /// <param name="data">原始数据 / The raw data</param>
        /// <returns>截断后的数据 / The truncated data</returns>
        private static string TruncateLogData(string data)
        {
            if (data.Length > MaxLogDataLength)
            {
                return string.Concat(data.AsSpan(0, MaxLogDataLength), $"...[truncated, total {data.Length} chars]");
            }
            return data;
        }

        /// <summary>
        /// 判断名称是否命中敏感关键字（忽略大小写）/ Determines whether the name hits a sensitive keyword (case-insensitive)
        /// </summary>
        /// <param name="key">待检查的名称 / The name to check</param>
        /// <returns>命中返回 true / True on a hit</returns>
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
        /// Masks the values of sensitive property names inside a JSON text; returned as-is when parsing fails
        /// </summary>
        /// <param name="json">JSON 文本 / The JSON text</param>
        /// <returns>掩码后的 JSON 文本 / The masked JSON text</returns>
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

        /// <summary>
        /// 递归遍历 JSON 节点，把对象中命中敏感关键字的叶子值替换为 ***
        /// Recursively walks the JSON node, replacing leaf values whose property name hits a sensitive keyword with ***
        /// </summary>
        /// <param name="node">JSON 节点 / The JSON node</param>
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
