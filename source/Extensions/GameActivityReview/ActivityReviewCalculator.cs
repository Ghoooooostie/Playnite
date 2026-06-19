using System;
using System.Collections.Generic;
using System.Linq;

namespace GameActivityReview
{
    // 纯统计逻辑：把原始会话记录汇总为页面可展示的数据。
    public static class ActivityReviewCalculator
    {
        // 按指定时间范围生成统计摘要。
        public static ActivityReviewSummary BuildSummary(IEnumerable<GameSessionRecord> sessions, ActivityReviewPeriod period, DateTime now)
        {
            var window = GetWindow(period, now);
            var rows = (sessions ?? Enumerable.Empty<GameSessionRecord>())
                .Select(session => new SessionSlice(session, GetOverlapSeconds(session, window.Start, window.End)))
                .Where(slice => slice.Seconds > 0)
                .ToList();

            var topGames = rows
                .GroupBy(slice => new { slice.Record.GameId, slice.Record.GameName })
                .Select(group => new GameActivityRankItem
                {
                    GameId = group.Key.GameId,
                    GameName = string.IsNullOrWhiteSpace(group.Key.GameName) ? "未命名游戏" : group.Key.GameName,
                    TotalSeconds = (ulong)group.Sum(slice => (decimal)slice.Seconds),
                    SessionCount = group.Count()
                })
                .OrderByDescending(item => item.TotalSeconds)
                .ThenBy(item => item.GameName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            ApplyRankPercent(topGames);

            var totalSeconds = (ulong)rows.Sum(slice => (decimal)slice.Seconds);
            var dailyItems = BuildDailyItems(sessions, window, period);
            var topGame = topGames.FirstOrDefault();
            var summary = new ActivityReviewSummary
            {
                Period = period,
                PeriodTitle = GetPeriodTitle(period),
                TotalSeconds = totalSeconds,
                TotalTimeText = FormatSeconds(totalSeconds),
                AverageDailyTimeText = FormatAverageDailySeconds(totalSeconds, dailyItems.Count),
                AverageLabel = GetAverageLabel(period),
                ChartUnitLabel = GetChartUnitLabel(period),
                SessionCount = rows.Count,
                GameCount = topGames.Count,
                TopGames = topGames,
                DailyItems = dailyItems,
                TopGameName = topGame == null ? "暂无" : topGame.GameName,
                DateRangeText = FormatDateRange(window.Start, window.End, period)
            };
            summary.ReviewText = BuildReviewText(summary);
            return summary;
        }

        // 将秒数格式化为中文时长。
        public static string FormatSeconds(ulong seconds)
        {
            var totalMinutes = seconds / 60;
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;
            if (hours > 0 && minutes > 0)
            {
                return string.Format("{0} 小时 {1} 分钟", hours, minutes);
            }

            if (hours > 0)
            {
                return string.Format("{0} 小时", hours);
            }

            return string.Format("{0} 分钟", minutes);
        }

        // 获取时间范围边界。
        public static ActivityReviewWindow GetWindow(ActivityReviewPeriod period, DateTime now)
        {
            switch (period)
            {
                case ActivityReviewPeriod.Day:
                    return new ActivityReviewWindow(now.Date, now.Date.AddDays(1));
                case ActivityReviewPeriod.Week:
                    var weekStart = now.Date.AddDays(-GetMondayOffset(now.Date));
                    return new ActivityReviewWindow(weekStart, weekStart.AddDays(7));
                case ActivityReviewPeriod.Month:
                    var monthStart = now.Date.AddDays(-29);
                    return new ActivityReviewWindow(monthStart, now.Date.AddDays(1));
                case ActivityReviewPeriod.Year:
                    var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, now.Kind);
                    return new ActivityReviewWindow(yearStart, yearStart.AddYears(1));
                default:
                    return new ActivityReviewWindow(DateTime.MinValue, DateTime.MaxValue);
            }
        }

        // 计算游戏排行条相对榜首的百分比。
        private static void ApplyRankPercent(List<GameActivityRankItem> topGames)
        {
            var maxSeconds = topGames.Count == 0 ? 0 : topGames.Max(item => item.TotalSeconds);
            foreach (var item in topGames)
            {
                item.Percent = maxSeconds == 0 ? 0 : Math.Max(4, (int)Math.Round(item.TotalSeconds * 100d / maxSeconds));
            }
        }

        // 计算图表卡片里的日均时长。
        private static string FormatAverageDailySeconds(ulong totalSeconds, int dayCount)
        {
            if (dayCount <= 0 || totalSeconds == 0)
            {
                return FormatSeconds(0);
            }

            return FormatSeconds((ulong)Math.Round(totalSeconds / (double)dayCount));
        }
        // 生成图表时间桶数据。
        private static List<GameActivityDailyItem> BuildDailyItems(IEnumerable<GameSessionRecord> sessions, ActivityReviewWindow window, ActivityReviewPeriod period)
        {
            var records = (sessions ?? Enumerable.Empty<GameSessionRecord>())
                .Where(session => session != null && session.EndedAt > session.StartedAt)
                .ToList();

            if (period == ActivityReviewPeriod.Year)
            {
                return BuildMonthlyItems(records, window);
            }

            return BuildDayItems(records, window, period);
        }

        // 生成按日展示的图表数据。
        private static List<GameActivityDailyItem> BuildDayItems(List<GameSessionRecord> records, ActivityReviewWindow window, ActivityReviewPeriod period)
        {
            var days = period == ActivityReviewPeriod.All ? BuildPlayedDayBuckets(records) : BuildWindowDayBuckets(window);
            foreach (var session in records)
            {
                var start = period == ActivityReviewPeriod.All ? session.StartedAt.Date : window.Start.Date;
                var end = period == ActivityReviewPeriod.All ? session.EndedAt.Date.AddDays(1) : window.End.Date;
                foreach (var day in days.Keys.ToList())
                {
                    if (day < start || day >= end)
                    {
                        continue;
                    }

                    var seconds = GetOverlapSeconds(session, day, day.AddDays(1));
                    if (seconds > 0)
                    {
                        days[day] = days[day] + seconds;
                    }
                }
            }

            var maxSeconds = days.Count == 0 ? 0 : days.Values.Max();
            return days.Select(pair => new GameActivityDailyItem
                {
                    Date = pair.Key,
                    Label = FormatDailyLabel(pair.Key, period),
                    TotalSeconds = pair.Value,
                    Percent = GetPercent(pair.Value, maxSeconds)
                })
                .ToList();
        }

        // 生成按月展示的图表数据。
        private static List<GameActivityDailyItem> BuildMonthlyItems(List<GameSessionRecord> records, ActivityReviewWindow window)
        {
            var months = BuildYearMonthBuckets(window.Start);
            foreach (var session in records)
            {
                foreach (var month in months.Keys.ToList())
                {
                    var monthEnd = month.AddMonths(1);
                    var seconds = GetOverlapSeconds(session, month, monthEnd);
                    if (seconds > 0)
                    {
                        months[month] = months[month] + seconds;
                    }
                }
            }

            var maxSeconds = months.Count == 0 ? 0 : months.Values.Max();
            return months.Select(pair => new GameActivityDailyItem
                {
                    Date = pair.Key,
                    Label = pair.Key.ToString("M月"),
                    TotalSeconds = pair.Value,
                    Percent = GetPercent(pair.Value, maxSeconds)
                })
                .ToList();
        }

        // 创建固定范围内的每日桶。
        private static Dictionary<DateTime, ulong> BuildWindowDayBuckets(ActivityReviewWindow window)
        {
            var days = new Dictionary<DateTime, ulong>();
            for (var day = window.Start.Date; day < window.End.Date; day = day.AddDays(1))
            {
                days[day] = 0;
            }

            return days;
        }

        // 创建当前年份的 12 个按月桶。
        private static Dictionary<DateTime, ulong> BuildYearMonthBuckets(DateTime yearStart)
        {
            var months = new Dictionary<DateTime, ulong>();
            var firstMonth = new DateTime(yearStart.Year, 1, 1, 0, 0, 0, yearStart.Kind);
            for (var month = firstMonth; month < firstMonth.AddYears(1); month = month.AddMonths(1))
            {
                months[month] = 0;
            }

            return months;
        }

        // 创建全部记录中实际有会话覆盖的每日桶。
        private static Dictionary<DateTime, ulong> BuildPlayedDayBuckets(IEnumerable<GameSessionRecord> sessions)
        {
            var days = new Dictionary<DateTime, ulong>();
            foreach (var session in sessions)
            {
                for (var day = session.StartedAt.Date; day < session.EndedAt.Date.AddDays(1); day = day.AddDays(1))
                {
                    if (!days.ContainsKey(day))
                    {
                        days[day] = 0;
                    }
                }
            }

            return days;
        }

        // 计算图表条高度百分比。
        private static int GetPercent(ulong value, ulong maxValue)
        {
            return maxValue == 0 ? 0 : Math.Max(4, (int)Math.Round(value * 100d / maxValue));
        }
        // 计算一次会话落在范围内的秒数。
        private static ulong GetOverlapSeconds(GameSessionRecord session, DateTime start, DateTime end)
        {
            if (session == null || session.EndedAt <= session.StartedAt)
            {
                return 0;
            }

            var overlapStart = session.StartedAt > start ? session.StartedAt : start;
            var overlapEnd = session.EndedAt < end ? session.EndedAt : end;
            if (overlapEnd <= overlapStart)
            {
                return 0;
            }

            return (ulong)Math.Floor((overlapEnd - overlapStart).TotalSeconds);
        }

        // 返回均值标题。
        private static string GetAverageLabel(ActivityReviewPeriod period)
        {
            return period == ActivityReviewPeriod.Year ? "月均" : "日均";
        }

        // 返回图表单位说明。
        private static string GetChartUnitLabel(ActivityReviewPeriod period)
        {
            return period == ActivityReviewPeriod.Year ? "按月" : "每日";
        }
        // 生成中文回顾文案。
        private static string BuildReviewText(ActivityReviewSummary summary)
        {
            if (summary.TotalSeconds == 0)
            {
                return "这个范围内还没有游戏记录。开始游戏后，这里会自动生成时长统计。";
            }

            return string.Format("{0}共游玩 {1}，启动 {2} 次，最常玩的游戏是 {3}。", summary.PeriodTitle, summary.TotalTimeText, summary.SessionCount, summary.TopGameName);
        }

        // 返回周期标题。
        private static string GetPeriodTitle(ActivityReviewPeriod period)
        {
            switch (period)
            {
                case ActivityReviewPeriod.Day:
                    return "今天";
                case ActivityReviewPeriod.Week:
                    return "本周";
                case ActivityReviewPeriod.Month:
                    return "本月";
                case ActivityReviewPeriod.Year:
                    return "今年";
                default:
                    return "全部时间";
            }
        }

        // 生成日期范围描述。
        private static string FormatDateRange(DateTime start, DateTime end, ActivityReviewPeriod period)
        {
            if (period == ActivityReviewPeriod.All)
            {
                return "全部记录";
            }

            if (period == ActivityReviewPeriod.Day)
            {
                return string.Format("{0:yyyy-MM-dd}", start);
            }

            return string.Format("{0:yyyy-MM-dd} 至 {1:yyyy-MM-dd}", start, end.AddSeconds(-1));
        }

        // 生成图表横轴标签。
        private static string FormatDailyLabel(DateTime date, ActivityReviewPeriod period)
        {
            if (period == ActivityReviewPeriod.Day)
            {
                return "今天";
            }

            if (period == ActivityReviewPeriod.All || period == ActivityReviewPeriod.Month)
            {
                return date.ToString("M-d");
            }

            return date.ToString("d日");
        }

        // 计算周一偏移量。
        private static int GetMondayOffset(DateTime date)
        {
            return ((int)date.DayOfWeek + 6) % 7;
        }

        private class SessionSlice
        {
            public GameSessionRecord Record { get; private set; }
            public ulong Seconds { get; private set; }

            public SessionSlice(GameSessionRecord record, ulong seconds)
            {
                Record = record;
                Seconds = seconds;
            }
        }
    }
}