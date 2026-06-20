// 文件用途：验证待确认列表视图模型能确认导入并触发资料刷新。
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SwitchSmartImport.Tests
{
    // 验证待确认列表中心的最小行为。
    [TestFixture]
    public class SwitchPendingImportViewTests
    {
        [Test]
        public void Pending_view_imports_selected_candidates_and_refreshes_metadata()
        {
            var customPlatformId = Guid.Parse("99999999-9999-9999-9999-999999999999");
            var settings = new SwitchSmartImportSettings
            {
                MetadataSource = SwitchMetadataSource.SwitchLocalMetadata,
                DefaultPlatformId = Guid.Parse("88888888-8888-8888-8888-888888888888")
            };
            var store = new FakePendingStore(new SwitchPendingImportSnapshot
            {
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "A-Edited", BasePath = @"H:\A.nsp", Import = true, SelectedPlatformId = customPlatformId },
                    new SwitchImportCandidate { GameName = "B", BasePath = @"H:\B.nsp", Import = false }
                }
            });
            var executor = new FakeImportExecutor();
            var refresh = new FakeMetadataRefreshService();
            var progress = new FakeProgressService();
            var viewModel = new SwitchPendingImportViewModel(settings, store, executor, refresh, progressService: progress);

            viewModel.ImportSelected();

            Assert.AreEqual(1, progress.RunCount);
            Assert.AreEqual(1, executor.ImportCallCount);
            Assert.AreEqual(1, executor.LastCandidates.Count);
            Assert.AreEqual("A-Edited", executor.LastCandidates[0].GameName);
            Assert.AreEqual(customPlatformId, executor.LastCandidates[0].SelectedPlatformId);
            Assert.AreEqual(1, refresh.RefreshCallCount);
            Assert.AreEqual(SwitchMetadataSource.SwitchLocalMetadata, refresh.LastSource);
            Assert.AreEqual(1, store.SaveCallCount);
            Assert.AreEqual(1, store.LastSaved.Candidates.Count);
            Assert.AreEqual("B", store.LastSaved.Candidates[0].GameName);
        }

        [Test]
        public void Pending_view_builds_candidate_and_skipped_lists()
        {
            var viewModel = new SwitchPendingImportViewModel(
                new SwitchSmartImportSettings { DefaultPlatformId = Guid.Parse("77777777-7777-7777-7777-777777777777") },
                new FakePendingStore(new SwitchPendingImportSnapshot
                {
                    Candidates = new List<SwitchImportCandidate>
                    {
                        new SwitchImportCandidate { GameName = "A", BasePath = @"H:\A.nsp", Import = true }
                    },
                    SkippedItems = new List<SwitchSkippedItem>
                    {
                        new SwitchSkippedItem { Path = @"H:\dlc.nsp", Reason = "跳过DLC" }
                    }
                }),
                new FakeImportExecutor(),
                new FakeMetadataRefreshService());
            var view = new SwitchPendingImportView(viewModel);

            var candidateList = BuildCandidatesList(view);
            var skippedList = BuildSkippedList(view);

            Assert.IsInstanceOf<ScrollViewer>(candidateList);
            Assert.IsInstanceOf<ItemsControl>(((ScrollViewer)candidateList).Content);
            Assert.IsInstanceOf<ScrollViewer>(skippedList);
            Assert.IsInstanceOf<ItemsControl>(((ScrollViewer)skippedList).Content);
        }

        [Test]
        public void Pending_view_assigns_default_platform_and_exposes_platform_options()
        {
            var defaultPlatformId = Guid.Parse("12121212-1212-1212-1212-121212121212");
            var viewModel = new SwitchPendingImportViewModel(
                new SwitchSmartImportSettings { DefaultPlatformId = defaultPlatformId },
                new FakePendingStore(new SwitchPendingImportSnapshot
                {
                    Candidates = new List<SwitchImportCandidate>
                    {
                        new SwitchImportCandidate { GameName = "A", BasePath = @"H:\A.nsp", Import = true }
                    }
                }),
                new FakeImportExecutor(),
                new FakeMetadataRefreshService());

            Assert.AreEqual(defaultPlatformId, viewModel.Candidates[0].SelectedPlatformId);
            Assert.AreEqual(2, viewModel.PlatformOptions.Count);
            Assert.AreEqual("Nintendo Switch", viewModel.PlatformOptions[1].Name);
        }

        [Test]
        public void Pending_view_refreshes_summary_after_import()
        {
            var store = new FakePendingStore(new SwitchPendingImportSnapshot
            {
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "A", BasePath = @"H:\A.nsp", Import = true },
                    new SwitchImportCandidate { GameName = "B", BasePath = @"H:\B.nsp", Import = false }
                }
            });
            var viewModel = new SwitchPendingImportViewModel(
                new SwitchSmartImportSettings(),
                store,
                new FakeImportExecutor(),
                new FakeMetadataRefreshService());
            var view = new SwitchPendingImportView(viewModel);

            viewModel.ImportSelected();
            var summary = BuildSummary(view) as TextBlock;

            Assert.IsNotNull(summary);
            Assert.AreEqual("候选数量：1", summary.Text);
        }

        [Test]
        public void Pending_view_shows_saved_time_in_summary()
        {
            var store = new FakePendingStore(new SwitchPendingImportSnapshot
            {
                SavedAt = new DateTime(2026, 6, 20, 21, 30, 0),
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "A", BasePath = @"H:\A.nsp", Import = true }
                }
            });
            var viewModel = new SwitchPendingImportViewModel(
                new SwitchSmartImportSettings(),
                store,
                new FakeImportExecutor(),
                new FakeMetadataRefreshService());
            var view = new SwitchPendingImportView(viewModel);

            var summary = BuildSummary(view) as TextBlock;

            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Text.Contains("最近扫描：2026-06-20 21:30"));
        }

        [Test]
        public void Pending_view_does_not_throw_when_import_configuration_is_missing()
        {
            var store = new FakePendingStore(new SwitchPendingImportSnapshot
            {
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "A", BasePath = @"H:\A.nsp", Import = true }
                }
            });
            var viewModel = new SwitchPendingImportViewModel(
                new SwitchSmartImportSettings(),
                store,
                new ThrowingImportExecutor(new InvalidOperationException("默认模拟器未配置。")),
                new FakeMetadataRefreshService(),
                progressService: new FakeProgressService());

            Assert.DoesNotThrow(() => viewModel.ImportSelected());
            Assert.AreEqual("默认模拟器未配置。", viewModel.LastErrorMessage);
        }

        [Test]
        public void Pending_view_runs_import_in_progress_service()
        {
            var progress = new FakeProgressService();
            var store = new FakePendingStore(new SwitchPendingImportSnapshot
            {
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "A", BasePath = @"H:\A.nsp", Import = true }
                }
            });
            var viewModel = new SwitchPendingImportViewModel(
                new SwitchSmartImportSettings(),
                store,
                new FakeImportExecutor(),
                new FakeMetadataRefreshService(),
                progressService: progress);

            viewModel.ImportSelected();

            Assert.AreEqual(1, progress.RunCount);
            Assert.AreEqual("正在导入 Switch 游戏...", progress.LastTitle);
        }

        [Test]
        public void Pending_view_runs_import_in_background_and_reports_notifications()
        {
            var progress = new FakeAsyncProgressService();
            var messages = new FakeMessageService();
            var store = new FakePendingStore(new SwitchPendingImportSnapshot
            {
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "A", BasePath = @"H:\A.nsp", Import = true }
                }
            });
            var executor = new FakeImportExecutor();
            var refresh = new FakeMetadataRefreshService();
            var viewModel = new SwitchPendingImportViewModel(
                new SwitchSmartImportSettings(),
                store,
                executor,
                refresh,
                messages,
                progress);

            viewModel.ImportSelected();

            Assert.IsTrue(progress.Started.WaitOne(1000));
            Assert.IsTrue(viewModel.IsImporting);
            Assert.AreEqual(1, messages.InfoMessages.Count);
            Assert.AreEqual("已开始后台导入 1 个 Switch 游戏。", messages.InfoMessages[0]);
            Assert.AreEqual(0, executor.ImportCallCount);

            progress.Release();

            Assert.IsTrue(progress.Completed.WaitOne(1000));
            Assert.AreEqual(1, executor.ImportCallCount);
            Assert.AreEqual(2, messages.InfoMessages.Count);
            Assert.AreEqual("Switch 智能导入完成，已导入 1 个游戏。", messages.InfoMessages[1]);
            Assert.IsFalse(viewModel.IsImporting);
            Assert.AreEqual(1, store.SaveCallCount);
            Assert.AreEqual(0, store.LastSaved.Candidates.Count);
        }

        [Test]
        public void Pending_view_reports_error_notification_when_background_import_fails()
        {
            var progress = new FakeAsyncProgressService();
            var messages = new FakeMessageService();
            var store = new FakePendingStore(new SwitchPendingImportSnapshot
            {
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "A", BasePath = @"H:\A.nsp", Import = true }
                }
            });
            var viewModel = new SwitchPendingImportViewModel(
                new SwitchSmartImportSettings(),
                store,
                new ThrowingImportExecutor(new InvalidOperationException("导入失败。")),
                new FakeMetadataRefreshService(),
                messages,
                progress);

            viewModel.ImportSelected();
            Assert.IsTrue(progress.Started.WaitOne(1000));

            progress.Release();

            Assert.IsTrue(progress.Completed.WaitOne(1000));
            Assert.AreEqual("导入失败。", viewModel.LastErrorMessage);
            Assert.IsFalse(viewModel.IsImporting);
            Assert.AreEqual(1, messages.ErrorMessages.Count);
            Assert.AreEqual("导入失败。", messages.ErrorMessages[0]);
            Assert.AreEqual(0, store.SaveCallCount);
        }

        private static UIElement BuildSummary(object view)
        {
            var method = view.GetType().GetMethod("BuildSummary", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (UIElement)method.Invoke(view, null);
        }

        private static UIElement BuildCandidatesList(object view)
        {
            var method = view.GetType().GetMethod("BuildCandidatesList", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (UIElement)method.Invoke(view, null);
        }

        private static UIElement BuildSkippedList(object view)
        {
            var method = view.GetType().GetMethod("BuildSkippedList", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (UIElement)method.Invoke(view, null);
        }

        private class FakePendingStore : ISwitchPendingImportStore
        {
            private readonly SwitchPendingImportSnapshot snapshot;

            public int SaveCallCount { get; private set; }
            public SwitchPendingImportSnapshot LastSaved { get; private set; } = new SwitchPendingImportSnapshot();

            public FakePendingStore(SwitchPendingImportSnapshot snapshot)
            {
                this.snapshot = snapshot;
            }

            public SwitchPendingImportSnapshot Load()
            {
                return snapshot;
            }

            public void Save(List<SwitchImportCandidate> candidates, DateTime savedAt, List<SwitchSkippedItem> skippedItems = null)
            {
                SaveCallCount++;
                LastSaved = new SwitchPendingImportSnapshot
                {
                    SavedAt = savedAt,
                    Candidates = candidates ?? new List<SwitchImportCandidate>(),
                    SkippedItems = skippedItems ?? new List<SwitchSkippedItem>()
                };
            }
        }

        private class FakeImportExecutor : ISwitchImportExecutor
        {
            public int ImportCallCount { get; private set; }
            public List<SwitchImportCandidate> LastCandidates { get; private set; } = new List<SwitchImportCandidate>();

            public List<Game> Import(IEnumerable<SwitchImportCandidate> candidates, SwitchSmartImportSettings settings)
            {
                ImportCallCount++;
                LastCandidates = new List<SwitchImportCandidate>(candidates);
                return new List<Game> { new Game("A") { Id = Guid.NewGuid() } };
            }
        }

        private class FakeMetadataRefreshService : ISwitchMetadataRefreshService
        {
            public int RefreshCallCount { get; private set; }
            public SwitchMetadataSource LastSource { get; private set; }

            public void Refresh(IEnumerable<Game> games, SwitchMetadataSource source)
            {
                RefreshCallCount++;
                LastSource = source;
            }
        }

        private class ThrowingImportExecutor : ISwitchImportExecutor
        {
            private readonly Exception error;

            public ThrowingImportExecutor(Exception error)
            {
                this.error = error;
            }

            public List<Game> Import(IEnumerable<SwitchImportCandidate> candidates, SwitchSmartImportSettings settings)
            {
                throw error;
            }
        }

        private class FakeProgressService : ISwitchProgressService
        {
            public int RunCount { get; private set; }
            public string LastTitle { get; private set; }

            public void Run(string title, Action action, Action onCompleted, Action<Exception> onFailed)
            {
                RunCount++;
                LastTitle = title;
                try
                {
                    action();
                    onCompleted();
                }
                catch (Exception ex)
                {
                    onFailed(ex);
                }
            }
        }

        private class FakeAsyncProgressService : ISwitchProgressService
        {
            public ManualResetEvent Started { get; } = new ManualResetEvent(false);
            public ManualResetEvent Completed { get; } = new ManualResetEvent(false);
            private readonly ManualResetEvent release = new ManualResetEvent(false);

            public void Run(string title, Action action, Action onCompleted, Action<Exception> onFailed)
            {
                Task.Run(() =>
                {
                    Started.Set();
                    release.WaitOne();

                    try
                    {
                        action();
                        onCompleted();
                    }
                    catch (Exception ex)
                    {
                        onFailed(ex);
                    }
                    finally
                    {
                        Completed.Set();
                    }
                });
            }

            public void Release()
            {
                release.Set();
            }
        }

        private class FakeMessageService : ISwitchMessageService
        {
            public List<string> InfoMessages { get; } = new List<string>();
            public List<string> ErrorMessages { get; } = new List<string>();

            public void ShowInfo(string message)
            {
                InfoMessages.Add(message);
            }

            public void ShowError(string message, string caption)
            {
                ErrorMessages.Add(message);
            }
        }
    }
}
