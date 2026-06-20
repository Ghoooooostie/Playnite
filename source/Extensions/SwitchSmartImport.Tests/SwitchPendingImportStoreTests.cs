// 文件用途：验证 Switch 智能导入设置默认值和待确认缓存读写。
using NUnit.Framework;
using Playnite;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SwitchSmartImport.Tests
{
    // 验证设置和缓存的最小行为。
    [TestFixture]
    public class SwitchPendingImportStoreTests
    {
        [Test]
        public void Settings_default_to_no_metadata_source_and_manual_confirmation()
        {
            var settings = new SwitchSmartImportSettings();

            Assert.AreEqual(SwitchMetadataSource.None, settings.MetadataSource);
            Assert.IsTrue(settings.RequireManualConfirmation);
        }

        [Test]
        public void Pending_store_round_trips_candidates()
        {
            var root = Path.Combine(Path.GetTempPath(), "SwitchSmartImportTests", Guid.NewGuid().ToString("N"));
            var store = new SwitchPendingImportStore(root);
            var items = new List<SwitchImportCandidate>
            {
                new SwitchImportCandidate
                {
                    GameName = "测试游戏",
                    BasePath = @"H:\乙女\测试游戏\base.nsp"
                }
            };

            store.Save(items, DateTime.Parse("2026-06-20T12:00:00"));
            var loaded = store.Load();

            Assert.AreEqual(1, loaded.Candidates.Count);
            Assert.AreEqual("测试游戏", loaded.Candidates[0].GameName);
        }

        [Test]
        public void Settings_view_model_allows_managing_scan_paths()
        {
            var viewModel = new SwitchSmartImportSettingsViewModel(null, new SwitchSmartImportSettings());

            viewModel.AddScanPath();
            viewModel.AddScanPath();
            viewModel.Settings.ScanPaths[0].Name = "本体目录";
            viewModel.Settings.ScanPaths[1].Name = "补丁目录";

            viewModel.MoveScanPathUp(viewModel.Settings.ScanPaths[1]);

            Assert.AreEqual(2, viewModel.Settings.ScanPaths.Count);
            Assert.AreEqual("补丁目录", viewModel.Settings.ScanPaths[0].Name);

            viewModel.RemoveScanPath(viewModel.Settings.ScanPaths[0]);

            Assert.AreEqual(1, viewModel.Settings.ScanPaths.Count);
            Assert.AreEqual("本体目录", viewModel.Settings.ScanPaths[0].Name);
        }

        [Test]
        public void Settings_view_model_reuses_smallest_scan_path_number()
        {
            var viewModel = new SwitchSmartImportSettingsViewModel(null, new SwitchSmartImportSettings
            {
                ScanPaths = new ObservableCollection<SwitchScanPathConfig>
                {
                    new SwitchScanPathConfig { Name = "扫描目录 1", Priority = 0 },
                    new SwitchScanPathConfig { Name = "扫描目录 3", Priority = 1 }
                }
            });

            viewModel.AddScanPath();

            Assert.AreEqual("扫描目录 2", viewModel.Settings.ScanPaths[2].Name);
        }

        [Test]
        public void Settings_view_model_rejects_invalid_scheduled_scan_minutes()
        {
            var viewModel = new SwitchSmartImportSettingsViewModel(null, new SwitchSmartImportSettings
            {
                EnableScheduledScan = true,
                ScheduledScanMinutes = 0
            });

            var verified = viewModel.VerifySettings(out var errors);

            Assert.IsFalse(verified);
            Assert.IsTrue(errors.Any(a => a.Contains("扫描间隔")));
        }

        [Test]
        public void Settings_view_model_runs_scan_and_opens_pending_list()
        {
            var scanCount = 0;
            var pendingCount = 0;
            var viewModel = new SwitchSmartImportSettingsViewModel(
                null,
                new SwitchSmartImportSettings(),
                delegate { scanCount++; },
                delegate { pendingCount++; });

            viewModel.RunManualScan();
            viewModel.OpenPendingImports();

            Assert.AreEqual(1, scanCount);
            Assert.AreEqual(1, pendingCount);
        }

        [Test]
        public void Settings_view_model_refreshes_platform_and_emulator_choices()
        {
            var api = new FakePlayniteApi(new FakeGameDatabaseApi());
            var plugin = new SwitchSmartImportPlugin(api);
            var viewModel = (SwitchSmartImportSettingsViewModel)plugin.GetSettings(false);

            Assert.AreEqual(0, viewModel.PlatformChoices.Count);
            Assert.AreEqual(0, viewModel.EmulatorChoices.Count);

            api.DatabaseImpl.PlatformsCollection.Add(new Platform("Nintendo Switch") { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") });
            var emulator = new Emulator("Ryujinx") { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") };
            emulator.CustomProfiles = new ObservableCollection<CustomEmulatorProfile>
            {
                new CustomEmulatorProfile
                {
                    Id = "ryujinx-default",
                    Name = "Default"
                }
            };
            api.DatabaseImpl.EmulatorsCollection.Add(emulator);

            viewModel.RefreshChoices();

            Assert.AreEqual(1, viewModel.PlatformChoices.Count);
            Assert.AreEqual("Nintendo Switch", viewModel.PlatformChoices[0].Name);
            Assert.AreEqual(1, viewModel.EmulatorChoices.Count);
            Assert.AreEqual("Ryujinx", viewModel.EmulatorChoices[0].Name);
            Assert.AreEqual(1, viewModel.EmulatorProfileChoices.Count);
            Assert.AreEqual("Default", viewModel.EmulatorProfileChoices[0].Name);
        }

        private class FakePlayniteApi : IPlayniteAPI
        {
            public FakeGameDatabaseApi DatabaseImpl { get; }

            public IMainViewAPI MainView => null;
            public IGameDatabaseAPI Database => DatabaseImpl;
            public IDialogsFactory Dialogs => null;
            public IPlaynitePathsAPI Paths => new FakePlaynitePaths();
            public INotificationsAPI Notifications => null;
            public IPlayniteInfoAPI ApplicationInfo => null;
            public IWebViewFactory WebViews => null;
            public IResourceProvider Resources => null;
            public IUriHandlerAPI UriHandler => null;
            public IPlayniteSettingsAPI ApplicationSettings => null;
            public IAddons Addons => null;
            public IEmulationAPI Emulation => null;

            public FakePlayniteApi(FakeGameDatabaseApi database)
            {
                DatabaseImpl = database;
            }

            public void AddCustomElementSupport(Plugin source, AddCustomElementSupportArgs args) { }
            public void AddSettingsSupport(Plugin source, AddSettingsSupportArgs args) { }
            public void AddConvertersSupport(Plugin source, AddConvertersSupportArgs args) { }
            public string ExpandGameVariables(Game game, string inputString) => inputString;
            public string ExpandGameVariables(Game game, string inputString, string emulatorDir) => inputString;
            public GameAction ExpandGameVariables(Game game, GameAction action) => action;
            public void StartGame(Guid gameId) { }
            public void InstallGame(Guid gameId) { }
            public void UninstallGame(Guid gameId) { }
            public List<GamepadController> GetConnectedControllers() => new List<GamepadController>();
        }

        private class FakePlaynitePaths : IPlaynitePathsAPI
        {
            public bool IsPortable => true;
            public string ApplicationPath => string.Empty;
            public string ConfigurationPath => string.Empty;
            public string ExtensionsDataPath => Path.Combine(Path.GetTempPath(), "SwitchSmartImportTests");
        }

        private class FakeGameDatabaseApi : IGameDatabaseAPI
        {
            public FakeItemCollection<Platform> PlatformsCollection { get; } = new FakeItemCollection<Platform>();
            public FakeItemCollection<Emulator> EmulatorsCollection { get; } = new FakeItemCollection<Emulator>();

            public string DatabasePath => string.Empty;
            public IItemCollection<Game> Games => new FakeItemCollection<Game>();
            public IItemCollection<Platform> Platforms => PlatformsCollection;
            public IItemCollection<Emulator> Emulators => EmulatorsCollection;
            public IItemCollection<Genre> Genres => new FakeItemCollection<Genre>();
            public IItemCollection<Company> Companies => new FakeItemCollection<Company>();
            public IItemCollection<Tag> Tags => new FakeItemCollection<Tag>();
            public IItemCollection<Category> Categories => new FakeItemCollection<Category>();
            public IItemCollection<Series> Series => new FakeItemCollection<Series>();
            public IItemCollection<AgeRating> AgeRatings => new FakeItemCollection<AgeRating>();
            public IItemCollection<Region> Regions => new FakeItemCollection<Region>();
            public IItemCollection<GameSource> Sources => new FakeItemCollection<GameSource>();
            public IItemCollection<GameFeature> Features => new FakeItemCollection<GameFeature>();
            public IItemCollection<GameScannerConfig> GameScanners => new FakeItemCollection<GameScannerConfig>();
            public IItemCollection<CompletionStatus> CompletionStatuses => new FakeItemCollection<CompletionStatus>();
            public IItemCollection<ImportExclusionItem> ImportExclusions => new FakeItemCollection<ImportExclusionItem>();
            public IItemCollection<FilterPreset> FilterPresets => new FakeItemCollection<FilterPreset>();
            public bool IsOpen => true;

            public event EventHandler DatabaseOpened { add { } remove { } }

            public string AddFile(string path, Guid parentId) => path;
            public void SaveFile(string id, string path) { }
            public void RemoveFile(string id) { }
            public IDisposable BufferedUpdate() => null;
            public void BeginBufferUpdate() { }
            public void EndBufferUpdate() { }
            public string GetFileStoragePath(Guid parentId) => string.Empty;
            public string GetFullFilePath(string databasePath) => databasePath;
            public Game ImportGame(GameMetadata game) => null;
            public Game ImportGame(GameMetadata game, LibraryPlugin sourcePlugin) => null;
            public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings) => false;
            public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings) => new List<Game>();
            public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings, bool useFuzzyNameMatch) => false;
            public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings, bool useFuzzyNameMatch) => new List<Game>();
        }

        private class FakeItemCollection<TItem> : IItemCollection<TItem> where TItem : DatabaseObject, new()
        {
            private readonly List<TItem> items = new List<TItem>();

            public GameDatabaseCollection CollectionType => GameDatabaseCollection.Uknown;
            public int Count => items.Count;
            public bool IsReadOnly => false;
            public TItem this[Guid id] { get => items.FirstOrDefault(a => a.Id == id); set { } }
            public event EventHandler<ItemCollectionChangedEventArgs<TItem>> ItemCollectionChanged { add { } remove { } }
            public event EventHandler<ItemUpdatedEventArgs<TItem>> ItemUpdated { add { } remove { } }

            public void Add(TItem item) => items.Add(item);
            public TItem Add(string itemName) => new TItem();
            public TItem Add(string itemName, Func<TItem, string, bool> existingComparer) => new TItem();
            public IEnumerable<TItem> Add(List<string> items) => new List<TItem>();
            public TItem Add(MetadataProperty property) => new TItem();
            public IEnumerable<TItem> Add(IEnumerable<MetadataProperty> properties) => new List<TItem>();
            public IEnumerable<TItem> Add(List<string> items, Func<TItem, string, bool> existingComparer) => new List<TItem>();
            public void Add(IEnumerable<TItem> items) => this.items.AddRange(items);
            public void BeginBufferUpdate() { }
            public IDisposable BufferedUpdate() => null;
            public void Clear() => items.Clear();
            public bool Contains(TItem item) => items.Contains(item);
            public bool ContainsItem(Guid id) => items.Any(a => a.Id == id);
            public void CopyTo(TItem[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
            public void Dispose() { }
            public void EndBufferUpdate() { }
            public TItem Get(Guid id) => items.FirstOrDefault(a => a.Id == id);
            public List<TItem> Get(IList<Guid> ids) => items.Where(a => ids.Contains(a.Id)).ToList();
            public IEnumerable<TItem> GetClone() => items.ToList();
            public IEnumerator<TItem> GetEnumerator() => items.GetEnumerator();
            public bool Remove(TItem item) => items.Remove(item);
            public bool Remove(Guid id)
            {
                var item = items.FirstOrDefault(a => a.Id == id);
                return item != null && items.Remove(item);
            }
            public bool Remove(IEnumerable<TItem> items)
            {
                var removed = false;
                foreach (var item in items.ToList())
                {
                    removed |= this.items.Remove(item);
                }

                return removed;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Update(TItem item) { }
            public void Update(IEnumerable<TItem> items) { }
        }

    }
}
