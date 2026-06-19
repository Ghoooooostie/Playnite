// 文件用途：验证截图插件入口选择当前游戏、注册热键和提供游戏菜单。
using GameScreenshots;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
namespace GameScreenshots.Tests
{
    // 验证插件行为不依赖真实屏幕截图。
    [TestFixture]
    public class GameScreenshotsPluginTests
    {
        [Test]
        public void Hotkey_captures_currently_selected_game()
        {
            var game = new Game("女神异闻录") { Id = Guid.Parse("55555555-5555-5555-5555-555555555555") };
            var capture = new FakeGameScreenshotService();
            var hotkey = new FakeScreenshotHotkeyService();
            var plugin = CreatePlugin(capture, hotkey, new FakeGameSelectionProvider(game));

            plugin.OnApplicationStarted(new OnApplicationStartedEventArgs());
            hotkey.Trigger();

            Assert.AreEqual(1, capture.CapturedGames.Count);
            Assert.AreEqual(game.Id, capture.CapturedGames[0].Id);
        }

        [Test]
        public void Hotkey_does_not_capture_when_no_game_is_selected()
        {
            var capture = new FakeGameScreenshotService();
            var hotkey = new FakeScreenshotHotkeyService();
            var messages = new FakeScreenshotMessageService();
            var plugin = CreatePlugin(capture, hotkey, new FakeGameSelectionProvider(null), messages);

            plugin.OnApplicationStarted(new OnApplicationStartedEventArgs());
            hotkey.Trigger();

            Assert.AreEqual(0, capture.CapturedGames.Count);
            Assert.AreEqual(1, messages.Messages.Count);
        }

        [Test]
        public void Plugin_adds_game_menu_items_for_capture_and_view()
        {
            var plugin = CreatePlugin();
            var game = new Game("双人成行") { Id = Guid.Parse("66666666-6666-6666-6666-666666666666") };

            var items = plugin.GetGameMenuItems(new GetGameMenuItemsArgs { Games = new List<Game> { game } }).ToList();

            Assert.AreEqual(2, items.Count);
            Assert.AreEqual("保存截图", items[0].Description);
            Assert.AreEqual("查看截图", items[1].Description);
            Assert.IsTrue(items.All(a => a.MenuSection == "截图"));
        }

        [Test]
        public void Plugin_adds_sidebar_gallery_entry()
        {
            var api = new FakePlayniteApi(Path.Combine(Path.GetTempPath(), "GameScreenshotsPluginTests", Guid.NewGuid().ToString()));
            var plugin = new GameScreenshotsPlugin(api);

            var items = plugin.GetSidebarItems().ToList();

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("画廊", items[0].Title);
        }

        [Test]
        public void Plugin_registers_fullscreen_home_screenshots_control()
        {
            var api = new FakePlayniteApi(Path.Combine(Path.GetTempPath(), "GameScreenshotsPluginTests", Guid.NewGuid().ToString()));
            var plugin = new GameScreenshotsPlugin(api);

            Assert.AreEqual("GameScreenshots", api.CustomElementSourceName);
            Assert.AreEqual("FullscreenHomeScreenshots", api.CustomElementName);

            var control = plugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = "FullscreenHomeScreenshots",
                Mode = ApplicationMode.Fullscreen
            });

            Assert.IsNotNull(control);
            Assert.AreEqual("GameScreenshotsHomeView", control.GetType().Name);
        }

        [Test]
        public void Fullscreen_home_control_shows_latest_screenshots_for_selected_game()
        {
            var root = Path.Combine(Path.GetTempPath(), "GameScreenshotsPluginTests", Guid.NewGuid().ToString());
            var api = new FakePlayniteApi(root);
            var plugin = new GameScreenshotsPlugin(api);
            var game = new Game("星之海") { Id = Guid.Parse("77777777-7777-7777-7777-777777777777") };
            var otherGame = new Game("其他游戏") { Id = Guid.Parse("88888888-8888-8888-8888-888888888888") };
            var store = new ScreenshotStore(Path.Combine(root, plugin.Id.ToString(), "screenshots"));

            for (var index = 0; index < 6; index++)
            {
                store.SaveScreenshot(game.Id, game.Name, new byte[] { 1, 2, 3 }, new DateTime(2026, 6, 19, 21, index, 0));
            }

            store.SaveScreenshot(otherGame.Id, otherGame.Name, new byte[] { 1, 2, 3 }, new DateTime(2026, 6, 19, 22, 0, 0));

            var control = plugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = "FullscreenHomeScreenshots",
                Mode = ApplicationMode.Fullscreen
            }) as PluginUserControl;

            Assert.IsNotNull(control);
            control.GameContext = game;
            var screenshots = GetControlScreenshots(control);

            Assert.AreEqual(5, screenshots.Count);
            Assert.AreEqual("20260619-210500.png", ((ScreenshotItem)screenshots[0]).FileName);
            Assert.IsTrue(screenshots.Cast<ScreenshotItem>().All(a => a.GameId == game.Id));
        }

        [Test]
        public void Dispose_unregisters_hotkey()
        {
            var hotkey = new FakeScreenshotHotkeyService();
            var plugin = CreatePlugin(new FakeGameScreenshotService(), hotkey, new FakeGameSelectionProvider(null));

            plugin.OnApplicationStarted(new OnApplicationStartedEventArgs());
            plugin.Dispose();

            Assert.AreEqual(1, hotkey.UnregisterCount);
        }

        // 读取控件数据上下文里的截图集合。
        private static IList GetControlScreenshots(FrameworkElement control)
        {
            var dataContext = control.DataContext;
            Assert.IsNotNull(dataContext);
            var property = dataContext.GetType().GetProperty("Screenshots");
            Assert.IsNotNull(property);
            return (IList)property.GetValue(dataContext, null);
        }

        // 创建带假服务的插件。
        private static GameScreenshotsPlugin CreatePlugin(
            IGameScreenshotService capture = null,
            IScreenshotHotkeyService hotkey = null,
            IGameSelectionProvider selection = null,
            IScreenshotMessageService messages = null)
        {
            return new GameScreenshotsPlugin(
                null,
                new GameScreenshotsSettings(),
                capture ?? new FakeGameScreenshotService(),
                hotkey ?? new FakeScreenshotHotkeyService(),
                selection ?? new FakeGameSelectionProvider(null),
                messages ?? new FakeScreenshotMessageService(),
                new FakeScreenshotWindowService());
        }

        // 测试用 Playnite API，只记录自定义元素注册信息。
        private class FakePlayniteApi : IPlayniteAPI
        {
            public string CustomElementSourceName { get; private set; }
            public string CustomElementName { get; private set; }
            public IPlaynitePathsAPI Paths { get; private set; }
            public IMainViewAPI MainView { get { return null; } }
            public IGameDatabaseAPI Database { get { return null; } }
            public IDialogsFactory Dialogs { get { return null; } }
            public INotificationsAPI Notifications { get { return null; } }
            public IPlayniteInfoAPI ApplicationInfo { get { return null; } }
            public IWebViewFactory WebViews { get { return null; } }
            public IResourceProvider Resources { get { return null; } }
            public IUriHandlerAPI UriHandler { get { return null; } }
            public IPlayniteSettingsAPI ApplicationSettings { get { return null; } }
            public IAddons Addons { get { return null; } }
            public IEmulationAPI Emulation { get { return null; } }

            public FakePlayniteApi(string extensionsDataPath)
            {
                Paths = new FakePlaynitePaths(extensionsDataPath);
            }

            public void AddCustomElementSupport(Plugin source, AddCustomElementSupportArgs args)
            {
                CustomElementSourceName = args.SourceName;
                CustomElementName = args.ElementList.Single();
            }

            public void AddSettingsSupport(Plugin source, AddSettingsSupportArgs args) { }
            public void AddConvertersSupport(Plugin source, AddConvertersSupportArgs args) { }
            public string ExpandGameVariables(Game game, string inputString) { return inputString; }
            public string ExpandGameVariables(Game game, string inputString, string emulatorDir) { return inputString; }
            public Playnite.SDK.Models.GameAction ExpandGameVariables(Game game, Playnite.SDK.Models.GameAction action) { return action; }
            public void StartGame(Guid gameId) { }
            public void InstallGame(Guid gameId) { }
            public void UninstallGame(Guid gameId) { }
            public List<GamepadController> GetConnectedControllers() { return new List<GamepadController>(); }
        }

        // 测试用 Playnite 路径 API。
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

        // 测试用截图服务，只记录捕获目标。
        private class FakeGameScreenshotService : IGameScreenshotService
        {
            public event EventHandler<ScreenshotCapturedEventArgs> ScreenshotCaptured;
            public List<Game> CapturedGames { get; private set; }

            public FakeGameScreenshotService()
            {
                CapturedGames = new List<Game>();
            }

            public ScreenshotItem CaptureGame(Game game, DateTime capturedAt)
            {
                CapturedGames.Add(game);
                var item = new ScreenshotItem
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    FileName = "fake.png",
                    FilePath = "fake.png",
                    CapturedAt = capturedAt
                };

                if (ScreenshotCaptured != null)
                {
                    ScreenshotCaptured(this, new ScreenshotCapturedEventArgs(item));
                }

                return item;
            }
        }

        // 测试用热键服务，手动触发回调。
        private class FakeScreenshotHotkeyService : IScreenshotHotkeyService
        {
            private Action action;
            public int RegisterCount { get; private set; }
            public int UnregisterCount { get; private set; }

            public void Register(ScreenshotHotkey hotkey, Action action)
            {
                RegisterCount++;
                this.action = action;
            }

            public void Unregister()
            {
                UnregisterCount++;
            }

            public void Trigger()
            {
                if (action != null) { action(); }
            }
        }

        // 测试用当前游戏选择器。
        private class FakeGameSelectionProvider : IGameSelectionProvider
        {
            private readonly Game game;

            public FakeGameSelectionProvider(Game game)
            {
                this.game = game;
            }

            public Game GetSelectedGame()
            {
                return game;
            }
        }

        // 测试用消息服务。
        private class FakeScreenshotMessageService : IScreenshotMessageService
        {
            public List<string> Messages { get; private set; }

            public FakeScreenshotMessageService()
            {
                Messages = new List<string>();
            }

            public void ShowInfo(string message)
            {
                Messages.Add(message);
            }

            public void ShowError(string message)
            {
                Messages.Add(message);
            }
        }

        // 测试用窗口服务，只记录调用。
        private class FakeScreenshotWindowService : IScreenshotWindowService
        {
            public int OpenCount { get; private set; }

            public void OpenGameScreenshots(Game game)
            {
                OpenCount++;
            }
        }
    }
}




