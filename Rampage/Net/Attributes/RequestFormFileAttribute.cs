using System;

namespace Rampage.Net
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequestFormFileAttribute : Attribute
    {

        public virtual string? Name
        {
            get; set;
        }

        public string? FileName
        {
            get; set;
        }

    }
}
