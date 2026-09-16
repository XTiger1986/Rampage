using Microsoft.AspNetCore.Http;
using Rampage.Utilities;
using Rampage.Web.Extensions;
using System;

namespace Rampage.Web.Utilities
{
    /// <summary>
    /// 上下文缓存助手
    /// </summary>
    public class ContextCacheTool
    {

        /// <summary>
        /// 尝试获取上下文缓存值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetValue<T>(string key, out T? value)
        {
            HttpContext? context = RequestContext.Current;
            if (context == null || !context.Items.ContainsKey(key))
            {
                value = default(T);
                return false;
            }
            else
            {
                if (context.Items[key] is T)
                {
                    value = (T)context.Items[key]!;
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
        public static bool Contains(string key)
        {
            return RequestContext.Current == null? false : (RequestContext.Current.Items.ContainsKey(key));
        }

        /// <summary>
        /// 设置上下文缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set(string key, object value)
        {
            if (value == null || RequestContext.Current == null)
                return;

            RequestContext.Current.Items[key] = value;
        }

        /// <summary>
        /// 移除上下文缓存值
        /// </summary>
        /// <param name="key"></param>
        public static void Remove(string key)
        {
            if (RequestContext.Current == null) { return; }
            RequestContext.Current.Items.Remove(key);
        }

        /// <summary>
        /// 移除指定前缀的上下文缓存
        /// </summary>
        /// <param name="keyPrefix"></param>
        public static void RemoveBySearch(string keyPrefix)
        {
            if (string.IsNullOrEmpty(keyPrefix) || RequestContext.Current == null)
                return;

            HttpContext context = RequestContext.Current!;
            List<string> keys = new List<string>();

            foreach (var elem in context.Items)
            {
                string? key = elem.Key.ToString();

                if (!string.IsNullOrEmpty(key) && StringTool.StartsWithIgnoreCase(key, keyPrefix))
                    keys.Add(key);
            }

            foreach (string key in keys)
            {
                try
                {
                    context.Items.Remove(key);
                }
                catch { }
            }
        }
    }
}
