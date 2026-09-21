using Rampage.Localization;
using Rampage.Utilities;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Rampage.Enums
{
    /// <summary>
    /// 枚举信息读取器：结合 EnumFieldAttribute 与多语言资源提供枚举文本及成员信息 / Enum info reader: resolves enum texts and member info from EnumFieldAttribute and localized resources
    /// </summary>
    /// <remarks>
    /// 解析结果按枚举类型全名缓存在 InfoDict；文本解析带多级回退链，详见 <see cref="GetText{T}(T, CultureInfo)"/>。
    /// Results are cached in InfoDict by enum type full name; text resolution uses a multi-level fallback chain, see GetText.
    /// </remarks>
    public class EnumReader
    {

        static EnumReader()
        {
            ResourceModule = "EnumReader";
        }

        /// <summary>
        /// 本地化资源模块名（默认 "EnumReader"） / Localization resource module name (default "EnumReader")
        /// </summary>
        public static string ResourceModule
        {
            get;
            set;
        }

        /// <summary>
        /// 枚举信息缓存字典（键为枚举类型全名） / Cache of enum info lists keyed by enum type full name
        /// </summary>
        public static ConcurrentDictionary<string, List<EnumInfo>> InfoDict
        {
            get;
        } = new ConcurrentDictionary<string, List<EnumInfo>>();

        /// <summary>
        /// 向缓存添加枚举信息列表（键已存在时忽略） / Adds an info list to the cache unless the key already exists
        /// </summary>
        private static void AddInfo(string typeName, List<EnumInfo> info)
        {
            InfoDict.TryAdd(typeName, info);
        }

        /// <summary>
        /// 获取枚举值的多语言显示文本 / Gets the localized display text of an enum value
        /// </summary>
        /// <remarks>
        /// culture 为空时先取 LocalizationManager.CultureInfoGetter() 的请求文化；
        /// 文本回退链：当前文化资源文本 → EnumFieldAttribute.Text → 默认语言资源文本 → 枚举字段名 → 整数值。
        /// When culture is null the request culture from LocalizationManager.CultureInfoGetter is used first;
        /// fallback chain: current-culture resource text, then EnumFieldAttribute.Text, then the default-language resource text,
        /// then the enum field name, then the integer value.
        /// </remarks>
        /// <typeparam name="T">枚举类型 / Enum type</typeparam>
        /// <param name="v">枚举值 / Enum value</param>
        /// <param name="culture">目标文化，空则使用请求文化 / Target culture; the request culture is used when null</param>
        /// <returns>解析出的文本，无法解析时为 null / The resolved text, or null when it cannot be resolved</returns>
        public static string? GetText<T>(T v, CultureInfo? culture = null) where T : struct
        {
            Type type = GenericTool.GetUnderlyingType<T>();
            if (culture == null && LocalizationManager.CultureInfoGetter != null)
            {
                culture = LocalizationManager.CultureInfoGetter();
            }

            string? text = null;
            if (type.IsEnum)
            {
                string name = v.ToString()!;
                string typeFullName = string.Concat(type.Namespace, ".", type.Name);

                EnumInfo ? info = null;
                if (InfoDict.TryGetValue(typeFullName, out List<EnumInfo>? cachedInfoList))
                {
                    info = cachedInfoList.Find(i => i.IntValue == StringTool.TryParse<int>(name, int.MinValue) || i.StringValue == name);
                }
                if (info == null)
                {
                    List<EnumInfo> infoList = GetInfoList<T>();
                    AddInfo(typeFullName, infoList);

                    if (InfoDict.TryGetValue(typeFullName, out List<EnumInfo>? addedInfoList))
                    {
                        info = addedInfoList.Find(i => i.IntValue == StringTool.TryParse<int>(name, int.MinValue) || i.StringValue == name);
                    }
                }
                

                if (info != null)
                {
                    string cultureName = culture == null ? LocalizationManager.Config.DefaultLanguageName : culture.Name;
                    // 优先取当前文化的多语言文本 / prefer the localized text of the current culture
                    if (info.TextDict.ContainsKey(cultureName))
                    {
                        text = info.TextDict[cultureName];
                    }

                    if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(info.Text))
                    {
                        text = info.Text;
                    }
                    if (string.IsNullOrEmpty(text) && info.TextDict.ContainsKey(LocalizationManager.Config.DefaultLanguageName))
                    {
                        text = info.TextDict[LocalizationManager.Config.DefaultLanguageName];
                    }

                    // 仍未取到则回退枚举字段名，再不行用整数值 / fall back to the enum field name, then the integer value
                    if (string.IsNullOrEmpty(text))
                        text = info.StringValue;

                    if (string.IsNullOrEmpty(text))
                        text = info.IntValue.ToString();
                }
            }

            return text;
        }

        /// <summary>
        /// 根据 EnumInfo 的字符串值获取其本地化文本 / Gets the localized text for the enum value described by the given EnumInfo
        /// </summary>
        /// <typeparam name="T">枚举类型 / Enum type</typeparam>
        /// <param name="info">枚举成员描述信息 / Enum member description</param>
        /// <returns>解析出的文本，无法解析时为 null / The resolved text, or null when it cannot be resolved</returns>
        public static string? GetText<T>(EnumInfo info) where T : struct
        {
            T v = StringTool.TryParse<T>(info.StringValue!);
            return GetText<T>(v);
        }

        /// <summary>
        /// 获取枚举类型的成员信息列表 / Gets the member info list of an enum type
        /// </summary>
        /// <typeparam name="T">枚举类型 / Enum type</typeparam>
        /// <returns>成员信息列表，非枚举类型返回空列表 / The member info list; an empty list for non-enum types</returns>
        public static List<EnumInfo> GetInfoList<T>() where T : struct
        {
            Type type = typeof(T);

            return GetInfoList(type);
        }

        /// <summary>
        /// 获取枚举类型的成员信息列表（带缓存） / Gets the member info list of an enum type (cached)
        /// </summary>
        /// <remarks>
        /// 首次解析后写入 InfoDict 缓存。未显式指定 SortOrder（=0）的成员按声明倒序计算排序值，最终按排序值降序输出；
        /// 多语言文本按候选键 LocalizerKey → 命名空间.类型.字段 → 类型.字段 → 字段名 依次查找，均未命中时回退 EnumFieldAttribute.Text。
        /// Results are cached in InfoDict on first resolution. Members without an explicit SortOrder (0) get a reverse
        /// declaration-order value, and the list is sorted by SortOrder descending; localized text is looked up by candidate keys
        /// LocalizerKey, Namespace.Type.Field, Type.Field, then Field, falling back to EnumFieldAttribute.Text.
        /// </remarks>
        /// <param name="type">枚举类型 / Enum type</param>
        /// <returns>成员信息列表，非枚举类型返回空列表 / The member info list; an empty list for non-enum types</returns>
        public static List<EnumInfo> GetInfoList(Type type)
        {
            List<EnumInfo>? list = new List<EnumInfo>();
            if (!type.IsEnum)
            {
                return list;   
            }
            string typeFullName = string.Concat(type.Namespace, ".", type.Name);

            if (!InfoDict.TryGetValue(typeFullName, out List<EnumInfo>? cachedList))
            {
                LocalizationModule? lm = LocalizationManager.GetModule(ResourceModule);

                string[] enumNames = Enum.GetNames(type);
                int len = enumNames.Length;
                for (int i = 0; i < len; i++)
                {
                    string name = enumNames[i];
                    FieldInfo fieldInfo = type.GetField(name)!;

                    object? enumValue;
                    if (Enum.TryParse(type, fieldInfo.Name, out enumValue))
                    {
                        EnumFieldAttribute? attr = null;
                        if (fieldInfo.IsDefined(typeof(EnumFieldAttribute)))
                        {
                            attr = (Attribute.GetCustomAttribute(fieldInfo, typeof(EnumFieldAttribute)) as EnumFieldAttribute);
                        }
                        if (attr == null)
                        {
                            attr = new EnumFieldAttribute(fieldInfo.Name);
                        }
                        string? text = attr.Text;
                        Dictionary<string, string?> multiTextDict = new Dictionary<string, string?>();

                        string? localizerKey = attr.LocalizerKey;
                        if (lm != null && lm.Resources != null && lm.Resources.Count > 0)
                        {
                            string localizerKey1 = $"{type.Namespace}.{type.Name}.{fieldInfo.Name}";
                            string localizerKey2 = $"{type.Name}.{fieldInfo.Name}";
                            string localizerKey3 = $"{fieldInfo.Name}";
                            foreach (var lmKV in lm.Resources)
                            {
                                LocalizationResource localizationResource = lmKV.Value;
                                string? langText = null;
                                if (!string.IsNullOrEmpty(localizerKey))
                                {
                                    langText = localizationResource.Get(localizerKey, string.Empty);
                                }
                                if (string.IsNullOrEmpty(langText))
                                {
                                    langText = localizationResource.Get(localizerKey1, string.Empty);
                                }
                                if (string.IsNullOrEmpty(langText))
                                {
                                    langText = localizationResource.Get(localizerKey2, string.Empty);
                                }
                                if (string.IsNullOrEmpty(langText))
                                {
                                    langText = localizationResource.Get(localizerKey3, string.Empty);
                                }
                                if (string.IsNullOrEmpty(langText))
                                {
                                    langText = attr.Text;
                                }
                                multiTextDict.Add(lmKV.Key, langText);
                            }
                        }

                        int intValue = Convert.ToInt32(enumValue);
                        string stringValue = fieldInfo.Name;

                        list.Add(new EnumInfo
                        {
                            Text = text,
                            TextDict = multiTextDict,
                            StringValue = stringValue,
                            IntValue = intValue,
                            SortOrder = attr.SortOrder == 0 ? len - i - 1 : attr.SortOrder
                        });
                    }
                }

                list.Sort(delegate (EnumInfo item1, EnumInfo item2)
                {
                    return item2.SortOrder - item1.SortOrder;
                });

                AddInfo(typeFullName, list);
            }
            else
            {
                list = cachedList;
            }

            return list;
        }


    }
}
