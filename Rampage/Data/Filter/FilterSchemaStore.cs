using Rampage.Data;
using Rampage.Utilities;
using System;
using System.Reflection;

namespace Rampage.Data.Filter
{
    /// <summary>
    /// 筛选器结构存储，用以优化反射产生的性能损耗
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
        /// 
        /// </summary>
        public static List<FilterSchema> FilterSchemaList
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
        public static FilterSchema? GetFilterSchema<F>() where F : DataFilter, new()
        {
            Type type = typeof(F);
            FilterSchema? schema = GetFilterSchema(type);
            return schema;
        }

    }
}
