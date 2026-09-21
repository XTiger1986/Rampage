using System;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// [Auto] 字段/属性解析失败时抛出的异常 / Thrown when an [Auto]-injected field/property fails to resolve.
    /// </summary>
    /// <remarks>
    /// 仅在 <see cref="AutoInjecter.FailFast"/> 开启时抛出；FailFast 关闭时只记录错误日志并跳过该成员。
    /// Only thrown when <see cref="AutoInjecter.FailFast"/> is enabled; otherwise the failure is logged and the member skipped.
    /// </remarks>
    public class AutoInjectException : Exception
    {
        /// <summary>声明注入成员的宿主类型 / The host type declaring the injected member.</summary>
        public Type HostType { get; }
        /// <summary>注入成员名（属性或字段名）/ The injected member name (property or field).</summary>
        public string MemberName { get; }
        /// <summary>请求解析的服务类型 / The requested service type.</summary>
        public Type ServiceType { get; }

        /// <summary>初始化异常 / Initializes the exception.</summary>
        /// <param name="hostType">宿主类型 / The host type.</param>
        /// <param name="memberName">成员名 / The member name.</param>
        /// <param name="serviceType">服务类型 / The service type.</param>
        /// <param name="message">错误消息 / The error message.</param>
        /// <param name="inner">内部异常 / The inner exception.</param>
        public AutoInjectException(Type hostType, string memberName, Type serviceType, string message, Exception? inner = null)
            : base(message, inner)
        {
            HostType = hostType;
            MemberName = memberName;
            ServiceType = serviceType;
        }
    }
}
