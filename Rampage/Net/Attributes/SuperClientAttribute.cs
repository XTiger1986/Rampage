using System;

namespace Rampage.Net
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class SuperClientAttribute : Attribute
    {

        public string? Name
        {
            get; set;
        }

        public string? Root
        {
            get; set;
        }

        public Type? Handler
        {
            get; set;
        }

    }
}
