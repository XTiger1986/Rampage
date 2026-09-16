using System;

namespace Rampage.Net
{
    public interface ISuperHttpClient
    {

        ISuperClientInterceptor? Interceptor { get; set; }

    }
}
