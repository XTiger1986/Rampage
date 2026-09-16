using System;

namespace Rampage.Caching
{
	/// <summary>
	/// 单机内存缓存配置（配置节 MemoryCache）
	/// Memory Cache Configurations (config section "MemoryCache")
	/// </summary>
	public class MemoryCacheConfig
	{

		/// <summary>
		/// 缓存键名前缀
		/// Cache Key Prefix
		/// </summary>
		public string? CacheKeyPrefix
		{
			get;
			set;
		}

		/// <summary>
		/// 缓存条目数上限（每条目计 1）。达到上限后按 CompactionPercentage 淘汰。null/0 表示不限制（不建议在生产使用）。默认 100000。
		/// Max number of entries (each entry counts as 1). null/0 = unlimited (not recommended). Default 100000.
		/// </summary>
		public long? SizeLimit
		{
			get;
			set;
		} = 100_000;

		/// <summary>
		/// 达到上限时一次淘汰的比例，默认 0.2
		/// Fraction of entries to evict when the limit is hit, default 0.2
		/// </summary>
		public double CompactionPercentage
		{
			get;
			set;
		} = 0.2;

		/// <summary>
		/// 未显式指定过期时间的条目默认绝对过期秒数。null/0 表示永不过期（旧行为）。默认 3600。
		/// Default absolute expiration (seconds) for entries set without an explicit TTL. null/0 = never (legacy). Default 3600.
		/// </summary>
		public double? DefaultExpirationSeconds
		{
			get;
			set;
		} = 3600;

		/// <summary>
		/// 过期扫描间隔秒数，默认 60
		/// Expiration scan frequency in seconds, default 60
		/// </summary>
		public double ExpirationScanFrequencySeconds
		{
			get;
			set;
		} = 60;

	}
}
