using Dapper;
using System;
using System.Data;

namespace Rampage.Dapper
{
    /// <summary>
    /// Dapper 原生不支持 <see cref="DateOnly"/> 参数/读写；
    /// Npgsql 10+ 将 PG <c>date</c> 读成 DateOnly，与 PO 对齐后需本 Handler 完成往返。
    /// Dapper has no native <see cref="DateOnly"/> support; Npgsql 10+ reads PG <c>date</c> as DateOnly,
    /// so this handler is required to round-trip the value after PO alignment.
    /// </summary>
    internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        /// <summary>把 <see cref="DateOnly"/> 写入 DATE 参数 / Writes the <see cref="DateOnly"/> value into a DATE parameter.</summary>
        /// <param name="parameter">目标参数 / the target parameter.</param>
        /// <param name="value">要写入的日期 / the date to write.</param>
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            // 写入 DATE：优先传 DateOnly（Npgsql 10）；兼容旧驱动时也可落到 DateTime
            // Write as DATE: pass DateOnly first (Npgsql 10); old drivers fall back to DateTime
            parameter.DbType = DbType.Date;
            parameter.Value = value;
        }

        /// <summary>把驱动返回的 DateOnly / DateTime / string 转换为 <see cref="DateOnly"/> / Converts a driver-returned DateOnly / DateTime / string to <see cref="DateOnly"/>.</summary>
        /// <param name="value">驱动返回值 / the value returned by the driver.</param>
        /// <returns>转换后的日期 / the converted date.</returns>
        public override DateOnly Parse(object value)
        {
            return value switch
            {
                DateOnly d => d,
                DateTime dt => DateOnly.FromDateTime(dt),
                string s => DateOnly.Parse(s),
                _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
            };
        }
    }

    /// <summary>
    /// Npgsql 10+ 默认将 PG <c>time</c> 读成 <see cref="TimeOnly"/>，兼容映射到 <see cref="TimeSpan"/>。
    /// Npgsql 10+ reads PG <c>time</c> as <see cref="TimeOnly"/> by default; this handler maps it back to <see cref="TimeSpan"/>.
    /// </summary>
    internal sealed class TimeSpanCompatTypeHandler : SqlMapper.TypeHandler<TimeSpan>
    {
        /// <summary>把 <see cref="TimeSpan"/> 写入 TIME 参数 / Writes the <see cref="TimeSpan"/> value into a TIME parameter.</summary>
        /// <param name="parameter">目标参数 / the target parameter.</param>
        /// <param name="value">要写入的时间 / the time to write.</param>
        public override void SetValue(IDbDataParameter parameter, TimeSpan value)
        {
            parameter.DbType = DbType.Time;
            parameter.Value = value;
        }

        /// <summary>把驱动返回的 TimeSpan / TimeOnly / DateTime / string 转换为 <see cref="TimeSpan"/> / Converts a driver-returned TimeSpan / TimeOnly / DateTime / string to <see cref="TimeSpan"/>.</summary>
        /// <param name="value">驱动返回值 / the value returned by the driver.</param>
        /// <returns>转换后的时间 / the converted time.</returns>
        public override TimeSpan Parse(object value)
        {
            return value switch
            {
                TimeSpan ts => ts,
                TimeOnly t => t.ToTimeSpan(),
                DateTime dt => dt.TimeOfDay,
                string s => TimeSpan.Parse(s),
                _ => (TimeSpan)Convert.ChangeType(value, typeof(TimeSpan))
            };
        }
    }

    /// <summary>
    /// 注册 DateOnly / TimeSpan 与 Dapper 的 TypeHandler（进程内只注册一次）。
    /// Registers the DateOnly / TimeSpan Dapper TypeHandlers (only once per process).
    /// </summary>
    public static class NpgsqlDateCompat
    {
        /// <summary>注册标志：0 未注册，1 已注册 / registration flag: 0 = not registered, 1 = registered.</summary>
        private static int _registered;

        /// <summary>确保 TypeHandler 已注册；重复调用为无操作 / Ensures the TypeHandlers are registered; repeat calls are no-ops.</summary>
        public static void EnsureRegistered()
        {
            // Interlocked.Exchange 原子置位，保证进程内只注册一次 / atomic flag so registration happens exactly once per process
            if (System.Threading.Interlocked.Exchange(ref _registered, 1) != 0)
            {
                return;
            }

            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            // TimeSpan 已在 Dapper typeMap 中；不 Remove 则无法覆盖 Npgsql 10 的 TimeOnly
            // TimeSpan already sits in Dapper's typeMap; without Remove the Npgsql 10 TimeOnly mapping cannot be overridden
            SqlMapper.RemoveTypeMap(typeof(TimeSpan));
            SqlMapper.AddTypeHandler(new TimeSpanCompatTypeHandler());
        }
    }
}
