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
    /// HttpClient 请求扩展：封装 query string/表单/JSON body 的普通请求与 multipart 表单请求，并提供请求头、Cookie 设置及响应读取工具
    /// HttpClient request extensions: wraps plain requests (query string/form/JSON body) and multipart form requests, plus helpers for headers, cookies and response reading
    /// </summary>
    public static class SuperClientExtensions
    {
        /// <summary>
        /// 批量设置请求头：同名请求头先移除再添加（新值覆盖旧值）
        /// Sets request headers in batch: an existing header of the same name is removed first so the new value overwrites it
        /// </summary>
        /// <param name="request">请求报文 / The request message</param>
        /// <param name="headers">请求头字典；为 null 或空时不处理 / The header dictionary; skipped when null or empty</param>
        /// <returns>原请求报文（便于链式调用）/ The same request message (for chaining)</returns>
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
        /// 批量设置请求 Cookie：手工拼接 "name=value; ..." 写入 Cookie 请求头（不经过 CookieContainer）
        /// Sets request cookies in batch by manually joining "name=value; ..." into the Cookie request header (bypassing CookieContainer)
        /// </summary>
        /// <param name="request">请求报文 / The request message</param>
        /// <param name="cookies">Cookie 集合；为 null 或空时不处理 / The cookie collection; skipped when null or empty</param>
        /// <returns>原请求报文（便于链式调用）/ The same request message (for chaining)</returns>
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
        /// 获取响应结果中设置的 COOKIE 数据
        /// Gets the cookies set by the response (Set-Cookie headers)
        /// </summary>
        /// <param name="response">响应报文 / The response message</param>
        /// <returns>解析出的 Cookie 集合 / The parsed cookie collection</returns>
        /// <remarks>
        /// 按 ';' 与 '=' 逐段拆分 Set-Cookie：不含 '=' 的段按空值 Cookie 处理，
        /// 因此 Path、Expires、HttpOnly 等属性段也会被收集为同名 Cookie（不解析属性语义）。
        /// Each Set-Cookie header is split by ';' and '='; a token without '=' becomes a cookie with an empty value,
        /// so attribute tokens such as Path, Expires and HttpOnly are also collected as cookies (attribute semantics are not parsed).
        /// </remarks>
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
        /// Reads the response content as a string
        /// </summary>
        /// <param name="response">响应报文 / The response message</param>
        /// <param name="needClose">读取后是否关闭并释放响应流与解压流，默认 true / Whether to close and dispose the response/decompression streams after reading, true by default</param>
        /// <returns>响应文本；读取失败时返回 null / The response text, or null on failure</returns>
        /// <remarks>
        /// 字符集取自 Content-Type 的 CharSet（缺省 UTF-8）；gzip/deflate 压缩的响应先解压再读取；
        /// 读取过程中抛出的异常会被吞掉（仅输出到控制台）并返回 null。
        /// The charset comes from Content-Type's CharSet (UTF-8 when absent); gzip/deflate responses are decompressed before reading;
        /// exceptions thrown while reading are swallowed (printed to the console only) and null is returned.
        /// </remarks>
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
        /// Sends an HTTP request asynchronously and returns the response message (prefer this in async environments such as ASP.NET Core to avoid blocking thread-pool threads)
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">请求选项 / The request options</param>
        /// <returns>响应报文任务 / A task returning the response message</returns>
        /// <remarks>
        /// 按 UseBody 分两条路径：UseBody=false 时非 POST 请求的 InputData 拼入 query string（值类型按名拼接、IEnumerable 逐项同名追加、其余对象按属性展开），
        /// POST 请求以 FormUrlEncoded 表单提交；UseBody=true 时 InputData 整体序列化为 JSON body。
        /// 拦截器在发送前（HandleHttpRequestAsync）与响应后（HandleHttpResponseAsync）介入，后者返回非 null 的新响应时释放并替换原响应。
        /// Two paths by UseBody: when false, InputData goes to the query string for non-POST requests (value types by name, IEnumerable item by item, other objects flattened by property)
        /// and POST requests are submitted as a FormUrlEncoded form; when true, InputData is serialized as a whole into a JSON body.
        /// The interceptor hooks before sending (HandleHttpRequestAsync) and after receiving (HandleHttpResponseAsync); a non-null new response from the latter disposes and replaces the original.
        /// </remarks>
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
        /// Sends an HTTP request asynchronously and returns the string result
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">请求选项 / The request options</param>
        /// <returns>响应文本任务 / A task returning the response text</returns>
        public static async Task<string?> RequestStringAsync(this HttpClient client, string url, SuperRequestOptions? options = null)
        {
            using (HttpResponseMessage response = (await RequestMessageAsync(client, url, options).ConfigureAwait(false))!)
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 异步发送HTTP请求并得到结果
        /// Sends an HTTP request asynchronously and returns the deserialized result
        /// </summary>
        /// <typeparam name="M">响应结果类型（JSON 反序列化）/ The response result type (deserialized from JSON)</typeparam>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">请求选项 / The request options</param>
        /// <returns>反序列化结果；响应为空白时返回 default(M) / The deserialized result, or default(M) when the response is blank</returns>
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
        /// Sends an HTTP request asynchronously and returns the binary result
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">请求选项 / The request options</param>
        /// <returns>响应字节内容任务 / A task returning the response byte content</returns>
        public static async Task<byte[]> RequestBufferAsync(this HttpClient client, string url, SuperRequestOptions? options = null)
        {
            using (HttpResponseMessage response = (await RequestMessageAsync(client, url, options).ConfigureAwait(false))!)
            {
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 异步发送HTTP请求并得到字符串结果
        /// Sends an HTTP request asynchronously with strongly-typed options and returns the string result
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型请求选项，转换时仅复制 Method/InputData/Headers/Cookies/UseBody（不含 Handler 与 Config）/ The strongly-typed options; the conversion copies only Method/InputData/Headers/Cookies/UseBody (not Handler or Config)</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应文本任务 / A task returning the response text</returns>
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
        /// Sends an HTTP request asynchronously with strongly-typed options and returns the deserialized result
        /// </summary>
        /// <typeparam name="M">响应结果类型（JSON 反序列化）/ The response result type (deserialized from JSON)</typeparam>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型请求选项，转换时仅复制 Method/InputData/Headers/Cookies/UseBody（不含 Handler 与 Config）/ The strongly-typed options; the conversion copies only Method/InputData/Headers/Cookies/UseBody (not Handler or Config)</param>
        /// <returns>反序列化结果；响应为空时返回 default(M) / The deserialized result, or default(M) when the response is empty</returns>
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
        /// Sends an HTTP request asynchronously with strongly-typed options and returns the binary result
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型请求选项，转换时仅复制 Method/InputData/Headers/Cookies/UseBody（不含 Handler 与 Config）/ The strongly-typed options; the conversion copies only Method/InputData/Headers/Cookies/UseBody (not Handler or Config)</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应字节内容任务 / A task returning the response byte content</returns>
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
        /// Sends an HTTP request asynchronously with strongly-typed options and returns the response message
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型请求选项，转换时仅复制 Method/InputData/Headers/Cookies/UseBody（不含 Handler 与 Config）/ The strongly-typed options; the conversion copies only Method/InputData/Headers/Cookies/UseBody (not Handler or Config)</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应报文任务 / A task returning the response message</returns>
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
        /// Submits a multipart/form-data request asynchronously with strongly-typed options
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型 multipart 请求选项，转换时仅复制 Method/InputData/Headers/Cookies/UseBody（不含 Handler、Config 与 UploadFiles）/ The strongly-typed multipart options; the conversion copies only Method/InputData/Headers/Cookies/UseBody (not Handler, Config or UploadFiles)</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应报文任务 / A task returning the response message</returns>
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
        /// Submits a multipart/form-data request asynchronously
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">multipart 请求选项 / The multipart request options</param>
        /// <returns>响应报文任务 / A task returning the response message</returns>
        /// <remarks>
        /// 组装 MultipartFormDataContent：InputData 为文本字段、UploadFiles 为文件（字段名/文件名缺省 "UploadFile"）；
        /// 拦截器在发送前与响应后介入（同普通模式），后者返回非 null 的新响应时替换原响应；
        /// 响应非 200 时抛出 SuperHttpException（与普通请求模式不抛异常的行为不同）。
        /// Builds a MultipartFormDataContent: InputData becomes text fields and UploadFiles the files (field/file names default to "UploadFile");
        /// the interceptor hooks before sending and after receiving like the plain mode, and a non-null new response replaces the original;
        /// a non-200 response throws a SuperHttpException (unlike the plain request mode, which does not throw).
        /// </remarks>
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
        /// Submits a multipart/form-data request asynchronously and returns the string result
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">multipart 请求选项 / The multipart request options</param>
        /// <returns>响应文本任务 / A task returning the response text</returns>
        public static async Task<string?> RequestStringAsync(this HttpClient client, string url, HttpRequestMultipartOptions? options = null)
        {
            using (HttpResponseMessage response = await RequestMessageAsync(client, url, options).ConfigureAwait(false))
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求并得到结果
        /// Submits a multipart/form-data request asynchronously and returns the deserialized result
        /// </summary>
        /// <typeparam name="M">响应结果类型（JSON 反序列化）/ The response result type (deserialized from JSON)</typeparam>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">multipart 请求选项 / The multipart request options</param>
        /// <returns>反序列化结果；响应为空时返回 default(M) / The deserialized result, or default(M) when the response is empty</returns>
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
        /// Submits a multipart/form-data request asynchronously and returns the binary result
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">multipart 请求选项 / The multipart request options</param>
        /// <returns>响应字节内容任务 / A task returning the response byte content</returns>
        public static async Task<byte[]?> RequestBufferAsync(this HttpClient client, string url, HttpRequestMultipartOptions? options = null)
        {
            using (HttpResponseMessage response = await RequestMessageAsync(client, url, options).ConfigureAwait(false))
            {
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 异步以MultipartFormData的方式提交表单数据请求并得到字符串结果
        /// Submits a multipart/form-data request asynchronously with strongly-typed options and returns the string result
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型 multipart 请求选项，转换时仅复制 Method/InputData/Headers/Cookies/UseBody（不含 Handler、Config 与 UploadFiles）/ The strongly-typed multipart options; the conversion copies only Method/InputData/Headers/Cookies/UseBody (not Handler, Config or UploadFiles)</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应文本任务 / A task returning the response text</returns>
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
        /// Submits a multipart/form-data request asynchronously with strongly-typed options and returns the deserialized result
        /// </summary>
        /// <typeparam name="M">响应结果类型（JSON 反序列化）/ The response result type (deserialized from JSON)</typeparam>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型 multipart 请求选项，转换时仅复制 Method/InputData/Headers/Cookies/UseBody（不含 Handler、Config 与 UploadFiles）/ The strongly-typed multipart options; the conversion copies only Method/InputData/Headers/Cookies/UseBody (not Handler, Config or UploadFiles)</param>
        /// <returns>反序列化结果；响应为空时返回 default(M) / The deserialized result, or default(M) when the response is empty</returns>
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
        /// Submits a multipart/form-data request asynchronously with strongly-typed options and returns the binary result
        /// </summary>
        /// <param name="client">HTTP 客户端 / The HTTP client</param>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型 multipart 请求选项，转换时仅复制 Method/InputData/Headers/Cookies/UseBody（不含 Handler、Config 与 UploadFiles）/ The strongly-typed multipart options; the conversion copies only Method/InputData/Headers/Cookies/UseBody (not Handler, Config or UploadFiles)</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应字节内容任务 / A task returning the response byte content</returns>
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
