using System;

namespace Rampage.Validation
{
    /// <summary>
    /// 标记指定参数对象需要进行数据验证
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class ValidAttribute : Attribute
    {
    }
}
