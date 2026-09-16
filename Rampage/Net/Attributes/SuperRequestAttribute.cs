using System;

namespace Rampage.Net
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public abstract class SuperRequestAttribute : Attribute
    {

        public SuperRequestAttribute() { }

        public SuperRequestAttribute(string resource)
        {
            Resource = resource;
        }

        public string? Resource
        {
            get; set;
        }

    }
}
