// 文件用途：验证 Switch 智能导入插件的主菜单入口和设置页骨架。
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SwitchSmartImport.Tests
{
    // 验证插件第一版基础入口。
    [TestFixture]
    public class SwitchSmartImportPluginTests
    {
        [Test]
        public void Plugin_adds_main_menu_entries()
        {
            var plugin = new SwitchSmartImportPlugin(new FakePlayniteApi());

            var items = plugin.GetMainMenuItems(new GetMainMenuItemsArgs()).ToList();

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual("Switch 智能导入", items[0].Description);
            Assert.AreEqual("立即扫描 Switch 智能导入", items[1].Description);
            Assert.AreEqual("Switch 智能导入设置", items[2].Description);
        }

        [Test]
        public void Plugin_exposes_settings()
        {
            var plugin = new SwitchSmartImportPlugin(new FakePlayniteApi());

            Assert.IsNotNull(plugin.GetSettings(false));
            Assert.IsNotNull(plugin.GetSettingsView(false));
        }

        [Test]
        public void Plugin_main_menu_opens_pending_import_window()
        {
            var windowService = new FakePendingWindowService();
            var plugin = new SwitchSmartImportPlugin(
                new FakePlayniteApi(),
                new SwitchSmartImportSettings(),
                new FakeScanner(),
                new FakePendingStore(),
                null,
                new FakeImportExecutor(),
                new FakeMetadataRefreshService(),
                new FakeMessageService(),
                new FakeProgressService(),
                windowService);

            var item = plugin.GetMainMenuItems(new GetMainMenuItemsArgs()).First();
            item.Action(new MainMenuItemActionArgs { SourceItem = item });

            Assert.AreEqual(1, windowService.ShowCount);
            Assert.IsNotNull(windowService.LastViewModel);
        }

        [Test]
        public void Plugin_run_scan_auto_imports_when_manual_confirmation_is_disabled()
        {
            var store = new FakePendingStore();
            var scanner = new FakeScanner(new SwitchCandidateMergeResult
            {
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "测试游戏", BasePath = @"H:\乙女\测试游戏\base.nsp", Import = true }
                }
            });
            var executor = new FakeImportExecutor();
            var metadata = new FakeMetadataRefreshService();
            var messages = new FakeMessageService();
            var plugin = new SwitchSmartImportPlugin(
                new FakePlayniteApi(),
                new SwitchSmartImportSettings
                {
                    ScanOnStartup = false,
                    RequireManualConfirmation = false,
                    MetadataSource = SwitchMetadataSource.SwitchLocalMetadata
                },
                scanner,
                store,
                null,
                executor,
                metadata,
                messages,
                new FakeProgressService(),
                new FakePendingWindowService());

            plugin.RunScan();

            Assert.AreEqual(1, scanner.ScanCount);
            Assert.AreEqual(1, executor.ImportCallCount);
            Assert.AreEqual(1, metadata.RefreshCallCount);
            Assert.IsTrue(messages.InfoMessages.Any(a => a.Contains("扫描完成")));
            Assert.IsTrue(messages.InfoMessages.Any(a => a.Contains("导入完成")));
        }

        [Test]
        public void Plugin_scheduled_scan_auto_imports_and_notifies_when_manual_confirmation_is_disabled()
        {
            var store = new FakePendingStore();
            var scanner = new FakeScanner(new SwitchCandidateMergeResult
            {
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "测试游戏", BasePath = @"H:\乙女\测试游戏\base.nsp", Import = true }
                }
            });
            var executor = new FakeImportExecutor();
            var metadata = new FakeMetadataRefreshService();
            var messages = new FakeMessageService();
            var scheduledService = new SwitchScheduledScanService(scanner, store, 60);
            var plugin = new SwitchSmartImportPlugin(
                new FakePlayniteApi(),
                new SwitchSmartImportSettings
                {
                    RequireManualConfirmation = false,
                    MetadataSource = SwitchMetadataSource.SwitchLocalMetadata
                },
                scanner,
                store,
                scheduledService,
                executor,
                metadata,
                messages,
                new FakeProgressService(),
                new FakePendingWindowService());

            scheduledService.RunOnce();

            Assert.AreEqual(1, scanner.ScanCount);
            Assert.AreEqual(1, executor.ImportCallCount);
            Assert.AreEqual(1, metadata.RefreshCallCount);
            Assert.IsTrue(messages.InfoMessages.Any(a => a.Contains("扫描完成")));
            Assert.IsTrue(messages.InfoMessages.Any(a => a.Contains("导入完成")));
        }

        private class FakePlayniteApi : IPlayniteAPI
        {
            private readonly IDialogsFactory dialogs;

            public IMainViewAPI MainView { get { return null; } }
            public IGameDatabaseAPI Database { get { return null; } }
            public IDialogsFactory Dialogs { get { return dialogs; } }
            public IPlaynitePathsAPI Paths { get { return new FakePlaynitePaths(); } }
            public INotificationsAPI Notifications { get { return null; } }
            public IPlayniteInfoAPI ApplicationInfo { get { return null; } }
            public IWebViewFactory WebViews { get { return null; } }
            public IResourceProvider Resources { get { return null; } }
            public IUriHandlerAPI UriHandler { get { return null; } }
            public IPlayniteSettingsAPI ApplicationSettings { get { return null; } }
            public IAddons Addons { get { return null; } }
            public IEmulationAPI Emulation { get { return null; } }

            public FakePlayniteApi(IDialogsFactory dialogs = null)
            {
                this.dialogs = dialogs;
            }

            public void AddCustomElementSupport(Plugin source, AddCustomElementSupportArgs args) { }
            public void AddSettingsSupport(Plugin source, AddSettingsSupportArgs args) { }
            public void AddConvertersSupport(Plugin source, AddConvertersSupportArgs args) { }
            public string ExpandGameVariables(Game game, string inputString) { return inputString; }
            public string ExpandGameVariables(Game game, string inputString, string emulatorDir) { return inputString; }
            public GameAction ExpandGameVariables(Game game, GameAction action) { return action; }
            public void StartGame(Guid gameId) { }
            public void InstallGame(Guid gameId) { }
            public void UninstallGame(Guid gameId) { }
            public List<GamepadController> GetConnectedControllers() { return new List<GamepadController>(); }
        }

        private class FakePendingWindowService : ISwitchPendingImportWindowService
        {
            public int ShowCount { get; private set; }
            public SwitchPendingImportViewModel LastViewModel { get; private set; }

            public void Show(SwitchPendingImportViewModel viewModel)
            {
                ShowCount++;
                LastViewModel = viewModel;
            }
        }

        private class FakeScanner : ISwitchImportScanner
        {
            private readonly SwitchCandidateMergeResult result;

            public int ScanCount { get; private set; }

            public FakeScanner(SwitchCandidateMergeResult result = null)
            {
                this.result = result ?? new SwitchCandidateMergeResult();
            }

            public SwitchCandidateMergeResult Scan()
            {
                ScanCount++;
                return result;
            }
        }

        private class FakePendingStore : ISwitchPendingImportStore
        {
            public int SaveCount { get; private set; }
            public List<SwitchImportCandidate> LastCandidates { get; private set; } = new List<SwitchImportCandidate>();
            public List<SwitchSkippedItem> LastSkippedItems { get; private set; } = new List<SwitchSkippedItem>();
            public DateTime LastSavedAt { get; private set; }

            public SwitchPendingImportSnapshot Load() => new SwitchPendingImportSnapshot
            {
                Candidates = LastCandidates.ToList(),
                SkippedItems = LastSkippedItems.ToList(),
                SavedAt = LastSavedAt
            };
            public void Save(List<SwitchImportCandidate> candidates, DateTime savedAt, List<SwitchSkippedItem> skippedItems = null)
            {
                SaveCount++;
                LastCandidates = candidates?.Select(CloneCandidate).ToList() ?? new List<SwitchImportCandidate>();
                LastSkippedItems = skippedItems?.ToList() ?? new List<SwitchSkippedItem>();
                LastSavedAt = savedAt;
            }

            private static SwitchImportCandidate CloneCandidate(SwitchImportCandidate candidate)
            {
                if (candidate == null)
                {
                    return null;
                }

                return new SwitchImportCandidate
                {
                    GameName = candidate.GameName,
                    BasePath = candidate.BasePath,
                    HighestPatchVersion = candidate.HighestPatchVersion,
                    Import = candidate.Import,
                    SelectedPlatformId = candidate.SelectedPlatformId
                };
            }
        }

        private class FakeImportExecutor : ISwitchImportExecutor
        {
            public int ImportCallCount { get; private set; }

            public List<Game> Import(IEnumerable<SwitchImportCandidate> candidates, SwitchSmartImportSettings settings)
            {
                ImportCallCount++;
                return new List<Game> { new Game("测试游戏") { Id = Guid.NewGuid() } };
            }
        }

        private class FakeMetadataRefreshService : ISwitchMetadataRefreshService
        {
            public int RefreshCallCount { get; private set; }

            public void Refresh(IEnumerable<Game> games, SwitchMetadataSource source)
            {
                RefreshCallCount++;
            }
        }

        private class FakeMessageService : ISwitchMessageService
        {
            public List<string> InfoMessages { get; } = new List<string>();

            public void ShowInfo(string message)
            {
                InfoMessages.Add(message);
            }
            public void ShowError(string message, string caption) { }
        }

        private class FakeProgressService : ISwitchProgressService
        {
            public void Run(string title, Action action, Action onCompleted, Action<Exception> onFailed)
            {
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

        private class FakePlaynitePaths : IPlaynitePathsAPI
        {
            public bool IsPortable { get { return true; } }
            public string ApplicationPath { get { return string.Empty; } }
            public string ConfigurationPath { get { return string.Empty; } }
            public string ExtensionsDataPath { get { return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SwitchSmartImportTests"); } }
        }
    }
}
