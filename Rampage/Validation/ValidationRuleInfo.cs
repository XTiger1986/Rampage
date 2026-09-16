using System;
using System.Reflection;

namespace Rampage.Validation
{
    public class ValidationRuleInfo
    {


        /// <summary>
        /// 自定义提示文本模板(最终被string.Format调用进行格式化得到最终文本，{0}为验证对象值，从1开始为自定义验证规则的附带参数)
        /// </summary>
        public virtual string? MessageTemplate
        {
            get;
            internal set;
        }

        /// <summary>
        /// 多语言提示文本模板
        /// </summary>
        public Dictionary<string, string?> MessageTemplateDict
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证属性所属的完整类名
        /// </summary>
        public string FullTypeName
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
        /// 自定义文本资源的格式化参数，即用string.Format的格式化参数{0},{1},{2}...
        /// </summary>
        public object?[]? TextArguments
        {
            get;
            internal set;
        }

        /// <summary>
        /// 是否必须有值
        /// </summary>
        public bool Required
        {
            get; 
            internal set;
        }

        /// <summary>
        /// 不符合规则仅警告
        /// </summary>
        public bool IsWarning
        {
            get;
            internal set;
        }

        public Func<object?, bool> ValidateHandler
        {
            get;
            internal set;
        } = default!;


    }
}
