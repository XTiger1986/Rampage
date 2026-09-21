using Rampage.Log;
using System;

namespace Rampage.Threading
{
    /// <summary>
    /// 多线程批处理工作者：按批次名注册处理器，将数据分块交给线程池并行执行，块内以 WaitAll 屏障同步
    /// Batched multi-threaded worker: registers handlers per batch name, chunks data to the thread pool and synchronizes each chunk with a WaitAll barrier
    /// </summary>
    public class ThreadingWorker
    {

        private static readonly object _workHandlerContainerLock = new object();

        private static Dictionary<string, List<Action<object?>>> WorkHandlerContainer
        {
            get;
            set;
        }

        static ThreadingWorker()
        {
            lock (_workHandlerContainerLock)
            {
                WorkHandlerContainer = new Dictionary<string, List<Action<object?>>> ();
            }
        }


        /// <summary>
        /// 向指定批次注册工作处理器 / Register a work handler for the batch
        /// </summary>
        /// <param name="batchName">批次名 / The batch name</param>
        /// <param name="workHandler">处理器委托；同一委托重复注册会被忽略 / The handler delegate; registering the same delegate twice is ignored</param>
        public static void Register(string batchName, Action<object?> workHandler)
        {
            if (!WorkHandlerContainer.ContainsKey(batchName))
            {
                lock (_workHandlerContainerLock)
                {
                    if (!WorkHandlerContainer.ContainsKey(batchName))
                    {
                        WorkHandlerContainer.Add(batchName, new List<Action<object?>>());
                    }
                }
            }

            List<Action<object?>> handlers = WorkHandlerContainer[batchName];
            if (!handlers.Contains(workHandler))
            {
                handlers.Add(workHandler);
            }
        }

        /// <summary>
        /// 移除指定批次的全部处理器 / Remove all handlers of the specified batch
        /// </summary>
        /// <param name="batchName">批次名 / The batch name</param>
        public static void Clear(string batchName)
        {
            if (WorkHandlerContainer.ContainsKey(batchName))
            {
                lock (_workHandlerContainerLock)
                {
                    if (WorkHandlerContainer.ContainsKey(batchName))
                    {
                        WorkHandlerContainer.Remove(batchName);
                    }
                }
            }
        }

        /// <summary>
        /// 清空全部批次的处理器 / Remove the handlers of all batches
        /// </summary>
        public static void ClearAll()
        {
            lock (_workHandlerContainerLock)
            {
                WorkHandlerContainer.Clear();
            }
        }


        /// <summary>
        /// 对数据列表按批次执行已注册的处理器 / Run the registered handlers of the batch over the data list
        /// </summary>
        /// <typeparam name="T">数据元素类型 / The data element type</typeparam>
        /// <param name="dataList">待处理数据；批次未注册时直接返回 / The data to process; returns immediately when the batch is unregistered</param>
        /// <param name="batchName">批次名 / The batch name</param>
        /// <param name="maxThreads">每块最大并行数；为 1 时退化为同步串行执行 / Max parallel workers per chunk; 1 degrades to synchronous sequential execution</param>
        /// <param name="millisecondsTimeout">每块的等待超时（毫秒）；默认 null 表示无限等待 / Per-chunk wait timeout in milliseconds; null waits indefinitely</param>
        /// <remarks>
        /// 数据按 maxThreads 分块逐块执行：块内任务经 ThreadPool 并行，WaitAll 屏障等待全部完成后才进入下一块；
        /// Data is processed chunk by chunk (maxThreads items each): within a chunk tasks run in parallel on the thread pool and the worker waits for all of them before the next chunk;
        /// 指定超时且等待超时后，仍会无限期等待块内尾任务完成（仅记录超时错误日志），避免半成品数据流入下一批/下一阶段；
        /// When a timeout is specified and exceeded, the worker still waits indefinitely for stragglers (only a timeout error is logged) so half-processed data never leaks into the next batch/stage;
        /// 处理器抛出的异常会被记录日志，不影响同块其他任务的执行与信号置位。
        /// Exceptions thrown by handlers are logged and do not affect sibling tasks in the same chunk or their completion signals.
        /// </remarks>
        public static void Work<T>(IList<T> dataList, string batchName, int maxThreads, int? millisecondsTimeout = null)
        {
            int dataCount = dataList.Count;
            if (dataCount == 0)
            {
                return;
            }
            if (!WorkHandlerContainer.ContainsKey(batchName))
            {
                return;
            }

            List<Action<object?>> workHandlers = WorkHandlerContainer[batchName];

            if (maxThreads == 1)
            {
                foreach (T item in dataList)
                {
                    foreach (Action<object?> handler in workHandlers)
                    {
                        handler(item);
                    }
                }
                return;
            }

            if (dataCount == 1)
            {
                foreach (Action<object?> handler in workHandlers)
                {
                    handler(dataList.First());
                }
                return;
            }

            for (int t = 0; t < dataCount; t += maxThreads)
            {
                int threadNum = maxThreads;
                if (t + maxThreads > dataCount && dataCount % maxThreads > 0)
                {
                    threadNum = dataCount % maxThreads;
                }

                ManualResetEvent[] resetEvents = new ManualResetEvent[threadNum];
                for (int i = t, j = 0; i < t + threadNum; i++, j++)
                {
                    resetEvents[j] = new ManualResetEvent(false);

                    T data = dataList[i];
                    ThreadStateInfo stateInfo = new ThreadStateInfo();
                    stateInfo.Data = data;
                    stateInfo.ResetEvent = resetEvents[j];
                    stateInfo.ThreadIndex = j;
                    stateInfo.BatchName = batchName;

                    ThreadPool.QueueUserWorkItem(new WaitCallback((oState) =>
                    {
                        ThreadStateInfo? threadStateInfo = null;
                        ManualResetEvent? resetEvent = null;
                        try
                        {
                            threadStateInfo = (ThreadStateInfo)oState!;
                            resetEvent = (ManualResetEvent)threadStateInfo.ResetEvent!;

                            string stateBatchName = threadStateInfo.BatchName!;
                            if (!WorkHandlerContainer.ContainsKey(stateBatchName))
                            {
                                return;
                            }

                            List<Action<object?>> stateWorkHandlers = WorkHandlerContainer[stateBatchName];
                            foreach (Action<object?> handler in stateWorkHandlers)
                            {
                                handler(threadStateInfo.Data);
                            }
                        }
                        catch (Exception ex)
                        {
                            RampageLogger.Error(ex);
                        }
                        finally
                        {
                            if (resetEvent != null)
                            {
                                resetEvent.Set();
                            }
                        }
                    }), stateInfo);
                }

                if (millisecondsTimeout != null)
                {
                    if (!WaitHandle.WaitAll(resetEvents, millisecondsTimeout.Value))
                    {
                        // 超时后必须等余下任务完成，避免半成品数据流入下一批/下一阶段 / After the timeout, still wait for stragglers so half-processed data never leaks into the next batch/stage
                        RampageLogger.Error(new TimeoutException($"ThreadingWorker '{batchName}' batch wait timeout, waiting for stragglers to finish."));
                        WaitHandle.WaitAll(resetEvents);
                    }
                }
                else
                {
                    WaitHandle.WaitAll(resetEvents);
                }
            }
        }

    }
}
