using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 条件服务匹配器：当同一基类型存在多个候选实现时判定选用哪一个 / Conditional service matcher that decides which candidate implementation to use when a base type has several.
    /// </summary>
    public interface IConditionalHandler
    {

        /// <summary>
        /// 判断候选实现类型是否匹配目标基类型 / Determines whether the candidate implementation matches the target base type.
        /// </summary>
        /// <param name="baseType">待注入的基类型 / The base type to inject.</param>
        /// <param name="injectType">候选实现类型 / The candidate implementation type.</param>
        /// <returns>匹配返回 true / True when the candidate matches.</returns>
        bool IsMatch(Type baseType, Type injectType);

    }
}
