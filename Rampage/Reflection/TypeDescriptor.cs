using System;
using System.Reflection;

namespace Rampage.Reflection
{
    /// <summary>
    /// 类型详情
    /// </summary>
    public class TypeDescriptor
    {
        /// <summary>
        /// 接口列表
        /// </summary>
        public List<Type>? InterfaceList
        {
            get;
            set;
        }

        /// <summary>
        /// 属性集合
        /// </summary>
        public List<PropertyInfo>? PropertyList
        {
            get;
            set;
        }

        /// <summary>
        /// 字段集合
        /// </summary>
        public List<FieldInfo>? FieldList
        {
            get;
            set;
        }

        /// <summary>
        /// 函数集合
        /// </summary>
        public List<MethodInfo>? MethodList
        {
            get;
            set;
        }

        /// <summary>
        /// 自定义特性
        /// </summary>
        public List<object>? CustomAttributes
        {
            get;
            set;
        }

    }
}
