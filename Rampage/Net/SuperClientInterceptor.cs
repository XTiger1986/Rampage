using Microsoft.Extensions.DependencyInjection;
using Rampage.DependencyInjection;
using System;

namespace Rampage.Net
{
    /// <summary>
    /// HTTP 客户端拦截器基类：构造时创建作用域服务定位器并完成属性自动注入；请求/响应拦截默认为空实现
    /// Base class for HTTP client interceptors: creates a scoped service locator and performs property auto-injection in the constructor; request/response interception is a no-op by default
    /// </summary>
    public abstract class SuperClientInterceptor : ISuperClientInterceptor
    {
        /// <summary>构造时创建的作用域服务提供者 / The scoped service provider created in the constructor</summary>
        public virtual IServiceProvider? ServiceProvider
        {
            get;
            protected set;
        }

        /// <summary>
        /// 作用域服务定位器（由 Initialize 创建），供派生类解析作用域服务
        /// The scoped service locator (created by Initialize) for resolving scoped services in derived classes
        /// </summary>
        public virtual IScopedServiceLocator? ServiceLocator
        {
            get;
            protected set;
        }

        /// <summary>默认构造：初始化作用域并执行自动注入 / Default constructor: initializes the scope and performs auto-injection</summary>
        public SuperClientInterceptor()
        {
            Initialize();
        }

        /// <summary>
        /// 从应用根创建作用域服务提供者与定位器，并执行自动注入
        /// Creates the scoped service provider and locator from the application root, then performs auto-injection
        /// </summary>
        /// <remarks>拦截器实例由 EmitTool 在 TryLoadConfig 中创建，不在请求作用域内，故此处自行创建 Scope / Interceptor instances are created by EmitTool inside TryLoadConfig, outside any request scope, so a scope is created here</remarks>
        protected virtual void Initialize()
        {
            ServiceProvider = RampageBooter.Application.GetScopeProvider()!;
            ServiceLocator = ServiceProvider.GetService<IScopedServiceLocator>();

            AutoInject();
        }

        /// <summary>
        /// 属性自动注入器（由 AutoInject 构建）
        /// The property auto-injector (built by AutoInject)
        /// </summary>
        protected AutoInjecter? Injecter;

        /// <summary>
        /// 使用 ServiceLocator 对本实例执行属性自动注入
        /// Performs property auto-injection on this instance via the ServiceLocator
        /// </summary>
        protected virtual void AutoInject()
        {
            if (ServiceLocator != null)
            {
                Injecter = new AutoInjecter(ServiceLocator);
                Injecter.Build(this);
            }
        }

        /// <summary>
        /// 将请求报文重新打包为新的 HttpRequestMessage（复制方法、地址、请求头与内容引用）
        /// Repacks a request message into a new HttpRequestMessage (copies the method, URI, headers and content reference)
        /// </summary>
        /// <param name="request">原始请求报文 / The original request message</param>
        /// <returns>重新打包后的新请求报文 / The repacked new request message</returns>
        /// <remarks>同名请求头仅保留第一个值 / Only the first value of a duplicated header name is kept</remarks>
        protected HttpRequestMessage RepackHttpRequest(HttpRequestMessage request)
        {
            HttpRequestMessage reqpackReq = new HttpRequestMessage();
            reqpackReq.Method = request.Method;
            reqpackReq.RequestUri = request.RequestUri;
            if (request.Headers != null && request.Headers.Any())
            {
                foreach (var header in request.Headers)
                {
                    if (!reqpackReq.Headers.Contains(header.Key))
                    {
                        reqpackReq.Headers.Add(header.Key, header.Value.FirstOrDefault());
                    }
                    else
                    {
                        reqpackReq.Headers.Remove(header.Key);
                        reqpackReq.Headers.Add(header.Key, header.Value.FirstOrDefault());
                    }
                }
            }

            reqpackReq.Content = request.Content;
            return reqpackReq;
        }

        /// <summary>请求发送前拦截；默认不做任何处理 / Intercepted before the request is sent; no-op by default</summary>
        /// <param name="client">发起请求的客户端 / The client sending the request</param>
        /// <param name="request">即将发送的请求报文 / The request message about to be sent</param>
        /// <param name="config">当前客户端配置，可能为 null / The client config, possibly null</param>
        /// <returns>已完成的任务 / A completed task</returns>
        public virtual Task HandleHttpRequestAsync(HttpClient client, HttpRequestMessage request, SuperClientConfig? config)
        {
            return Task.CompletedTask;
        }

        /// <summary>响应返回后拦截；默认返回 null（沿用原响应）/ Intercepted after the response arrives; returns null by default (keeps the original response)</summary>
        /// <param name="client">发起请求的客户端 / The client that sent the request</param>
        /// <param name="response">收到的原始响应报文 / The received response message</param>
        /// <param name="request">对应的请求报文 / The originating request message</param>
        /// <param name="config">当前客户端配置，可能为 null / The client config, possibly null</param>
        /// <returns>替换用的新响应；null 表示沿用原响应 / A replacement response; null to keep the original</returns>
        public virtual Task<HttpResponseMessage?> HandleHttpResponseAsync(HttpClient client, HttpResponseMessage response, HttpRequestMessage request, SuperClientConfig? config)
        {
            return Task.FromResult<HttpResponseMessage?>(null);
        }
    }
}
