using System;
using System.Collections.Generic;
using System.Globalization;

namespace Rampage.Scheduling
{
    /// <summary>
    /// 简化版 Cron 表达式（标准5字段：分 时 日 月 周）。 / Simplified cron expression (standard 5 fields: minute hour day-of-month month day-of-week).
    /// 支持语法：* 、数字、区间 a-b、步进 /s（如 */5、10-30/2）、逗号组合； / Supported syntax: *, plain numbers, ranges a-b, steps /s (e.g. */5, 10-30/2), and comma-separated combinations;
    /// 周字段 0 与 7 均表示周日。日与周同时受限时按任一命中（OR）处理。 / In the weekday field both 0 and 7 mean Sunday; when day-of-month and weekday are both restricted, a hit on either counts (OR).
    /// </summary>
    public sealed class CronExpression
    {
        private readonly int[] _minutes;
        private readonly int[] _hours;
        private readonly int[] _daysOfMonth;
        private readonly int[] _months;
        private readonly int[] _daysOfWeek;
        private readonly bool _dayOfMonthRestricted;
        private readonly bool _dayOfWeekRestricted;

        private CronExpression(int[] minutes, int[] hours, int[] daysOfMonth, int[] months, int[] daysOfWeek)
        {
            _minutes = minutes;
            _hours = hours;
            _daysOfMonth = daysOfMonth;
            _months = months;
            _daysOfWeek = daysOfWeek;
            _dayOfMonthRestricted = daysOfMonth.Length != 31;
            _dayOfWeekRestricted = daysOfWeek.Length != 7;
        }

        /// <summary>
        /// 解析 Cron 表达式 / Parses a cron expression
        /// </summary>
        /// <param name="expression">如 "0 3 * * *"（每天 3:00）、"*/30 * * * *"（每半小时） / e.g. "0 3 * * *" (daily at 3:00), "*/30 * * * *" (every half hour)</param>
        /// <returns>解析得到的 CronExpression 实例 / The parsed CronExpression instance</returns>
        public static CronExpression Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("Cron expression is required.", nameof(expression));
            }
            string[] fields = expression.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 5)
            {
                throw new FormatException($"Cron 表达式必须为5个字段（分 时 日 月 周）: {expression}");
            }
            int[] minutes = ParseField(fields[0], 0, 59);
            int[] hours = ParseField(fields[1], 0, 23);
            int[] daysOfMonth = ParseField(fields[2], 1, 31);
            int[] months = ParseField(fields[3], 1, 12);
            int[] daysOfWeek = NormalizeWeek(ParseField(fields[4], 0, 7));
            return new CronExpression(minutes, hours, daysOfMonth, months, daysOfWeek);
        }

        /// <summary>
        /// 指定时间（分钟粒度）是否命中 / Whether the given time (minute granularity) matches the expression
        /// </summary>
        /// <param name="time">待检查的时间（忽略秒及以下部分） / The time to check (seconds and below are ignored)</param>
        /// <returns>命中返回 true / true if it matches</returns>
        public bool Matches(DateTime time)
        {
            if (Array.BinarySearch(_minutes, time.Minute) < 0)
            {
                return false;
            }
            if (Array.BinarySearch(_hours, time.Hour) < 0)
            {
                return false;
            }
            if (Array.BinarySearch(_months, time.Month) < 0)
            {
                return false;
            }
            bool dayMatch = Array.BinarySearch(_daysOfMonth, time.Day) >= 0;
            bool weekMatch = Array.BinarySearch(_daysOfWeek, (int)time.DayOfWeek) >= 0;
            if (_dayOfMonthRestricted && _dayOfWeekRestricted)
            {
                return dayMatch || weekMatch;
            }
            if (_dayOfMonthRestricted)
            {
                return dayMatch;
            }
            if (_dayOfWeekRestricted)
            {
                return weekMatch;
            }
            return true;
        }

        /// <summary>
        /// 计算指定时间之后的下一次命中时间（分钟粒度；最多向前扫描一年） / Computes the next match strictly after the given time (minute granularity; scans forward at most one year)
        /// </summary>
        /// <param name="after">起始时间（不含该分钟本身） / The reference time (its own minute is excluded)</param>
        /// <returns>下一次命中时间；一年内无可命中时间则抛出 InvalidOperationException / The next occurrence; throws InvalidOperationException if nothing matches within a year</returns>
        public DateTime NextOccurrence(DateTime after)
        {
            DateTime candidate = new DateTime(after.Year, after.Month, after.Day, after.Hour, after.Minute, 0).AddMinutes(1);
            DateTime limit = after.AddYears(1);
            while (candidate <= limit)
            {
                if (Matches(candidate))
                {
                    return candidate;
                }
                candidate = candidate.AddMinutes(1);
            }
            throw new InvalidOperationException($"Cron 表达式在一年内无可命中时间: {ToString()}");
        }

        public override string ToString()
        {
            return string.Join(" ",
                Describe(_minutes, 0, 59), Describe(_hours, 0, 23),
                Describe(_daysOfMonth, 1, 31), Describe(_months, 1, 12),
                Describe(_daysOfWeek, 0, 6));
        }

        private static int[] ParseField(string field, int min, int max)
        {
            SortedSet<int> values = new SortedSet<int>();
            foreach (string part in field.Split(','))
            {
                string rangeText = part;
                int step = 1;
                int slashIndex = part.IndexOf('/');
                if (slashIndex >= 0)
                {
                    rangeText = part[..slashIndex];
                    step = int.Parse(part[(slashIndex + 1)..], CultureInfo.InvariantCulture);
                    if (step <= 0)
                    {
                        throw new FormatException($"Cron 步进必须为正整数: {part}");
                    }
                }
                int from;
                int to;
                if (rangeText == "*")
                {
                    from = min;
                    to = max;
                }
                else
                {
                    int dashIndex = rangeText.IndexOf('-');
                    if (dashIndex >= 0)
                    {
                        from = int.Parse(rangeText[..dashIndex], CultureInfo.InvariantCulture);
                        to = int.Parse(rangeText[(dashIndex + 1)..], CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        from = int.Parse(rangeText, CultureInfo.InvariantCulture);
                        to = slashIndex >= 0 ? max : from;
                    }
                }
                if (from < min || to > max || from > to)
                {
                    throw new FormatException($"Cron 字段越界（{min}-{max}）: {part}");
                }
                for (int value = from; value <= to; value += step)
                {
                    values.Add(value);
                }
            }
            int[] result = new int[values.Count];
            values.CopyTo(result);
            return result;
        }

        private static int[] NormalizeWeek(int[] values)
        {
            // 0 与 7 均归一化为周日（0） / Both 0 and 7 are normalized to Sunday (0)
            if (Array.IndexOf(values, 7) < 0)
            {
                return values;
            }
            SortedSet<int> normalized = new SortedSet<int>();
            foreach (int value in values)
            {
                normalized.Add(value == 7 ? 0 : value);
            }
            int[] result = new int[normalized.Count];
            normalized.CopyTo(result);
            return result;
        }

        private static string Describe(int[] values, int min, int max)
        {
            if (values.Length == max - min + 1)
            {
                return "*";
            }
            return string.Join(",", values);
        }
    }
}
