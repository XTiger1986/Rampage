using Rampage.Reflection;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 
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
        /// 
        /// </summary>
        public static ConcurrentDictionary<string, AutoInjectionSchema?> SchemaContainer
        {
            get;
            private set;
        }

        /// <summary>
        /// 根据服务名对应注入类
        /// </summary>
        internal static ConcurrentDictionary<string, Type> ServiceNameMappings
        {
            get;
            private set;
        }

        /// <summary>
        /// 根据条件对应注入类
        /// key: 接口/基类完整名;
        /// </summary>
        internal static ConcurrentDictionary<string, Type> ConditionalMappings
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 获取宿主类型的 Auto 注入 schema（线程安全缓存）。
        /// </summary>
        public static AutoInjectionSchema? GetSchema(Type hostType)
        {
            string key = hostType.FullName ?? hostType.Name;
            return SchemaContainer.GetOrAdd(key, _ => GetSchemaFromType(hostType));
        }

        /// <summary>
        /// 旧方法名，转发到 <see cref="GetSchema"/>。
        /// </summary>
        [Obsolete("Use GetSchema instead.")]
        public static AutoInjectionSchema? GeSchema(Type hostType) => GetSchema(hostType);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="injectType"></param>
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

            if (!ServiceNameMappings.TryAdd(serviceName, injectType)
                && ServiceNameMappings.TryGetValue(serviceName, out existsType)
                && existsType != injectType)
            {
                throw new ArgumentException($"Service Name '{serviceName}' already exists!", serviceName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="injectTypeList"></param>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static Type? GetServiceType(string serviceName)
        {
            if (ServiceNameMappings.TryGetValue(serviceName, out Type? type))
            {
                return type;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
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
