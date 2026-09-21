using System;

namespace Rampage.Net
{
    /// <summary>
    /// 路径参数标注：以参数值替换资源路径中的 {name} 占位符
    /// Path parameter attribute: replaces the {name} placeholder in the resource path with the argument value
    /// </summary>
    /// <remarks>
    /// 占位符名默认取参数名，可由 <see cref="Name"/> 指定；资源路径中不存在同名占位符时不产生任何效果。
    /// The placeholder name defaults to the parameter name and can be set via <see cref="Name"/>; a missing placeholder has no effect.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestPathParamAttribute : Attribute
    {
        /// <summary>路径占位符名：缺省时取参数名 / The path placeholder name; defaults to the parameter name</summary>
        public virtual string? Name
        {
            get; set;
        }

    }
}
