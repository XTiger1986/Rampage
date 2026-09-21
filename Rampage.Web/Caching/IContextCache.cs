using Rampage.Caching;
using System;

namespace Rampage.Web.Caching
{
	/// <summary>
	/// HTTP请求上下文缓存类接口
	/// Cache In HttpRequest Scope interface
	/// </summary>
	public interface IContextCache : IDisposable
	{

		/// <summary>
		/// 尝试获取上下文缓存值 / Tries to get a value from the context cache.
		/// </summary>
		/// <typeparam name="T">值类型 / The value type.</typeparam>
		/// <param name="key">键名 / The cache key.</param>
		/// <param name="value">获取到的值，失败时为类型默认值 / The retrieved value, or the type default on failure.</param>
		/// <returns>存在且类型匹配返回 true / true when the key exists and the value matches T.</returns>
		bool TryGetValue<T>(string key, out T? value);

		/// <summary>
		/// 上下文缓存是否包含某个键值 / Determines whether the context cache contains a key.
		/// </summary>
		/// <param name="key">键名 / The cache key.</param>
		/// <returns>包含返回 true / true when contained.</returns>
		bool Contains(string key);

		/// <summary>
		/// 设置上下文缓存值 / Sets a context cache value.
		/// </summary>
		/// <param name="key">键名 / The cache key.</param>
		/// <param name="value">键值 / The value.</param>
		void Set(string key, object value);

		/// <summary>
		/// 移除上下文缓存值 / Removes a context cache value.
		/// </summary>
		/// <param name="key">键名 / The cache key.</param>
		void Remove(string key);

		/// <summary>
		/// 移除指定前缀的上下文缓存 / Removes context cache entries whose key starts with the given prefix.
		/// </summary>
		/// <param name="keyPrefix">键名前缀 / The key prefix.</param>
		void RemoveBySearch(string keyPrefix);

	}
}
