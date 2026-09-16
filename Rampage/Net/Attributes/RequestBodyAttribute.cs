using System;

namespace Rampage.Net
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestBodyAttribute : Attribute
    {

        public virtual string? Name
        {
            get; set;
        }

    }
}
