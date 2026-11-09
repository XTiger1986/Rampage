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
    /// 反射相关助手：集中缓存类型元数据（接口/属性/字段/方法/类级特性），提供高性能成员查找、属性拷贝与深拷贝等工具
    /// Reflection helper: caches type metadata (interfaces/properties/fields/methods/class-level attributes) and provides fast member lookup, property copying and deep copy utilities
    /// </summary>
    public class ReflectionTool
    {
        /// <summary>
        /// 进程入口程序集（由非托管宿主启动时可能为 null） / The process entry assembly (may be null when hosted by unmanaged code)
        /// </summary>
        public static Assembly? EntryAssembly
        {
            get;
            private set;
        }

        /// <summary>
        /// 入口程序集名称 / The entry assembly name
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
        /// 获取依赖注入扫描所需的程序集：应用目录扫描（松散部署）+ 入口程序集静态引用闭包 + 已加载程序集（单文件发布兜底）三来源按简单名去重合并。
        /// Collect assemblies for DI scanning by merging three sources (deduplicated by simple name): app-directory scan (loose deployment), static reference closure from the entry assembly, and already-loaded assemblies (single-file fallback).
        /// </summary>
        /// <returns>去重后的程序集列表（目录扫描结果在前，顺序不变） / The deduplicated assemblies (directory-scan results first, order preserved)</returns>
        /// <remarks>
        /// 单文件发布(PublishSingleFile)时托管 DLL 全部打包进 exe，应用目录里没有 DLL，目录扫描为空；
        /// 此时由入口程序集的静态引用闭包(Assembly.Load 按名加载，单文件宿主自动从 bundle 解析)与已加载程序集兜底，保证 DI/实体映射/权限发现不失效。
        /// 松散部署下目录扫描结果始终排在最前且顺序不变，手工放置 DLL 的既有行为完全不受影响。
        /// Under single-file publish all managed DLLs are bundled inside the exe so the directory scan finds nothing;
        /// the reference closure (loaded by name, resolved from the bundle by the single-file host) plus already-loaded assemblies cover that case.
        /// In loose deployments the directory scan still comes first in unchanged order, so drop-in DLLs keep working exactly as before.
        /// </remarks>
        public static List<Assembly> GetEssentialAssemblies()
        {
            Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            List<Assembly> result = new List<Assembly>();

            // 来源1：应用目录扫描 —— 松散部署下的原有行为，结果顺序保持不变
            // Source 1: app-directory scan - original behaviour for loose deployments, order preserved
            DirectoryInfo rootDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            if (rootDir.Exists)
            {
                FileInfo[] dllFiles = rootDir.GetFiles("*dll");
                dllFiles = dllFiles.Where(f => !StringTool.StartsWithIgnoreCase(f.Name, "Microsoft.") && !StringTool.StartsWithIgnoreCase(f.Name, "System.")).ToArray();
                foreach (FileInfo dllFile in dllFiles)
                {
                    Assembly assembly = Assembly.LoadFrom(dllFile.FullName);
                    if (assemblies.TryAdd(assembly.GetName().Name!, assembly))
                    {
                        result.Add(assembly);
                    }
                }
            }

            // 来源2：入口程序集静态引用闭包(BFS) —— 单文件发布时的主要来源；按名加载失败的可选/平台引用直接跳过
            // Source 2: static reference closure (BFS) from the entry assembly - primary source under single-file publish; optional/platform-specific references that fail to load are skipped
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Queue<AssemblyName> pending = new Queue<AssemblyName>();
            Assembly entry = EntryAssembly ?? typeof(ReflectionTool).Assembly;
            pending.Enqueue(entry.GetName());
            while (pending.Count > 0)
            {
                AssemblyName assemblyName = pending.Dequeue();
                if (!visited.Add(assemblyName.Name!))
                {
                    continue;
                }
                if (StringTool.StartsWithIgnoreCase(assemblyName.Name, "Microsoft.") || StringTool.StartsWithIgnoreCase(assemblyName.Name, "System."))
                {
                    continue;
                }

                Assembly assembly;
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch
                {
                    continue;
                }

                if (assemblies.TryAdd(assembly.GetName().Name!, assembly))
                {
                    result.Add(assembly);
                }

                foreach (AssemblyName childName in assembly.GetReferencedAssemblies())
                {
                    if (!visited.Contains(childName.Name!))
                    {
                        pending.Enqueue(childName);
                    }
                }
            }

            // 来源3：已加载程序集 —— 兜住经 JIT 或其他机制动态加载而未进闭包的程序集
            // Source 3: already-loaded assemblies - catches those loaded dynamically (JIT or otherwise) outside the closure
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string? assemblySimpleName = assembly.GetName().Name;
                if (assemblySimpleName == null || StringTool.StartsWithIgnoreCase(assemblySimpleName, "Microsoft.") || StringTool.StartsWithIgnoreCase(assemblySimpleName, "System."))
                {
                    continue;
                }
                if (assemblies.TryAdd(assemblySimpleName, assembly))
                {
                    result.Add(assembly);
                }
            }

            return result;
        }

        #region TypeDescriptor

        /// <summary>
        /// 类型详情缓存按运行时 Type 隔离，防止不同程序集的同名匿名类型混用 SQL 参数元数据。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, TypeDescriptor> TypeDescriptorDict = new ConcurrentDictionary<Type, TypeDescriptor>();

        /// <summary>
        /// 获取类型详情（含 NonPublic 成员），按运行时 Type 缓存，保留程序集和加载上下文身份。
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <returns>类型的元数据描述 / The type descriptor</returns>
        /// <remarks>
        /// 首次访问时一次性反射收集：全部接口、实例+静态、公共+非公共的属性/字段/方法以及类级自定义特性，之后全部走缓存。
        /// On first access the metadata (all interfaces; instance/static, public/non-public members; class-level attributes) is collected once via reflection, then served from cache.
        /// </remarks>
        public static TypeDescriptor GetTypeDescriptor(Type type)
        {
            return TypeDescriptorDict.GetOrAdd(type, _ => new TypeDescriptor
            {
                InterfaceList = type.GetInterfaces().ToList(),
                PropertyList = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList(),
                FieldList = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList(),
                MethodList = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList(),
                CustomAttributes = type.GetCustomAttributes(true).ToList()
            });
        }

        /// <summary>
        /// 获取类型实现（含继承链）的全部接口 / Get all interfaces implemented by the type (including inherited ones)
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <returns>接口列表 / The interface list</returns>
        public static List<Type> GetInterfaceList(Type type)
        {
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            return typeDetail.InterfaceList!;
        }

        /// <summary>
        /// 获取类型的属性列表（含 NonPublic） / Get the property list of the type (including non-public properties)
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <param name="includeStatic">是否包含静态属性，默认 true / Whether to include static properties, default true</param>
        /// <returns>属性列表 / The property list</returns>
        /// <remarks>includeStatic=false 时仅保留 getter 或 setter 至少一端为实例方法的属性 / When false, keeps only properties whose getter or setter is an instance method</remarks>
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
        /// 获取类型的全部类级自定义特性（含继承的特性实例） / Get all class-level custom attributes of the type (including inherited attribute instances)
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <returns>特性实例列表 / The attribute instances</returns>
        public static List<object> GetCustomAttributeList(Type type)
        {
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            return typeDetail.CustomAttributes!;
        }

        /// <summary>
        /// 获取类型上第一个指定类型的类级特性 / Get the first class-level attribute of the specified type on the type
        /// </summary>
        /// <typeparam name="T">特性类型 / The attribute type</typeparam>
        /// <param name="type">目标类型 / The target type</param>
        /// <returns>特性实例，不存在时为 null / The attribute instance, or null when absent</returns>
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
        /// Type.GetProperty 的性能优化版 / Performance-optimized version of Type.GetProperty
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <param name="propertyName">属性名 / The property name</param>
        /// <returns>匹配的属性，未找到时为 null / The matching property, or null when not found</returns>
        /// <remarks>在缓存的属性列表中按名称线性查找（不区分可访问性） / Performs a linear name search over the cached property list, regardless of accessibility</remarks>
        public static PropertyInfo? GetProperty(Type type, string propertyName)
        {
            PropertyInfo? propertyInfo = null;
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            propertyInfo = typeDetail.PropertyList!.Find(p => p.Name == propertyName);

            return propertyInfo;
        }

        /// <summary>
        /// 获取类型的字段列表（含 NonPublic 与编译器生成字段） / Get the field list of the type (including non-public and compiler-generated fields)
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <param name="includeStatic">是否包含静态字段，默认 true / Whether to include static fields, default true</param>
        /// <returns>字段列表 / The field list</returns>
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
        /// Type.GetField 的性能优化版 / Performance-optimized version of Type.GetField
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <param name="fieldName">字段名 / The field name</param>
        /// <returns>匹配的字段，未找到时为 null / The matching field, or null when not found</returns>
        /// <remarks>在缓存的字段列表中按名称线性查找，可命中私有与编译器生成字段 / Linear name search over the cached field list; can match private and compiler-generated fields</remarks>
        public static FieldInfo? GetField(Type type, string fieldName)
        {
            FieldInfo? fieldInfo = null;
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            fieldInfo = typeDetail.FieldList!.Find(p => p.Name == fieldName);

            return fieldInfo;
        }

        /// <summary>
        /// 获取类型的方法列表（含 NonPublic 与继承方法） / Get the method list of the type (including non-public and inherited methods)
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <param name="includeStatic">是否包含静态方法，默认 true / Whether to include static methods, default true</param>
        /// <returns>方法列表 / The method list</returns>
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
        /// Type.GetMethod 的性能优化版 / Performance-optimized version of Type.GetMethod
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <param name="methodName">方法名 / The method name</param>
        /// <returns>匹配的方法，未找到时为 null / The matching method, or null when not found</returns>
        /// <remarks>在缓存的方法列表中按名称线性查找，同名重载时返回列表中的第一个 / Linear name search over the cached method list; returns the first match when overloads share a name</remarks>
        public static MethodInfo? GetMethod(Type type, string methodName)
        {
            MethodInfo? methodInfo = null;
            TypeDescriptor typeDetail = GetTypeDescriptor(type);
            methodInfo = typeDetail.MethodList!.Find(m => m.Name == methodName);

            return methodInfo;
        }

        #endregion

        /// <summary>
        /// 获取列表的元素类型 / Get the element type of the list
        /// </summary>
        /// <param name="list">列表对象 / The list instance</param>
        /// <returns>元素类型 / The element type</returns>
        public static Type GetListElementType(IEnumerable list)
        {
            return list.AsQueryable().ElementType;
        }

        /// <summary>
        /// 获取属性上第一个指定类型的特性 / Get the first attribute of the specified type on a property
        /// </summary>
        /// <param name="propertyInfo">目标属性 / The target property</param>
        /// <param name="attrType">特性类型 / The attribute type</param>
        /// <param name="inherit">是否查找继承链，默认 true / Whether to search the inheritance chain, default true</param>
        /// <returns>特性实例，不存在时为 null / The attribute instance, or null when absent</returns>
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
        /// 获取属性上第一个指定类型的特性 / Get the first attribute of the specified type on a property
        /// </summary>
        /// <typeparam name="T">特性类型 / The attribute type</typeparam>
        /// <param name="propertyInfo">目标属性 / The target property</param>
        /// <param name="inherit">是否查找继承链，默认 true / Whether to search the inheritance chain, default true</param>
        /// <returns>特性实例，不存在时为 null / The attribute instance, or null when absent</returns>
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
        /// 获取字段上第一个指定类型的特性 / Get the first attribute of the specified type on a field
        /// </summary>
        /// <param name="fieldInfo">目标字段 / The target field</param>
        /// <param name="attrType">特性类型 / The attribute type</param>
        /// <param name="inherit">是否查找继承链，默认 true / Whether to search the inheritance chain, default true</param>
        /// <returns>特性实例，不存在时为 null / The attribute instance, or null when absent</returns>
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
        /// 获取字段上第一个指定类型的特性 / Get the first attribute of the specified type on a field
        /// </summary>
        /// <typeparam name="T">特性类型 / The attribute type</typeparam>
        /// <param name="fieldInfo">目标字段 / The target field</param>
        /// <param name="inherit">是否查找继承链，默认 true / Whether to search the inheritance chain, default true</param>
        /// <returns>特性实例，不存在时为 null / The attribute instance, or null when absent</returns>
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
        /// 获取方法上第一个指定类型的特性 / Get the first attribute of the specified type on a method
        /// </summary>
        /// <param name="methodInfo">目标方法 / The target method</param>
        /// <param name="attrType">特性类型 / The attribute type</param>
        /// <param name="inherit">是否查找继承链，默认 true / Whether to search the inheritance chain, default true</param>
        /// <returns>特性实例，不存在时为 null / The attribute instance, or null when absent</returns>
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
        /// 获取方法上第一个指定类型的特性 / Get the first attribute of the specified type on a method
        /// </summary>
        /// <typeparam name="T">特性类型 / The attribute type</typeparam>
        /// <param name="methodInfo">目标方法 / The target method</param>
        /// <param name="inherit">是否查找继承链，默认 true / Whether to search the inheritance chain, default true</param>
        /// <returns>特性实例，不存在时为 null / The attribute instance, or null when absent</returns>
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
        /// 获取参数上第一个指定类型的特性 / Get the first attribute of the specified type on a parameter
        /// </summary>
        /// <param name="parameterInfo">目标参数 / The target parameter</param>
        /// <param name="attrType">特性类型 / The attribute type</param>
        /// <param name="inherit">是否查找继承链，默认 true / Whether to search the inheritance chain, default true</param>
        /// <returns>特性实例，不存在时为 null / The attribute instance, or null when absent</returns>
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
        /// 获取参数上第一个指定类型的特性 / Get the first attribute of the specified type on a parameter
        /// </summary>
        /// <typeparam name="T">特性类型 / The attribute type</typeparam>
        /// <param name="parameterInfo">目标参数 / The target parameter</param>
        /// <param name="inherit">是否查找继承链，默认 true / Whether to search the inheritance chain, default true</param>
        /// <returns>特性实例，不存在时为 null / The attribute instance, or null when absent</returns>
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
        /// 尝试获取泛型类型的泛型定义；非泛型类型返回 null / Try to get the generic type definition of a generic type; returns null for non-generic types
        /// </summary>
        /// <param name="type">目标类型 / The target type</param>
        /// <returns>泛型类型定义；非泛型时为 null / The generic type definition, or null when the type is not generic</returns>
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
        /// 判断类型是否实现了指定的开放式泛型接口定义 / Check whether the type implements the given open generic interface definition
        /// </summary>
        /// <param name="typeToCheck">待检查类型 / The type to check</param>
        /// <param name="genericBaseType">开放式泛型接口定义 / The open generic interface definition</param>
        /// <returns>实现了该泛型接口返回 true / true when the generic interface is implemented</returns>
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
        /// 将源对象的所有匹配属性拷贝到目标对象（浅拷贝） / Copy all matching properties from the source to the target (shallow copy)
        /// </summary>
        /// <param name="source">源对象 / The source object</param>
        /// <param name="target">目标对象 / The target object</param>
        /// <param name="ignoreNull">是否跳过值为 null 的属性，默认 false / Whether to skip null-valued properties, default false</param>
        /// <remarks>按属性名匹配双方的可写属性；类型不一致时自动转换（枚举忽略大小写、TypeConverter、Convert.ChangeType 兜底）/ Properties are matched by name; values are auto-converted when types differ (case-insensitive enums, TypeConverter, then Convert.ChangeType)</remarks>
        public static void CopyProperties(object source, object target, bool ignoreNull = false)
        {
            CopyProperties(source, target, ignoreNull, ignoreFields: null, includeFields: null);
        }

        /// <summary>
        /// 拷贝属性并忽略指定名称的属性 / Copy properties while ignoring the given names
        /// </summary>
        /// <param name="source">源对象 / The source object</param>
        /// <param name="target">目标对象 / The target object</param>
        /// <param name="ignoreNull">是否跳过值为 null 的属性，默认 false / Whether to skip null-valued properties, default false</param>
        /// <param name="ignoreFields">要忽略的属性名集合 / Property names to ignore</param>
        public static void CopyProperties(object source, object target, bool ignoreNull = false, params string[] ignoreFields)
        {
            CopyProperties(source, target, ignoreNull, ignoreFields, null);
        }

        /// <summary>
        /// 仅拷贝指定名称的属性 / Copy only the specified properties
        /// </summary>
        /// <param name="source">源对象 / The source object</param>
        /// <param name="target">目标对象 / The target object</param>
        /// <param name="ignoreNull">是否跳过值为 null 的属性，默认 false / Whether to skip null-valued properties, default false</param>
        /// <param name="includeFields">要包含的属性名集合；为空集合时拷贝全部 / Property names to include; an empty set copies everything</param>
        public static void CopyPropertiesInclude(object source, object target, bool ignoreNull = false, params string[] includeFields)
        {
            CopyProperties(source, target, ignoreNull, null, includeFields);
        }

        /// <summary>
        /// 新建目标类型实例并拷贝源对象属性 / Create a new target instance and copy properties from the source
        /// </summary>
        /// <typeparam name="TSource">源类型 / The source type</typeparam>
        /// <typeparam name="TTarget">目标类型，须有无参构造 / The target type, must have a parameterless constructor</typeparam>
        /// <param name="source">源对象 / The source object</param>
        /// <param name="ignoreNull">是否跳过值为 null 的属性，默认 false / Whether to skip null-valued properties, default false</param>
        /// <param name="ignoreFields">要忽略的属性名集合 / Property names to ignore</param>
        /// <returns>填充后的新目标实例 / The populated new target instance</returns>
        public static TTarget Copy<TSource, TTarget>(TSource source, bool ignoreNull = false, params string[] ignoreFields)
            where TTarget : new()
        {
            var target = new TTarget();
            CopyProperties(source!, target, ignoreNull, ignoreFields);
            return target;
        }

        /// <summary>
        /// 新建目标类型实例并仅拷贝指定名称的属性 / Create a new target instance and copy only the specified properties
        /// </summary>
        /// <typeparam name="TSource">源类型 / The source type</typeparam>
        /// <typeparam name="TTarget">目标类型，须有无参构造 / The target type, must have a parameterless constructor</typeparam>
        /// <param name="source">源对象 / The source object</param>
        /// <param name="ignoreNull">是否跳过值为 null 的属性，默认 false / Whether to skip null-valued properties, default false</param>
        /// <param name="includeFields">要包含的属性名集合；为空集合时拷贝全部 / Property names to include; an empty set copies everything</param>
        /// <returns>填充后的新目标实例 / The populated new target instance</returns>
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

            // 获取属性列表（走类型缓存，含 NonPublic 成员）/ Get property lists via the type cache (non-public members included)
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
        /// / Auto-converts common types: string↔int, bool, DateTime, enums, nullables, etc.
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
        /// 深拷贝：将源对象序列化为 JSON 后再反序列化为目标类型 / Deep copy: serialize the source to JSON then deserialize into the target type
        /// </summary>
        /// <typeparam name="TSource">源类型 / The source type</typeparam>
        /// <typeparam name="TTarget">目标类型 / The target type</typeparam>
        /// <param name="source">源对象 / The source object</param>
        /// <returns>深拷贝得到的新实例；source 为 null 时返回默认值 / The deep-copied instance, or default when source is null</returns>
        /// <remarks>基于 System.Text.Json，ReferenceHandler.Preserve 保留引用以支持循环引用；仅处理公共属性，字段不参与 / Uses System.Text.Json with ReferenceHandler.Preserve for circular references; public properties only, fields excluded</remarks>
        public static TTarget? DeepCopy<TSource, TTarget>(TSource source)
        {
            if (source == null) return default;

            var options = new JsonSerializerOptions
            {
                // 处理循环引用（若存在） / Preserve references to survive circular references
                ReferenceHandler = ReferenceHandler.Preserve,
                // 不序列化字段（仅属性） / Fields are not serialized (properties only)
                IncludeFields = false,
            };

            string json = JsonSerializer.Serialize(source, options);
            return JsonSerializer.Deserialize<TTarget>(json, options);
        }

    }
}
