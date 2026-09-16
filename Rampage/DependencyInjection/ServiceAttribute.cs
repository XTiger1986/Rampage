using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// Inject custom attributes of the service
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class ServiceAttribute : Attribute
    {

        /// <summary>
        /// Custom service name, if defined, must be globally unique, repeating it will throw an error
        /// </summary>
        public string? Name
        {
            get;
            set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public IConditionalHandler? ConditionalHandler
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public ServiceAttribute() { }

        /// <summary>
        /// 
        /// </summary>
        public ServiceAttribute(string serviceName)
        {
            this.Name = serviceName;
        }

    }
}
