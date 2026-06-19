using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GameActivityReview.TestRunner
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            var passed = 0;
            var failed = 0;
            RunTest(WeekOverlap, ref passed, ref failed);
            RunTest(MonthOverlap, ref passed, ref failed);
            RunTest(YearOverlapAndReviewText, ref passed, ref failed);
            RunTest(RecordFactoryUsesElapsedSeconds, ref passed, ref failed);
            RunTest(ToolbarButtonUsesFixedTextContent, ref passed, ref failed);
            RunTest(LayoutLeavesTitleBarSafeArea, ref passed, ref failed);
            RunTest(ContentHasListAndChartTabs, ref passed, ref failed);
            RunTest(DailyChartSplitsSessionAcrossDays, ref passed, ref failed);
            RunTest(DayPeriodCountsOnlyToday, ref passed, ref failed);
            RunTest(AllPeriodBuildsChartFromPlayedDays, ref passed, ref failed);
            RunTest(ChartViewUsesOverviewCard, ref passed, ref failed);
            RunTest(ChartSummaryIncludesDailyAverageAndGamePercent, ref passed, ref failed);
            RunTest(ChartUsesFixedBucketsForWeekMonthAndYear, ref passed, ref failed);
            RunTest(ChartColumnsUseUniformGridToFillWidth, ref passed, ref failed);

            Console.WriteLine("Passed=" + passed + " Failed=" + failed);
            return failed == 0 ? 0 : 1;
        }

        private static void RunTest(Action test, ref int passed, ref int failed)
        {
            try
            {
                test();
                passed++;
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine(test.Method.Name + ": " + ex.Message);
            }
        }

        private static void WeekOverlap()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "跨周游戏",
                    StartedAt = new DateTime(2026, 6, 14, 23, 30, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 15, 0, 30, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "本周游戏",
                    StartedAt = new DateTime(2026, 6, 16, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 16, 21, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Week, now);
            AssertEqual((ulong)5400, summary.TotalSeconds, "week total seconds");
            AssertEqual(2, summary.SessionCount, "week session count");
            AssertEqual("本周游戏", summary.TopGames.First().GameName, "week top game");
        }

        private static void MonthOverlap()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "跨月游戏",
                    StartedAt = new DateTime(2026, 5, 31, 23, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 1, 1, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 7200
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "上月游戏",
                    StartedAt = new DateTime(2026, 5, 10, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 5, 10, 21, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Month, now);
            AssertEqual((ulong)7200, summary.TotalSeconds, "month total seconds");
            AssertEqual(1, summary.SessionCount, "month session count");
            AssertEqual("跨月游戏", summary.TopGames.Single().GameName, "month top game");
        }

        private static void YearOverlapAndReviewText()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "跨年游戏",
                    StartedAt = new DateTime(2025, 12, 31, 23, 30, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 1, 1, 0, 30, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "今年游戏",
                    StartedAt = new DateTime(2026, 3, 1, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 3, 1, 22, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 7200
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Year, now);
            AssertEqual((ulong)9000, summary.TotalSeconds, "year total seconds");
            AssertContains(summary.ReviewText, "今年游戏", "year review top game");
            AssertContains(summary.ReviewText, "2 小时 30 分钟", "year review total time");
        }

        private static void RecordFactoryUsesElapsedSeconds()
        {
            var gameId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var trackedStart = new DateTime(2026, 6, 17, 19, 0, 0, DateTimeKind.Local);
            var endedAt = new DateTime(2026, 6, 17, 20, 30, 0, DateTimeKind.Local);

            var record = GameSessionRecordFactory.Create(gameId, "计时游戏", trackedStart, endedAt, 3600);
            AssertEqual(new DateTime(2026, 6, 17, 19, 30, 0, DateTimeKind.Local), record.StartedAt, "record start");
            AssertEqual(endedAt, record.EndedAt, "record end");
            AssertEqual((ulong)3600, record.DurationSeconds, "record duration");
        }

        private static void DayPeriodCountsOnlyToday()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "今天游戏",
                    StartedAt = new DateTime(2026, 6, 17, 9, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 17, 10, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "昨天游戏",
                    StartedAt = new DateTime(2026, 6, 16, 9, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Day, now);
            AssertEqual((ulong)3600, summary.TotalSeconds, "day total seconds");
            AssertEqual("今天", summary.PeriodTitle, "day title");
            AssertEqual(1, summary.DailyItems.Count, "day chart item count");
            AssertEqual("今天", summary.DailyItems[0].Label, "day chart label");
        }

        private static void AllPeriodBuildsChartFromPlayedDays()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "全部游戏一",
                    StartedAt = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 10, 9, 7, 0, DateTimeKind.Local),
                    DurationSeconds = 420
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "全部游戏二",
                    StartedAt = new DateTime(2026, 6, 12, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 12, 20, 2, 0, DateTimeKind.Local),
                    DurationSeconds = 120
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.All, now);
            AssertEqual(2, summary.DailyItems.Count, "all chart played day count");
            AssertEqual(new DateTime(2026, 6, 10), summary.DailyItems[0].Date, "all chart first day");
            AssertEqual((ulong)420, summary.DailyItems[0].TotalSeconds, "all chart first day seconds");
            AssertEqual("6-10", summary.DailyItems[0].Label, "all chart first label");
            AssertEqual(100, summary.DailyItems[0].Percent, "all chart first percent");
        }

        private static void DailyChartSplitsSessionAcrossDays()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "跨天游戏",
                    StartedAt = new DateTime(2026, 6, 16, 23, 30, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 17, 0, 30, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Week, now);
            var june16 = summary.DailyItems.Single(item => item.Date == new DateTime(2026, 6, 16));
            var june17 = summary.DailyItems.Single(item => item.Date == new DateTime(2026, 6, 17));
            AssertEqual((ulong)1800, june16.TotalSeconds, "june 16 chart seconds");
            AssertEqual((ulong)1800, june17.TotalSeconds, "june 17 chart seconds");
            AssertEqual(100, june16.Percent, "june 16 chart percent");
            AssertEqual(100, june17.Percent, "june 17 chart percent");
        }

        private static void ChartSummaryIncludesDailyAverageAndGamePercent()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "第一游戏",
                    StartedAt = new DateTime(2026, 6, 17, 9, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 17, 9, 10, 0, DateTimeKind.Local),
                    DurationSeconds = 600
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "第二游戏",
                    StartedAt = new DateTime(2026, 6, 17, 10, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 17, 10, 5, 0, DateTimeKind.Local),
                    DurationSeconds = 300
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Day, now);
            AssertEqual("15 分钟", summary.AverageDailyTimeText, "chart average time");
            AssertEqual(100, summary.TopGames[0].Percent, "top game percent");
            AssertEqual(50, summary.TopGames[1].Percent, "second game percent");
        }

        private static void ChartColumnsUseUniformGridToFillWidth()
        {
            var view = (GameActivityReviewView)FormatterServices.GetUninitializedObject(typeof(GameActivityReviewView));
            var method = typeof(GameActivityReviewView).GetMethod("BuildHorizontalItemsPanel", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("BuildHorizontalItemsPanel method missing");
            }

            var template = method.Invoke(view, null) as ItemsPanelTemplate;
            if (template == null || template.VisualTree == null)
            {
                throw new InvalidOperationException("chart items panel template missing");
            }

            if (template.VisualTree.Type != typeof(UniformGrid))
            {
                throw new InvalidOperationException("chart columns should use UniformGrid to fill the available width");
            }
        }
        private static void ChartUsesFixedBucketsForWeekMonthAndYear()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "一月游戏",
                    StartedAt = new DateTime(2026, 1, 5, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 1, 5, 21, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "三月游戏",
                    StartedAt = new DateTime(2026, 3, 1, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 3, 1, 22, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 7200
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    GameName = "本月游戏",
                    StartedAt = new DateTime(2026, 6, 16, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 16, 21, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                }
            };

            var week = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Week, now);
            AssertEqual(7, week.DailyItems.Count, "week chart bucket count");
            AssertEqual("15日", week.DailyItems[0].Label, "week first label");
            AssertEqual("21日", week.DailyItems[6].Label, "week last label");

            var month = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Month, now);
            AssertEqual(30, month.DailyItems.Count, "month chart bucket count");
            AssertEqual("6-17", month.DailyItems[29].Label, "month last label");

            var year = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Year, now);
            AssertEqual(12, year.DailyItems.Count, "year chart bucket count");
            AssertEqual("1月", year.DailyItems[0].Label, "year first label");
            AssertEqual((ulong)3600, year.DailyItems[0].TotalSeconds, "year january seconds");
            AssertEqual("3月", year.DailyItems[2].Label, "year march label");
            AssertEqual((ulong)7200, year.DailyItems[2].TotalSeconds, "year march seconds");
            AssertEqual("月均", year.AverageLabel, "year average label");
            AssertEqual("按月", year.ChartUnitLabel, "year chart unit label");
        }
        private static void ChartViewUsesOverviewCard()
        {
            var view = (GameActivityReviewView)FormatterServices.GetUninitializedObject(typeof(GameActivityReviewView));
            var method = typeof(GameActivityReviewView).GetMethod("BuildDailyChart", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("BuildDailyChart method missing");
            }

            var panel = method.Invoke(view, null) as StackPanel;
            if (panel == null)
            {
                throw new InvalidOperationException("chart view should be a StackPanel");
            }

            if (!panel.Children.OfType<Border>().Any())
            {
                throw new InvalidOperationException("chart view should include an overview card");
            }
        }

        private static void ContentHasListAndChartTabs()
        {
            var view = (GameActivityReviewView)FormatterServices.GetUninitializedObject(typeof(GameActivityReviewView));
            var method = typeof(GameActivityReviewView).GetMethod("BuildContent", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("BuildContent method missing");
            }

            var content = method.Invoke(view, null) as StackPanel;
            if (content == null)
            {
                throw new InvalidOperationException("content should be a StackPanel");
            }

            var tabs = content.Children.OfType<TabControl>().FirstOrDefault();
            if (tabs == null)
            {
                throw new InvalidOperationException("content should include display mode tabs");
            }

            AssertEqual(2, tabs.Items.Count, "display mode tab count");
            AssertEqual("榜单", ((TabItem)tabs.Items[0]).Header, "first display tab");
            AssertEqual("图表", ((TabItem)tabs.Items[1]).Header, "second display tab");
        }

        private static void LayoutLeavesTitleBarSafeArea()
        {
            var view = (GameActivityReviewView)FormatterServices.GetUninitializedObject(typeof(GameActivityReviewView));
            var method = typeof(GameActivityReviewView).GetMethod("BuildLayout", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("BuildLayout method missing");
            }

            var root = method.Invoke(view, null) as DockPanel;
            if (root == null)
            {
                throw new InvalidOperationException("root layout should be a DockPanel");
            }

            if (root.Margin.Top < 56)
            {
                throw new InvalidOperationException("top margin should leave room for window controls");
            }
        }

        private static void ToolbarButtonUsesFixedTextContent()
        {
            var view = (GameActivityReviewView)FormatterServices.GetUninitializedObject(typeof(GameActivityReviewView));
            var method = typeof(GameActivityReviewView).GetMethod("CreateButton", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("CreateButton method missing");
            }

            var button = (Button)method.Invoke(view, new object[] { "生成分享海报", "ExportCommand" });
            var label = button.Content as TextBlock;
            if (label == null)
            {
                throw new InvalidOperationException("button content should be a TextBlock");
            }

            AssertEqual("生成分享海报", label.Text, "button text");
            AssertEqual(TextWrapping.NoWrap, label.TextWrapping, "button text wrapping");
            if (button.MinWidth < 132)
            {
                throw new InvalidOperationException("share button min width too small");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(label + " expected " + expected + " actual " + actual);
            }
        }

        private static void AssertContains(string text, string expected, string label)
        {
            if (text == null || !text.Contains(expected))
            {
                throw new InvalidOperationException(label + " missing " + expected);
            }
        }
    }
}
