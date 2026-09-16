using System;

namespace Rampage.Net
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestPathParamAttribute : Attribute
    {

        public virtual string? Name
        {
            get; set;
        }

    }
}
