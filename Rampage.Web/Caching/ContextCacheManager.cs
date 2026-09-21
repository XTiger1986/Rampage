using Microsoft.AspNetCore.Http;
using Rampage.Utilities;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Web.Caching
{
    /// <summary>
    /// HTTP 请求上下文缓存管理器，封装 HttpContext.Items 的请求级键值存取（请求结束即失效）/ HTTP request context cache manager wrapping HttpContext.Items for request-scoped key-value storage (data dies with the request).
    /// </summary>
    public class ContextCacheManager : IContextCache
    {
        /// <summary>
        /// 当前请求 HttpContext（来自 RequestContext.Current，可为 null）/ The current HttpContext (from RequestContext.Current; may be null).
        /// </summary>
        public HttpContext? HttpContext => RequestContext.Current;

        /// <summary>
        /// 尝试获取上下文缓存值 / Tries to get a value from the context cache.
        /// </summary>
        /// <typeparam name="T">值类型 / The value type.</typeparam>
        /// <param name="key">键名 / The cache key.</param>
        /// <param name="value">获取到的值，失败时为类型默认值 / The retrieved value, or the type default on failure.</param>
        /// <returns>存在且类型匹配返回 true / true when the key exists and the value matches T.</returns>
        public bool TryGetValue<T>(string key, out T? value)
        {
            if (HttpContext == null)
            {
                value = default(T);
                return false;
            }
            if (!HttpContext.Items.ContainsKey(key))
            {
                value = default(T);
                return false;
            }
            else
            {
                if (HttpContext.Items[key] is T)
                {
                    value = (T)HttpContext.Items[key]!;
                    return true;
                }
            }
            value = default(T);
            return false;
        }

        /// <summary>
        /// 上下文缓存是否包含某个键值 / Determines whether the context cache contains a key.
        /// </summary>
        /// <param name="key">键名 / The cache key.</param>
        /// <returns>包含返回 true；无请求上下文返回 false / true when contained; false when there is no request context.</returns>
        public bool Contains(string key)
        {
            return HttpContext == null ? false : (HttpContext.Items.ContainsKey(key));
        }

        /// <summary>
        /// 设置上下文缓存值 / Sets a context cache value.
        /// </summary>
        /// <param name="key">键名 / The cache key.</param>
        /// <param name="value">键值；为 null 或无请求上下文时不做任何事 / The value; a null value or no request context is a no-op.</param>
        public void Set(string key, object value)
        {
            if (value == null || HttpContext == null)
                return;

            HttpContext.Items[key] = value;
        }

        /// <summary>
        /// 移除上下文缓存值 / Removes a context cache value.
        /// </summary>
        /// <param name="key">键名；无请求上下文时不做任何事 / The cache key; no-op when there is no request context.</param>
        public void Remove(string key)
        {
            if (HttpContext == null) { return; }
            HttpContext.Items.Remove(key);
        }

        /// <summary>
        /// 移除指定前缀的上下文缓存 / Removes context cache entries whose key starts with the given prefix.
        /// </summary>
        /// <param name="keyPrefix">键名前缀（忽略大小写）/ The key prefix (case-insensitive).</param>
        /// <remarks>先收集再删除以避免遍历时修改集合；单个键删除失败会被吞掉。</remarks>
        public void RemoveBySearch(string keyPrefix)
        {
            if (string.IsNullOrEmpty(keyPrefix) || HttpContext == null)
                return;

            List<string> keys = new List<string>();

            foreach (var elem in HttpContext.Items)
            {
                string? key = elem.Key.ToString();

                if (!string.IsNullOrEmpty(key) && StringTool.StartsWithIgnoreCase(key, keyPrefix))
                    keys.Add(key);
            }

            foreach (string key in keys)
            {
                try
                {
                    HttpContext.Items.Remove(key);
                }
                catch { }
            }
        }

        /// <summary>
        /// 释放资源（当前无资源需要释放，仅为满足接口契约）/ Releases resources (nothing to release; kept for the interface contract).
        /// </summary>
        public virtual void Dispose()
        {

        }

    }
}
