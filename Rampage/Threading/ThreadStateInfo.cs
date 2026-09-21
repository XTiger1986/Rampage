using System;

namespace Rampage.Threading
{

    /// <summary>
    /// 线程任务状态信息：由 ThreadingWorker 随任务传入 ThreadPool 回调 / Per-task state passed to the thread pool callback by ThreadingWorker
    /// </summary>
    public class ThreadStateInfo
    {

        /// <summary>
        /// 任务完成信号事件（finally 中置位，用于块内 WaitAll 屏障）/ The completion signal event (set in finally, used by the per-chunk WaitAll barrier)
        /// </summary>
        public ManualResetEvent? ResetEvent
        {
            get; set;
        }

        /// <summary>
        /// 任务携带的数据 / The data carried by the task
        /// </summary>
        public object? Data
        {
            get; set;
        }

        /// <summary>
        /// 任务在所属块内的序号（从 0 开始）/ Index of the task within its chunk (0-based)
        /// </summary>
        public int ThreadIndex
        {
            get; set;
        }

        /// <summary>
        /// 所属批次名 / The owning batch name
        /// </summary>
        public string? BatchName
        {
            get; set;
        }

    }
}
