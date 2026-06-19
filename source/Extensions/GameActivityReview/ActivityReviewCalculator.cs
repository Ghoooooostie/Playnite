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

            var totalSeconds = (ulong)rows.Sum(slice => (decimal)slice.Seconds);
            var topGame = topGames.FirstOrDefault();
            var summary = new ActivityReviewSummary
            {
                Period = period,
                PeriodTitle = GetPeriodTitle(period),
                TotalSeconds = totalSeconds,
                TotalTimeText = FormatSeconds(totalSeconds),
                SessionCount = rows.Count,
                GameCount = topGames.Count,
                TopGames = topGames,
                DailyItems = BuildDailyItems(sessions, window, period),
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
                    var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, now.Kind);
                    return new ActivityReviewWindow(monthStart, monthStart.AddMonths(1));
                case ActivityReviewPeriod.Year:
                    var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, now.Kind);
                    return new ActivityReviewWindow(yearStart, yearStart.AddYears(1));
                default:
                    return new ActivityReviewWindow(DateTime.MinValue, DateTime.MaxValue);
            }
        }

        // 生成每日图表数据。
        private static List<GameActivityDailyItem> BuildDailyItems(IEnumerable<GameSessionRecord> sessions, ActivityReviewWindow window, ActivityReviewPeriod period)
        {
            if (period == ActivityReviewPeriod.All)
            {
                return new List<GameActivityDailyItem>();
            }

            var days = new Dictionary<DateTime, ulong>();
            for (var day = window.Start.Date; day < window.End.Date; day = day.AddDays(1))
            {
                days[day] = 0;
            }

            foreach (var session in sessions ?? Enumerable.Empty<GameSessionRecord>())
            {
                if (session == null || session.EndedAt <= session.StartedAt)
                {
                    continue;
                }

                for (var day = window.Start.Date; day < window.End.Date; day = day.AddDays(1))
                {
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
                    Percent = maxSeconds == 0 ? 0 : Math.Max(4, (int)Math.Round(pair.Value * 100d / maxSeconds))
                })
                .ToList();
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

        // 生成中文回顾文案。
        private static string BuildReviewText(ActivityReviewSummary summary)
        {
            if (summary.TotalSeconds == 0)
            {
                return "这个范围内还没有游戏记录。开始游戏后，这里会自动生成回顾。";
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

            if (period == ActivityReviewPeriod.Year)
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