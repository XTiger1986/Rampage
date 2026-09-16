using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public class AutoInjectionSchema
    {

        /// <summary>
        /// 类型
        /// </summary>
        public Type? HostType
        {
            get; internal set;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<Auto>? AutoList
        {
            get; internal set;
        }

    }
}
