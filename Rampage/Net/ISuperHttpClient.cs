using System;

namespace Rampage.Net
{
    /// <summary>
    /// 声明式 HTTP 客户端标记接口：应用启动时框架扫描其派生接口（或标注 [SuperClient] 的接口），
    /// 由 DynamicHttpClientBuilder（Reflection.Emit）生成“接口名+Implementation”动态实现类（继承 <see cref="SuperHttpClient"/>）并注册为服务；
    /// 接口方法的各参数由生成的 IL 打包为 Dictionary&lt;string, object?&gt; 后统一调用 <see cref="SuperHttpClient.CallAsync{T}"/> 完成请求。
    /// Marker interface for declarative HTTP clients: at startup the framework scans its derived interfaces (or interfaces marked with [SuperClient]),
    /// generates an "interface-name+Implementation" dynamic class via DynamicHttpClientBuilder (Reflection.Emit) that derives from <see cref="SuperHttpClient"/>, and registers it as a service;
    /// each interface method's arguments are packed by the emitted IL into a Dictionary&lt;string, object?&gt; and dispatched to <see cref="SuperHttpClient.CallAsync{T}"/>.
    /// </summary>
    /// <remarks>
    /// 接口方法签名硬约束：返回类型必须为 Task&lt;T&gt;，否则动态实现生成阶段直接抛出 NotSupportedException。
    /// Hard constraint on interface method signatures: the return type must be Task&lt;T&gt;; otherwise a NotSupportedException is thrown while generating the dynamic implementation.
    /// </remarks>
    public interface ISuperHttpClient
    {
        /// <summary>获取或设置请求/响应拦截器 / Gets or sets the request/response interceptor</summary>
        ISuperClientInterceptor? Interceptor { get; set; }

    }
}
