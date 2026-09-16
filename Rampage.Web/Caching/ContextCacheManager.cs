using Microsoft.AspNetCore.Http;
using Rampage.Utilities;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Web.Caching
{
    /// <summary>
    /// 
    /// </summary>
    public class ContextCacheManager : IContextCache
    {
        public HttpContext? HttpContext => RequestContext.Current;

        /// <summary>
        /// 尝试获取上下文缓存值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// 上下文缓存是否包含某个键值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(string key)
        {
            return HttpContext == null ? false : (HttpContext.Items.ContainsKey(key));
        }

        /// <summary>
        /// 设置上下文缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(string key, object value)
        {
            if (value == null || HttpContext == null)
                return;

            HttpContext.Items[key] = value;
        }

        /// <summary>
        /// 移除上下文缓存值
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            if (HttpContext == null) { return; }
            HttpContext.Items.Remove(key);
        }

        /// <summary>
        /// 移除指定前缀的上下文缓存
        /// </summary>
        /// <param name="keyPrefix"></param>
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
        /// 
        /// </summary>
        public virtual void Dispose()
        {

        }

    }
}
