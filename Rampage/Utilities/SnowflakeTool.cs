using Rampage.ExceptionExtensions;
using System;
using System.Threading;

namespace Rampage.Utilities
{
    /// <summary>
    /// 雪花算法ID生成器（64位：1符号位 + 41毫秒时间戳 + 10位机器码 + 12位序列）。
    /// 趋势递增、本地生成无网络开销；时间回拨容忍5毫秒以内，超过则抛异常拒绝发号。
    /// WorkerId 必须在多实例间唯一（取值 0~1023），默认 0。
    /// </summary>
    public static class SnowflakeTool
    {
        /// <summary>起始纪元：2020-01-01T00:00:00Z（可用约69年）</summary>
        public const long Twepoch = 1577836800000L;

        private const int WorkerIdBits = 10;
        private const int SequenceBits = 12;
        private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);
        private const int WorkerIdShift = SequenceBits;
        private const int TimestampShift = SequenceBits + WorkerIdBits;
        private const long MaxBackwardMs = 5;

        private static long _workerId;
        private static long _lastTimestamp = -1L;
        private static long _sequence;
        private static readonly object _lock = new object();

        /// <summary>
        /// 设置机器码（0~1023），必须在首次 NextId 前设置
        /// </summary>
        /// <param name="workerId"></param>
        public static void SetWorkerId(long workerId)
        {
            if (workerId < 0 || workerId > MaxWorkerId)
            {
                throw new SuperException($"Snowflake workerId 必须在 0~{MaxWorkerId} 之间");
            }
            lock (_lock)
            {
                if (_lastTimestamp != -1L)
                {
                    throw new SuperException("Snowflake workerId 必须在首次生成ID之前设置");
                }
                _workerId = workerId;
            }
        }

        /// <summary>
        /// 生成下一个趋势递增的64位ID
        /// </summary>
        /// <returns></returns>
        public static long NextId()
        {
            lock (_lock)
            {
                long timestamp = CurrentTimeMillis();
                if (timestamp < _lastTimestamp)
                {
                    long offset = _lastTimestamp - timestamp;
                    if (offset <= MaxBackwardMs)
                    {
                        Thread.Sleep((int)offset);
                        timestamp = CurrentTimeMillis();
                    }
                    if (timestamp < _lastTimestamp)
                    {
                        throw new SuperException($"Snowflake 时钟回拨 {_lastTimestamp - timestamp}ms，超过容忍阈值，拒绝发号");
                    }
                }
                if (timestamp == _lastTimestamp)
                {
                    _sequence = (_sequence + 1) & SequenceMask;
                    if (_sequence == 0)
                    {
                        timestamp = TillNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    _sequence = 0;
                }
                _lastTimestamp = timestamp;
                return ((timestamp - Twepoch) << TimestampShift) | (_workerId << WorkerIdShift) | _sequence;
            }
        }

        /// <summary>
        /// 生成下一个趋势递增的64位ID（字符串形式）
        /// </summary>
        /// <returns></returns>
        public static string NextIdString()
        {
            return NextId().ToString();
        }

        private static long TillNextMillis(long lastTimestamp)
        {
            long timestamp = CurrentTimeMillis();
            while (timestamp <= lastTimestamp)
            {
                timestamp = CurrentTimeMillis();
            }
            return timestamp;
        }

        private static long CurrentTimeMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
