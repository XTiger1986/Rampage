using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 分布式锁句柄，Dispose 时按 token 安全释放。
    /// Distributed lock handle; released safely by matching token on Dispose.
    /// </summary>
    public interface IDistributedLock : IDisposable
    {
        /// <summary>是否已成功获取锁 / Whether the lock is currently held</summary>
        bool IsAcquired { get; }

        /// <summary>逻辑锁键（不含 Redis Prefix）/ Logical lock key (without Redis prefix)</summary>
        string Key { get; }

        /// <summary>持有者 token / Holder token</summary>
        string Token { get; }
    }
}
