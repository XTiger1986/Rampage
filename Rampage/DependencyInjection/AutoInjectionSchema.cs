using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 类型的自动注入方案：宿主类型与其全部 [Auto] 成员的集合 / The auto-injection schema of a type: the host type together with all of its [Auto] members.
    /// </summary>
    public class AutoInjectionSchema
    {

        /// <summary>
        /// 宿主类型 / The host type.
        /// </summary>
        public Type? HostType
        {
            get; internal set;
        }

        /// <summary>
        /// 宿主类型上扫描到的全部 [Auto]/[Singleton] 特性实例 / All [Auto]/[Singleton] attribute instances scanned on the host type.
        /// </summary>
        public List<Auto>? AutoList
        {
            get; internal set;
        }

    }
}
