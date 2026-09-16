using System;

namespace Rampage.Net
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestHeaderAttribute : Attribute
    {

        public virtual string? Name
        {
            get; set;
        }

    }
}
