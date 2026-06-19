// 文件用途：验证游戏回顾插件入口在桌面和全屏中的可见性。
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace GameActivityReview.Tests
{
    // 验证插件入口按应用模式分流。
    [TestFixture]
    public class GameActivityReviewPluginTests
    {
        // 桌面模式保留主菜单入口。
        [Test]
        public void Plugin_adds_desktop_main_menu_entry()
        {
            var plugin = new GameActivityReviewPlugin(new FakePlayniteApi(ApplicationMode.Desktop));

            var items = plugin.GetMainMenuItems(new GetMainMenuItemsArgs()).ToList();

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("时长", items[0].Description);
            Assert.NotNull(items[0].Action);
        }

        // 桌面模式保留侧边栏入口。
        [Test]
        public void Plugin_adds_desktop_sidebar_entry()
        {
            var plugin = new GameActivityReviewPlugin(new FakePlayniteApi(ApplicationMode.Desktop));

            var items = plugin.GetSidebarItems().ToList();

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("时长", items[0].Title);
        }

        // 桌面模式不能初始化全屏状态，避免反射全屏主模型导致插件加载失败。
        [Test]
        public void Desktop_plugin_does_not_initialize_fullscreen_state()
        {
            var plugin = new GameActivityReviewPlugin(new FakePlayniteApi(ApplicationMode.Desktop));

            var stateField = typeof(GameActivityReviewPlugin).GetField("fullscreenState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.IsNotNull(stateField);
            Assert.IsNull(stateField.GetValue(plugin));
        }

        // 全屏模式不注册弹窗菜单，避免手柄场景下关闭路径不直观。
        [Test]
        public void Plugin_does_not_add_fullscreen_main_menu_dialog_entry()
        {
            var plugin = new GameActivityReviewPlugin(new FakePlayniteApi(ApplicationMode.Fullscreen));

            var items = plugin.GetMainMenuItems(new GetMainMenuItemsArgs()).ToList();

            Assert.AreEqual(0, items.Count);
        }

        private class FakePlayniteApi : IPlayniteAPI
        {
            public IMainViewAPI MainView { get { return null; } }
            public IGameDatabaseAPI Database { get { return null; } }
            public IDialogsFactory Dialogs { get { return null; } }
            public IPlaynitePathsAPI Paths { get; private set; }
            public INotificationsAPI Notifications { get { return null; } }
            public IPlayniteInfoAPI ApplicationInfo { get; private set; }
            public IWebViewFactory WebViews { get { return null; } }
            public IResourceProvider Resources { get { return null; } }
            public IUriHandlerAPI UriHandler { get { return null; } }
            public IPlayniteSettingsAPI ApplicationSettings { get { return null; } }
            public IAddons Addons { get { return null; } }
            public IEmulationAPI Emulation { get { return null; } }

            public FakePlayniteApi(ApplicationMode mode)
            {
                Paths = new FakePlaynitePaths();
                ApplicationInfo = new FakePlayniteInfo(mode);
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

        private class FakePlayniteInfo : IPlayniteInfoAPI
        {
            public Version ApplicationVersion { get { return new Version(10, 0); } }
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
            public string ExtensionsDataPath { get { return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GameActivityReviewTests"); } }
        }
    }
}
