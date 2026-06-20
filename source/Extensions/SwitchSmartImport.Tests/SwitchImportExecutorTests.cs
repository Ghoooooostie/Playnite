// 文件用途：验证 Switch 候选导入时只生成一个游戏并保留本体路径。
using NUnit.Framework;
using Playnite;
using Playnite.SDK.Models;
using Playnite.SDK.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SwitchSmartImport.Tests
{
    // 验证待确认候选到 Playnite 游戏的最小导入链路。
    [TestFixture]
    public class SwitchImportExecutorTests
    {
        [Test]
        public void Import_executor_creates_one_game_from_candidate()
        {
            var database = new FakeGameDatabaseApi();
            var executor = new SwitchImportExecutor(database);
            var settings = new SwitchSmartImportSettings
            {
                DefaultEmulatorId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DefaultEmulatorProfileId = "ryujinx-default",
                DefaultPlatformId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            };
            var customPlatformId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var candidates = new List<SwitchImportCandidate>
            {
                new SwitchImportCandidate
                {
                    GameName = "PanicPalette",
                    BasePath = @"H:\乙女\PanicPalette [010063C0212BE000][v0][Base].nsp",
                    HighestPatchVersion = "1.0.3",
                    SelectedPlatformId = customPlatformId
                }
            };

            var imported = executor.Import(candidates, settings);

            Assert.AreEqual(1, imported.Count);
            Assert.AreEqual(1, database.ImportedGames.Count);
            Assert.AreEqual("PanicPalette", imported[0].Name);
            Assert.AreEqual(@"H:\乙女\PanicPalette [010063C0212BE000][v0][Base].nsp", imported[0].Roms[0].Path);
            Assert.AreEqual("1.0.3", imported[0].Version);
            Assert.AreEqual(settings.DefaultEmulatorId, imported[0].GameActions[0].EmulatorId);
            Assert.AreEqual(settings.DefaultEmulatorProfileId, imported[0].GameActions[0].EmulatorProfileId);
            Assert.AreEqual(customPlatformId, imported[0].PlatformIds[0]);
        }

        [Test]
        public void Import_executor_requires_default_emulator_configuration()
        {
            var database = new FakeGameDatabaseApi();
            var executor = new SwitchImportExecutor(database);
            var settings = new SwitchSmartImportSettings
            {
                DefaultPlatformId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            };
            var candidates = new List<SwitchImportCandidate>
            {
                new SwitchImportCandidate
                {
                    GameName = "PanicPalette",
                    BasePath = @"H:\乙女\PanicPalette [010063C0212BE000][v0][Base].nsp",
                    Import = true
                }
            };

            var error = Assert.Throws<InvalidOperationException>(() => executor.Import(candidates, settings));

            Assert.AreEqual("默认模拟器未配置。", error.Message);
        }

        [Test]
        public void Import_executor_updates_existing_game_when_same_rom_path_already_exists()
        {
            var database = new FakeGameDatabaseApi();
            var existing = new Game("レンドフルール for Nintendo Switch")
            {
                Id = Guid.NewGuid(),
                InstallDirectory = @"H:\乙女",
                PlatformIds = new List<Guid> { Guid.Parse("22222222-2222-2222-2222-222222222222") },
                Roms = new ObservableCollection<GameRom>
                {
                    new GameRom("レンドフルール.xci", @"H:\乙女\レンドフルール for Nintendo Switch [www.yxwotome.com][0100B5800C0E4000].xci")
                }
            };
            database.StoredGames.Add(existing);
            var executor = new SwitchImportExecutor(database);
            var settings = new SwitchSmartImportSettings
            {
                DefaultEmulatorId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DefaultEmulatorProfileId = "ryujinx-default",
                DefaultPlatformId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            };
            var candidates = new List<SwitchImportCandidate>
            {
                new SwitchImportCandidate
                {
                    GameName = "レンドフルール",
                    BasePath = @"H:\乙女\レンドフルール for Nintendo Switch [www.yxwotome.com][0100B5800C0E4000].xci",
                    Import = true
                }
            };

            var imported = executor.Import(candidates, settings);

            Assert.AreEqual(1, imported.Count);
            Assert.AreEqual(0, database.ImportedGames.Count);
            Assert.AreEqual(1, database.UpdatedGames.Count);
            Assert.AreEqual(existing.Id, imported[0].Id);
            Assert.AreEqual("レンドフルール", existing.Name);
        }

        [Test]
        public void Import_executor_updates_existing_game_when_same_name_and_directory_use_another_base_format()
        {
            var database = new FakeGameDatabaseApi();
            var existing = new Game("レンドフルール for Nintendo Switch")
            {
                Id = Guid.NewGuid(),
                InstallDirectory = @"H:\乙女\【日文版】レンドフルール for Nintendo Switch",
                PlatformIds = new List<Guid> { Guid.Parse("22222222-2222-2222-2222-222222222222") },
                Roms = new ObservableCollection<GameRom>
                {
                    new GameRom("Reine des Fleurs .nsp", @"H:\乙女\【日文版】レンドフルール for Nintendo Switch\Reine des Fleurs .nsp")
                }
            };
            database.StoredGames.Add(existing);
            var executor = new SwitchImportExecutor(database);
            var settings = new SwitchSmartImportSettings
            {
                DefaultEmulatorId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DefaultEmulatorProfileId = "ryujinx-default",
                DefaultPlatformId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            };
            var candidates = new List<SwitchImportCandidate>
            {
                new SwitchImportCandidate
                {
                    GameName = "レンドフルール",
                    BasePath = @"H:\乙女\【日文版】レンドフルール for Nintendo Switch\Reine des Fleurs .nsp",
                    Import = true
                }
            };

            var imported = executor.Import(candidates, settings);

            Assert.AreEqual(1, imported.Count);
            Assert.AreEqual(0, database.ImportedGames.Count);
            Assert.AreEqual(1, database.UpdatedGames.Count);
            Assert.AreEqual(existing.Id, imported[0].Id);
            Assert.AreEqual("レンドフルール", existing.Name);
        }

        [Test]
        public void Import_executor_updates_existing_game_when_rom_directory_alias_matches_another_base_format()
        {
            var database = new FakeGameDatabaseApi();
            var existing = new Game("Reine des Fleurs")
            {
                Id = Guid.NewGuid(),
                InstallDirectory = @"H:\乙女\【日文版】レンドフルール for Nintendo Switch",
                PlatformIds = new List<Guid> { Guid.Parse("22222222-2222-2222-2222-222222222222") },
                Roms = new ObservableCollection<GameRom>
                {
                    new GameRom("Reine des Fleurs .nsp", @"H:\乙女\【日文版】レンドフルール for Nintendo Switch\Reine des Fleurs .nsp")
                }
            };
            database.StoredGames.Add(existing);
            var executor = new SwitchImportExecutor(database);
            var settings = new SwitchSmartImportSettings
            {
                DefaultEmulatorId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DefaultEmulatorProfileId = "ryujinx-default",
                DefaultPlatformId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            };
            var candidates = new List<SwitchImportCandidate>
            {
                new SwitchImportCandidate
                {
                    GameName = "レンドフルール",
                    BasePath = @"H:\乙女\レンドフルール for Nintendo Switch [www.yxwotome.com][0100B5800C0E4000].xci",
                    Import = true
                }
            };

            var imported = executor.Import(candidates, settings);

            Assert.AreEqual(1, imported.Count);
            Assert.AreEqual(0, database.ImportedGames.Count);
            Assert.AreEqual(1, database.UpdatedGames.Count);
            Assert.AreEqual(existing.Id, imported[0].Id);
        }

        private class FakeGameDatabaseApi : Playnite.SDK.IGameDatabaseAPI
        {
            public List<Game> StoredGames { get; } = new List<Game>();
            public List<Game> ImportedGames { get; } = new List<Game>();
            public List<Game> UpdatedGames { get; } = new List<Game>();

            public string DatabasePath => string.Empty;
            public Playnite.SDK.IItemCollection<Game> Games => new FakeGameCollection(StoredGames, UpdatedGames);
            public Playnite.SDK.IItemCollection<Platform> Platforms => new FakeItemCollection<Platform>();
            public Playnite.SDK.IItemCollection<Emulator> Emulators => new FakeItemCollection<Emulator>();
            public Playnite.SDK.IItemCollection<Genre> Genres => new FakeItemCollection<Genre>();
            public Playnite.SDK.IItemCollection<Company> Companies => new FakeItemCollection<Company>();
            public Playnite.SDK.IItemCollection<Tag> Tags => new FakeItemCollection<Tag>();
            public Playnite.SDK.IItemCollection<Category> Categories => new FakeItemCollection<Category>();
            public Playnite.SDK.IItemCollection<Series> Series => new FakeItemCollection<Series>();
            public Playnite.SDK.IItemCollection<AgeRating> AgeRatings => new FakeItemCollection<AgeRating>();
            public Playnite.SDK.IItemCollection<Region> Regions => new FakeItemCollection<Region>();
            public Playnite.SDK.IItemCollection<GameSource> Sources => new FakeItemCollection<GameSource>();
            public Playnite.SDK.IItemCollection<GameFeature> Features => new FakeItemCollection<GameFeature>();
            public Playnite.SDK.IItemCollection<GameScannerConfig> GameScanners => new FakeItemCollection<GameScannerConfig>();
            public Playnite.SDK.IItemCollection<CompletionStatus> CompletionStatuses => new FakeItemCollection<CompletionStatus>();
            public Playnite.SDK.IItemCollection<ImportExclusionItem> ImportExclusions => new FakeItemCollection<ImportExclusionItem>();
            public Playnite.SDK.IItemCollection<FilterPreset> FilterPresets => new FakeItemCollection<FilterPreset>();
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

            public Game ImportGame(GameMetadata game)
            {
                var imported = new Game(game.Name)
                {
                    Id = Guid.NewGuid(),
                    InstallDirectory = game.InstallDirectory,
                    Version = game.Version,
                    PlatformIds = game.Platforms?.Select(a => ((Playnite.SDK.Models.MetadataIdProperty)a).Id).ToList() ?? new List<Guid>(),
                    GameActions = game.GameActions == null ? null : new ObservableCollection<GameAction>(game.GameActions),
                    Roms = game.Roms == null ? null : new ObservableCollection<GameRom>(game.Roms)
                };
                ImportedGames.Add(imported);
                StoredGames.Add(imported);
                return imported;
            }

            public Game ImportGame(GameMetadata game, Playnite.SDK.Plugins.LibraryPlugin sourcePlugin) => ImportGame(game);
            public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings) => false;
            public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings) => ImportedGames;
            public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings, bool useFuzzyNameMatch) => false;
            public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings, bool useFuzzyNameMatch) => ImportedGames;

            private class FakeGameCollection : Playnite.SDK.IItemCollection<Game>
            {
                private readonly List<Game> items;
                private readonly List<Game> updatedGames;

                public FakeGameCollection(List<Game> items, List<Game> updatedGames)
                {
                    this.items = items;
                    this.updatedGames = updatedGames;
                }

                public Playnite.SDK.GameDatabaseCollection CollectionType => Playnite.SDK.GameDatabaseCollection.Games;
                public int Count => items.Count;
                public bool IsReadOnly => false;
                public Game this[Guid id] { get => items.FirstOrDefault(a => a.Id == id); set { } }
                public event EventHandler<Playnite.SDK.ItemCollectionChangedEventArgs<Game>> ItemCollectionChanged { add { } remove { } }
                public event EventHandler<Playnite.SDK.ItemUpdatedEventArgs<Game>> ItemUpdated { add { } remove { } }
                public void Add(Game item) => items.Add(item);
                public Game Add(string itemName) => new Game(itemName);
                public Game Add(string itemName, Func<Game, string, bool> existingComparer) => new Game(itemName);
                public IEnumerable<Game> Add(List<string> items) => new List<Game>();
                public Game Add(MetadataProperty property) => new Game(string.Empty);
                public IEnumerable<Game> Add(IEnumerable<MetadataProperty> properties) => new List<Game>();
                public IEnumerable<Game> Add(List<string> items, Func<Game, string, bool> existingComparer) => new List<Game>();
                public void Add(IEnumerable<Game> items) => this.items.AddRange(items);
                public void BeginBufferUpdate() { }
                public IDisposable BufferedUpdate() => null;
                public void Clear() => items.Clear();
                public bool Contains(Game item) => items.Contains(item);
                public bool ContainsItem(Guid id) => items.Any(a => a.Id == id);
                public void CopyTo(Game[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
                public void Dispose() { }
                public void EndBufferUpdate() { }
                public Game Get(Guid id) => items.FirstOrDefault(a => a.Id == id);
                public List<Game> Get(IList<Guid> ids) => items.Where(a => ids.Contains(a.Id)).ToList();
                public IEnumerable<Game> GetClone() => items.ToList();
                public IEnumerator<Game> GetEnumerator() => items.GetEnumerator();
                public bool Remove(Game item) => items.Remove(item);
                public bool Remove(Guid id)
                {
                    var item = items.FirstOrDefault(a => a.Id == id);
                    return item != null && items.Remove(item);
                }
                public bool Remove(IEnumerable<Game> items)
                {
                    var removed = false;
                    foreach (var item in items.ToList())
                    {
                        removed |= this.items.Remove(item);
                    }

                    return removed;
                }
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
                public void Update(Game item)
                {
                    updatedGames.Add(item);
                }
                public void Update(IEnumerable<Game> items)
                {
                    foreach (var item in items)
                    {
                        Update(item);
                    }
                }
            }

            private class FakeItemCollection<TItem> : Playnite.SDK.IItemCollection<TItem> where TItem : DatabaseObject, new()
            {
                public Playnite.SDK.GameDatabaseCollection CollectionType => Playnite.SDK.GameDatabaseCollection.Uknown;
                public int Count => 0;
                public bool IsReadOnly => false;
                public TItem this[Guid id] { get => default(TItem); set { } }
                public event EventHandler<Playnite.SDK.ItemCollectionChangedEventArgs<TItem>> ItemCollectionChanged { add { } remove { } }
                public event EventHandler<Playnite.SDK.ItemUpdatedEventArgs<TItem>> ItemUpdated { add { } remove { } }
                public void Add(TItem item) { }
                public TItem Add(string itemName) => new TItem();
                public TItem Add(string itemName, Func<TItem, string, bool> existingComparer) => new TItem();
                public IEnumerable<TItem> Add(List<string> items) => new List<TItem>();
                public TItem Add(MetadataProperty property) => new TItem();
                public IEnumerable<TItem> Add(IEnumerable<MetadataProperty> properties) => new List<TItem>();
                public IEnumerable<TItem> Add(List<string> items, Func<TItem, string, bool> existingComparer) => new List<TItem>();
                public void Add(IEnumerable<TItem> items) { }
                public void BeginBufferUpdate() { }
                public IDisposable BufferedUpdate() => null;
                public void Clear() { }
                public bool Contains(TItem item) => false;
                public bool ContainsItem(Guid id) => false;
                public void CopyTo(TItem[] array, int arrayIndex) { }
                public void Dispose() { }
                public void EndBufferUpdate() { }
                public TItem Get(Guid id) => default(TItem);
                public List<TItem> Get(IList<Guid> ids) => new List<TItem>();
                public IEnumerable<TItem> GetClone() => new List<TItem>();
                public IEnumerator<TItem> GetEnumerator() { yield break; }
                public bool Remove(TItem item) => false;
                public bool Remove(Guid id) => false;
                public bool Remove(IEnumerable<TItem> items) => false;
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
                public void Update(TItem item) { }
                public void Update(IEnumerable<TItem> items) { }
            }
        }
    }
}
