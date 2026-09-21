using System;
using System.Diagnostics;

namespace Rampage.Web.Utilities
{
    /// <summary>
    /// 请求上下文耗时监视器，按名称管理请求内的 Stopwatch / Request-scoped timing monitor that manages named Stopwatches within the current request.
    /// </summary>
    /// <remarks>计时数据经 ContextCacheTool 存于 HttpContext.Items，仅当前请求内有效。</remarks>
    public class ContextMonitor
    {
        /// <summary>
        /// 获取（不存在则创建）当前请求的计时器字典 / Gets (creating when absent) the stopwatch dictionary of the current request.
        /// </summary>
        /// <returns>计时器字典 / The stopwatch dictionary.</returns>
        private static Dictionary<string, Stopwatch> GetContextWatches()
        {
            Dictionary<string, Stopwatch>? watches;
            string key = "ContextWatches";
            if (!ContextCacheTool.TryGetValue<Dictionary<string, Stopwatch>>(key, out watches))
            {
                watches = new Dictionary<string, Stopwatch>();
                ContextCacheTool.Set(key, watches);
            }
            return watches!;
        }

        /// <summary>
        /// 获取指定名称的 Stopwatch，不存在则创建 / Gets the Stopwatch with the given name, creating it when absent.
        /// </summary>
        /// <param name="name">计时器名称 / The stopwatch name.</param>
        /// <returns>对应的 Stopwatch / The Stopwatch instance.</returns>
        public static Stopwatch GetWatch(string name)
        {
            Dictionary<string, Stopwatch> watches = GetContextWatches();
            Stopwatch watch;
            if (!watches.ContainsKey(name))
            {
                watch = new Stopwatch();
                watches.Add(name, watch);

                ContextCacheTool.Set("ContextWatches", watches);
            }
            else
            {
                watch = watches[name];
            }

            return watch;
        }

        /// <summary>
        /// 启动计时器 / Starts a Stopwatch.
        /// </summary>
        /// <param name="name">计时器名称；null 或空时使用默认键（页面处理耗时）/ The stopwatch name; null/empty uses the default page-monitor key.</param>
        public static void Start(string? name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = WebGlobals.PAGE_MONITOR_KEY;
            }
            Stopwatch watch = GetWatch(name);
            watch.Start();
        }

        /// <summary>
        /// 停止计时器 / Stops a Stopwatch.
        /// </summary>
        /// <param name="name">计时器名称；null 或空时使用默认键（页面处理耗时）/ The stopwatch name; null/empty uses the default page-monitor key.</param>
        public static void Stop(string? name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = WebGlobals.PAGE_MONITOR_KEY;
            }
            Stopwatch watch = GetWatch(name);
            watch.Stop();
        }

        /// <summary>
        /// 获取计时器的已耗时毫秒数 / Gets the elapsed milliseconds of a Stopwatch.
        /// </summary>
        /// <param name="name">计时器名称；null 或空时使用默认键（页面处理耗时）/ The stopwatch name; null/empty uses the default page-monitor key.</param>
        /// <returns>已耗时（毫秒）/ The elapsed time in milliseconds.</returns>
        public static double GetElapsedMilliseconds(string? name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = WebGlobals.PAGE_MONITOR_KEY;
            }
            Stopwatch watch = GetWatch(name);
            return watch.Elapsed.TotalMilliseconds;
        }

    }
}
