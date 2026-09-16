using Rampage.Reflection;
using System;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Rampage.Utilities
{
    /// <summary>
    ///
    /// </summary>
    public class GenericTool
    {

        /// <summary>
        /// 数据库标量值转换：同类型直接返回，数值/可转换类型走 Convert，枚举与可空类型已处理。
        /// 与字符串往返解析相比无额外分配；类型不兼容时抛异常而非静默返回默认值。
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="value">数据库返回的标量值</param>
        public static T? ConvertScalar<T>(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return default(T);
            }
            if (value is T matched)
            {
                return matched;
            }
            Type targetType = typeof(T);
            if (targetType.IsEnum)
            {
                return (T)Enum.ToObject(targetType, Convert.ChangeType(value, Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture));
            }
            Type? underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                targetType = underlyingType;
                if (value.GetType() == targetType)
                {
                    return (T)value;
                }
            }
            if (value is IConvertible)
            {
                return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            return (T)value;
        }

        /// <summary>
        /// check whether the specified type can be null (such as int?, decimal?, DateTime?...)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNullableType(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// check if a type is anonymous
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsAnonymousType(Type type)
        {
            return (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic
                && (type.Attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed
                && type.BaseType == typeof(object)
                && type.Namespace == null;
        }

        /// <summary>
        /// Get the underlying type argument of the specified nullable type, if the type is not nullable, then return the type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetUnderlyingType(Type type)
        {
            if (false == IsNullableType(type))
            {
                return type;
            }
            Type? resultType = Nullable.GetUnderlyingType(type);
            if (resultType == null)
            {
                resultType = type;
            }
            return resultType;
        }

        /// <summary>
        /// Get the underlying type argument of the specified nullable type, if the type is not nullable, then return the type
        /// </summary>
        /// <returns></returns>
        public static Type GetUnderlyingType<T>()
        {
            Type type = typeof(T);
            if (false == IsNullableType(type))
            {
                return type;
            }
            Type? resultType = Nullable.GetUnderlyingType(type);
            if (resultType == null)
            {
                resultType = type;
            }
            return resultType;
        }

        /// <summary>
        /// check if "derivedType" Derived from "genericInterfaceType"
        /// </summary>
        /// <param name="derivedType"></param>
        /// <param name="genericInterfaceType"></param>
        /// <returns></returns>
        public static bool IsDerivedFromGenericInterface(Type derivedType, Type genericInterfaceType)
        {
            if (derivedType == null || genericInterfaceType == null || !genericInterfaceType.IsInterface || !genericInterfaceType.IsGenericTypeDefinition)
            {
                return false;
            }

            return derivedType.GetInterfaces().Any(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == genericInterfaceType);
        }

        #region Dynamic

        /// <summary>
        /// Assign values to dynamic data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        public static dynamic SetDynamicItem<T>(dynamic data, string key, T obj)
        {
            IDictionary<string, T>? tempData = data as IDictionary<string, T>;
            if (tempData != null)
            {
                if (!tempData.ContainsKey(key))
                    tempData.Add(key, obj);
                else
                    tempData[key] = obj;
            }

            return data;
        }

        /// <summary>
        /// Assign values to dynamic data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        public static dynamic SetDynamicItem(dynamic data, string key, object obj)
        {
            IDictionary<string, object>? tempData = data as IDictionary<string, object>;
            if (tempData != null)
            {
                if (!tempData.ContainsKey(key))
                    tempData.Add(key, obj);
                else
                    tempData[key] = obj;
            }

            return data;
        }

        /// <summary>
        /// Get the value of the dynamic type specified key name
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        public static T? GetDynamicItem<T>(dynamic data, string key)
        {
            T? v = default(T);
            object? ov = GetDynamicItem(data, key);
            if (ov != null)
            {
                Type ovType = ov.GetType();
                TypeCode ovTC = Type.GetTypeCode(ovType);
                if (ovType.IsValueType || ovType.IsEnum || ovType == typeof(Guid))
                {
                    v = StringTool.TryParse<T>(ov.ToString()!);
                }
                else
                {
                    v = (T)ov;
                }
            }

            return v;
        }

        /// <summary>
        /// Get the value of the dynamic type specified key name
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        public static object? GetDynamicItem(dynamic data, string key)
        {
            object? item = null;
            if (data != null)
            {
                ICollection<KeyValuePair<string, object>>? tempParamData = data as ICollection<KeyValuePair<string, object>>;
                if (tempParamData != null && tempParamData.Any(d => d.Key == key))
                {
                    var tempData = tempParamData.Where(d => d.Key == key).FirstOrDefault();
                    item = tempData.Value;
                    return item;
                }
            }

            return item;
        }

        #endregion


        /// <summary>
        /// Retrieve the value list of the specified attribute from the specified instance list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="P"></typeparam>
        /// <param name="list"></param>
        /// <param name="propertyName"></param>
        /// <param name="unique"></param>
        /// <returns></returns>
        public static List<P> GetValueList<T, P>(IList<T> list, string propertyName, object? defaultItemValue = null, bool unique = true)
        {
            List<P> valueList = new List<P>();
            if (list != null && list.Count > 0)
            {
                Type type = typeof(T);
                foreach (T item in list)
                {
                    object? objValue = EmitTool.GetPropertyValue(item!, propertyName);
                    if (objValue != null)
                    {
                        P pValue = (P)objValue;
                        if (unique == true)
                        {
                            if (!valueList.Contains(pValue))
                            {
                                valueList.Add(pValue);
                            }
                        }
                        else
                        {
                            valueList.Add(pValue);
                        }
                    }
                }
            }
            if (valueList.Count == 0)
            {
                if (defaultItemValue != null)
                {
                    P? pv = StringTool.TryParse<P>(defaultItemValue.ToString()!);
                    if (pv != null)
                    {
                        valueList.Add(pv);
                    }
                }
            }

            return valueList;
        }

        /// <summary>
        /// check whether the object is a enum value
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsEnum(object obj)
        {
            Type type = obj.GetType();
            return type.IsEnum;
        }

        /// <summary>
        /// check whether the object is a numerical value
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNumber(object obj)
        {
            Type type = obj.GetType();
            return IsNumberType(type);
        }

        /// <summary>
        /// 是否数字类型(整型，小数类型等)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNumberType(Type type)
        {
            Type baseType = GetUnderlyingType(type);
            if (!baseType.IsValueType)
            {
                return false;
            }
            TypeCode typeCode = Type.GetTypeCode(baseType);
            switch (typeCode)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Dictionary<string, object?>? ConvertToDictionary(object? obj)
        {
            if (obj == null)
            {
                return null;
            }
            Type type = obj.GetType();
            TypeCode typeCode = Type.GetTypeCode(type);
            if (type.IsValueType || typeCode == TypeCode.String)
            {
                return null;
            }

            Dictionary<string, object?> result = new Dictionary<string, object?>();
            if (obj is IDictionary)
            {
                IDictionary dict = (IDictionary)obj;
                foreach (DictionaryEntry kv in dict)
                {
                    result.Add(kv.Key.ToString()!, kv.Value);
                }
                return result;
            }

            List<PropertyInfo> prpList = ReflectionTool.GetPropertyList(type);
            foreach (PropertyInfo prp in prpList)
            {
                object? value = EmitTool.GetPropertyValue(obj, prp.Name);
                result.Add(prp.Name, value);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public static Dictionary<string, object?>? ConvertToDictionary<T>(T? model) where T : class
        {
            if (model == null)
            {
                return null;
            }

            Dictionary<string, object?> result = new Dictionary<string, object?>();
            if (model is IDictionary)
            {
                IDictionary dict = (IDictionary)model;
                foreach (DictionaryEntry kv in dict)
                {
                    result.Add(kv.Key.ToString()!, kv.Value);
                }
                return result;
            }

            List<PropertyInfo> prpList = ReflectionTool.GetPropertyList(typeof(T));
            foreach (PropertyInfo prp in prpList)
            {
                object? value = EmitTool.GetPropertyValue(model, prp.Name);
                result.Add(prp.Name, value);
            }
            return result;
        }

    }
}
