using System;

namespace Rampage.Net
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestCookieAttribute : Attribute
    {

        public virtual string? Name
        {
            get; set;
        }

    }
}
