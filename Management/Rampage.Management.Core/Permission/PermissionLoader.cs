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
    /// 
    /// </summary>
    public class PermissionLoader
    {

        public static readonly string DefaultAreaKeyName = "Main";
        public static readonly string ResourceModule = "ManagementPermission";

        static PermissionLoader()
        {
            Initialize();
        }

        private static object _locker = new object();
        /// <summary>
        /// Key: AreaName_ControllerName_ActionName
        /// </summary>
        public static Dictionary<string, PermissionActionInfo> ActionDict
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// Key: Resource
        /// </summary>
        public static Dictionary<string, PermissionResourceInfo> ResourceDict
        {
            get;
            private set;
        } = default!;

        /// <summary>
        /// Key: Resource
        /// </summary>
        public static Dictionary<string, List<PermissionActionInfo>> ResourceActionListDict
        {
            get;
            private set;
        } = default!;

        public static List<PermissionActionInfo> ActionList
        {
            get;
            private set;
        } = default!;

        public static void Initialize()
        {
            lock (_locker)
            {
                LoadPermissionDict();
            }
        }


        private static readonly Regex RegexAreaName = new Regex(@"\.Areas\.(?<AreaName>[a-zA-Z0-9_]+?)\.");

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
                    // Retrieve from the specified namespace
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
                        // 异步 Action 返回 Task<IActionResult> / Task<R>，必须解包后再判定，否则全部漏出权限系统（fail-open）
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

                    // 审计：控制器自身声明的公共方法中既不是 Action 结果类型、也没有任何显式标记的，很可能是未被权限系统覆盖的入口
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
        /// </summary>
        public static List<string> UnregisteredActionList
        {
            get;
            private set;
        } = new List<string>();

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
