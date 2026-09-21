using System;
using System.Reflection;

namespace Rampage.Validation
{

    /// <summary>
    /// 验证特性基类：所有声明式验证规则的抽象基类，可同时标注于属性与字段，单成员允许多个规则
    /// Abstract base class for all declarative validation rule attributes; applicable to properties and fields, multiple rules per member allowed
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class ValidationAttributeBase : Attribute
    {

        /// <summary>
        /// 自定义提示文本模板（最终经 string.Format 格式化：{0} 为被验证值，{1} 起为 GetTextArguments 提供的参数）
        /// / Custom message template (formatted via string.Format: {0} is the value, {1}+ are GetTextArguments)
        /// </summary>
        public virtual string? MessageTemplate
        {
            get;
            set;
        }

        /// <summary>
        /// 验证属性所属的完整类名（加载时由 Validator 填充）/ Full name of the class owning the property (filled by Validator at load time)
        /// </summary>
        public string FullTypeName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证属性所属的类名（不含命名空间，加载时由 Validator 填充）/ Name of the owning class without namespace (filled by Validator at load time)
        /// </summary>
        public string TypeName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证的属性名（加载时由 Validator 填充）/ The validated property name (filled by Validator at load time)
        /// </summary>
        public string PropertyName
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 验证的属性元数据（加载时由 Validator 填充）/ The validated property metadata (filled by Validator at load time)
        /// </summary>
        public PropertyInfo Property
        {
            get;
            internal set;
        } = default!;

        /// <summary>
        /// 执行验证 / Perform the validation
        /// </summary>
        /// <param name="v">被验证的属性值 / The property value to validate</param>
        /// <returns>通过返回 true / true when the value passes</returns>
        public abstract bool Validate(object? v);

        /// <summary>
        /// 自定义文本资源键名，默认 "类名.属性名.规则名"（规则名去掉 Attribute 后缀）
        /// / The localization resource key, by default "TypeName.PropertyName.RuleName" (rule name without the Attribute suffix)
        /// </summary>
        /// <returns>本地化键 / The localization key</returns>
        public virtual string? GetLocalizerKey()
        {
            return $"{TypeName}.{PropertyName}.{this.GetType().Name.Replace("Attribute", "")}";
        }

        /// <summary>
        /// 自定义文本的格式化参数：string.Format 中 {1}、{2}... 对应的参数（{0} 固定为被验证值）
        /// / Format arguments for the message: they fill {1}, {2}... of string.Format ({0} is always the value)
        /// </summary>
        /// <returns>参数数组；无参数时为 null / The argument array, or null when none</returns>
        public virtual object?[]? GetTextArguments()
        {
            return null;
        }

        /// <summary>
        /// 是否必须有值：为 true 时，null 值（以及部分规则的无法解析值）直接判为失败
        /// / Whether a value is mandatory: when true, a null (or unparseable) value fails the rule
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
