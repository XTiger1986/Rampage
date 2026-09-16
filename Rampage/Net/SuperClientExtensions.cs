using Rampage.ExceptionExtensions;
using Rampage.Json;
using Rampage.Utilities;
using System;
using System.Collections;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Rampage.Net
{
    /// <summary>
    ///
    /// </summary>
    public static class SuperClientExtensions
    {

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static HttpRequestMessage SetRequestHeaders(this HttpRequestMessage request, IDictionary<string, string>? headers)
        {
            if (headers != null && headers.Count > 0)
            {
                foreach (var header in headers)
                {
                    if (!request.Headers.Contains(header.Key))
                    {
                        request.Headers.Add(header.Key, header.Value.ToString());
                    }
                    else
                    {
                        request.Headers.Remove(header.Key);
                        request.Headers.Add(header.Key, header.Value.ToString());
                    }
                }
            }
            return request;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cookies"></param>
        public static HttpRequestMessage SetRequestCookies(this HttpRequestMessage request, CookieCollection? cookies)
        {
            if (cookies != null && cookies.Count > 0)
            {
                string cookiesHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                request.Headers.Add("Cookie", cookiesHeader);
            }
            return request;
        }

        /// <summary>
        /// 获取响应结果中设置的COOKIE数据
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static CookieCollection GetSetCookies(this HttpResponseMessage response)
        {
            var cookies = new CookieCollection();
            if (response == null)
                return cookies;
            var setCookie = Enumerable.Empty<string>();
            if (response.Headers.TryGetValues("Set-Cookie", out setCookie))
            {
                foreach (var cookieStr in setCookie)
                {
                    foreach (var cookieToken in cookieStr.Split(';'))
                    {
                        var name = cookieToken.Trim();
                        var value = "";
                        if (cookieToken.Contains('='))
                        {
                            var keyValueTokens = cookieToken.Split('=');
                            name = (keyValueTokens[0]).Trim();
                            if (keyValueTokens.Length > 1 && !string.IsNullOrEmpty(keyValueTokens[1]))
                            {
                                value = (keyValueTokens[1]).Trim();
                            }
                        }
                        if (!string.IsNullOrEmpty(name))
                        {
                            var cookie = new Cookie(name, value);
                            cookies.Add(cookie);
                        }
                    }
                }
            }
            return cookies;
        }

        /// <summary>
        /// 获取响应结果中的字符串结果
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string? GetString(this HttpResponseMessage response, bool needClose = true)
        {
            string? content = null;

            Encoding encoding = Encoding.UTF8;
            if (response.Content.Headers.ContentType != null && !string.IsNullOrEmpty(response.Content.Headers.ContentType.CharSet))
            {
                encoding = Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet);
            }

            Stream? resolveStream = null;
            Stream? responseStream = null;
            StreamReader? sr = null;
            try
            {
                responseStream = response.Content.ReadAsStream();
                responseStream.Position = 0;
                if (response.Content.Headers.ContentEncoding.Count(c => c.ToLower() == "gzip") > 0)
                {
                    resolveStream = new GZipStream(responseStream, CompressionMode.Decompress);
                }
                else if (response.Content.Headers.ContentEncoding.Count(c => c.ToLower() == "deflate") > 0)
                {
                    resolveStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                }
                else
                {
                    resolveStream = responseStream;
                }
                resolveStream.Position = 0;
                sr = new StreamReader(resolveStream, encoding);
                content = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (needClose)
                {
                    if (sr != null)
                    {
                        sr.Dispose();
                    }

                    if (resolveStream != null)
                    {
                        resolveStream.Close();
                        resolveStream.Dispose();
                    }
                    if (responseStream != null)
                    {
                        responseStream.Close();
                        responseStream.Dispose();
                    }
                }
            }

            return content;
        }

        #region Async

        /// <summary>
        /// 异步发送HTTP请求并得到响应结果（在ASP.NET Core等异步环境请优先使用，避免阻塞线程池线程）
        /// </summary>
        /// <param name="client"></param>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage?> RequestMessageAsync(this HttpClient client, string url, SuperRequestOptions? options = null)
        {
            options = options ?? new SuperRequestOptions();
            HttpMethod httpMethod = options.Method;
            ISuperClientInterceptor? handler = options.Handler;
            IDictionary<string, object?> data = options.InputData;
            IDictionary<string, string> headers = options.Headers;
            CookieCollection cookies = options.Cookies;
            bool useBody = options.UseBody;

            if (!useBody)
            {
                if (httpMethod != HttpMethod.Post)
                {
                    if (data != null)
                    {
                        foreach (var kv in data)
                        {
                            if (kv.Value != null)
                            {
                                Type vt = kv.Value.GetType();
                                TypeCode vtc = Type.GetTypeCode(vt);
                                if (vt.IsValueType || vt.IsEnum || vtc == TypeCode.String || vt == typeof(Guid))
                                {
                                    url = PathTool.SetUrlQuery(url, kv.Key, kv.Value.ToString()!, true);
                                }
                                else
                                {
                                    if (kv.Value is IEnumerable)
                                    {
                                        IEnumerable vs = (IEnumerable)kv.Value;
                                        foreach (var o in vs)
                                        {
                                            if (o == null) { continue; }
                                            url = PathTool.SetUrlQuery(url, kv.Key, o.ToString()!, true, false);
                                        }
                                    }
                                    else
                                    {
                                        string partQuery = PathTool.GetUrlQueryFromModel(kv.Value);
                                        url = PathTool.CombileUrlQuery(url, partQuery, true);
                                    }
                                }
                            }
                        }
                    }
                }

                using (HttpRequestMessage request = new HttpRequestMessage(httpMethod, url))
                {
                    SetRequestHeaders(request, headers);
                    SetRequestCookies(request, cookies);

                    if (handler != null)
                    {
                        await handler.HandleHttpRequestAsync(client, request, options.Config).ConfigureAwait(false);
                    }

                    if (data != null && httpMethod == HttpMethod.Post)
                    {
                        List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
                        foreach (var formItem in data)
                        {
                            values.Add(new KeyValuePair<string, string>(formItem.Key, formItem.Value != null ? formItem.Value.ToString()! : string.Empty));
                        }
                        request.Content = new FormUrlEncodedContent(values);
                    }

                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

                    if (handler != null)
                    {
                        HttpResponseMessage? newResponse = await handler.HandleHttpResponseAsync(client, response, request, options.Config).ConfigureAwait(false);
                        if (newResponse != null)
                        {
                            response.Dispose();
                            response = newResponse;
                        }
                    }
                    return response;
                }
            }
            else
            {
                using (HttpRequestMessage request = new HttpRequestMessage(httpMethod, url))
                {
                    SetRequestHeaders(request, headers);
                    SetRequestCookies(request, cookies);

                    if (handler != null)
                    {
                        await handler.HandleHttpRequestAsync(client, request, options.Config).ConfigureAwait(false);
                    }

                    if (data != null)
                    {
                        request.Content = JsonContent.Create(data, data.GetType());
                    }

                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

                    if (handler != null)
                    {
                        HttpResponseMessage? newResponse = await handler.HandleHttpResponseAsync(client, response, request, options.Config).ConfigureAwait(false);
                        if (newResponse != null)
                        {
                            response.Dispose();
                            response = newResponse;
                        }
                    }
                    return response;
                }
            }
        }

        /// <summary>
        /// 异步发送HTTP请求并得到字符串结果
        /// </summary>
        public static async Task<string?> RequestStringAsync(this HttpClient client, string url, SuperRequestOptions? options = null)
        {
            using (HttpResponseMessage response = (await RequestMessageAsync(client, url, options).ConfigureAwait(false))!)
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 异步发送HTTP请求并得到结果
        /// </summary>
        public static async Task<M?> RequestAsync<M>(this HttpClient client, string url, SuperRequestOptions? options = null)
        {
            string? rspStr = await RequestStringAsync(client, url, options).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(rspStr))
            {
                return JsonTool.Parse<M>(rspStr);
            }
            return default(M);
        }

        /// <summary>
        /// 异步发送HTTP请求并得到二进制结果
        /// </summary>
        public static async Task<byte[]> RequestBufferAsync(this HttpClient client, string url, SuperRequestOptions? options = null)
        {
            using (HttpResponseMessage response = (await RequestMessageAsync(client, url, options).ConfigureAwait(false))!)
            {
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 异步发送HTTP请求并得到字符串结果
        /// </summary>
        public static Task<string?> RequestStringAsync<T>(this HttpClient client, string url, HttpRequestOptions<T>? options)
        {
            options = options ?? new HttpRequestOptions<T>();
            SuperRequestOptions httpRequestOptions = new SuperRequestOptions
            {
                Method = options.Method,
                InputData = options.InputData,
                Headers = options.Headers,
                Cookies = options.Cookies,
                UseBody = options.UseBody
            };
            return RequestStringAsync(client, url, httpRequestOptions);
        }

        /// <summary>
        /// 异步发送HTTP请求并得到结果
        /// </summary>
        public static async Task<M?> RequestAsync<M, T>(this HttpClient client, string url, HttpRequestOptions<T>? options)
        {
            string? rspStr = await RequestStringAsync<T>(client, url, options).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(rspStr))
            {
                return JsonTool.Parse<M>(rspStr);
            }
            return default(M);
        }

        /// <summary>
        /// 异步发送HTTP请求并得到二进制结果
        /// </summary>
        public static Task<byte[]> RequestBufferAsync<T>(this HttpClient client, string url, HttpRequestOptions<T>? options)
        {
            options = options ?? new HttpRequestOptions<T>();
            SuperRequestOptions httpRequestOptions = new SuperRequestOptions
            {
                Method = options.Method,
                InputData = options.InputData,
                Headers = options.Headers,
                Cookies = options.Cookies,
                UseBody = options.UseBody
            };
            return RequestBufferAsync(client, url, httpRequestOptions);
        }

        /// <summary>
        /// 异步发送HTTP请求并得到响应结果
        /// </summary>
        public static Task<HttpResponseMessage?> RequestMessageAsync<T>(this HttpClient client, string url, HttpRequestOptions<T>? options)
        {
            options = options ?? new HttpRequestOptions<T>();
            SuperRequestOptions httpRequestOptions = new SuperRequestOptions
            {
                Method = options.Method,
                InputData = options.InputData,
                Headers = options.Headers,
                Cookies = options.Cookies,
                UseBody = options.UseBody
            };
            return RequestMessageAsync(client, url, httpRequestOptions);
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求
        /// </summary>
        public static Task<HttpResponseMessage> RequestMessageAsync<T>(this HttpClient client, string url, HttpRequestMultipartOptions<T>? options)
        {
            options = options ?? new HttpRequestMultipartOptions<T>();
            HttpRequestMultipartOptions httpRequestOptions = new HttpRequestMultipartOptions
            {
                Method = options.Method,
                InputData = options.InputData,
                Headers = options.Headers,
                Cookies = options.Cookies,
                UseBody = options.UseBody
            };
            return RequestMessageAsync(client, url, httpRequestOptions);
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求
        /// </summary>
        public static async Task<HttpResponseMessage> RequestMessageAsync(this HttpClient client, string url, HttpRequestMultipartOptions? options = null)
        {
            options = options ?? new HttpRequestMultipartOptions();
            HttpMethod httpMethod = options.Method;
            ISuperClientInterceptor? handler = options.Handler;
            IDictionary<string, object?> data = options.InputData;
            IDictionary<string, string> headers = options.Headers;
            CookieCollection cookies = options.Cookies;
            Dictionary<string, MultipartFileInfo>? uploadFiles = options.UploadFiles;

            using (HttpRequestMessage request = new HttpRequestMessage(httpMethod, url))
            {
                SetRequestHeaders(request, headers);
                SetRequestCookies(request, cookies);

                if (handler != null)
                {
                    await handler.HandleHttpRequestAsync(client, request, options.Config).ConfigureAwait(false);
                }

                MultipartFormDataContent multipartContent = new MultipartFormDataContent();
                if (data != null)
                {
                    foreach (var kv in data)
                    {
                        if (kv.Value == null)
                        {
                            continue;
                        }
                        multipartContent.Add(new StringContent(kv.Value.ToString()!), kv.Key);
                    }
                }
                if (uploadFiles != null && uploadFiles.Count > 0)
                {
                    foreach (var item in uploadFiles)
                    {
                        string formName = item.Value.Name ?? "UploadFile";
                        string fileName = item.Value.FileName ?? "UploadFile";
                        byte[] fileBuff = item.Value.Data!;
                        multipartContent.Add(new ByteArrayContent(fileBuff), formName, fileName);
                    }
                }

                request.Content = multipartContent;
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new SuperHttpException("SuperClient_BadRequest", response.StatusCode, url);
                }

                if (handler != null)
                {
                    HttpResponseMessage? newResponse = await handler.HandleHttpResponseAsync(client, response, request, options.Config).ConfigureAwait(false);
                    if (newResponse != null)
                    {
                        response.Dispose();
                        response = newResponse;
                    }
                }
                return response;
            }
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求并得到字符串结果
        /// </summary>
        public static async Task<string?> RequestStringAsync(this HttpClient client, string url, HttpRequestMultipartOptions? options = null)
        {
            using (HttpResponseMessage response = await RequestMessageAsync(client, url, options).ConfigureAwait(false))
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求并得到结果
        /// </summary>
        public static async Task<M?> RequestAsync<M>(this HttpClient client, string url, HttpRequestMultipartOptions? options)
        {
            string? rspStr = await RequestStringAsync(client, url, options).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(rspStr))
            {
                return JsonTool.Parse<M>(rspStr);
            }
            return default(M);
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求并得到二进制结果
        /// </summary>
        public static async Task<byte[]?> RequestBufferAsync(this HttpClient client, string url, HttpRequestMultipartOptions? options = null)
        {
            using (HttpResponseMessage response = await RequestMessageAsync(client, url, options).ConfigureAwait(false))
            {
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求并得到字符串结果
        /// </summary>
        public static Task<string?> RequestStringAsync<T>(this HttpClient client, string url, HttpRequestMultipartOptions<T>? options)
        {
            options = options ?? new HttpRequestMultipartOptions<T>();
            HttpRequestMultipartOptions httpRequestOptions = new HttpRequestMultipartOptions
            {
                Method = options.Method,
                InputData = options.InputData,
                Headers = options.Headers,
                Cookies = options.Cookies,
                UseBody = options.UseBody
            };
            return RequestStringAsync(client, url, httpRequestOptions);
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求并得到结果
        /// </summary>
        public static async Task<M?> RequestAsync<M, T>(this HttpClient client, string url, HttpRequestMultipartOptions<T>? options)
        {
            string? rspStr = await RequestStringAsync<T>(client, url, options).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(rspStr))
            {
                return JsonTool.Parse<M>(rspStr);
            }
            return default(M);
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求并得到二进制结果
        /// </summary>
        public static Task<byte[]?> RequestBufferAsync<T>(this HttpClient client, string url, HttpRequestMultipartOptions<T>? options)
        {
            options = options ?? new HttpRequestMultipartOptions<T>();
            HttpRequestMultipartOptions httpRequestOptions = new HttpRequestMultipartOptions
            {
                Method = options.Method,
                InputData = options.InputData,
                Headers = options.Headers,
                Cookies = options.Cookies,
                UseBody = options.UseBody
            };
            return RequestBufferAsync(client, url, httpRequestOptions);
        }

        #endregion


    }
}
