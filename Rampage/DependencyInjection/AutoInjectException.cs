using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// [Auto] 字段/属性解析失败。
    /// </summary>
    public class AutoInjectException : Exception
    {
        public Type HostType { get; }
        public string MemberName { get; }
        public Type ServiceType { get; }

        public AutoInjectException(Type hostType, string memberName, Type serviceType, string message, Exception? inner = null)
            : base(message, inner)
        {
            HostType = hostType;
            MemberName = memberName;
            ServiceType = serviceType;
        }
    }
}
