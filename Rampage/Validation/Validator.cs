using Rampage.Localization;
using Rampage.Reflection;
using System;
using System.Globalization;

namespace Rampage.Validation
{
    /// <summary>
    /// Validator
    /// </summary>
    public class Validator
    {
        private static object _locker = new object();

        static Validator()
        {
            ResourceModule = "Validator";
        }

        /// <summary>
        /// 
        /// </summary>
        public static string ResourceModule
        {
            get;
            set;
        }

        private static Dictionary<string, List<ValidationRule>>? _ruleDict;
        /// <summary>
        /// Validation rules Dictionary Store
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
        /// Retrieve the list of validation rule information for the specified object type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<ValidationRule> GetRuleList<T>() where T : class
        {
            Type type = typeof(T);

            return GetRuleList(type);
        }

        /// <summary>
        /// Retrieve the list of validation rule information for the specified object type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static ValidationRule GetRule<T>(string propertyName) where T : class
        {
            List<ValidationRule> list = GetRuleList<T>();
            return list.Find(r => r.PropertyName == propertyName)!;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="cultureName"></param>
        /// <returns></returns>
        public static ValidationResult Validate<T>(T? obj, string? cultureName = null) where T : class
        {
            return Validate(obj, cultureName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
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
