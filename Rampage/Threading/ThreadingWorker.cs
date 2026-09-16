using Rampage.Log;
using System;

namespace Rampage.Threading
{
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

        public static void ClearAll()
        {
            lock (_workHandlerContainerLock)
            {
                WorkHandlerContainer.Clear();
            }
        }


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
                        // 超时后必须等余下任务完成，避免半成品数据流入下一批/下一阶段
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
