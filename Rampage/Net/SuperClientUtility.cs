using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace Rampage.Net
{
	/// <summary>
	/// HTTP 客户端工具：按 scheme+host[:port] 缓存共享的 HttpClient 实例，并提供各形态请求的静态快捷入口
	/// HTTP client utility: caches shared HttpClient instances by scheme+host[:port] and provides static shortcuts for various request shapes
	/// </summary>
	public class SuperClientUtility
	{

		/// <summary>
		/// 全局共享连接处理器：连接池化 + 定期回收连接（DNS 变更可自动生效）+ 自动解压
		/// The handler shared by all clients: pooled connections with periodic recycling (picks up DNS changes) and automatic decompression
		/// </summary>
		private static readonly SocketsHttpHandler _sharedHandler = new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(5),
			AutomaticDecompression = DecompressionMethods.All
		};

		// 按 scheme+host[:port] 缓存的客户端实例，全部复用 _sharedHandler / Client instances cached by scheme+host[:port], all reusing _sharedHandler
		private static readonly ConcurrentDictionary<string, HttpClient> _httpClients = new ConcurrentDictionary<string, HttpClient>();

        /// <summary>
        /// 从请求的地址获取（或创建并缓存）HTTP 客户端实例
        /// Gets (or creates and caches) an HTTP client instance for the requested address
        /// </summary>
        /// <param name="url">绝对 URL / An absolute URL</param>
        /// <returns>以 scheme+host[:port] 为键缓存的 <see cref="HttpClient"/> 实例 / The cached <see cref="HttpClient"/> instance keyed by scheme+host[:port]</returns>
        /// <exception cref="ArgumentException">url 不是合法的绝对地址时抛出 / Thrown when url is not a valid absolute URI</exception>
        /// <remarks>
        /// 客户端按 scheme+host[+port] 缓存于 ConcurrentDictionary，全部共享同一个 SocketsHttpHandler（连接池 5 分钟回收、自动解压）；
        /// 缓存键中 80 端口省略、其余端口（含 443）保留，因此同一主机的 http 与 https 会得到不同实例。
        /// Clients are cached in a ConcurrentDictionary by scheme+host[+port] and share a single SocketsHttpHandler (5-minute pooled connection lifetime, automatic decompression);
        /// port 80 is omitted from the cache key while other ports (including 443) are kept, so http and https on the same host yield different instances.
        /// </remarks>
        public static HttpClient GetClient(string url)
		{
			string baseAddress;
			Uri? uri;
			if (Uri.TryCreate(url, UriKind.Absolute, out uri))
			{
				baseAddress = string.Concat(uri.GetLeftPart(UriPartial.Scheme), uri.Host, uri.Port == 80 ? string.Empty : ":" + uri.Port);
			}
			else 
			{
				throw new ArgumentException("invalid url!", "url");
			}

			return _httpClients.GetOrAdd(baseAddress, _ =>
			{
				HttpClient client = new HttpClient(_sharedHandler, disposeHandler: false);
				client.BaseAddress = new Uri(baseAddress);
				return client;
			});
		}


        /// <summary>
        /// 发送HTTP请求并得到响应结果
        /// Send an HTTP request and get the response result
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">请求选项 / The request options</param>
        /// <returns>响应报文任务 / A task returning the response message</returns>
        public static Task<HttpResponseMessage?> RequestMessageAsync(string url, SuperRequestOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestMessageAsync(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到响应结果
        /// Send an HTTP request and get the response result
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型请求选项 / The strongly-typed request options</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应报文任务 / A task returning the response message</returns>
        public static Task<HttpResponseMessage?> RequestMessageAsync<T>(string url, HttpRequestOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestMessageAsync<T>(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到字符串结果
        /// Send an HTTP request and get the string result
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">请求选项 / The request options</param>
        /// <returns>响应文本任务 / A task returning the response text</returns>
        public static Task<string?> RequestStringAsync(string url, SuperRequestOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestStringAsync(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到结果
        /// Send an HTTP request and get the result
        /// </summary>
        /// <typeparam name="M">响应结果类型（JSON 反序列化）/ The response result type (deserialized from JSON)</typeparam>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">请求选项 / The request options</param>
        /// <returns>反序列化结果；响应为空时返回 default / The deserialized result, or default when the response is empty</returns>
        public static Task<M?> RequestAsync<M>(string url, SuperRequestOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestAsync<M>(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到字符串结果
        /// Send an HTTP request and get the string result
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型请求选项 / The strongly-typed request options</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应文本任务 / A task returning the response text</returns>
        public static Task<string?> RequestStringAsync<T>(string url, HttpRequestOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestStringAsync<T>(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到结果
        /// Send an HTTP request and get the result
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型请求选项 / The strongly-typed request options</param>
        /// <typeparam name="M">响应结果类型（JSON 反序列化）/ The response result type (deserialized from JSON)</typeparam>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>反序列化结果；响应为空时返回 default / The deserialized result, or default when the response is empty</returns>
        public static Task<M?> RequestAsync<M, T>(string url, HttpRequestOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestAsync<M, T>(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到二进制流
        /// Send an HTTP request and get a binary stream
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">请求选项 / The request options</param>
        /// <returns>响应字节内容任务 / A task returning the response byte content</returns>
        public static Task<byte[]> RequestBufferAsync(string url, SuperRequestOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestBufferAsync(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到二进制流
        /// Send an HTTP request and get a binary stream
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型请求选项 / The strongly-typed request options</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应字节内容任务 / A task returning the response byte content</returns>
        public static Task<byte[]> RequestBufferAsync<T>(string url, HttpRequestOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestBufferAsync<T>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求
        /// Submit form data requests in MultipartFormData mode
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">multipart 请求选项 / The multipart request options</param>
        /// <returns>响应报文任务 / A task returning the response message</returns>
        /// <remarks>响应非 200 时抛出 SuperHttpException（与普通请求模式不抛异常的行为不同）/ Throws a SuperHttpException when the response is not 200 (unlike the plain request mode, which does not throw)</remarks>
        public static Task<HttpResponseMessage> RequestMessageAsync(string url, HttpRequestMultipartOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestMessageAsync(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求
        /// Submit form data requests in MultipartFormData mode
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型 multipart 请求选项 / The strongly-typed multipart request options</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应报文任务 / A task returning the response message</returns>
        public static Task<HttpResponseMessage> RequestMessageAsync<T>(string url, HttpRequestMultipartOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestMessageAsync<T>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到字符串结果
        /// Submit form data requests in MultipartFormData format and get string results
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">multipart 请求选项 / The multipart request options</param>
        /// <returns>响应文本任务 / A task returning the response text</returns>
        public static Task<string?> RequestStringAsync(string url, HttpRequestMultipartOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestStringAsync(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到结果
        /// Submit form data requests using MultipartFormData and get the results
        /// </summary>
        /// <typeparam name="M">响应结果类型（JSON 反序列化）/ The response result type (deserialized from JSON)</typeparam>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">multipart 请求选项 / The multipart request options</param>
        /// <returns>反序列化结果；响应为空时返回 default / The deserialized result, or default when the response is empty</returns>
        public static Task<M?> RequestAsync<M>(string url, HttpRequestMultipartOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestAsync<M>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到字符串结果
        /// Submit form data requests in MultipartFormData format and get string results
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型 multipart 请求选项 / The strongly-typed multipart request options</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应文本任务 / A task returning the response text</returns>
        public static Task<string?> RequestStringAsync<T>(string url, HttpRequestMultipartOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestStringAsync<T>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到结果
        /// Submit form data requests using MultipartFormData and get the results
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型 multipart 请求选项 / The strongly-typed multipart request options</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <typeparam name="M">响应结果类型（JSON 反序列化）/ The response result type (deserialized from JSON)</typeparam>
        /// <returns>反序列化结果；响应为空时返回 default / The deserialized result, or default when the response is empty</returns>
        public static Task<M?> RequestAsync<M, T>(string url, HttpRequestMultipartOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestAsync<M, T>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到二进制结果
        /// Submit form data requests using MultipartFormData and get binary results
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">multipart 请求选项 / The multipart request options</param>
        /// <returns>响应字节内容任务 / A task returning the response byte content</returns>
        public static Task<byte[]?> RequestBufferAsync(string url, HttpRequestMultipartOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestBufferAsync(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到二进制结果
        /// Submit form data requests using MultipartFormData and get binary results
        /// </summary>
        /// <param name="url">请求 URL / The request URL</param>
        /// <param name="options">强类型 multipart 请求选项 / The strongly-typed multipart request options</param>
        /// <typeparam name="T">输入模型类型 / The input model type</typeparam>
        /// <returns>响应字节内容任务 / A task returning the response byte content</returns>
        public static Task<byte[]?> RequestBufferAsync<T>(string url, HttpRequestMultipartOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestBufferAsync<T>(url, options);
		}


	}
}
