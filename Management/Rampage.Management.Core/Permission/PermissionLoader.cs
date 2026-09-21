using Microsoft.AspNetCore.Mvc;
using Rampage.Localization;
using Rampage.Log;
using Rampage.Management.Configuration;
using Rampage.Model;
using Rampage.Reflection;
using Rampage.Utilities;
using Rampage.Web.Mvc;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Rampage.Management.Permission
{
    /// <summary>
    /// 权限加载器：静态构造触发启动扫描，反射遍历程序集收集管理系统的功能（Action）与资源（Resource）字典
    /// Permission loader: the static constructor triggers a startup scan that reflectively walks the assemblies to collect the management system's actions and resource dictionaries
    /// </summary>
    /// <remarks>
    /// 扫描范围由 ManagementConfig.NamespaceList 与 AssignableControllerTypes 共同过滤；Area 名从类型 FullName 的 .Areas.X. 段提取，缺失时归入 DefaultAreaKeyName。
    /// 异步 Action 的 Task&lt;T&gt;/ValueTask&lt;T&gt; 返回类型会解包后再判定，避免异步入口漏出权限系统（fail-open）。
    /// 控制器自身声明的公共方法若既非 Action 结果类型、又无显式标记（NonAction/匿名/仅需登录），计入 UnregisteredActionList 审计清单，
    /// 其访问策略由 ManagementConfig.UnregisteredActionPolicy 决定，默认 Deny（fail-closed）。
    /// </remarks>
    public class PermissionLoader
    {

        /// <summary>
        /// 无 Area 的功能归入的默认 Area 名（"Main"）/ The default area name for actions without an area ("Main")
        /// </summary>
        public static readonly string DefaultAreaKeyName = "Main";
        /// <summary>
        /// 权限文案的语言模块名（"ManagementPermission"）/ The language module name for permission texts ("ManagementPermission")
        /// </summary>
        public static readonly string ResourceModule = "ManagementPermission";

        /// <summary>
        /// 静态构造：首次访问任一静态成员时触发一次全量权限扫描
        /// Static constructor: triggers the full permission scan once on first access to any static member
        /// </summary>
        static PermissionLoader()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize 的同步锁 / The synchronization lock for Initialize
        /// </summary>
        private static object _locker = new object();
        /// <summary>
        /// 功能字典 / The action dictionary
        /// Key: AreaName_ControllerName_ActionName
        /// </summary>
        public static Dictionary<string, PermissionActionInfo> ActionDict
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 资源字典 / The resource dictionary
        /// Key: Resource
        /// </summary>
        public static Dictionary<string, PermissionResourceInfo> ResourceDict
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 资源-功能列表字典 / The resource-to-action-list dictionary
        /// Key: Resource
        /// </summary>
        public static Dictionary<string, List<PermissionActionInfo>> ResourceActionListDict
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 全部已收录功能的列表（取自 ActionDict 的值）/ The list of all registered actions (the values of ActionDict)
        /// </summary>
        public static List<PermissionActionInfo> ActionList
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// 重新执行权限扫描（加锁）/ Re-runs the permission scan (under the lock)
        /// </summary>
        /// <remarks>静态构造即调用一次；再次调用会整体重建各字典。</remarks>
        public static void Initialize()
        {
            lock (_locker)
            {
                LoadPermissionDict();
            }
        }


        /// <summary>
        /// 从类型 FullName 中提取 .Areas.{AreaName}. 段的正则 / The regex extracting the .Areas.{AreaName}. segment from a type's FullName
        /// </summary>
        private static readonly Regex RegexAreaName = new Regex(@"\.Areas\.(?<AreaName>[a-zA-Z0-9_]+?)\.");

        /// <summary>
        /// 全量装载权限字典：扫描程序集内符合条件的管理控制器，产出功能字典、资源字典与资源-功能列表，并审计未覆盖 Action
        /// Fully loads the permission dictionaries: scans the qualifying management controllers across assemblies to produce the action dictionary, resource dictionary and resource-action lists, and audits uncovered actions
        /// </summary>
        /// <remarks>
        /// 控制器须落在 NamespaceList（配置时）内、继承配置的视图/API 控制器基类与 AssignableControllerTypes 之一、非抽象，且未被 PermissionModule(Ignore) 排除；
        /// 控制器名为 "ManagementBase"、匿名或仅需登录的控制器整类跳过。
        /// 功能的默认资源码为 {Area}_{Controller}_{Action}，可用 PermissionAction.Resource 覆盖；标题与资源名的多语言取 ResourceModule 语言模块。
        /// </remarks>
        private static void LoadPermissionDict()
        {
            Type controllerType = typeof(SuperController);
            Type apiControllerType = typeof(SuperApiController);
            ActionDict = new Dictionary<string, PermissionActionInfo>();
            ResourceDict = new Dictionary<string, PermissionResourceInfo>();
            ResourceActionListDict = new Dictionary<string, List<PermissionActionInfo>>();
            List<PermissionActionInfo> actionInfoList = new List<PermissionActionInfo>();
            List<string> unregisteredActions = new List<string>();

            LocalizationModule? lm = LocalizationManager.GetModule(ResourceModule);

            IList<Type> assignableControllerTypes = ManagementConfig.AssignableControllerTypes!;
            List<Assembly> assemblies = ReflectionTool.GetEssentialAssemblies();
      
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    // 仅在指定的命名空间列表中检索（配置了 NamespaceList 时）/ Retrieve only from the specified namespace list (when NamespaceList is configured)
                    if (ManagementConfig.NamespaceList != null && ManagementConfig.NamespaceList.Count > 0)
                    {
                        if (!ManagementConfig.NamespaceList.Contains(type.Namespace!))
                        {
                            continue;
                        }
                    }

                    bool isActionController = ManagementConfig.ActionViewBaseControllerType != null && ManagementConfig.ActionViewBaseControllerType!.IsAssignableFrom(type);
                    bool isApiController = ManagementConfig.WebApiBaseControllerType != null && ManagementConfig.WebApiBaseControllerType!.IsAssignableFrom(type);
                    if (!isActionController && !isApiController)
                    {
                        continue;
                    }

                    bool isAssignableFrom = assignableControllerTypes.Any(t =>
                    {
                        return t.IsAssignableFrom(type);
                    });
                    if (!isAssignableFrom || type.IsAbstract)
                    {
                        continue;
                    }

                    PermissionModule? module = null;
                    if (type.IsDefined(typeof(PermissionModule), true))
                    {
                        module = (PermissionModule)type.GetCustomAttributes(typeof(PermissionModule), true)[0];
                    }
                    if (module == null)
                    {
                        module = new PermissionModule();
                    }
                    if (module.Ignore)
                    {
                        continue;
                    }

                    bool isAnonymous = type.IsDefined(ManagementConfig.DefinedAnonymousFilterType!, true);
                    bool isLoggedIn = type.IsDefined(ManagementConfig.DefinedLoggedInFilterType!, true);

                    string? areaName = null;
                    string controllerName = type.Name;
                    if (StringTool.EndsWith(controllerName, "Controller"))
                    {
                        controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);
                    }
                    if (string.IsNullOrEmpty(controllerName) || string.Compare(controllerName, "ManagementBase") == 0 || isAnonymous || isLoggedIn)
                    {
                        continue;
                    }
                    
                    if (RegexAreaName.IsMatch(type.FullName!))
                    {
                        Match areaNameMatch = RegexAreaName.Match(type.FullName!);
                        areaName = areaNameMatch.Groups["AreaName"].Value;
                    }

                    Type actionResultType = ManagementConfig.ActionResultType == null ? typeof(IActionResult) : ManagementConfig.ActionResultType;
                    Type apiResultType = ManagementConfig.ApiResultType == null ? typeof(R) : ManagementConfig.ApiResultType;
                    MethodInfo[] allPublicMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                    MethodInfo[] moduleMethods = allPublicMethods.Where(m =>
                    {
                        Type methodDeclareType = m.DeclaringType!;
                        if (m.IsSpecialName || string.Compare(methodDeclareType.Namespace, "Microsoft.AspNetCore.Mvc") == 0)
                        {
                            return false;
                        }
                        // 异步 Action 返回 Task<IActionResult> / Task<R>，必须解包后再判定，否则全部漏出权限系统（fail-open）/ Async actions return Task<IActionResult> / Task<R>; they must be unwrapped before the check or they would all slip out of the permission system (fail-open)
                        Type methodReturnType = UnwrapTaskType(m.ReturnType);
                        if (isActionController && actionResultType.IsAssignableFrom(methodReturnType))
                        {
                            return true;
                        }
                        if (isApiController && apiResultType.IsAssignableFrom(methodReturnType))
                        {
                            return true;
                        }
                        return false;
                    }).ToArray();

                    // 审计：控制器自身声明的公共方法中既不是 Action 结果类型、也没有任何显式标记的，很可能是未被权限系统覆盖的入口 / Audit: public methods declared by the controller itself that are neither Action result types nor explicitly marked are likely uncovered entrances to the permission system
                    foreach (MethodInfo other in allPublicMethods)
                    {
                        if (moduleMethods.Contains(other) || other.IsSpecialName || other.DeclaringType != type)
                        {
                            continue;
                        }
                        if (other.IsDefined(typeof(NonActionAttribute), true)
                            || other.IsDefined(ManagementConfig.DefinedAnonymousFilterType!, true)
                            || other.IsDefined(ManagementConfig.DefinedLoggedInFilterType!, true))
                        {
                            continue;
                        }
                        unregisteredActions.Add($"{type.FullName}.{other.Name} (returns {other.ReturnType.Name})");
                    }

                    if (moduleMethods.Any())
                    {
                        foreach (MethodInfo methodInfo in moduleMethods)
                        {
                            PermissionAction? action = null;
                            PermissionActionInfo? actionInfo = null;
                            if (methodInfo.IsDefined(typeof(PermissionAction), true))
                            {
                                action = (PermissionAction)methodInfo.GetCustomAttributes(typeof(PermissionAction), true)[0];
                            }
                            if (action == null)
                            {
                                action = new PermissionAction();
                            }
                            if (action.Ignore)
                            {
                                continue;
                            }

                            string resourceCode = action.Resource ?? $"{areaName}_{controllerName}_{methodInfo.Name}";
                            Dictionary<string, string?> multiResNameDict = new Dictionary<string, string?>();

                            Dictionary<string, string?> multiTextDict = new Dictionary<string, string?>();
                            string? localizerKey = action.LocalizerKey;
                            if (string.IsNullOrEmpty(localizerKey))
                            {
                                localizerKey = $"{type.Namespace}.{type.Name}.{methodInfo.Name}";
                            }

                            if (lm != null && lm.Resources != null && lm.Resources.Count > 0)
                            {
                                foreach (var lmKV in lm.Resources)
                                {
                                    LocalizationResource localizationResource = lmKV.Value;
                                    string langText = localizationResource.Get(localizerKey!) ?? string.Empty;
                                    if (!string.IsNullOrEmpty(langText))
                                    {
                                        multiTextDict.Add(lmKV.Key, langText);
                                    }

                                    string resText = localizationResource.Get(resourceCode) ?? string.Empty;
                                    if (!string.IsNullOrEmpty(resText))
                                    {
                                        multiResNameDict.Add(lmKV.Key, resText);
                                    }
                                }
                            }

                            actionInfo = new PermissionActionInfo();
                            actionInfo.ActionId = string.Concat(type.FullName, ".", methodInfo.Name);
                            actionInfo.AreaName = areaName;
                            actionInfo.ControllerName = controllerName;
                            actionInfo.ActionName = methodInfo.Name;
                            actionInfo.Title = action.Title;
                            actionInfo.TitleDict = multiTextDict;
                            actionInfo.Resource = resourceCode;
                            actionInfo.Description = action.Description;
                            actionInfo.SortOrder = action.SortOrder;

                            actionInfoList.Add(actionInfo);

                            if (!ResourceDict.ContainsKey(resourceCode))
                            {
                                ResourceDict.Add(resourceCode, new PermissionResourceInfo
                                {
                                    Resource = resourceCode,
                                    ResourceTextDict = multiResNameDict
                                });
                            }
                        }
                    }
                }
            }

            foreach (PermissionActionInfo actionInfo in actionInfoList)
            {
                string areaName = string.IsNullOrEmpty(actionInfo.AreaName) ? DefaultAreaKeyName : actionInfo.AreaName;
                string actionDictKey = $"{areaName}_{actionInfo.ControllerName}_{actionInfo.ActionName}";
                if (!ActionDict.ContainsKey(actionDictKey))
                {
                    ActionDict.Add(actionDictKey, actionInfo);
                }
                if (!ResourceActionListDict.ContainsKey(actionInfo.Resource))
                {
                    ResourceActionListDict.Add(actionInfo.Resource, new List<PermissionActionInfo>());
                }
                ResourceActionListDict[actionInfo.Resource].Add(actionInfo);
            }

            ActionList = ActionDict.Values.ToList();
            UnregisteredActionList = unregisteredActions;

            if (unregisteredActions.Count > 0)
            {
                RampageLogger.Warn(
                    $"PermissionLoader: {unregisteredActions.Count} public controller method(s) are not covered by the permission system " +
                    $"(policy={ManagementConfig.UnregisteredActionPolicy}). Mark them with PermissionLoggedInFilter / AllowAnonymous / NonAction " +
                    "or make them return IActionResult / R:" + Environment.NewLine + string.Join(Environment.NewLine, unregisteredActions));
            }
        }

        /// <summary>
        /// 启动期审计出的、未纳入权限系统且无显式标记的公共控制器方法
        /// Public controller methods discovered at startup that are outside the permission system and carry no explicit marker
        /// </summary>
        public static List<string> UnregisteredActionList
        {
            get;
            private set;
        } = new List<string>();

        /// <summary>
        /// 解包 Task&lt;T&gt;/ValueTask&lt;T&gt; 的泛型参数；非泛型任务或普通类型原样返回
        /// Unwraps the generic argument of Task&lt;T&gt;/ValueTask&lt;T&gt;; non-generic tasks and other types are returned as-is
        /// </summary>
        /// <param name="returnType">方法返回类型 / The method return type</param>
        /// <returns>解包后的类型 / The unwrapped type</returns>
        private static Type UnwrapTaskType(Type returnType)
        {
            if (returnType.IsGenericType)
            {
                Type def = returnType.GetGenericTypeDefinition();
                if (def == typeof(Task<>) || def == typeof(ValueTask<>))
                {
                    return returnType.GetGenericArguments()[0];
                }
            }
            return returnType;
        }

        /// <summary>
        /// 按 Area/控制器/Action 名查询已收录的功能信息
        /// Gets the registered action info by area/controller/action names
        /// </summary>
        /// <param name="areaName">Area 名，为空时使用默认 Area 名 / The area name; the default area when empty</param>
        /// <param name="controllerName">控制器名（不含 Controller 后缀）/ The controller name (without the Controller suffix)</param>
        /// <param name="actionName">Action 名 / The action name</param>
        /// <returns>功能信息，未收录返回 null / The action info, or null when not registered</returns>
        public static PermissionActionInfo? GetAction(string? areaName, string controllerName, string actionName)
        {
            if (string.IsNullOrEmpty(areaName))
            {
                areaName = DefaultAreaKeyName;
            }
            string key = $"{areaName}_{controllerName}_{actionName}";
            if (ActionDict.ContainsKey(key))
            {
                return ActionDict[key];
            }
            return null;
        }

    }
}
