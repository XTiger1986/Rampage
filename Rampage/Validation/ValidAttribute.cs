using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 标记指定参数对象需要进行数据验证（无成员，仅作标记）
    /// Marks a property/parameter object as requiring data validation (marker only, no members)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class ValidAttribute : Attribute
    {
    }
}
