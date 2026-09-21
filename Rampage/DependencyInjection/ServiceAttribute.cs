using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 服务标注特性：控制程序集扫描注册行为 / Service annotation attribute that drives assembly-scan registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class ServiceAttribute : Attribute
    {

        /// <summary>
        /// 自定义服务名；定义后必须全局唯一，重复将抛出异常 / Custom service name; must be globally unique when defined, duplicates throw.
        /// </summary>
        public string? Name
        {
            get;
            set;
        }
        
        /// <summary>
        /// 条件处理器：同一基类型存在多个候选实现时决定注入哪一个 / Conditional handler that decides which candidate implementation to inject when a base type has several.
        /// </summary>
        public IConditionalHandler? ConditionalHandler
        {
            get;
            set;
        }

        /// <summary>
        /// 初始化服务标注特性 / Initializes the service attribute.
        /// </summary>
        public ServiceAttribute() { }

        /// <summary>
        /// 以全局唯一服务名初始化服务标注特性 / Initializes the service attribute with a globally unique service name.
        /// </summary>
        /// <param name="serviceName">服务名 / The service name.</param>
        public ServiceAttribute(string serviceName)
        {
            this.Name = serviceName;
        }

    }
}
