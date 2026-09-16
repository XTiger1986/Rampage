using Rampage.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Rampage.Reflection
{
    /// <summary>
    /// 反射相关助手
    /// </summary>
    public class ReflectionTool
    {
        /// <summary>
        /// 
        /// </summary>
        public static Assembly? EntryAssembly
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string? EntryAssemblyName
        {
            get;
            private set;
        }

        static ReflectionTool()
        {
            EntryAssembly = Assembly.GetEntryAssembly();
            EntryAssemblyName = EntryAssembly!.GetName().Name!;
        }

        /// <summary>
        /// get essential essemblies list
        /// </summary>
        /// <returns></returns>
        public static List<Assembly> GetEssentialAssemblies()
        {
            DirectoryInfo rootDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            FileInfo[] dllFiles = rootDir.GetFiles("*dll");
            dllFiles = dllFiles.Where(f => !StringTool.StartsWithIgnoreCase(f.Name, "Microsoft.") && !StringTool.StartsWithIgnoreCase(f.Name, "System.")).ToArray();
            List<Assembly> assemblies = new List<Assembly>();
            foreach (FileInfo dllFile in dllFiles)
            {
                Assembly assembly = Assembly.LoadFrom(dllFile.FullName);
                assemblies.Add(assembly);
            }

            return assemblies;
        }

        #region TypeDescriptor

        /// <summary>
        /// 类型详情字典
        /// </summary>
        private static readonly ConcurrentDictionary<string, TypeDescriptor> TypeDescriptorDict = new ConcurrentDictionary<string, TypeDescriptor>();

        /// <summary>
        /// 获取类型详情
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TypeDescriptor GetTypeDescriptor(Type type)
        {
            string typeName = type.FullName!;
            return TypeDescriptorDict.GetOrAdd(typeName, _ => new TypeDescriptor
            {
                InterfaceList = type.GetInterfaces().ToList(),
                PropertyList = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList(),
                FieldList = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList(),
                MethodList = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList(),
                CustomAttributes = type.GetCustomAttributes(true).ToList()
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Type> GetInterfaceList(Type type)
        {
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            return typeDetail.InterfaceList!;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="includeStatic"></param>
        /// <returns></returns>
        public static List<PropertyInfo> GetPropertyList(Type type, bool includeStatic = true)
        {
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            List<PropertyInfo> list = typeDetail.PropertyList!;
            if (!includeStatic)
            {
                return list.FindAll(p => (p.GetMethod != null && !p.GetMethod.IsStatic) || (p.SetMethod != null && !p.SetMethod.IsStatic));
            }
            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<object> GetCustomAttributeList(Type type)
        {
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            return typeDetail.CustomAttributes!;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static T? GetCustomAttribute<T>(Type type)
            where T : Attribute
        {
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            var attrs = typeDetail.CustomAttributes!;
            foreach (var attr in attrs)
            {
                T? t = attr as T;
                if (t != null)
                {
                    return t;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Type.GetProperty的性能优化版
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static PropertyInfo? GetProperty(Type type, string propertyName)
        {
            PropertyInfo? propertyInfo = null;
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            propertyInfo = typeDetail.PropertyList!.Find(p => p.Name == propertyName);

            return propertyInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="includeStatic"></param>
        /// <returns></returns>
        public static List<FieldInfo> GetFieldList(Type type, bool includeStatic = true)
        {
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            List<FieldInfo> list = typeDetail.FieldList!;
            if (!includeStatic)
            {
                return list.FindAll(f => !f.IsStatic);
            }
            return list;
        }

        /// <summary>
        /// Type.GetField的性能优化版
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static FieldInfo? GetField(Type type, string fieldName)
        {
            FieldInfo? fieldInfo = null;
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            fieldInfo = typeDetail.FieldList!.Find(p => p.Name == fieldName);

            return fieldInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="includeStatic"></param>
        /// <returns></returns>
        public static List<MethodInfo> GetMethodList(Type type, bool includeStatic = true)
        {
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            List<MethodInfo> list = typeDetail.MethodList!;
            if (!includeStatic)
            {
                return list.FindAll(f => !f.IsStatic);
            }
            return list;
        }

        /// <summary>
        /// Type.GetProperty的性能优化版
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static MethodInfo? GetMethod(Type type, string methodName)
        {
            MethodInfo? methodInfo = null;
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            methodInfo = typeDetail.MethodList!.Find(m => m.Name == methodName);

            return methodInfo;
        }

        #endregion

        /// <summary>
        /// 获取列表元素类型
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Type GetListElementType(IEnumerable list)
        {
            return list.AsQueryable().ElementType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="attrType"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static object? GetCustomAttribute(PropertyInfo propertyInfo, Type attrType, bool inherit = true)
        {
            if (!propertyInfo.IsDefined(attrType, inherit))
            {
                return null;
            }
            var attrs = propertyInfo.GetCustomAttributes(attrType, inherit);
            if (attrs != null && attrs.Any())
            {
                return attrs[0];
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static T? GetCustomAttribute<T>(PropertyInfo propertyInfo, bool inherit = true)
        {
            Type attrType = typeof(T);
            object? o = GetCustomAttribute(propertyInfo, attrType, inherit);
            if (o != null)
            {
                return (T)o;
            }
            return default(T);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <param name="attrType"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static object? GetCustomAttribute(FieldInfo fieldInfo, Type attrType, bool inherit = true)
        {
            if (!fieldInfo.IsDefined(attrType, inherit))
            {
                return null;
            }
            var attrs = fieldInfo.GetCustomAttributes(attrType, inherit);
            if (attrs != null && attrs.Any())
            {
                return attrs[0];
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static T? GetCustomAttribute<T>(FieldInfo fieldInfo, bool inherit = true)
        {
            Type attrType = typeof(T);
            object? o = GetCustomAttribute(fieldInfo, attrType, inherit);
            if (o != null)
            {
                return (T)o;
            }
            return default(T);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="attrType"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static object? GetCustomAttribute(MethodInfo methodInfo, Type attrType, bool inherit = true)
        {
            if (!methodInfo.IsDefined(attrType, inherit))
            {
                return null;
            }
            var attrs = methodInfo.GetCustomAttributes(attrType, inherit);
            if (attrs != null && attrs.Any())
            {
                return attrs[0];
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static T? GetCustomAttribute<T>(MethodInfo methodInfo, bool inherit = true)
        {
            Type attrType = typeof(T);
            object? o = GetCustomAttribute(methodInfo, attrType, inherit);
            if (o != null)
            {
                return (T)o;
            }
            return default(T);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <param name="attrType"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static object? GetCustomAttribute(ParameterInfo parameterInfo, Type attrType, bool inherit = true)
        {
            if (!parameterInfo.IsDefined(attrType, inherit))
            {
                return null;
            }
            var attrs = parameterInfo.GetCustomAttributes(attrType, inherit);
            if (attrs != null && attrs.Any())
            {
                return attrs[0];
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static T? GetCustomAttribute<T>(ParameterInfo parameterInfo, bool inherit = true)
        {
            Type attrType = typeof(T);
            object? o = GetCustomAttribute(parameterInfo, attrType, inherit);
            if (o != null)
            {
                return (T)o;
            }
            return default(T);
        }

        /// <summary>
        /// try Returns a System.Type object that represents a generic type definition from which  the current generic type can be constructed.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type? GetGenericTypeDefinition(Type type)
        {
            Type? genTypeDef = null;
            if (type.IsGenericType)
            {
                genTypeDef = type.GetGenericTypeDefinition();
            }
            return genTypeDef;
        }

        /// <summary>
        /// check if typeToCheck is sub interface of genericBaseType
        /// </summary>
        /// <param name="typeToCheck"></param>
        /// <param name="genericBaseType"></param>
        /// <returns></returns>
        public static bool IsSubInterface(Type typeToCheck, Type genericBaseType)
        {
            var interfaces = typeToCheck.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericBaseType)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// 拷贝源对象所有匹配属性到目标对象（浅拷贝）
        /// </summary>
        public static void CopyProperties(object source, object target, bool ignoreNull = false)
        {
            CopyProperties(source, target, ignoreNull, ignoreFields: null, includeFields: null);
        }

        /// <summary>
        /// 拷贝并忽略指定字段
        /// </summary>
        public static void CopyProperties(object source, object target, bool ignoreNull = false, params string[] ignoreFields)
        {
            CopyProperties(source, target, ignoreNull, ignoreFields, null);
        }

        /// <summary>
        /// 只拷贝指定字段
        /// </summary>
        public static void CopyPropertiesInclude(object source, object target, bool ignoreNull = false, params string[] includeFields)
        {
            CopyProperties(source, target, ignoreNull, null, includeFields);
        }

        /// <summary>
        /// 新建目标实例并拷贝属性
        /// </summary>
        public static TTarget Copy<TSource, TTarget>(TSource source, bool ignoreNull = false, params string[] ignoreFields)
            where TTarget : new()
        {
            var target = new TTarget();
            CopyProperties(source!, target, ignoreNull, ignoreFields);
            return target;
        }

        /// <summary>
        /// 只拷贝指定字段新建对象
        /// </summary>
        public static TTarget CopyInclude<TSource, TTarget>(TSource source, bool ignoreNull = false, params string[] includeFields)
            where TTarget : new()
        {
            var target = new TTarget();
            CopyPropertiesInclude(source!, target, ignoreNull, includeFields);
            return target;
        }

        private static void CopyProperties(object source, object target, bool ignoreNull, string[]? ignoreFields, string[]? includeFields)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            Type srcType = source.GetType();
            Type destType = target.GetType();

            // 读取并缓存所有公共可读属性
            var srcProps = GetPropertyList(srcType);
            var destProps = GetPropertyList(destType);

            HashSet<string> ignoreSet = ignoreFields?.ToHashSet() ?? new();
            HashSet<string> includeSet = includeFields?.ToHashSet() ?? new();

            foreach (var srcProp in srcProps)
            {
                string propName = srcProp.Name;

                // 只包含模式过滤
                if (includeSet != null && includeSet.Count > 0 && !includeSet.Contains(propName))
                    continue;
                // 忽略字段过滤
                if (ignoreSet.Contains(propName))
                    continue;

                // 目标存在同名可写属性
                var destProp = destProps.FirstOrDefault(p => p.Name == propName);
                if (destProp == null || !destProp.CanWrite) continue;

                object? srcValue = srcProp.GetValue(source);
                if (srcValue == null)
                {
                    if (ignoreNull)
                    {
                        continue;
                    }
                }
                object? destValue = ConvertValue(srcValue, srcProp.PropertyType, destProp.PropertyType);
                destProp.SetValue(target, destValue);
            }
        }

        /// <summary>
        /// 自动兼容常见类型转换：string↔int、bool、DateTime、枚举、可空类型等
        /// </summary>
        private static object? ConvertValue(object? value, Type srcType, Type destType)
        {
            // 源值为空
            if (value == null)
            {
                // 目标是可空类型直接返回null
                return destType.IsValueType && Nullable.GetUnderlyingType(destType) == null
                    ? Activator.CreateInstance(destType) // 值类型赋默认值
                    : null;
            }

            // 类型完全一致无需转换
            if (srcType == destType)
                return value;

            Type destUnderlying = Nullable.GetUnderlyingType(destType) ?? destType;
            object rawVal = value;

            // 枚举转换
            if (destUnderlying.IsEnum)
            {
                if (rawVal is string str)
                    return Enum.Parse(destUnderlying, str, ignoreCase: true);
                return Enum.ToObject(destUnderlying, rawVal);
            }

            // 通用类型转换器
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(destUnderlying);
            if (converter.CanConvertFrom(srcType))
                return converter.ConvertFrom(rawVal);

            var srcConverter = System.ComponentModel.TypeDescriptor.GetConverter(srcType);
            if (srcConverter.CanConvertTo(destUnderlying))
                return srcConverter.ConvertTo(rawVal, destUnderlying);

            // 基础数字/字符串兜底转换
            return Convert.ChangeType(rawVal, destUnderlying);
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static TTarget? DeepCopy<TSource, TTarget>(TSource source)
        {
            if (source == null) return default;

            var options = new JsonSerializerOptions
            {
                // 处理循环引用（若存在）
                ReferenceHandler = ReferenceHandler.Preserve,
                // 支持私有字段（若需要）
                IncludeFields = false,
            };

            string json = JsonSerializer.Serialize(source, options);
            return JsonSerializer.Deserialize<TTarget>(json, options);
        }

    }
}
