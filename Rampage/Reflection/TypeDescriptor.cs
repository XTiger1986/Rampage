using System;
using System.Reflection;

namespace Rampage.Reflection
{
    /// <summary>
    /// 类型详情：缓存于 ReflectionTool，避免重复反射 / Cached type metadata used by ReflectionTool to avoid repeated reflection
    /// </summary>
    public class TypeDescriptor
    {
        /// <summary>
        /// 接口列表 / The interface list
        /// </summary>
        public List<Type>? InterfaceList
        {
            get;
            set;
        }

        /// <summary>
        /// 属性集合 / The property list
        /// </summary>
        public List<PropertyInfo>? PropertyList
        {
            get;
            set;
        }

        /// <summary>
        /// 字段集合 / The field list
        /// </summary>
        public List<FieldInfo>? FieldList
        {
            get;
            set;
        }

        /// <summary>
        /// 方法集合 / The method list
        /// </summary>
        public List<MethodInfo>? MethodList
        {
            get;
            set;
        }

        /// <summary>
        /// 类级自定义特性 / Class-level custom attributes
        /// </summary>
        public List<object>? CustomAttributes
        {
            get;
            set;
        }

    }
}
