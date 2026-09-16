using Rampage.Localization;
using Rampage.Utilities;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Rampage.Enums
{
    /// <summary>
    /// Enum info reader
    /// </summary>
    public class EnumReader
    {

        static EnumReader()
        {
            ResourceModule = "EnumReader";
        }

        /// <summary>
        /// Localization Resource Module Name
        /// </summary>
        public static string ResourceModule
        {
            get;
            set;
        }

        /// <summary>
        /// Enum Info Dictionary
        /// </summary>
        public static ConcurrentDictionary<string, List<EnumInfo>> InfoDict
        {
            get;
        } = new ConcurrentDictionary<string, List<EnumInfo>>();

        /// <summary>
        ///
        /// </summary>
        private static void AddInfo(string typeName, List<EnumInfo> info)
        {
            InfoDict.TryAdd(typeName, info);
        }

        /// <summary>
        /// Get Localized text of enum value text
        /// </summary>
        /// <typeparam name="T">enum type</typeparam>
        /// <param name="v">enum value</param>
        /// <param name="culture"></param>
        /// <returns></returns>
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
                    // If the custom text is empty or specifies the use of localized language
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

                    // if still can not get text, then use enum string as text
                    if (string.IsNullOrEmpty(text))
                        text = info.StringValue;

                    if (string.IsNullOrEmpty(text))
                        text = info.IntValue.ToString();
                }
            }

            return text;
        }

        /// <summary>
        /// get enum localized text
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info"></param>
        /// <returns></returns>
        public static string? GetText<T>(EnumInfo info) where T : struct
        {
            T v = StringTool.TryParse<T>(info.StringValue!);
            return GetText<T>(v);
        }

        /// <summary>
        /// get enum info list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<EnumInfo> GetInfoList<T>() where T : struct
        {
            Type type = typeof(T);

            return GetInfoList(type);
        }

        /// <summary>
        /// get enum info list
        /// </summary>
        /// <returns></returns>
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
