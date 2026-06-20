// 文件用途：验证截图提示不会使用阻塞游戏的弹窗。
using GameScreenshots;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace GameScreenshots.Tests
{
    // 验证保存成功提示走通知中心，不打断游戏窗口。
    [TestFixture]
    public class PlayniteScreenshotMessageServiceTests
    {
        [Test]
        public void Info_message_uses_notification_instead_of_dialog()
        {
            var api = new FakePlayniteApi();
            var service = new PlayniteScreenshotMessageService(api);

            service.ShowInfo("截图已保存：20260619-202421.png");

            Assert.AreEqual(1, api.Notifications.Messages.Count);
            Assert.AreEqual("截图已保存：20260619-202421.png", api.Notifications.Messages[0].Text);
            Assert.AreEqual(0, api.Dialogs.ShowMessageCount);
        }

        [Test]
        public void Background_service_updates_game_background_image()
        {
            var game = new Game("Dave the Diver") { Id = Guid.Parse("c884ec6e-4ae5-4083-af3f-6da1de5aafb5") };
            var database = new FakeDatabaseApi(game);
            var api = new FakePlayniteApi(database);
            var service = new PlayniteGameBackgroundService(api);
            var imagePath = Path.Combine(Path.GetTempPath(), "GameScreenshots-" + Guid.NewGuid().ToString("N") + ".png");

            try
            {
                File.WriteAllBytes(imagePath, new byte[] { 1, 2, 3, 4 });

                service.SetBackground(game, imagePath);

                Assert.AreEqual("db::" + Path.GetFileName(imagePath), database.Games.Get(game.Id).BackgroundImage);
                Assert.AreEqual(imagePath, database.LastAddedFilePath);
                Assert.AreEqual(game.Id, database.LastAddedParentId);
                Assert.AreEqual(1, database.Games.UpdateCount);
            }
            finally
            {
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }
        }

        // 测试用 Playnite API。
        private class FakePlayniteApi : IPlayniteAPI
        {
            public FakeDialogs Dialogs { get; private set; }
            public FakeNotifications Notifications { get; private set; }
            public IGameDatabaseAPI Database { get; private set; }

            public FakePlayniteApi()
            {
                Dialogs = new FakeDialogs();
                Notifications = new FakeNotifications();
            }

            public FakePlayniteApi(IGameDatabaseAPI database)
            {
                Dialogs = new FakeDialogs();
                Notifications = new FakeNotifications();
                Database = database;
            }

            IDialogsFactory IPlayniteAPI.Dialogs { get { return Dialogs; } }
            INotificationsAPI IPlayniteAPI.Notifications { get { return Notifications; } }
            public IMainViewAPI MainView { get { throw new NotImplementedException(); } }
            public IPlaynitePathsAPI Paths { get { throw new NotImplementedException(); } }
            public IPlayniteInfoAPI ApplicationInfo { get { throw new NotImplementedException(); } }
            public IWebViewFactory WebViews { get { throw new NotImplementedException(); } }
            public IResourceProvider Resources { get { throw new NotImplementedException(); } }
            public IUriHandlerAPI UriHandler { get { throw new NotImplementedException(); } }
            public IPlayniteSettingsAPI ApplicationSettings { get { throw new NotImplementedException(); } }
            public IAddons Addons { get { throw new NotImplementedException(); } }
            public IEmulationAPI Emulation { get { throw new NotImplementedException(); } }
            public string ExpandGameVariables(Game game, string inputString) { throw new NotImplementedException(); }
            public string ExpandGameVariables(Game game, string inputString, string emulatorDir) { throw new NotImplementedException(); }
            public GameAction ExpandGameVariables(Game game, GameAction action) { throw new NotImplementedException(); }
            public void StartGame(Guid gameId) { throw new NotImplementedException(); }
            public void InstallGame(Guid gameId) { throw new NotImplementedException(); }
            public void UninstallGame(Guid gameId) { throw new NotImplementedException(); }
            public void AddCustomElementSupport(Plugin source, AddCustomElementSupportArgs args) { throw new NotImplementedException(); }
            public void AddSettingsSupport(Plugin source, AddSettingsSupportArgs args) { throw new NotImplementedException(); }
            public void AddConvertersSupport(Plugin source, AddConvertersSupportArgs args) { throw new NotImplementedException(); }
            public List<GamepadController> GetConnectedControllers() { throw new NotImplementedException(); }
        }

        // 测试用数据库 API。
        private class FakeDatabaseApi : IGameDatabaseAPI
        {
            public FakeGameCollection Games { get; private set; }
            IItemCollection<Game> IGameDatabase.Games { get { return Games; } }
            public string LastAddedFilePath { get; private set; }
            public Guid LastAddedParentId { get; private set; }

            public FakeDatabaseApi(Game game)
            {
                Games = new FakeGameCollection(game);
            }

            public string DatabasePath { get { throw new NotImplementedException(); } }
            public bool IsOpen { get { return true; } }
            public event EventHandler DatabaseOpened;
            public IItemCollection<Platform> Platforms { get { throw new NotImplementedException(); } }
            public IItemCollection<Emulator> Emulators { get { throw new NotImplementedException(); } }
            public IItemCollection<Genre> Genres { get { throw new NotImplementedException(); } }
            public IItemCollection<Company> Companies { get { throw new NotImplementedException(); } }
            public IItemCollection<Tag> Tags { get { throw new NotImplementedException(); } }
            public IItemCollection<Category> Categories { get { throw new NotImplementedException(); } }
            public IItemCollection<Series> Series { get { throw new NotImplementedException(); } }
            public IItemCollection<AgeRating> AgeRatings { get { throw new NotImplementedException(); } }
            public IItemCollection<Region> Regions { get { throw new NotImplementedException(); } }
            public IItemCollection<GameSource> Sources { get { throw new NotImplementedException(); } }
            public IItemCollection<GameFeature> Features { get { throw new NotImplementedException(); } }
            public IItemCollection<GameScannerConfig> GameScanners { get { throw new NotImplementedException(); } }
            public IItemCollection<CompletionStatus> CompletionStatuses { get { throw new NotImplementedException(); } }
            public IItemCollection<Playnite.ImportExclusionItem> ImportExclusions { get { throw new NotImplementedException(); } }
            public IItemCollection<FilterPreset> FilterPresets { get { throw new NotImplementedException(); } }

            public string AddFile(string path, Guid parentId)
            {
                LastAddedFilePath = path;
                LastAddedParentId = parentId;
                return "db::" + System.IO.Path.GetFileName(path);
            }

            public void SaveFile(string id, string path) { throw new NotImplementedException(); }
            public void RemoveFile(string id) { throw new NotImplementedException(); }
            public IDisposable BufferedUpdate() { throw new NotImplementedException(); }
            public void BeginBufferUpdate() { throw new NotImplementedException(); }
            public void EndBufferUpdate() { throw new NotImplementedException(); }
            public string GetFileStoragePath(Guid parentId) { throw new NotImplementedException(); }
            public string GetFullFilePath(string databasePath) { throw new NotImplementedException(); }
            public Game ImportGame(GameMetadata game) { throw new NotImplementedException(); }
            public Game ImportGame(GameMetadata game, LibraryPlugin sourcePlugin) { throw new NotImplementedException(); }
            public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings) { throw new NotImplementedException(); }
            public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings) { throw new NotImplementedException(); }
            public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings, bool useFuzzyNameMatch) { throw new NotImplementedException(); }
            public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings, bool useFuzzyNameMatch) { throw new NotImplementedException(); }
        }

        // 测试用游戏集合。
        private class FakeGameCollection : IItemCollection<Game>
        {
            private readonly Dictionary<Guid, Game> items;
            public int UpdateCount { get; private set; }

            public FakeGameCollection(Game game)
            {
                items = new Dictionary<Guid, Game> { { game.Id, game } };
            }

            public Game this[Guid id] { get { return Get(id); } set { items[id] = value; } }
            public int Count { get { return items.Count; } }
            public bool IsReadOnly { get { return false; } }
            public GameDatabaseCollection CollectionType { get { return GameDatabaseCollection.Games; } }
            public event EventHandler<ItemCollectionChangedEventArgs<Game>> ItemCollectionChanged;
            public event EventHandler<ItemUpdatedEventArgs<Game>> ItemUpdated;

            public Game Get(Guid id)
            {
                Game game;
                items.TryGetValue(id, out game);
                return game;
            }

            public void Update(Game item)
            {
                UpdateCount++;
                items[item.Id] = item;
            }

            public void Add(Game item) { items[item.Id] = item; }
            public void Clear() { items.Clear(); }
            public bool Contains(Game item) { return item != null && items.ContainsKey(item.Id); }
            public void CopyTo(Game[] array, int arrayIndex) { items.Values.CopyTo(array, arrayIndex); }
            public bool Remove(Game item) { return item != null && items.Remove(item.Id); }
            public IEnumerator<Game> GetEnumerator() { return items.Values.GetEnumerator(); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
            public void Dispose() { }
            public bool ContainsItem(Guid id) { return items.ContainsKey(id); }
            public List<Game> Get(IList<Guid> ids) { throw new NotImplementedException(); }
            public Game Add(string itemName) { throw new NotImplementedException(); }
            public Game Add(string itemName, Func<Game, string, bool> existingComparer) { throw new NotImplementedException(); }
            public IEnumerable<Game> Add(List<string> items) { throw new NotImplementedException(); }
            public Game Add(MetadataProperty property) { throw new NotImplementedException(); }
            public IEnumerable<Game> Add(IEnumerable<MetadataProperty> properties) { throw new NotImplementedException(); }
            public IEnumerable<Game> Add(List<string> items, Func<Game, string, bool> existingComparer) { throw new NotImplementedException(); }
            public void Add(IEnumerable<Game> items) { throw new NotImplementedException(); }
            public bool Remove(Guid id) { throw new NotImplementedException(); }
            public bool Remove(IEnumerable<Game> items) { throw new NotImplementedException(); }
            public void Update(IEnumerable<Game> items) { throw new NotImplementedException(); }
            public IDisposable BufferedUpdate() { throw new NotImplementedException(); }
            public void BeginBufferUpdate() { throw new NotImplementedException(); }
            public void EndBufferUpdate() { throw new NotImplementedException(); }
            public IEnumerable<Game> GetClone() { throw new NotImplementedException(); }
        }

        // 测试用通知 API。
        private class FakeNotifications : INotificationsAPI
        {
            public ObservableCollection<NotificationMessage> Messages { get; private set; }
            public int Count { get { return Messages.Count; } }

            public FakeNotifications()
            {
                Messages = new ObservableCollection<NotificationMessage>();
            }

            public void Add(NotificationMessage message)
            {
                Messages.Add(message);
            }

            public void Add(string id, string text, NotificationType type)
            {
                Add(new NotificationMessage(id, text, type));
            }

            public void Remove(string id)
            {
                for (var i = Messages.Count - 1; i >= 0; i--)
                {
                    if (Messages[i].Id == id)
                    {
                        Messages.RemoveAt(i);
                    }
                }
            }

            public void RemoveAll()
            {
                Messages.Clear();
            }
        }

        // 测试用对话框 API。
        private class FakeDialogs : IDialogsFactory
        {
            public int ShowMessageCount { get; private set; }

            public MessageBoxResult ShowErrorMessage(string messageBoxText) { return MessageBoxResult.OK; }
            public MessageBoxResult ShowErrorMessage(string messageBoxText, string caption) { return MessageBoxResult.OK; }
            public MessageBoxResult ShowMessage(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) { ShowMessageCount++; return MessageBoxResult.OK; }
            public MessageBoxResult ShowMessage(string messageBoxText, string caption, MessageBoxButton button) { ShowMessageCount++; return MessageBoxResult.OK; }
            public MessageBoxResult ShowMessage(string messageBoxText, string caption) { ShowMessageCount++; return MessageBoxResult.OK; }
            public MessageBoxResult ShowMessage(string messageBoxText) { ShowMessageCount++; return MessageBoxResult.OK; }
            public MessageBoxOption ShowMessage(string messageBoxText, string caption, MessageBoxImage icon, List<MessageBoxOption> options) { ShowMessageCount++; return null; }
            public string SelectFolder() { throw new NotImplementedException(); }
            public string SelectFolder(string initialDir) { throw new NotImplementedException(); }
            public string SelectFile(string filter) { throw new NotImplementedException(); }
            public string SelectFile(string filter, string initialDir) { throw new NotImplementedException(); }
            public List<string> SelectFiles(string filter) { throw new NotImplementedException(); }
            public List<string> SelectFiles(string filter, string initialDir) { throw new NotImplementedException(); }
            public string SelectIconFile() { throw new NotImplementedException(); }
            public string SelectIconFile(string initialDir) { throw new NotImplementedException(); }
            public string SelectImagefile() { throw new NotImplementedException(); }
            public string SelectImagefile(string initialDir) { throw new NotImplementedException(); }
            public string SaveFile(string filter) { throw new NotImplementedException(); }
            public string SaveFile(string filter, string initialDir) { throw new NotImplementedException(); }
            public string SaveFile(string filter, bool promptOverwrite) { throw new NotImplementedException(); }
            public string SaveFile(string filter, bool promptOverwrite, string initialDir) { throw new NotImplementedException(); }
            public StringSelectionDialogResult SelectString(string messageBoxText, string caption, string defaultInput) { throw new NotImplementedException(); }
            public StringSelectionDialogResult SelectString(string messageBoxText, string caption, string defaultInput, List<MessageBoxToggle> options) { throw new NotImplementedException(); }
            public void ShowSelectableString(string messageBoxText, string caption, string defaultInput) { throw new NotImplementedException(); }
            public ImageFileOption ChooseImageFile(List<ImageFileOption> files, string caption = null, double itemWidth = 240, double itemHeight = 180) { throw new NotImplementedException(); }
            public GenericItemOption ChooseItemWithSearch(List<GenericItemOption> items, Func<string, List<GenericItemOption>> searchFunction, string defaultSearch = null, string caption = null) { throw new NotImplementedException(); }
            public GlobalProgressResult ActivateGlobalProgress(Action<GlobalProgressActionArgs> progresAction, GlobalProgressOptions progressOptions) { throw new NotImplementedException(); }
            public GlobalProgressResult ActivateGlobalProgress(Func<GlobalProgressActionArgs, Task> progresAction, GlobalProgressOptions progressOptions) { throw new NotImplementedException(); }
            public Window CreateWindow(WindowCreationOptions options) { throw new NotImplementedException(); }
            public Window GetCurrentAppWindow() { throw new NotImplementedException(); }
        }
    }
}
