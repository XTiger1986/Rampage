using Rampage.Reflection;
using Rampage.Utilities;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Rampage.Mapping
{
    /// <summary>
    /// 同名属性对象映射器：属性访问复用 EmitTool 编译委托（无逐次反射），映射对按类型组合缓存。
    /// 支持 Nullable 目标类型与 IConvertible 类型转换。
    /// </summary>
    public static class MapperTool
    {
        private static readonly ConcurrentDictionary<string, List<PropertyPair>> MappingCache = new ConcurrentDictionary<string, List<PropertyPair>>();

        private sealed class PropertyPair
        {
            public string SourceName = string.Empty;
            public string TargetName = string.Empty;
            public Type TargetType = typeof(object);
        }

        /// <summary>
        /// 映射到新目标实例（同名属性自动赋值）
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static TTarget Map<TSource, TTarget>(TSource source) where TTarget : new()
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            TTarget target = new TTarget();
            MapTo(source, target);
            return target;
        }

        /// <summary>
        /// 映射到已有目标实例（同名属性覆盖写入）
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void MapTo<TSource, TTarget>(TSource source, TTarget target)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            foreach (PropertyPair pair in GetMapping(typeof(TSource), typeof(TTarget)))
            {
                object? value = EmitTool.GetPropertyValue(source, pair.SourceName);
                if (value == null)
                {
                    continue;
                }
                if (value.GetType() != pair.TargetType)
                {
                    Type convertTargetType = Nullable.GetUnderlyingType(pair.TargetType) ?? pair.TargetType;
                    if (value is IConvertible)
                    {
                        value = Convert.ChangeType(value, convertTargetType, CultureInfo.InvariantCulture);
                    }
                    else if (!pair.TargetType.IsInstanceOfType(value))
                    {
                        continue;
                    }
                }
                EmitTool.SetPropertyValue(target, pair.TargetName, value);
            }
        }

        private static List<PropertyPair> GetMapping(Type sourceType, Type targetType)
        {
            string cacheKey = sourceType.FullName + "=>" + targetType.FullName;
            return MappingCache.GetOrAdd(cacheKey, _ =>
            {
                List<PropertyPair> pairs = new List<PropertyPair>();
                Dictionary<string, PropertyInfo> sourceProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (PropertyInfo property in sourceType.GetProperties())
                {
                    if (property.CanRead && property.GetIndexParameters().Length == 0 && !sourceProperties.ContainsKey(property.Name))
                    {
                        sourceProperties.Add(property.Name, property);
                    }
                }
                foreach (PropertyInfo targetProperty in targetType.GetProperties())
                {
                    if (!targetProperty.CanWrite || targetProperty.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }
                    if (sourceProperties.TryGetValue(targetProperty.Name, out PropertyInfo? sourceProperty))
                    {
                        Type targetValueType = Nullable.GetUnderlyingType(targetProperty.PropertyType) ?? targetProperty.PropertyType;
                        bool convertible = sourceProperty.PropertyType == targetProperty.PropertyType
                            || sourceProperty.PropertyType.IsAssignableTo(targetProperty.PropertyType)
                            || (typeof(IConvertible).IsAssignableFrom(sourceProperty.PropertyType) && typeof(IConvertible).IsAssignableFrom(targetValueType));
                        if (convertible)
                        {
                            pairs.Add(new PropertyPair
                            {
                                SourceName = sourceProperty.Name,
                                TargetName = targetProperty.Name,
                                TargetType = targetProperty.PropertyType
                            });
                        }
                    }
                }
                return pairs;
            });
        }
    }
}
