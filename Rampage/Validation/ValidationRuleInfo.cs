using System;
using System.Reflection;

namespace Rampage.Validation
{
    /// <summary>
    /// 单条验证规则信息（由验证特性在规则加载时转换而来）/ One validation rule (converted from a validation attribute at rule-load time)
    /// </summary>
    public class ValidationRuleInfo
    {


        /// <summary>
        /// 自定义提示文本模板（最终经 string.Format 格式化：{0} 为被验证值，{1} 起为 TextArguments）
        /// / Custom message template (formatted via string.Format: {0} is the value, {1}+ are TextArguments)
        /// </summary>
        public virtual string? MessageTemplate
        {
            get;
            internal set;
        }

        /// <summary>
        /// 多语言提示文本模板（键为语言名）/ Localized message templates keyed by language name
        /// </summary>
        public Dictionary<string, string?> MessageTemplateDict
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证属性所属的完整类名 / Full name of the class owning the property
        /// </summary>
        public string FullTypeName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证的属性名 / The validated property name
        /// </summary>
        public string PropertyName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证的属性元数据 / The validated property metadata
        /// </summary>
        public PropertyInfo Property
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 自定义文本的格式化参数：string.Format 中 {1}、{2}... 对应的参数（{0} 固定为被验证值）
        /// / Format arguments for the message: they fill {1}, {2}... of string.Format ({0} is always the value)
        /// </summary>
        public object?[]? TextArguments
        {
            get;
            internal set;
        }

        /// <summary>
        /// 是否必须有值 / Whether a value is mandatory
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

        /// <summary>
        /// 验证执行委托（包装自特性的 Validate 方法）/ The delegate performing the validation (wraps the attribute's Validate method)
        /// </summary>
        public Func<object?, bool> ValidateHandler
        {
            get;
            internal set;
        } = default!;


    }
}
