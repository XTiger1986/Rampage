using Dapper;
using System;
using System.Data;

namespace Rampage.Dapper
{
    /// <summary>
    /// Dapper 原生不支持 <see cref="DateOnly"/> 参数/读写；
    /// Npgsql 10+ 将 PG <c>date</c> 读成 DateOnly，与 PO 对齐后需本 Handler 完成往返。
    /// </summary>
    internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            // 写入 DATE：优先传 DateOnly（Npgsql 10）；兼容旧驱动时也可落到 DateTime
            parameter.DbType = DbType.Date;
            parameter.Value = value;
        }

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
    /// </summary>
    internal sealed class TimeSpanCompatTypeHandler : SqlMapper.TypeHandler<TimeSpan>
    {
        public override void SetValue(IDbDataParameter parameter, TimeSpan value)
        {
            parameter.DbType = DbType.Time;
            parameter.Value = value;
        }

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
    /// </summary>
    public static class NpgsqlDateCompat
    {
        private static int _registered;

        public static void EnsureRegistered()
        {
            if (System.Threading.Interlocked.Exchange(ref _registered, 1) != 0)
            {
                return;
            }

            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            // TimeSpan 已在 Dapper typeMap 中；不 Remove 则无法覆盖 Npgsql 10 的 TimeOnly
            SqlMapper.RemoveTypeMap(typeof(TimeSpan));
            SqlMapper.AddTypeHandler(new TimeSpanCompatTypeHandler());
        }
    }
}
