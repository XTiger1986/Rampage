using System;

namespace Rampage.RedisLibrary
{
    /// <summary>
    /// 分布式锁句柄。Dispose 时按 token 安全释放。
    /// </summary>
    public interface IDistributedLock : IDisposable
    {
        /// <summary>是否已成功获取锁。</summary>
        bool IsAcquired { get; }

        /// <summary>逻辑锁键（不含 Redis Prefix）。</summary>
        string Key { get; }

        /// <summary>持有者 token。</summary>
        string Token { get; }
    }
}
