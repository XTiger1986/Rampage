using Rampage.Data;
using Rampage.Utilities;
using System;
using System.Reflection;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器结构存储，用以优化反射产生的性能损耗（首次访问某筛选器类型时解析并缓存其结构）
    /// Filter schema store caching parsed schemas to avoid repeated reflection cost (a type is parsed and cached on first access)
    /// </summary>
    public class FilterSchemaStore
    {
        private static object _storeLock = new object();

        static FilterSchemaStore()
        {
            if (FilterSchemaList == null)
            {
                lock (_storeLock)
                {
                    if (FilterSchemaList == null)
                    {
                        FilterSchemaList = new List<FilterSchema>();
                    }
                }
            }
        }

        /// <summary>
        /// 已缓存的筛选器结构列表（静态共享，全程只增不减）
        /// Cached filter schemas (static, shared, append-only)
        /// </summary>
        public static List<FilterSchema> FilterSchemaList
        {
            get;
            private set;
        }

        /// <summary>
        /// 反射解析一个筛选器类型并构建其结构：读取类级 <see cref="FilterAttribute"/>（缺省时表名取类名并去除 "Filter" 后缀），
        /// 遍历公开属性生成筛选项（无特性时按属性名给默认项），最后按 SortOrder 降序排序
        /// Reflect over a filter type and build its schema: reads the class-level <see cref="FilterAttribute"/>
        /// (table name defaults to the class name with a trailing "Filter" suffix stripped), walks public properties into
        /// filter items (defaulted by property name when unattributed), then sorts by SortOrder descending
        /// </summary>
        /// <param name="type">筛选器类型 / Filter type</param>
        /// <returns>构建的结构 / The built schema</returns>
        private static FilterSchema GetFilterSchemaFromType(Type type)
        {
            FilterSchema schema = new FilterSchema();
            schema.ModelType = type;

            FilterAttribute? filterAttr = null;
            if (type.IsDefined(typeof(FilterAttribute), true))
            {
                filterAttr = (FilterAttribute)type.GetCustomAttributes(typeof(FilterAttribute), true)[0];
            }
            if (filterAttr == null)
            {
                string tabName = type.Name;
                filterAttr = new FilterAttribute
                {
                    TableName = tabName
                };
            }

            if (string.IsNullOrEmpty(filterAttr.TableName))
                filterAttr.TableName = type.Name;
            int filterSuffixPos = filterAttr.TableName.LastIndexOf("Filter");
            if (filterSuffixPos > 0)
            {
                filterAttr.TableName = filterAttr.TableName.Substring(0, filterSuffixPos);
            }

            schema.Filter = filterAttr;
            schema.ItemList = new List<FilterItemAttribute>();

            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            if (propertyInfos != null && propertyInfos.Length > 0)
            {
                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    Type prpType = propertyInfo.PropertyType;
                    FilterItemAttribute? filterItemAttr = null;
                    if (propertyInfo.IsDefined(typeof(FilterItemAttribute), true))
                    {
                        filterItemAttr = (FilterItemAttribute)propertyInfo.GetCustomAttributes(typeof(FilterItemAttribute), true)[0];
                    }

                    if (filterItemAttr == null)
                    {
                        filterItemAttr = new FilterItemAttribute
                        {
                            FieldName = propertyInfo.Name,
                            OrFields = string.Empty,
                            FormName = propertyInfo.Name,
                            Connector = FilterConnectorType.And,
                            Operator = FilterOperatorType.Equal,
                            Ignore = false,
                            OrFieldList = new List<string>()
                        };
                    }

                    if (string.IsNullOrEmpty(filterItemAttr.FieldName))
                    {
                        filterItemAttr.FieldName = propertyInfo.Name;
                    }
                    if (string.IsNullOrEmpty(filterItemAttr.FormName))
                    {
                        filterItemAttr.FormName = propertyInfo.Name;
                    }
                    if (filterItemAttr.OrFieldList == null)
                    {
                        filterItemAttr.OrFieldList = new List<string>();
                    }

                    filterItemAttr.Property = propertyInfo;
                    if (!string.IsNullOrEmpty(filterItemAttr.OrFields))
                    {
                        filterItemAttr.OrFieldList = StringTool.Explode<string>(filterItemAttr.OrFields, ",");
                    }

                    schema.ItemList.Add(filterItemAttr);
                }
            }

            schema.ItemList.Sort(new Comparison<FilterItemAttribute>(delegate (FilterItemAttribute item1, FilterItemAttribute item2)
            {
                if (item1.SortOrder > item2.SortOrder)
                {
                    return -1;
                }
                else if (item1.SortOrder < item2.SortOrder)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }));

            return schema;
        }

        /// <summary>
        /// 取得筛选器类型的结构：命中缓存直接返回，否则解析、缓存并返回（全程持锁）
        /// Get the schema of a filter type: returns the cached one, otherwise parses, caches and returns it (lock-held throughout)
        /// </summary>
        /// <param name="type">筛选器类型 / Filter type</param>
        /// <returns>筛选器结构 / The filter schema</returns>
        public static FilterSchema? GetFilterSchema(Type type)
        {
            lock (_storeLock)
            {
                FilterSchema? schema = FilterSchemaList.Find(t => t.ModelType!.FullName == type.FullName);
                if (schema == null)
                {
                    schema = GetFilterSchemaFromType(type);
                    FilterSchemaList.Add(schema);
                }
                return schema;
            }
        }

        /// <summary>
        /// 泛型便捷重载：取得指定筛选器类型的结构
        /// Generic convenience overload: get the schema of the specified filter type
        /// </summary>
        /// <typeparam name="F">筛选器类型 / Filter type</typeparam>
        /// <returns>筛选器结构 / The filter schema</returns>
        public static FilterSchema? GetFilterSchema<F>() where F : DataFilter, new()
        {
            Type type = typeof(F);
            FilterSchema? schema = GetFilterSchema(type);
            return schema;
        }

    }
}
