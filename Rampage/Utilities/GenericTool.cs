using Rampage.Reflection;
using System;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Rampage.Utilities
{
    /// <summary>
    /// 泛型/反射辅助工具（数据库标量转换、可空与匿名类型判断、动态对象读写、字典转换等） /
    /// Generic/reflection helpers (database scalar conversion, nullable & anonymous type checks, dynamic object access, dictionary conversion, etc.)
    /// </summary>
    public class GenericTool
    {

        /// <summary>
        /// 数据库标量值转换：同类型直接返回，数值/可转换类型走 Convert，枚举与可空类型已处理 /
        /// Convert a database scalar: same-type values return directly, numeric/convertible types go through Convert, enums and nullable types are handled
        /// </summary>
        /// <remarks>
        /// 与字符串往返解析相比无额外分配；类型不兼容时抛异常而非静默返回默认值。
        /// Avoids the extra allocation of a parse-to-string round trip; incompatible types throw instead of silently returning a default.
        /// </remarks>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <param name="value">数据库返回的标量值 / The scalar value returned by the database</param>
        /// <returns>转换结果；null 或 DBNull 返回 default(T) / The converted value; default(T) for null or DBNull</returns>
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
        /// 判断指定类型是否为可空类型（如 int?、decimal?、DateTime?） /
        /// Check whether the specified type is nullable (such as int?, decimal?, DateTime?)
        /// </summary>
        /// <param name="type">待判断类型 / The type to check</param>
        /// <returns>可空类型返回 true / true for nullable types</returns>
        public static bool IsNullableType(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// 判断类型是否为匿名类型 /
        /// Check whether the type is anonymous
        /// </summary>
        /// <remarks>按编译器特征判定：internal + sealed + 直接继承 object + 无命名空间。Detected by compiler traits: internal + sealed + deriving directly from object + no namespace.</remarks>
        /// <param name="type">待判断类型 / The type to check</param>
        /// <returns>匿名类型返回 true / true for anonymous types</returns>
        public static bool IsAnonymousType(Type type)
        {
            return (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic
                && (type.Attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed
                && type.BaseType == typeof(object)
                && type.Namespace == null;
        }

        /// <summary>
        /// 获取可空类型的底层类型参数；类型本身不可空时原样返回 /
        /// Get the underlying type argument of the specified nullable type; returns the type itself when it is not nullable
        /// </summary>
        /// <param name="type">待处理类型 / The type to process</param>
        /// <returns>可空类型的底层类型，或原类型 / The underlying type of the nullable type, or the type itself</returns>
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
        /// 获取泛型类型参数的底层类型；类型不可空时原样返回 /
        /// Get the underlying type of the generic type argument; returns the type itself when it is not nullable
        /// </summary>
        /// <returns>可空类型的底层类型，或原类型 / The underlying type of the nullable type, or the type itself</returns>
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
        /// 判断类型是否实现了指定的泛型接口定义 /
        /// Check whether the type implements the specified generic interface definition
        /// </summary>
        /// <param name="derivedType">待判断的派生类型 / The type to check</param>
        /// <param name="genericInterfaceType">泛型接口定义类型（须为 IsGenericTypeDefinition 的接口）/ The generic interface definition (must be an interface with IsGenericTypeDefinition)</param>
        /// <returns>实现了该泛型接口的任一构造形式返回 true；参数为 null 或传入的不是泛型接口定义时返回 false / true when any constructed form of the interface is implemented; false when either argument is null or genericInterfaceType is not a generic interface definition</returns>
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
        /// 向 dynamic 数据写入指定键的值 /
        /// Assign a value to the specified key of dynamic data
        /// </summary>
        /// <param name="data">dynamic 数据（须可转为 IDictionary&lt;string, T&gt;，如 ExpandoObject）/ The dynamic data (must be castable to IDictionary&lt;string, T&gt;, e.g. ExpandoObject)</param>
        /// <param name="key">键名 / The key</param>
        /// <param name="obj">要写入的值 / The value to assign</param>
        /// <returns>原 dynamic 数据（便于链式调用）/ The original dynamic data (for chaining)</returns>
        /// <remarks>data 未实现相应字典接口时不做任何写入，静默返回原对象。/ No write happens when data does not implement the matching dictionary interface; the original object is returned silently.</remarks>
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
        /// 向 dynamic 数据写入指定键的值（object 重载）/
        /// Assign a value to the specified key of dynamic data (object overload)
        /// </summary>
        /// <param name="data">dynamic 数据（须可转为 IDictionary&lt;string, object&gt;，如 ExpandoObject）/ The dynamic data (must be castable to IDictionary&lt;string, object&gt;, e.g. ExpandoObject)</param>
        /// <param name="key">键名 / The key</param>
        /// <param name="obj">要写入的值 / The value to assign</param>
        /// <returns>原 dynamic 数据（便于链式调用）/ The original dynamic data (for chaining)</returns>
        /// <remarks>data 未实现相应字典接口时不做任何写入，静默返回原对象。/ No write happens when data does not implement the matching dictionary interface; the original object is returned silently.</remarks>
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
        /// 读取 dynamic 数据中指定键的值并转换为目标类型 /
        /// Get the value of the specified key of dynamic data and convert it to the target type
        /// </summary>
        /// <typeparam name="T">目标类型 / The target type</typeparam>
        /// <param name="data">dynamic 数据 / The dynamic data</param>
        /// <param name="key">键名 / The key</param>
        /// <returns>转换后的值；键不存在或值为 null 时返回 default(T) / The converted value; default(T) when the key is missing or the value is null</returns>
        /// <remarks>值为值类型、枚举或 Guid 时经字符串往返（TryParse）转换，其余直接强制转换。/ Value-type, enum and Guid values are converted via a string round trip (TryParse); others are cast directly.</remarks>
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
        /// 读取 dynamic 数据中指定键的值 /
        /// Get the value of the specified key of dynamic data
        /// </summary>
        /// <param name="data">dynamic 数据 / The dynamic data</param>
        /// <param name="key">键名 / The key</param>
        /// <returns>键存在时返回对应值；data 为 null 或键不存在时返回 null / The value when the key exists; null when data is null or the key is missing</returns>
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
        /// 从实例列表中提取指定属性的值列表 /
        /// Retrieve the list of values of the specified property from the specified instance list
        /// </summary>
        /// <typeparam name="T">实例类型 / The instance type</typeparam>
        /// <typeparam name="P">属性值类型 / The property value type</typeparam>
        /// <param name="list">实例列表 / The list of instances</param>
        /// <param name="propertyName">属性名 / The property name</param>
        /// <param name="defaultItemValue">结果为空列表时的兜底默认值（经 TryParse 转为 P）/ The fallback value used when the result would be empty (converted to P via TryParse)</param>
        /// <param name="unique">是否对结果去重，默认去重 / Whether to deduplicate the result; true by default</param>
        /// <returns>属性值列表 / The list of property values</returns>
        /// <remarks>属性值为 null 的项被跳过；属性值经 EmitTool 反射读取。/ Items whose property value is null are skipped; property values are read via EmitTool reflection.</remarks>
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
        /// 判断对象的运行时类型是否为枚举 /
        /// Check whether the object's runtime type is an enum
        /// </summary>
        /// <param name="obj">待判断对象 / The object to check</param>
        /// <returns>运行时类型为枚举返回 true / true when the runtime type is an enum</returns>
        public static bool IsEnum(object obj)
        {
            Type type = obj.GetType();
            return type.IsEnum;
        }

        /// <summary>
        /// 判断对象是否为数值 /
        /// Check whether the object is a numeric value
        /// </summary>
        /// <param name="obj">待判断对象 / The object to check</param>
        /// <returns>运行时类型为数值类型返回 true / true when the runtime type is numeric</returns>
        public static bool IsNumber(object obj)
        {
            Type type = obj.GetType();
            return IsNumberType(type);
        }

        /// <summary>
        /// 判断类型是否为数字类型（整型、浮点型等；可空类型先取底层类型）/
        /// Check whether the type is numeric (integer, floating point, etc.; nullable types are resolved to their underlying type first)
        /// </summary>
        /// <param name="type">待判断类型 / The type to check</param>
        /// <returns>数字类型返回 true / true for numeric types</returns>
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
        /// 将对象转换为字典：字典实例按键值对搬运，其余对象按运行时公开属性展开 /
        /// Convert an object to a dictionary: dictionary instances are copied entry by entry, other objects are expanded by their runtime public properties
        /// </summary>
        /// <param name="obj">待转换对象 / The object to convert</param>
        /// <returns>键值字典；obj 为 null、值类型或字符串时返回 null / The dictionary; null when obj is null, a value type or a string</returns>
        /// <remarks>属性列表基于 obj.GetType()（运行时类型），属性值经 EmitTool 反射读取。/ The property list is based on obj.GetType() (the runtime type); property values are read via EmitTool reflection.</remarks>
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
        /// 将模型实例转换为字典：字典实例按键值对搬运，其余实例按声明类型的公开属性展开 /
        /// Convert a model instance to a dictionary: dictionary instances are copied entry by entry, others are expanded by the public properties of the declared type
        /// </summary>
        /// <typeparam name="T">模型类型（引用类型）/ The model type (reference type)</typeparam>
        /// <param name="model">模型实例 / The model instance</param>
        /// <returns>属性字典；model 为 null 时返回 null / The property dictionary; null when model is null</returns>
        /// <remarks>属性列表基于 typeof(T)（声明类型）而非运行时类型，与返回基类引用的实例配合时只会展开 T 上定义的属性。/ The property list is based on typeof(T) (the declared type) rather than the runtime type; with an instance held as a base-class reference, only the properties defined on T are expanded.</remarks>
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
