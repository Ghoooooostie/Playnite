using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using Playnite.SDK.Events;

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
            RunTest(RankingUsesFullscreenBarsInsteadOfGridView, ref passed, ref failed);
            RunTest(DailyChartSplitsSessionAcrossDays, ref passed, ref failed);
            RunTest(DayPeriodCountsOnlyToday, ref passed, ref failed);
            RunTest(AllPeriodBuildsChartFromPlayedDays, ref passed, ref failed);
            RunTest(ChartViewUsesOverviewCard, ref passed, ref failed);
            RunTest(ChartSummaryIncludesDailyAverageAndGamePercent, ref passed, ref failed);
            RunTest(ChartUsesFixedBucketsForWeekMonthAndYear, ref passed, ref failed);
            RunTest(ChartColumnsUseUniformGridToFillWidth, ref passed, ref failed);
            RunTest(DesktopMainMenuExposesReviewEntry, ref passed, ref failed);
            RunTest(DesktopSidebarExposesDurationEntry, ref passed, ref failed);
            RunTest(DesktopPluginDoesNotInitializeFullscreenState, ref passed, ref failed);
            RunTest(FullscreenMainMenuDoesNotExposeDialogEntry, ref passed, ref failed);
            RunTest(PluginRegistersFullscreenHomeControl, ref passed, ref failed);
            RunTest(FullscreenHomeControlIsClickable, ref passed, ref failed);
            RunTest(FullscreenHomeEntryMatchesQuickPresetVisualStyle, ref passed, ref failed);
            RunTest(FullscreenReviewPanelUsesFocusableScrollViewer, ref passed, ref failed);
            RunTest(FullscreenReviewUsesNativeContentFlowWithoutDesktopTabs, ref passed, ref failed);
            RunTest(FullscreenScrollViewerReleasesBoundaryDirections, ref passed, ref failed);
            RunTest(FullscreenPanelFocusSearchGuardsRecursiveTreeWalk, ref passed, ref failed);
            RunTest(FullscreenStateTracksNativePresetChanges, ref passed, ref failed);
            RunTest(FullscreenThemeExposesReviewPanelRegion, ref passed, ref failed);
            RunTest(FullscreenReviewEntryIsNextToFilterPresetSelector, ref passed, ref failed);
            RunTest(FullscreenPeriodPickerDoesNotUseOrangeFocusBorder, ref passed, ref failed);

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

        // 验证桌面模式保留主菜单入口。
        private static void DesktopMainMenuExposesReviewEntry()
        {
            var api = new FakePlayniteApi(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameActivityReviewTests", Guid.NewGuid().ToString()), ApplicationMode.Desktop);
            var plugin = new GameActivityReviewPlugin(api);
            var items = plugin.GetMainMenuItems(new GetMainMenuItemsArgs()).ToList();

            AssertEqual(1, items.Count, "desktop main menu item count");
            AssertEqual("时长", items[0].Description, "desktop main menu item title");
            if (items[0].Action == null)
            {
                throw new InvalidOperationException("desktop main menu item should open the review view");
            }
        }

        // 验证桌面模式保留侧边栏入口。
        private static void DesktopSidebarExposesDurationEntry()
        {
            var api = new FakePlayniteApi(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameActivityReviewTests", Guid.NewGuid().ToString()), ApplicationMode.Desktop);
            var plugin = new GameActivityReviewPlugin(api);
            var items = plugin.GetSidebarItems().ToList();

            AssertEqual(1, items.Count, "desktop sidebar item count");
            AssertEqual("时长", items[0].Title, "desktop sidebar item title");
        }

        // 验证桌面模式不初始化全屏状态，避免插件加载失败。
        private static void DesktopPluginDoesNotInitializeFullscreenState()
        {
            var api = new FakePlayniteApi(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameActivityReviewTests", Guid.NewGuid().ToString()), ApplicationMode.Desktop);
            var plugin = new GameActivityReviewPlugin(api);
            var stateField = typeof(GameActivityReviewPlugin).GetField("fullscreenState", BindingFlags.Instance | BindingFlags.NonPublic);

            if (stateField == null)
            {
                throw new InvalidOperationException("fullscreen state field missing");
            }

            if (stateField.GetValue(plugin) != null)
            {
                throw new InvalidOperationException("desktop plugin should not initialize fullscreen state");
            }
        }

        // 验证全屏扩展菜单不再暴露弹窗入口。
        private static void FullscreenMainMenuDoesNotExposeDialogEntry()
        {
            var api = new FakePlayniteApi(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameActivityReviewTests", Guid.NewGuid().ToString()), ApplicationMode.Fullscreen);
            var plugin = new GameActivityReviewPlugin(api);
            var items = plugin.GetMainMenuItems(new GetMainMenuItemsArgs()).ToList();

            AssertEqual(0, items.Count, "fullscreen main menu item count");
        }

        // 验证插件注册并提供全屏首页内嵌控件。
        private static void PluginRegistersFullscreenHomeControl()
        {
            var api = new FakePlayniteApi(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameActivityReviewTests"));
            var plugin = new GameActivityReviewPlugin(api);

            AssertEqual("GameActivityReview", api.CustomElementSourceName, "custom element source name");
            if (api.CustomElementNames == null || !api.CustomElementNames.Contains("FullscreenHomeReview"))
            {
                throw new InvalidOperationException("custom element names missing FullscreenHomeReview");
            }

            var control = plugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = "FullscreenHomeReview",
                Mode = ApplicationMode.Fullscreen
            });

            if (control == null || control.GetType().Name != "GameActivityReviewHomeView")
            {
                throw new InvalidOperationException("fullscreen home review control missing");
            }

            var panel = plugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = "FullscreenReviewPanel",
                Mode = ApplicationMode.Fullscreen
            });

            if (panel == null || panel.GetType().Name != "GameActivityReviewFullscreenPanelView")
            {
                throw new InvalidOperationException("fullscreen review panel control missing");
            }
        }

        // 验证全屏顶部回顾入口本身可点击。
        private static void FullscreenHomeControlIsClickable()
        {
            var api = new FakePlayniteApi(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameActivityReviewTests", Guid.NewGuid().ToString()));
            var plugin = new GameActivityReviewPlugin(api);
            var control = plugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = "FullscreenHomeReview",
                Mode = ApplicationMode.Fullscreen
            });

            var contentControl = control as ContentControl;
            var toggle = contentControl == null ? null : contentControl.Content as ToggleButton;
            if (toggle == null)
            {
                throw new InvalidOperationException("fullscreen home review entry should be a selectable top item");
            }

            if (toggle.Command == null)
            {
                throw new InvalidOperationException("fullscreen home review entry should expose an open command");
            }
        }

        // 验证顶部入口视觉上接近 Recently Played，只保留栏目文字和选中圆点。
        private static void FullscreenHomeEntryMatchesQuickPresetVisualStyle()
        {
            var api = new FakePlayniteApi(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameActivityReviewTests", Guid.NewGuid().ToString()));
            var plugin = new GameActivityReviewPlugin(api);
            var control = plugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = "FullscreenHomeReview",
                Mode = ApplicationMode.Fullscreen
            });

            var contentControl = control as ContentControl;
            var toggle = contentControl == null ? null : contentControl.Content as ToggleButton;
            if (toggle == null)
            {
                throw new InvalidOperationException("fullscreen home review entry should use a top selector toggle");
            }

            AssertEqual("Play Time", toggle.Content, "review entry title");
            AssertEqual(new Thickness(0), toggle.BorderThickness, "review entry border thickness");
            if (toggle.FocusVisualStyle != null)
            {
                throw new InvalidOperationException("fullscreen review entry should not show default focus rectangle");
            }

            if (toggle.Template == null)
            {
                throw new InvalidOperationException("fullscreen review entry should define the quick preset visual template");
            }
        }

        // 验证回顾面板查找焦点目标时不会递归进逻辑树循环。
        private static void FullscreenPanelFocusSearchGuardsRecursiveTreeWalk()
        {
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\GameActivityReview\GameActivityReviewFullscreenPanelView.cs"));
            var source = System.IO.File.ReadAllText(path);
            AssertContains(source, "HashSet<DependencyObject>", "fullscreen panel recursive guard");
            AssertContains(source, "visited.Add", "fullscreen panel visited tracking");
            AssertContains(source, "GetVisualChildren", "fullscreen panel visual child guard");
        }

        // 验证回顾面板可通过键盘和手柄方向键滚动。
        private static void FullscreenReviewPanelUsesFocusableScrollViewer()
        {
            var api = new FakePlayniteApi(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameActivityReviewTests", Guid.NewGuid().ToString()));
            var plugin = new GameActivityReviewPlugin(api);
            var control = plugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = "FullscreenReviewPanel",
                Mode = ApplicationMode.Fullscreen
            });

            if (control == null)
            {
                throw new InvalidOperationException("fullscreen review panel missing");
            }

            var scroll = FindLogicalChild<ScrollViewer>(control);
            if (scroll == null || !scroll.Focusable)
            {
                throw new InvalidOperationException("fullscreen review panel should contain a focusable scroll viewer");
            }
        }

        // 验证全屏回顾页不用桌面 TabControl 流程。
        private static void FullscreenReviewUsesNativeContentFlowWithoutDesktopTabs()
        {
            var view = new GameActivityReviewView(null, true);

            if (FindLogicalChild<TabControl>(view) != null)
            {
                throw new InvalidOperationException("fullscreen review should not use desktop tabs");
            }

            if (FindLogicalChild<ItemsControl>(view) == null)
            {
                throw new InvalidOperationException("fullscreen review should keep native item lists");
            }
        }

        // 验证滚动容器在边界处不吞掉方向键。
        private static void FullscreenScrollViewerReleasesBoundaryDirections()
        {
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\GameActivityReview\GameActivityReviewScrollViewer.cs"));
            var source = System.IO.File.ReadAllText(path);
            AssertContains(source, "CanScrollUp", "scroll viewer top boundary check");
            AssertContains(source, "CanScrollDown", "scroll viewer bottom boundary check");
            AssertContains(source, "MoveFocus", "scroll viewer boundary focus release");
        }

        // 验证原生筛选项变化时关闭回顾面板。
        private static void FullscreenStateTracksNativePresetChanges()
        {
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\GameActivityReview\GameActivityReviewFullscreenState.cs"));
            var source = System.IO.File.ReadAllText(path);
            AssertContains(source, "AttachToFullscreenMainModel", "fullscreen state native model hook");
            AssertContains(source, "ActiveFilterPreset", "fullscreen state preset change listener");
            AssertContains(source, "isOpeningPanel", "fullscreen state ignores its own reset");
        }

        // 验证默认全屏主题提供回顾主内容区插槽。
        private static void FullscreenThemeExposesReviewPanelRegion()
        {
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Playnite.FullscreenApp\Themes\Fullscreen\Default\Views\Main.xaml"));
            var xaml = System.IO.File.ReadAllText(path);
            AssertContains(xaml, "GameActivityReview_FullscreenReviewPanel", "fullscreen review panel region");
        }


        // 验证回顾入口放在顶部栏目选择器后面同一行。
        private static void FullscreenReviewEntryIsNextToFilterPresetSelector()
        {
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Playnite.FullscreenApp\Themes\Fullscreen\Default\Views\Main.xaml"));
            var xaml = System.IO.File.ReadAllText(path);
            var selectorIndex = xaml.IndexOf("<FilterPresetSelector", StringComparison.Ordinal);
            var entryIndex = xaml.IndexOf("GameActivityReview_FullscreenHomeReview", StringComparison.Ordinal);
            var stackIndex = xaml.LastIndexOf("<StackPanel", entryIndex, StringComparison.Ordinal);
            var stackCloseIndex = xaml.IndexOf("</StackPanel>", entryIndex, StringComparison.Ordinal);

            if (selectorIndex < 0 || entryIndex < 0 || stackIndex < 0 || stackCloseIndex < 0)
            {
                throw new InvalidOperationException("fullscreen review entry should be declared next to FilterPresetSelector");
            }

            if (entryIndex <= selectorIndex || selectorIndex <= stackIndex || entryIndex >= stackCloseIndex)
            {
                throw new InvalidOperationException("fullscreen review entry should sit after Recently Played selector in the same top row");
            }
        }

        // 验证全屏时间范围下拉框不用默认橙色聚焦框。
        private static void FullscreenPeriodPickerDoesNotUseOrangeFocusBorder()
        {
            var source = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\GameActivityReview\GameActivityReviewView.cs")));
            AssertContains(source, "BuildFullscreenPeriodPickerStyle", "fullscreen period picker custom style");
            AssertContains(source, "#88FFFFFF", "fullscreen period picker calm focus border");
            AssertContains(source, "BuildFullscreenPeriodPickerTemplate", "fullscreen period picker internal border template");
            AssertContains(source, "Margin=\\\"0\\\"", "fullscreen period picker should not use negative border margins");
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

        // 验证全屏榜单不用桌面表格样式。
        private static void RankingUsesFullscreenBarsInsteadOfGridView()
        {
            var view = (GameActivityReviewView)FormatterServices.GetUninitializedObject(typeof(GameActivityReviewView));
            var method = typeof(GameActivityReviewView).GetMethod("BuildRanking", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("BuildRanking method missing");
            }

            var ranking = method.Invoke(view, null) as StackPanel;
            if (ranking == null)
            {
                throw new InvalidOperationException("ranking view should be a StackPanel");
            }

            if (FindLogicalChild<ListView>(ranking) != null || FindLogicalChild<GridViewColumnHeader>(ranking) != null)
            {
                throw new InvalidOperationException("fullscreen ranking should not use desktop table view");
            }

            if (FindLogicalChild<ItemsControl>(ranking) == null)
            {
                throw new InvalidOperationException("fullscreen ranking should use a simple items list");
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

        private class FakePlayniteApi : IPlayniteAPI
        {
            public string CustomElementSourceName { get; private set; }
            public List<string> CustomElementNames { get; private set; }
            public IPlaynitePathsAPI Paths { get; private set; }
            public IMainViewAPI MainView { get { return null; } }
            public IGameDatabaseAPI Database { get { return null; } }
            public IDialogsFactory Dialogs { get { return null; } }
            public INotificationsAPI Notifications { get { return null; } }
            public IPlayniteInfoAPI ApplicationInfo { get; private set; }
            public IWebViewFactory WebViews { get { return null; } }
            public IResourceProvider Resources { get { return null; } }
            public IUriHandlerAPI UriHandler { get { return null; } }
            public IPlayniteSettingsAPI ApplicationSettings { get { return null; } }
            public IAddons Addons { get { return null; } }
            public IEmulationAPI Emulation { get { return null; } }

            public FakePlayniteApi(string extensionsDataPath)
                : this(extensionsDataPath, ApplicationMode.Desktop)
            {
            }

            public FakePlayniteApi(string extensionsDataPath, ApplicationMode mode)
            {
                Paths = new FakePlaynitePaths(extensionsDataPath);
                ApplicationInfo = new FakePlayniteInfo(mode);
            }

            public void AddCustomElementSupport(Plugin source, AddCustomElementSupportArgs args)
            {
                CustomElementSourceName = args.SourceName;
                CustomElementNames = args.ElementList.ToList();
            }

            public void AddSettingsSupport(Plugin source, AddSettingsSupportArgs args) { }
            public void AddConvertersSupport(Plugin source, AddConvertersSupportArgs args) { }
            public string ExpandGameVariables(Playnite.SDK.Models.Game game, string inputString) { return inputString; }
            public string ExpandGameVariables(Playnite.SDK.Models.Game game, string inputString, string emulatorDir) { return inputString; }
            public Playnite.SDK.Models.GameAction ExpandGameVariables(Playnite.SDK.Models.Game game, Playnite.SDK.Models.GameAction action) { return action; }
            public void StartGame(Guid gameId) { }
            public void InstallGame(Guid gameId) { }
            public void UninstallGame(Guid gameId) { }
            public List<GamepadController> GetConnectedControllers() { return new List<GamepadController>(); }
        }

        private class FakePlayniteInfo : IPlayniteInfoAPI
        {
            public System.Version ApplicationVersion { get { return new System.Version(10, 0); } }
            public ApplicationMode Mode { get; private set; }
            public bool IsPortable { get { return true; } }
            public bool InOfflineMode { get { return false; } }
            public bool IsDebugBuild { get { return false; } }
            public bool ThrowAllErrors { get { return false; } }

            public FakePlayniteInfo(ApplicationMode mode)
            {
                Mode = mode;
            }
        }

        private class FakePlaynitePaths : IPlaynitePathsAPI
        {
            public bool IsPortable { get { return true; } }
            public string ApplicationPath { get { return string.Empty; } }
            public string ConfigurationPath { get { return string.Empty; } }
            public string ExtensionsDataPath { get; private set; }

            public FakePlaynitePaths(string extensionsDataPath)
            {
                ExtensionsDataPath = extensionsDataPath;
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

        private static T FindLogicalChild<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                if (child is T match)
                {
                    return match;
                }

                var nested = FindLogicalChild<T>(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}






