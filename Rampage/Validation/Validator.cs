using Rampage.Localization;
using Rampage.Reflection;
using System;
using System.Globalization;

namespace Rampage.Validation
{
    /// <summary>
    /// 声明式验证器：按类型加载特性规则并执行验证，失败不抛异常而是返回 ValidationResult
    /// Declarative validator: loads attribute-based rules per type and validates without throwing; failures are reported via ValidationResult
    /// </summary>
    /// <remarks>
    /// 规则按类型缓存；错误消息经 string.Format 格式化，{0} 为属性当前值，{1} 起为规则 GetTextArguments 提供的参数；
    /// Rules are cached per type; message templates are formatted with string.Format where {0} is the property value and {1}+ are the rule's TextArguments;
    /// 本地化资源键为 "类名.属性名.规则名"（规则名去掉 Attribute 后缀）；每个属性只记录第一条失败规则；
    /// Localization keys follow "TypeName.PropertyName.RuleName" (rule name without the Attribute suffix); only the first failing rule per property is recorded;
    /// ResourceModule 默认为 "Validator"，但框架本身不内置该资源目录，需应用程序自行提供对应多语言资源才能生效；
    /// ResourceModule defaults to "Validator", but the framework does not ship that resource module — the application must provide it for localization to work.
    /// </remarks>
    public class Validator
    {
        private static object _locker = new object();

        static Validator()
        {
            ResourceModule = "Validator";
        }

        /// <summary>
        /// 本地化资源模块名，默认 "Validator" / The localization resource module name, defaults to "Validator"
        /// </summary>
        public static string ResourceModule
        {
            get;
            set;
        }

        private static Dictionary<string, List<ValidationRule>>? _ruleDict;
        /// <summary>
        /// 验证规则存储：类型 FullName → 该类型各属性的规则列表 / The rule store: type full name to the per-property rule list
        /// </summary>
        public static Dictionary<string, List<ValidationRule>> RuleDict
        {
            get
            {
                if (_ruleDict == null)
                {
                    lock (_locker)
                    {
                        if (_ruleDict == null)
                        {
                            _ruleDict = new Dictionary<string, List<ValidationRule>>();
                        }
                    }
                }
                return _ruleDict;
            }
        }

        /// <summary>
        /// 扫描类型属性上的验证特性并构建规则缓存 / Scan the validation attributes on the type's properties and cache the rules
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <remarks>
        /// 为每个特性预构建各语言的 MessageTemplateDict（本地化键优先，缺省回退到 "完整类名.属性名.规则名" 再回退到 MessageTemplate）；
        /// Pre-builds a MessageTemplateDict per language for every attribute (localized key first, falling back to "FullTypeName.PropertyName.RuleName" then MessageTemplate);
        /// 规则以 Dictionary.Add 语义写入缓存，重复加载同一类型会抛异常 / Rules are stored with Dictionary.Add semantics; loading the same type twice throws.
        /// </remarks>
        public static void LoadRules(Type type)
        {
            lock (_locker)
            {
                List<ValidationRule> list = new List<ValidationRule>();
                var propertyInfos = ReflectionTool.GetPropertyList(type);
                if (propertyInfos != null && propertyInfos.Count > 0)
                {
                    LocalizationModule? lm = LocalizationManager.GetModule(ResourceModule);

                    foreach (var propertyInfo in propertyInfos)
                    {
                        if (propertyInfo == null) { continue; }
                        ValidationRule rule = new ValidationRule();

                        List<ValidationRuleInfo>? ruleInfoList = null;
                        if (propertyInfo.IsDefined(typeof(ValidationAttributeBase), true))
                        {
                            var customeAttrList = propertyInfo.GetCustomAttributes(typeof(ValidationAttributeBase), true);
                            ruleInfoList = customeAttrList.Select(o =>
                            {
                                ValidationAttributeBase attr = (ValidationAttributeBase)o;
                                attr.FullTypeName = $"{type.Namespace!}.{type.Name}";
                                attr.TypeName = type.Name;
                                attr.PropertyName = propertyInfo.Name;
                                attr.Property = propertyInfo;

                                string? messageTemplate = attr.MessageTemplate;
                                string? localizerKey = attr.GetLocalizerKey();
                                Dictionary<string, string?> messageTemplateDict = new Dictionary<string, string?>();

                                if (lm != null && lm.Resources != null && lm.Resources.Count > 0)
                                {
                                    foreach (var lmKV in lm.Resources)
                                    {
                                        LocalizationResource localizationResource = lmKV.Value;
                                        string langText = localizationResource.Get(localizerKey!) ?? string.Empty;
                                        if (string.IsNullOrEmpty(langText))
                                        {
                                            langText = localizationResource.Get($"{attr.FullTypeName}.{attr.PropertyName}.{attr.GetType().Name.Replace("Attribute", "")}", attr.MessageTemplate) ?? string.Empty;
                                        }
                                        messageTemplateDict.Add(lmKV.Key, langText);
                                    }
                                }

                                return new ValidationRuleInfo
                                {
                                    MessageTemplate = messageTemplate,
                                    MessageTemplateDict = messageTemplateDict,
                                    TextArguments = attr.GetTextArguments(),
                                    ValidateHandler = new Func<object?, bool>(attr.Validate),
                                    Required = attr.Required,
                                    IsWarning = attr.IsWarning,
                                    FullTypeName = $"{type.Namespace!}.{type.Name}",
                                    PropertyName = propertyInfo.Name,
                                    Property = propertyInfo
                                };
                            }).ToList();
                        }

                        if (ruleInfoList != null)
                        {
                            rule.Property = propertyInfo;
                            rule.PropertyName = propertyInfo.Name;
                            rule.RuleList = ruleInfoList;
                            list.Add(rule);
                        }
                    }
                }
                RuleDict.Add(type.FullName!, list);
            }
        }

        /// <summary>
        /// 获取指定类型的验证规则列表 / Retrieve the validation rule list of the specified type
        /// </summary>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <returns>各属性的规则列表（首次访问自动加载）/ The per-property rule list (auto-loaded on first access)</returns>
        public static List<ValidationRule> GetRuleList<T>() where T : class
        {
            Type type = typeof(T);

            return GetRuleList(type);
        }

        /// <summary>
        /// 获取指定类型的验证规则列表 / Retrieve the validation rule list of the specified type
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <returns>各属性的规则列表（首次访问自动加载）/ The per-property rule list (auto-loaded on first access)</returns>
        public static List<ValidationRule> GetRuleList(Type type)
        {
            List<ValidationRule> list;
            if (!RuleDict.ContainsKey(type.FullName!))
            {
                LoadRules(type);
            }
            list = RuleDict[type.FullName!];

            return list;
        }

        /// <summary>
        /// 获取指定类型某属性的验证规则 / Get the validation rule of one property on the type
        /// </summary>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <param name="propertyName">属性名 / The property name</param>
        /// <returns>该属性的规则；属性无任何验证特性时为 null / The property's rule, or null when the property carries no validation attribute</returns>
        public static ValidationRule GetRule<T>(string propertyName) where T : class
        {
            List<ValidationRule> list = GetRuleList<T>();
            return list.Find(r => r.PropertyName == propertyName)!;
        }

        /// <summary>
        /// 按文化名验证指定对象 / Validate the given object by culture name
        /// </summary>
        /// <typeparam name="T">对象类型 / The object type</typeparam>
        /// <param name="obj">待验证对象；为 null 时直接返回失败结果 / The object to validate; a null object yields a failed result</param>
        /// <param name="cultureName">消息文化名（如 zh-CN），为 null 时回退到当前文化 / The message culture name (e.g. zh-CN); falls back to the current culture when null</param>
        /// <returns>验证结果 / The validation result</returns>
        public static ValidationResult Validate<T>(T? obj, string? cultureName = null) where T : class
        {
            return Validate(obj, cultureName);
        }

        /// <summary>
        /// 验证指定对象 / Validate the given object
        /// </summary>
        /// <param name="obj">待验证对象；为 null 时直接返回失败结果（无明细）/ The object to validate; a null object yields a failed result with no details</param>
        /// <param name="culture">消息文化；为 null 时依次取 LocalizationManager.CultureInfoGetter 与默认语言 / The message culture; falls back to CultureInfoGetter then the default language when null</param>
        /// <returns>验证结果；失败明细按属性名收录，每属性仅首条失败 / The validation result; failures are keyed by property name, first failure only per property</returns>
        public static ValidationResult Validate(object? obj, CultureInfo? culture = null)
        {
            if (obj == null)
            {
                return new ValidationResult
                {
                    Success = false,
                    Details = new Dictionary<string, ValidationResultItem>()
                };
            }
            if (culture == null && LocalizationManager.CultureInfoGetter != null)
            {
                culture = LocalizationManager.CultureInfoGetter();
            }

            ValidationResult result = new ValidationResult
            {
                Success = true,
                Details = new Dictionary<string, ValidationResultItem>()
            };

            Type type = obj.GetType();
            List<ValidationRule> ruleList = GetRuleList(type);
            if (ruleList.Count > 0)
            {
                LocalizationResource? localizationResource = LocalizationManager.GetResource(ResourceModule, culture);

                foreach (ValidationRule rule in ruleList)
                {
                    var ruleItemList = rule.RuleList;

                    if (ruleItemList.Count > 0)
                    {
                        object? v = EmitTool.GetPropertyValue(obj, rule.PropertyName);
                        foreach (var item in ruleItemList)
                        {
                            ValidationResultItem resultItem = new ValidationResultItem
                            {
                                Name = rule.PropertyName
                            };

                            bool validateResult = item.ValidateHandler(v);
                            resultItem.Code = !validateResult ? (item.IsWarning ? ValidationResultCode.Warning : ValidationResultCode.Failure) : ValidationResultCode.Success;

                            string? text = null;
                            if (false == validateResult)
                            {
                                string cultureName = culture == null ? LocalizationManager.Config.DefaultLanguageName : culture.Name;
                                // If the custom text is empty or specifies the use of localized language
                                if (item.MessageTemplateDict.ContainsKey(cultureName))
                                {
                                    text = item.MessageTemplateDict[cultureName];
                                }
                                if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(item.MessageTemplate))
                                {
                                    text = item.MessageTemplate;
                                }
                                if (string.IsNullOrEmpty(text) && item.MessageTemplateDict.ContainsKey(LocalizationManager.Config.DefaultLanguageName))
                                {
                                    text = item.MessageTemplateDict[LocalizationManager.Config.DefaultLanguageName];
                                }
                                if (string.IsNullOrEmpty(text))
                                {
                                    text = $"Illegal \"{item.PropertyName}\" value!";
                                }
                            }

                            object?[] args = new object?[] { v };
                            object?[]? args2 = item.TextArguments;
                            if (args2 != null && args2.Length > 0)
                            {
                                args = args.Concat(args2).ToArray();
                            }
                            if (!validateResult && !item.IsWarning)
                            {
                                result.Success = false;
                                resultItem.Description = string.Format(text!, args);
                            }
                            else
                            {
                                continue;
                            }
                            if (!validateResult && item.IsWarning)
                            {
                                result.HasWarnings = true;
                            }

                            if (!result.Details.ContainsKey(item.PropertyName))
                            {
                                result.Details.Add(item.PropertyName, resultItem);
                            }
                        }
                    }
                }
            }

            return result;
        }

    }
}
