using System;
using System.Diagnostics;

namespace Rampage.Web.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class ContextMonitor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
        /// Start a Stopwatch
        /// </summary>
        /// <param name="name"></param>
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
        /// Stop the Stopwatch
        /// </summary>
        /// <param name="name"></param>
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
        /// Get Elapsed Milliseconds of the Stopwatch
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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
