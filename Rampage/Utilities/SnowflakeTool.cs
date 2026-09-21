using Rampage.ExceptionExtensions;
using System;
using System.Threading;

namespace Rampage.Utilities
{
    /// <summary>
    /// 雪花算法 ID 生成器（64 位：1 符号位 + 41 位毫秒时间戳 + 10 位机器码 + 12 位序列） /
    /// Snowflake ID generator (64-bit: 1 sign bit + 41-bit millisecond timestamp + 10-bit worker id + 12-bit sequence)
    /// </summary>
    /// <remarks>
    /// 趋势递增、本地生成无网络开销；同一毫秒内序列从 0 递增，溢出（每毫秒 4096 个）后自旋等待下一毫秒；
    /// 时间回拨 5 毫秒以内通过睡眠等待追平，超过则抛异常拒绝发号。
    /// WorkerId 必须在多实例间唯一（取值 0~1023），默认 0，且必须在首次发号前设置。
    /// IDs are trend-increasing and generated locally with no network overhead; within the same millisecond
    /// the sequence increments from 0 and spins into the next millisecond on overflow (4096 ids per ms);
    /// clock rollback within 5 ms is tolerated by sleeping, beyond that an exception is thrown and no id
    /// is issued. WorkerId must be unique across instances (0~1023), defaults to 0, and must be set before
    /// the first id is issued.
    /// </remarks>
    public static class SnowflakeTool
    {
        /// <summary>起始纪元：2020-01-01T00:00:00Z（41 位毫秒自纪元起可用约 69 年）/ Epoch: 2020-01-01T00:00:00Z (41 bits of milliseconds give ~69 years)</summary>
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
        /// 设置机器码（0~1023），必须在首次 NextId 前设置 /
        /// Set the worker id (0~1023); must be called before the first NextId
        /// </summary>
        /// <param name="workerId">机器码，取值 0~1023 / The worker id, 0~1023</param>
        /// <exception cref="SuperException">workerId 超出 0~1023，或在首次发号之后才调用 / workerId out of 0~1023, or called after the first id was issued</exception>
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
        /// 生成下一个趋势递增的 64 位 ID /
        /// Generate the next trend-increasing 64-bit id
        /// </summary>
        /// <remarks>
        /// 同一毫秒内序列自增，溢出后自旋等待下一毫秒；检测到时钟回拨 ≤5ms 时睡眠追平，
        /// 睡眠后仍落后或回拨超阈值则抛 <see cref="SuperException"/> 拒绝发号。
        /// The sequence increments within a millisecond and spins into the next on overflow; a clock
        /// rollback of 5 ms or less is slept off, and a <see cref="SuperException"/> (no id issued) is
        /// thrown when the clock is still behind afterwards or the rollback exceeds the threshold.
        /// </remarks>
        /// <returns>雪花算法 64 位 ID / The 64-bit snowflake id</returns>
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
                // 自纪元毫秒左移22位 | 机器码左移12位 | 序列号，拼装为64位ID / (ms since epoch)<<22 | workerId<<12 | sequence, assembled into the 64-bit id
                return ((timestamp - Twepoch) << TimestampShift) | (_workerId << WorkerIdShift) | _sequence;
            }
        }

        /// <summary>
        /// 生成下一个趋势递增的 64 位 ID（字符串形式） /
        /// Generate the next trend-increasing 64-bit id (string form)
        /// </summary>
        /// <returns>ID 的十进制字符串 / The id as a decimal string</returns>
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
