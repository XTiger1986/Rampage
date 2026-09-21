using Rampage.Reflection;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 自动注入方案存储：按宿主类型缓存 [Auto] 成员扫描结果，并维护服务名/条件映射 / Auto-injection schema store: caches [Auto] member scan results per host type and maintains service-name/conditional mappings.
    /// </summary>
    public class AutoInjectionSchemaStore
    {
        static AutoInjectionSchemaStore()
        {
            SchemaContainer = new ConcurrentDictionary<string, AutoInjectionSchema?>(StringComparer.Ordinal);
            ServiceNameMappings = new ConcurrentDictionary<string, Type>(StringComparer.Ordinal);
            ConditionalMappings = new ConcurrentDictionary<string, Type>(StringComparer.Ordinal);
        }

        /// <summary>
        /// 宿主类型全名到注入方案的缓存容器 / Cache of host type full names to their injection schemas.
        /// </summary>
        public static ConcurrentDictionary<string, AutoInjectionSchema?> SchemaContainer
        {
            get;
            private set;
        }

        /// <summary>
        /// 服务名到注入实现类型的映射 / Mapping from service name to injection implementation type.
        /// </summary>
        internal static ConcurrentDictionary<string, Type> ServiceNameMappings
        {
            get;
            private set;
        }

        /// <summary>
        /// 条件注入映射：基类型到经 <see cref="IConditionalHandler"/> 选出的实现类型 / Conditional injection mapping: base type to the implementation selected by an <see cref="IConditionalHandler"/>.
        /// </summary>
        internal static ConcurrentDictionary<string, Type> ConditionalMappings
        {
            get;
            private set;
        }

        /// <summary>
        /// 扫描类型的实例属性与字段，收集 [Auto] 特性生成注入方案 / Scans the type's instance properties and fields to build an injection schema from [Auto] attributes.
        /// </summary>
        /// <param name="type">待扫描类型 / The type to scan.</param>
        /// <returns>含至少一个注入成员的方案；无成员返回 null / A schema with at least one member, or null when none.</returns>
        /// <exception cref="Exception">属性缺少 Get/Set 或使用 private 修饰 / A property lacks a getter/setter or uses the private modifier.</exception>
        private static AutoInjectionSchema? GetSchemaFromType(Type type)
        {
            AutoInjectionSchema schema = new AutoInjectionSchema();
            schema.HostType = type;
            schema.AutoList = new List<Auto>();

            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            if (propertyInfos != null && propertyInfos.Length > 0)
            {
                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    if (!propertyInfo.IsDefined(typeof(Auto), true))
                    {
                        continue;
                    }
                    if (propertyInfo.SetMethod == null || propertyInfo.GetMethod == null)
                    {
                        throw new Exception($"The automatically injected properties must include Get and Set methods: {type.FullName}.{propertyInfo.Name}");
                    }
                    if (propertyInfo.SetMethod.IsPrivate || propertyInfo.GetMethod.IsPrivate)
                    {
                        throw new Exception($"The property injected automatically cannot use the private modifier: {type.FullName}.{propertyInfo.Name}");
                    }

                    Auto attr = ReflectionTool.GetCustomAttribute<Auto>(propertyInfo)!;
                    attr.Property = propertyInfo;
                    schema.AutoList.Add(attr);
                }
            }

            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField);
            if (fieldInfos != null && fieldInfos.Length > 0)
            {
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    if (!fieldInfo.IsDefined(typeof(Auto), true))
                    {
                        continue;
                    }
                    if (fieldInfo.IsPrivate)
                    {
                        throw new Exception($"The field injected automatically cannot use the private modifier: {type.FullName}.{fieldInfo.Name}");
                    }

                    Auto attr = ReflectionTool.GetCustomAttribute<Auto>(fieldInfo)!;
                    attr.Field = fieldInfo;
                    schema.AutoList.Add(attr);
                }
            }

            if (schema.AutoList.Any())
            {
                return schema;
            }
            return null;
        }

        /// <summary>
        /// 获取宿主类型的 Auto 注入方案（线程安全缓存）/ Gets the [Auto] injection schema of a host type (thread-safe, cached).
        /// </summary>
        /// <param name="hostType">宿主类型 / The host type.</param>
        /// <returns>注入方案；无注入成员时为 null / The schema, or null when the type has no [Auto] members.</returns>
        public static AutoInjectionSchema? GetSchema(Type hostType)
        {
            string key = hostType.FullName ?? hostType.Name;
            return SchemaContainer.GetOrAdd(key, _ => GetSchemaFromType(hostType));
        }

        /// <summary>
        /// 旧方法名（拼写错误），转发到 <see cref="GetSchema"/> / Obsolete misspelled alias that forwards to <see cref="GetSchema"/>.
        /// </summary>
        /// <param name="hostType">宿主类型 / The host type.</param>
        /// <returns>注入方案 / The schema.</returns>
        [Obsolete("Use GetSchema instead.")]
        public static AutoInjectionSchema? GeSchema(Type hostType) => GetSchema(hostType);

        /// <summary>
        /// 登记服务名到实现类型的映射；同名映射到不同类型时抛出异常 / Registers a service-name mapping; throws when the same name maps to a different type.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <param name="injectType">实现类型 / The implementation type.</param>
        /// <exception cref="ArgumentException">服务名已映射到其他类型 / The service name is already mapped to another type.</exception>
        internal static void AddServiceName(string serviceName, Type injectType)
        {
            if (ServiceNameMappings.TryGetValue(serviceName, out Type? existsType))
            {
                if (existsType != injectType)
                {
                    throw new ArgumentException($"Service Name '{serviceName}' already exists!", serviceName);
                }
                return;
            }

            // 竞态兜底：TryAdd 失败后重读，仅当映射到不同类型时才视为冲突
            // Race guard: re-read after a failed TryAdd; only treat it as a conflict when mapped to a different type
            if (!ServiceNameMappings.TryAdd(serviceName, injectType)
                && ServiceNameMappings.TryGetValue(serviceName, out existsType)
                && existsType != injectType)
            {
                throw new ArgumentException($"Service Name '{serviceName}' already exists!", serviceName);
            }
        }

        /// <summary>
        /// 依据候选实现的 <see cref="ServiceAttribute.ConditionalHandler"/> 选定基类型的注入实现 / Selects the implementation for a base type via each candidate's <see cref="ServiceAttribute.ConditionalHandler"/>.
        /// </summary>
        /// <param name="baseType">目标基类型 / The target base type.</param>
        /// <param name="injectTypeList">候选实现类型列表 / The candidate implementation types.</param>
        internal static void AddConditionalService(Type baseType, List<Type> injectTypeList)
        {
            string key = $"{baseType.Name}.{baseType.Name}";
            foreach (Type injectType in injectTypeList)
            {
                ServiceAttribute? service = ReflectionTool.GetCustomAttribute<ServiceAttribute>(injectType);
                if (service == null || service.ConditionalHandler == null)
                {
                    continue;
                }

                IConditionalHandler conditionalHandler = service.ConditionalHandler;
                bool isMatch = conditionalHandler.IsMatch(baseType, injectType);
                if (isMatch)
                {
                    ConditionalMappings.TryAdd(key, injectType);
                    break;
                }
            }
        }

        /// <summary>
        /// 按服务名查询映射的实现类型 / Looks up the implementation type mapped to a service name.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        /// <returns>实现类型；未登记返回 null / The implementation type, or null when unregistered.</returns>
        public static Type? GetServiceType(string serviceName)
        {
            if (ServiceNameMappings.TryGetValue(serviceName, out Type? type))
            {
                return type;
            }
            return null;
        }

        /// <summary>
        /// 查询基类型经条件处理器选定的实现类型 / Looks up the conditionally-selected implementation type for a base type.
        /// </summary>
        /// <param name="baseType">基类型 / The base type.</param>
        /// <returns>选定的实现类型；无匹配返回 null / The selected implementation type, or null when none matched.</returns>
        public static Type? GetConditionalType(Type baseType)
        {
            string key = $"{baseType.Name}.{baseType.Name}";
            if (ConditionalMappings.TryGetValue(key, out Type? type))
            {
                return type;
            }
            return null;
        }

    }
}
