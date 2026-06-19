using System;
using System.Collections.Generic;

namespace GameActivityReview
{
    // 页面展示用统计摘要。
    public class ActivityReviewSummary
    {
        public ActivityReviewPeriod Period { get; set; }
        public string PeriodTitle { get; set; }
        public string DateRangeText { get; set; }
        public ulong TotalSeconds { get; set; }
        public string TotalTimeText { get; set; }
        public string AverageDailyTimeText { get; set; }
        public string AverageLabel { get; set; }
        public string ChartUnitLabel { get; set; }
        public int SessionCount { get; set; }
        public int GameCount { get; set; }
        public string TopGameName { get; set; }
        public string ReviewText { get; set; }
        public List<GameActivityRankItem> TopGames { get; set; }
        public List<GameActivityDailyItem> DailyItems { get; set; }

        public ActivityReviewSummary()
        {
            TopGames = new List<GameActivityRankItem>();
            DailyItems = new List<GameActivityDailyItem>();
        }
    }

    // 单个游戏在榜单中的统计结果。
    public class GameActivityRankItem
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public ulong TotalSeconds { get; set; }
        public int SessionCount { get; set; }
        public int Percent { get; set; }
        public string IconPath { get; set; }

        public string Initial
        {
            get
            {
                return string.IsNullOrWhiteSpace(GameName) ? "?" : GameName.Substring(0, 1).ToUpperInvariant();
            }
        }

        public string TimeText
        {
            get { return ActivityReviewCalculator.FormatSeconds(TotalSeconds); }
        }
    }

    // 图表中单个时间桶的游玩时长。
    public class GameActivityDailyItem
    {
        public DateTime Date { get; set; }
        public string Label { get; set; }
        public ulong TotalSeconds { get; set; }
        public int Percent { get; set; }

        public string TimeText
        {
            get { return ActivityReviewCalculator.FormatSeconds(TotalSeconds); }
        }
    }

    // 半开区间时间范围，End 不包含在范围内。
    public struct ActivityReviewWindow
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public ActivityReviewWindow(DateTime start, DateTime end)
            : this()
        {
            Start = start;
            End = end;
        }
    }
}