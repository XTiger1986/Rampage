using System;
using System.Reflection;

namespace Rampage.Validation
{

    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class ValidationAttributeBase : Attribute
    {

        /// <summary>
        /// 自定义提示文本模板(最终被string.Format调用进行格式化得到最终文本，{0}为验证对象值，从1开始为自定义验证规则的附带参数)
        /// </summary>
        public virtual string? MessageTemplate
        {
            get;
            set;
        }

        /// <summary>
        /// 验证属性所属的完整类名
        /// </summary>
        public string FullTypeName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证属性所属的类名
        /// </summary>
        public string TypeName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证的属性名
        /// </summary>
        public string PropertyName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证的属性
        /// </summary>
        public PropertyInfo Property
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public abstract bool Validate(object? v);

        /// <summary>
        /// 自定义文本资源键名
        /// </summary>
        /// <returns></returns>
        public virtual string? GetLocalizerKey()
        {
            return $"{TypeName}.{PropertyName}.{this.GetType().Name.Replace("Attribute", "")}";
        }

        /// <summary>
        /// 自定义文本资源的格式化参数，即用string.Format的格式化参数{0},{1},{2}...
        /// </summary>
        /// <returns></returns>
        public virtual object?[]? GetTextArguments()
        {
            return null;
        }

        /// <summary>
        /// 是否必须有值
        /// </summary>
        public virtual bool Required
        {
            get; set;
        }

        /// <summary>
        /// 不符合规则仅警告
        /// </summary>
        public virtual bool IsWarning
        {
            get; set;
        }

    }
}
