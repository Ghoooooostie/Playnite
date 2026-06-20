// 文件用途：验证 Switch Local Metadata 刷新规则为空时跳过，配置后全量覆盖。
using NUnit.Framework;
using Playnite;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SwitchSmartImport.Tests
{
    // 验证导入后资料刷新行为。
    [TestFixture]
    public class SwitchMetadataRefreshServiceTests
    {
        [Test]
        public void Metadata_refresh_is_skipped_when_source_is_none()
        {
            var game = new Game("旧名字");
            var service = new SwitchMetadataRefreshService(new FakePlayniteApi(null));

            service.Refresh(new[] { game }, SwitchMetadataSource.None);

            Assert.AreEqual("旧名字", game.Name);
        }

        [Test]
        public void Metadata_refresh_uses_switch_local_metadata_and_overwrites_existing_values()
        {
            var plugin = new FakeSwitchMetadataPlugin();
            var api = new FakePlayniteApi(plugin);
            SetGameDatabaseReference(api.Database);
            var service = new SwitchMetadataRefreshService(api);
            var game = new Game("旧名字")
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Version = "旧版本",
                InstallSize = 1,
                Roms = new ObservableCollection<GameRom> { new GameRom("base", @"H:\乙女\测试\base.nsp") }
            };

            service.Refresh(new[] { game }, SwitchMetadataSource.SwitchLocalMetadata);

            Assert.AreEqual("新名字", game.Name);
            Assert.AreEqual("新发行商", game.Developers[0].Name);
            Assert.AreEqual("新发行商", game.Publishers[0].Name);
            Assert.AreEqual("Switch Title ID", game.Links[0].Name);
            Assert.AreEqual("0100TEST00000000", game.Links[0].Url);
            Assert.AreEqual(99UL, game.InstallSize);
            Assert.AreEqual("db-background-id", game.BackgroundImage);
            Assert.AreEqual("db-icon-id", game.Icon);
            Assert.AreEqual("db-cover-id", game.CoverImage);
            Assert.AreEqual(1, plugin.CallCount);
            Assert.AreEqual(1, ((FakeGameDatabaseApi)api.Database).UpdatedGames.Count);
            Assert.AreEqual(game.Id, ((FakeGameDatabaseApi)api.Database).UpdatedGames[0].Id);
        }

        [Test]
        public void Metadata_refresh_continues_when_one_game_provider_fails()
        {
            var plugin = new FakeSwitchMetadataPlugin(game =>
            {
                if (game.Name == "坏游戏")
                {
                    throw new InvalidOperationException("读取失败");
                }

                return new FakeSwitchMetadataPlugin.FakeProvider();
            });
            var api = new FakePlayniteApi(plugin);
            SetGameDatabaseReference(api.Database);
            var service = new SwitchMetadataRefreshService(api);
            var broken = new Game("坏游戏")
            {
                Id = Guid.NewGuid(),
                Roms = new ObservableCollection<GameRom> { new GameRom("broken", @"H:\broken.nsp") }
            };
            var good = new Game("好游戏")
            {
                Id = Guid.NewGuid(),
                Roms = new ObservableCollection<GameRom> { new GameRom("good", @"H:\good.nsp") }
            };

            Assert.DoesNotThrow(() => service.Refresh(new[] { broken, good }, SwitchMetadataSource.SwitchLocalMetadata));
            Assert.AreEqual("坏游戏", broken.Name);
            Assert.AreEqual("新名字", good.Name);
            Assert.AreEqual(1, ((FakeGameDatabaseApi)api.Database).UpdatedGames.Count);
            Assert.AreEqual(good.Id, ((FakeGameDatabaseApi)api.Database).UpdatedGames[0].Id);
        }

        private static void SetGameDatabaseReference(IGameDatabaseAPI database)
        {
            typeof(Game).GetProperty("DatabaseReference", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, database);
        }

        private class FakePlayniteApi : IPlayniteAPI
        {
            public IMainViewAPI MainView => null;
            public IGameDatabaseAPI Database { get; } = new FakeGameDatabaseApi();
            public IDialogsFactory Dialogs => null;
            public IPlaynitePathsAPI Paths => null;
            public INotificationsAPI Notifications => null;
            public IPlayniteInfoAPI ApplicationInfo => null;
            public IWebViewFactory WebViews => null;
            public IResourceProvider Resources => null;
            public IUriHandlerAPI UriHandler => null;
            public IPlayniteSettingsAPI ApplicationSettings => null;
            public IAddons Addons { get; }
            public IEmulationAPI Emulation => null;

            public FakePlayniteApi(MetadataPlugin plugin)
            {
                Addons = new FakeAddons(plugin);
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

        private class FakeAddons : IAddons
        {
            public List<string> DisabledAddons { get; } = new List<string>();
            public List<string> Addons { get; } = new List<string>();
            public List<Plugin> Plugins { get; } = new List<Plugin>();

            public FakeAddons(MetadataPlugin plugin)
            {
                if (plugin != null)
                {
                    Plugins.Add(plugin);
                }
            }
        }

        private class FakeGameDatabaseApi : IGameDatabaseAPI
        {
            public List<Game> UpdatedGames { get; } = new List<Game>();
            public string DatabasePath => string.Empty;
            public IItemCollection<Game> Games { get; }
            public IItemCollection<Platform> Platforms { get; } = new FakePlatformCollection();
            public IItemCollection<Emulator> Emulators => new FakeItemCollection<Emulator>();
            public IItemCollection<Genre> Genres => new FakeItemCollection<Genre>();
            public IItemCollection<Company> Companies { get; } = new FakeCompanyCollection();
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
            public Game ImportGame(GameMetadata game) => new Game(game.Name);
            public Game ImportGame(GameMetadata game, LibraryPlugin sourcePlugin) => new Game(game.Name);
            public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings) => false;
            public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings) => new List<Game>();
            public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings, bool useFuzzyNameMatch) => false;
            public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings, bool useFuzzyNameMatch) => new List<Game>();

            public FakeGameDatabaseApi()
            {
                Games = new FakeGameCollection(UpdatedGames);
            }

            private class FakeGameCollection : FakeItemCollection<Game>
            {
                private readonly List<Game> updatedGames;

                public FakeGameCollection(List<Game> updatedGames)
                {
                    this.updatedGames = updatedGames;
                }

                public override void Update(Game item)
                {
                    updatedGames.Add(item);
                    base.Update(item);
                }
            }

            private class FakePlatformCollection : FakeItemCollection<Platform>
            {
                public override IEnumerable<Platform> Add(IEnumerable<MetadataProperty> properties)
                {
                    var items = properties.Select(a => new Platform("Nintendo Switch") { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), SpecificationId = ((MetadataSpecProperty)a).Id }).ToList();
                    Add(items);
                    return items;
                }
            }

            private class FakeCompanyCollection : FakeItemCollection<Company>
            {
                public override IEnumerable<Company> Add(IEnumerable<MetadataProperty> properties)
                {
                    var items = properties.Select(a => new Company(((MetadataNameProperty)a).Name) { Id = Guid.NewGuid() }).ToList();
                    Add(items);
                    return items;
                }
            }

            private class FakeItemCollection<TItem> : IItemCollection<TItem> where TItem : DatabaseObject, new()
            {
                private readonly Dictionary<Guid, TItem> items = new Dictionary<Guid, TItem>();

                public virtual GameDatabaseCollection CollectionType => GameDatabaseCollection.Uknown;
                public int Count => items.Count;
                public bool IsReadOnly => false;
                public TItem this[Guid id] { get => items.TryGetValue(id, out var item) ? item : default(TItem); set => items[id] = value; }
                public event EventHandler<ItemCollectionChangedEventArgs<TItem>> ItemCollectionChanged { add { } remove { } }
                public event EventHandler<ItemUpdatedEventArgs<TItem>> ItemUpdated { add { } remove { } }
                public void Add(TItem item) { items[item.Id] = item; }
                public TItem Add(string itemName) => new TItem();
                public TItem Add(string itemName, Func<TItem, string, bool> existingComparer) => new TItem();
                public IEnumerable<TItem> Add(List<string> items) => new List<TItem>();
                public TItem Add(MetadataProperty property) => new TItem();
                public virtual IEnumerable<TItem> Add(IEnumerable<MetadataProperty> properties) => new List<TItem>();
                public IEnumerable<TItem> Add(List<string> items, Func<TItem, string, bool> existingComparer) => new List<TItem>();
                public void Add(IEnumerable<TItem> items)
                {
                    foreach (var item in items)
                    {
                        Add(item);
                    }
                }
                public void BeginBufferUpdate() { }
                public IDisposable BufferedUpdate() => null;
                public void Clear() { items.Clear(); }
                public bool Contains(TItem item) => item != null && items.ContainsKey(item.Id);
                public bool ContainsItem(Guid id) => items.ContainsKey(id);
                public void CopyTo(TItem[] array, int arrayIndex) { }
                public void Dispose() { }
                public void EndBufferUpdate() { }
                public TItem Get(Guid id) => this[id];
                public List<TItem> Get(IList<Guid> ids) => ids.Select(Get).Where(a => a != null).ToList();
                public IEnumerable<TItem> GetClone() => items.Values.ToList();
                public IEnumerator<TItem> GetEnumerator() => items.Values.GetEnumerator();
                public bool Remove(TItem item) => item != null && items.Remove(item.Id);
                public bool Remove(Guid id) => items.Remove(id);
                public bool Remove(IEnumerable<TItem> items)
                {
                    var removed = false;
                    foreach (var item in items ?? Enumerable.Empty<TItem>())
                    {
                        removed |= Remove(item);
                    }

                    return removed;
                }
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
                public virtual void Update(TItem item) { Add(item); }
                public virtual void Update(IEnumerable<TItem> items) { Add(items); }
            }
        }

        private class FakeSwitchMetadataPlugin : MetadataPlugin
        {
            public int CallCount { get; private set; }
            private readonly Func<Game, OnDemandMetadataProvider> providerFactory;

            public override Guid Id { get; } = Guid.Parse("55555555-5555-5555-5555-555555555555");
            public override string Name => "Switch Local Metadata";
            public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
            {
                MetadataField.Name,
                MetadataField.Developers,
                MetadataField.Publishers,
                MetadataField.Platform,
                MetadataField.Links,
                MetadataField.Icon,
                MetadataField.CoverImage,
                MetadataField.InstallSize
            };

            public FakeSwitchMetadataPlugin(Func<Game, OnDemandMetadataProvider> providerFactory = null) : base(null)
            {
                this.providerFactory = providerFactory ?? (_ => new FakeProvider());
            }

            public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
            {
                CallCount++;
                return providerFactory(options.GameData);
            }

            public class FakeProvider : OnDemandMetadataProvider
            {
                public override List<MetadataField> AvailableFields { get; } = new List<MetadataField>
                {
                    MetadataField.Name,
                    MetadataField.Developers,
                    MetadataField.Publishers,
                    MetadataField.Platform,
                    MetadataField.Links,
                    MetadataField.Icon,
                    MetadataField.CoverImage,
                    MetadataField.InstallSize
                };

                public override string GetName(GetMetadataFieldArgs args) => "新名字";
                public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args) => new[] { new MetadataNameProperty("新发行商") };
                public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args) => new[] { new MetadataNameProperty("新发行商") };
                public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args) => new[] { new MetadataSpecProperty("nintendo_switch") };
                public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args) => new[] { new Link("Switch Title ID", "0100TEST00000000") };
                public override MetadataFile GetIcon(GetMetadataFieldArgs args) => new MetadataFile("icon.png", new byte[] { 1, 2, 3 }, "db-icon-id");
                public override MetadataFile GetCoverImage(GetMetadataFieldArgs args) => new MetadataFile("cover.png", new byte[] { 4, 5, 6 }, "db-cover-id");
                public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args) => new MetadataFile("bg.png", new byte[] { 7, 8, 9 }, "db-background-id");
                public override ulong? GetInstallSize(GetMetadataFieldArgs args) => 99;
            }
        }
    }
}
