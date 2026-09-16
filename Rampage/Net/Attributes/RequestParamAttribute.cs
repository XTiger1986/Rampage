using System;

namespace Rampage.Net
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestParamAttribute : Attribute
    {

        public virtual string? Name
        {
            get; set;
        }

    }
}
