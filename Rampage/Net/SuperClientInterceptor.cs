using Microsoft.Extensions.DependencyInjection;
using Rampage.DependencyInjection;
using System;

namespace Rampage.Net
{
    public abstract class SuperClientInterceptor : ISuperClientInterceptor
    {

        public virtual IServiceProvider? ServiceProvider
        {
            get;
            protected set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual IScopedServiceLocator? ServiceLocator
        {
            get;
            protected set;
        }

        public SuperClientInterceptor()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            ServiceProvider = RampageBooter.Application.GetScopeProvider()!;
            ServiceLocator = ServiceProvider.GetService<IScopedServiceLocator>();

            AutoInject();
        }

        /// <summary>
        /// 
        /// </summary>
        protected AutoInjecter? Injecter;

        /// <summary>
        /// 
        /// </summary>
        protected virtual void AutoInject()
        {
            if (ServiceLocator != null)
            {
                Injecter = new AutoInjecter(ServiceLocator);
                Injecter.Build(this);
            }
        }

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

        public virtual Task HandleHttpRequestAsync(HttpClient client, HttpRequestMessage request, SuperClientConfig? config)
        {
            return Task.CompletedTask;
        }

        public virtual Task<HttpResponseMessage?> HandleHttpResponseAsync(HttpClient client, HttpResponseMessage response, HttpRequestMessage request, SuperClientConfig? config)
        {
            return Task.FromResult<HttpResponseMessage?>(null);
        }
    }
}
