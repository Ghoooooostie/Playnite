// 文件用途：Playnite 截图插件入口，负责菜单、侧边栏、快捷键和截图流程。
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameScreenshots
{
    // 独立截图插件，不修改 Playnite 核心和默认主题。
    public class GameScreenshotsPlugin : GenericPlugin
    {
        private readonly GameScreenshotsSettingsViewModel settingsViewModel;
        private IScreenshotStore store;
        private IGameScreenshotService screenshotService;
        private readonly IScreenshotHotkeyService hotkeyService;
        private readonly IGameSelectionProvider selectionProvider;
        private readonly IScreenshotMessageService messages;
        private readonly IGameBackgroundService backgrounds;
        private IScreenshotWindowService windows;
        private bool hotkeyRegistered;

        public override Guid Id
        {
            get { return Guid.Parse("5139A212-C04C-419F-A534-71DA19581A63"); }
        }

        public GameScreenshotsPlugin(IPlayniteAPI api) : base(api)
        {
            hotkeyService = new ScreenshotHotkeyService();
            selectionProvider = new PlayniteGameSelectionProvider(api);
            messages = new PlayniteScreenshotMessageService(api);
            backgrounds = new PlayniteGameBackgroundService(api);
            settingsViewModel = new GameScreenshotsSettingsViewModel(this);
            ReloadScreenshotServices();
            Properties = new GenericPluginProperties { HasSettings = true };
            RegisterCustomElementSupport();
        }

        internal GameScreenshotsPlugin(
            IPlayniteAPI api,
            GameScreenshotsSettings settings,
            IGameScreenshotService screenshotService,
            IScreenshotHotkeyService hotkeyService,
            IGameSelectionProvider selectionProvider,
            IScreenshotMessageService messages,
            IScreenshotWindowService windows,
            IGameBackgroundService backgrounds) : base(api)
        {
            this.store = null;
            this.screenshotService = screenshotService;
            this.hotkeyService = hotkeyService;
            this.selectionProvider = selectionProvider;
            this.messages = messages;
            this.windows = windows;
            this.backgrounds = backgrounds;
            settingsViewModel = new GameScreenshotsSettingsViewModel(this, settings);
            Properties = new GenericPluginProperties { HasSettings = true };
            RegisterCustomElementSupport();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new GameScreenshotsSettingsView();
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            ReloadHotkey();
        }

        public override void Dispose()
        {
            UnregisterHotkey();
            base.Dispose();
        }

        // 重新注册快捷键，用于启动和保存设置后生效。
        internal void ReloadHotkey()
        {
            UnregisterHotkey();
            if (!settingsViewModel.Settings.HotkeyEnabled)
            {
                return;
            }

            try
            {
                hotkeyService.Register(settingsViewModel.CreateHotkey(), delegate { CaptureSelectedGame(DateTime.Now); });
                hotkeyRegistered = true;
            }
            catch (Exception e)
            {
                hotkeyRegistered = false;
                messages.ShowError("截图快捷键注册失败：" + e.Message);
            }
        }

        // 根据当前设置重新创建截图存储和截图服务。
        internal void ReloadScreenshotServices()
        {
            var storePath = ScreenshotPathResolver.Resolve(settingsViewModel.Settings.ScreenshotDirectory, GetDefaultScreenshotDirectory());
            store = new ScreenshotStore(storePath);
            screenshotService = new GameScreenshotService(new ScreenshotCaptureService(), store);
            windows = new PlayniteScreenshotWindowService(PlayniteApi, store, screenshotService, messages, backgrounds);
        }

        // 获取插件默认截图目录。
        internal string GetDefaultScreenshotDirectory()
        {
            return Path.Combine(GetPluginUserDataPath(), "screenshots");
        }

        // 打开 Playnite 目录选择器。
        internal string SelectScreenshotDirectory(string initialDirectory)
        {
            if (PlayniteApi == null || PlayniteApi.Dialogs == null)
            {
                return string.Empty;
            }

            return PlayniteApi.Dialogs.SelectFolder(initialDirectory);
        }

        // 仅在本插件确实注册过快捷键后注销。
        private void UnregisterHotkey()
        {
            if (!hotkeyRegistered)
            {
                return;
            }

            hotkeyService.Unregister();
            hotkeyRegistered = false;
        }

        // 注册游戏右键菜单入口。
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (args == null || args.Games == null || args.Games.Count != 1)
            {
                yield break;
            }

            var game = args.Games[0];
            yield return new GameMenuItem
            {
                Description = "保存截图",
                MenuSection = "截图",
                Action = delegate { CaptureGame(game, DateTime.Now); }
            };

            yield return new GameMenuItem
            {
                Description = "查看截图",
                MenuSection = "截图",
                Action = delegate { windows.OpenGameScreenshots(game); }
            };
        }

        // 注册侧边栏画廊入口。
        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            if (store == null)
            {
                yield break;
            }

            yield return new SidebarItem
            {
                Type = SiderbarItemType.View,
                Title = "画廊",
                Icon = CreateSidebarIcon(),
                Opened = delegate
                {
                    return new GameScreenshotsGalleryView(new GameScreenshotsViewModel(store, screenshotService, messages, backgrounds, null));
                }
            };
        }

        // 为全屏首页主题注册截图区域挂载点。
        private void RegisterCustomElementSupport()
        {
            if (PlayniteApi == null)
            {
                return;
            }

            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                SourceName = "GameScreenshots",
                ElementList = new List<string> { "FullscreenHomeScreenshots" }
            });
        }

        // 为全屏首页截图区域提供紧凑缩略图控件。
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args == null || args.Mode != ApplicationMode.Fullscreen || args.Name != "FullscreenHomeScreenshots")
            {
                return null;
            }

            if (store == null)
            {
                return null;
            }

            return new GameScreenshotsHomeView(new GameScreenshotsHomeViewModel(store, screenshotService));
        }

        // 快捷键触发当前选中游戏截图。
        internal ScreenshotItem CaptureSelectedGame(DateTime capturedAt)
        {
            var game = selectionProvider.GetSelectedGame();
            if (game == null)
            {
                messages.ShowInfo("请先在 Playnite 中选择一个游戏。");
                return null;
            }

            return CaptureGame(game, capturedAt);
        }

        // 保存指定游戏截图。
        internal ScreenshotItem CaptureGame(Game game, DateTime capturedAt)
        {
            try
            {
                var item = screenshotService.CaptureGame(game, capturedAt);
                messages.ShowInfo("截图已保存：" + item.FileName);
                return item;
            }
            catch (Exception e)
            {
                messages.ShowError(e.Message);
                throw;
            }
        }

        // 创建侧边栏图标。
        private static TextBlock CreateSidebarIcon()
        {
            var icon = new TextBlock
            {
                Text = char.ConvertFromUtf32(0xeb0d),
                FontSize = 20
            };
            var font = ResourceProvider.GetResource("FontIcoFont") as FontFamily;
            if (font != null)
            {
                icon.FontFamily = font;
            }

            return icon;
        }
    }
}
