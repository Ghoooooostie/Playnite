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
            public SwitchCandidateMergeResult Scan() => new SwitchCandidateMergeResult();
        }

        private class FakePendingStore : ISwitchPendingImportStore
        {
            public SwitchPendingImportSnapshot Load() => new SwitchPendingImportSnapshot();
            public void Save(List<SwitchImportCandidate> candidates, DateTime savedAt, List<SwitchSkippedItem> skippedItems = null) { }
        }

        private class FakeImportExecutor : ISwitchImportExecutor
        {
            public List<Game> Import(IEnumerable<SwitchImportCandidate> candidates, SwitchSmartImportSettings settings) => new List<Game>();
        }

        private class FakeMetadataRefreshService : ISwitchMetadataRefreshService
        {
            public void Refresh(IEnumerable<Game> games, SwitchMetadataSource source) { }
        }

        private class FakeMessageService : ISwitchMessageService
        {
            public void ShowInfo(string message) { }
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
