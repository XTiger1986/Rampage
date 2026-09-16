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
		/// 尝试获取上下文缓存值
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		bool TryGetValue<T>(string key, out T? value);

		/// <summary>
		/// 上下文缓存是否包含某个键值
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		bool Contains(string key);

		/// <summary>
		/// 设置上下文缓存值
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		void Set(string key, object value);

		/// <summary>
		/// 移除上下文缓存值
		/// </summary>
		/// <param name="key"></param>
		void Remove(string key);

		/// <summary>
		/// 移除指定前缀的上下文缓存
		/// </summary>
		/// <param name="keyPrefix"></param>
		void RemoveBySearch(string keyPrefix);

	}
}
