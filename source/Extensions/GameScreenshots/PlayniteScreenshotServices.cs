// 文件用途：为截图插件提供 Playnite API 适配器。
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace GameScreenshots
{
    // 从 Playnite 主视图读取当前选中的第一个游戏。
    public class PlayniteGameSelectionProvider : IGameSelectionProvider
    {
        private readonly IPlayniteAPI api;

        public PlayniteGameSelectionProvider(IPlayniteAPI api)
        {
            this.api = api;
        }

        public Game GetSelectedGame()
        {
            if (api == null || api.MainView == null || api.MainView.SelectedGames == null)
            {
                return null;
            }

            return api.MainView.SelectedGames.FirstOrDefault();
        }
    }

    // 使用 Playnite 对话框显示截图消息。
    public class PlayniteScreenshotMessageService : IScreenshotMessageService
    {
        private const string NotificationId = "GameScreenshots.LastMessage";
        private readonly IPlayniteAPI api;

        public PlayniteScreenshotMessageService(IPlayniteAPI api)
        {
            this.api = api;
        }

        public void ShowInfo(string message)
        {
            if (api != null && api.Notifications != null)
            {
                api.Notifications.Remove(NotificationId);
                api.Notifications.Add(NotificationId, message, NotificationType.Info);
            }
        }

        public void ShowError(string message)
        {
            if (api != null && api.Notifications != null)
            {
                api.Notifications.Remove(NotificationId);
                api.Notifications.Add(NotificationId, message, NotificationType.Error);
                return;
            }

            if (api != null && api.Dialogs != null)
            {
                api.Dialogs.ShowErrorMessage(message, "游戏截图");
            }
        }
    }

    // 使用 Playnite 默认窗口打开某个游戏的截图页。
    public class PlayniteScreenshotWindowService : IScreenshotWindowService
    {
        private readonly IPlayniteAPI api;
        private readonly IScreenshotStore store;
        private readonly IGameScreenshotService screenshotService;
        private readonly IScreenshotMessageService messages;

        public PlayniteScreenshotWindowService(
            IPlayniteAPI api,
            IScreenshotStore store,
            IGameScreenshotService screenshotService,
            IScreenshotMessageService messages)
        {
            this.api = api;
            this.store = store;
            this.screenshotService = screenshotService;
            this.messages = messages;
        }

        public void OpenGameScreenshots(Game game)
        {
            if (api == null || api.Dialogs == null || game == null)
            {
                return;
            }

            var viewModel = new GameScreenshotsViewModel(store, screenshotService, messages, game);
            var window = api.Dialogs.CreateWindow(new WindowCreationOptions());
            window.Title = "截图 - " + game.Name;
            window.Content = new GameScreenshotsGameView(viewModel);
            window.Owner = api.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Width = 920;
            window.Height = 640;
            window.Closed += delegate { viewModel.Dispose(); };
            window.ShowDialog();
        }
    }

    // 使用系统默认程序打开截图文件。
    public static class ScreenshotFileOpener
    {
        public static void Open(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            Process.Start(filePath);
        }
    }
}
