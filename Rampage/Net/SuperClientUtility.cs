using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace Rampage.Net
{
	/// <summary>
	/// 
	/// </summary>
	public class SuperClientUtility
	{

		/// <summary>
		/// 全局共享连接处理器：连接池化 + 定期回收连接（DNS 变更可自动生效）+ 自动解压
		/// </summary>
		private static readonly SocketsHttpHandler _sharedHandler = new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(5),
			AutomaticDecompression = DecompressionMethods.All
		};

		private static readonly ConcurrentDictionary<string, HttpClient> _httpClients = new ConcurrentDictionary<string, HttpClient>();

        /// <summary>
        /// 从请求的地址获取HTTP客户端实例
        /// Get an HTTP client instance from the requested address
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<HttpResponseMessage?> RequestMessageAsync(string url, SuperRequestOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestMessageAsync(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到响应结果
        /// Send an HTTP request and get the response result
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<HttpResponseMessage?> RequestMessageAsync<T>(string url, HttpRequestOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestMessageAsync<T>(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到字符串结果
        /// Send an HTTP request and get the string result
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<string?> RequestStringAsync(string url, SuperRequestOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestStringAsync(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到结果
        /// Send an HTTP request and get the result
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<M?> RequestAsync<M>(string url, SuperRequestOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestAsync<M>(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到字符串结果
        /// Send an HTTP request and get the string result
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<string?> RequestStringAsync<T>(string url, HttpRequestOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestStringAsync<T>(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到结果
        /// Send an HTTP request and get the result
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<M?> RequestAsync<M, T>(string url, HttpRequestOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestAsync<M, T>(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到二进制流
        /// Send an HTTP request and get a binary stream
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<byte[]> RequestBufferAsync(string url, SuperRequestOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestBufferAsync(url, options);
		}

        /// <summary>
        /// 发送HTTP请求并得到二进制流
        /// Send an HTTP request and get a binary stream
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<byte[]> RequestBufferAsync<T>(string url, HttpRequestOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestBufferAsync<T>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求
        /// Submit form data requests in MultipartFormData mode
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> RequestMessageAsync(string url, HttpRequestMultipartOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestMessageAsync(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求
        /// Submit form data requests in MultipartFormData mode
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<HttpResponseMessage> RequestMessageAsync<T>(string url, HttpRequestMultipartOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestMessageAsync<T>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到字符串结果
        /// Submit form data requests in MultipartFormData format and get string results
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<string?> RequestStringAsync(string url, HttpRequestMultipartOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestStringAsync(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到结果
        /// Submit form data requests using MultipartFormData and get the results
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<M?> RequestAsync<M>(string url, HttpRequestMultipartOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestAsync<M>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到字符串结果
        /// Submit form data requests in MultipartFormData format and get string results
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<string?> RequestStringAsync<T>(string url, HttpRequestMultipartOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestStringAsync<T>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到结果
        /// Submit form data requests using MultipartFormData and get the results
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="M"></typeparam>
        /// <returns></returns>
        public static Task<M?> RequestAsync<M, T>(string url, HttpRequestMultipartOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestAsync<M, T>(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到二进制结果
        /// Submit form data requests using MultipartFormData and get binary results
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<byte[]?> RequestBufferAsync(string url, HttpRequestMultipartOptions? options = null)
		{
			HttpClient client = GetClient(url);
			return client.RequestBufferAsync(url, options);
		}

        /// <summary>
        /// 以MultipartFormData的方式提交表单数据请求并得到二进制结果
        /// Submit form data requests using MultipartFormData and get binary results
        /// </summary>
        /// <param name="url"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task<byte[]?> RequestBufferAsync<T>(string url, HttpRequestMultipartOptions<T>? options)
		{
			HttpClient client = GetClient(url);
			return client.RequestBufferAsync<T>(url, options);
		}


	}
}
