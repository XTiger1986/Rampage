using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rampage.Net
{
    /// <summary>
    /// Http客户端请求/响应拦截器（异步）。请求拦截可修改请求头等；响应拦截返回非null的新响应则替换原响应。
    /// Async request/response interceptor for HTTP clients. The request hook may modify headers etc.; the response hook replaces the original response when it returns a non-null new one.
    /// </summary>
    public interface ISuperClientInterceptor
    {
        /// <summary>请求发送前拦截。/ Intercepted before the request is sent.</summary>
        /// <param name="client">发起请求的客户端 / The client sending the request</param>
        /// <param name="request">即将发送的请求报文（可修改） / The request message about to be sent (mutable)</param>
        /// <param name="config">当前客户端配置，可能为 null / The client config, possibly null</param>
        /// <returns>拦截处理任务 / The interception task</returns>
        Task HandleHttpRequestAsync(HttpClient client, HttpRequestMessage request, SuperClientConfig? config);

        /// <summary>响应返回后拦截；返回null表示沿用原响应。/ Intercepted after the response arrives; returning null keeps the original response.</summary>
        /// <param name="client">发起请求的客户端 / The client that sent the request</param>
        /// <param name="response">收到的原始响应报文 / The received response message</param>
        /// <param name="request">对应的请求报文 / The originating request message</param>
        /// <param name="config">当前客户端配置，可能为 null / The client config, possibly null</param>
        /// <returns>替换用的新响应；null 表示沿用原响应 / A replacement response, or null to keep the original</returns>
        Task<HttpResponseMessage?> HandleHttpResponseAsync(HttpClient client, HttpResponseMessage response, HttpRequestMessage request, SuperClientConfig? config);
    }
}
