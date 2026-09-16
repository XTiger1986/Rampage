using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rampage.Net
{
    /// <summary>
    /// Http客户端请求/响应拦截器（异步）。请求拦截可修改请求头等；响应拦截返回非null的新响应则替换原响应。
    /// </summary>
    public interface ISuperClientInterceptor
    {
        /// <summary>请求发送前拦截。</summary>
        Task HandleHttpRequestAsync(HttpClient client, HttpRequestMessage request, SuperClientConfig? config);

        /// <summary>响应返回后拦截；返回null表示沿用原响应。</summary>
        Task<HttpResponseMessage?> HandleHttpResponseAsync(HttpClient client, HttpResponseMessage response, HttpRequestMessage request, SuperClientConfig? config);
    }
}
