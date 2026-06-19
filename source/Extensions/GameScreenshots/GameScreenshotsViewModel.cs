// 文件用途：为截图画廊和单个游戏截图窗口提供数据与命令。
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace GameScreenshots
{
    // 截图页面视图模型，支持全部截图和单个游戏截图两种范围。
    public class GameScreenshotsViewModel : ObservableObject, IDisposable
    {
        private readonly IScreenshotStore store;
        private readonly IGameScreenshotService screenshotService;
        private readonly IScreenshotMessageService messages;
        private readonly Game game;
        private string title;
        private bool disposed;

        public ObservableCollection<ScreenshotItem> Screenshots { get; private set; }
        public ObservableCollection<ScreenshotGameGroup> ScreenshotGroups { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand CaptureCommand { get; private set; }
        public ICommand OpenScreenshotCommand { get; private set; }

        public string Title
        {
            get { return title; }
            private set { SetValue(ref title, value); }
        }

        public bool IsGameScope
        {
            get { return game != null; }
        }

        public GameScreenshotsViewModel(
            IScreenshotStore store,
            IGameScreenshotService screenshotService,
            IScreenshotMessageService messages,
            Game game)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            this.store = store;
            this.screenshotService = screenshotService;
            this.messages = messages;
            this.game = game;
            Screenshots = new ObservableCollection<ScreenshotItem>();
            ScreenshotGroups = new ObservableCollection<ScreenshotGameGroup>();
            RefreshCommand = new RelayCommand(Refresh);
            CaptureCommand = new RelayCommand(Capture, CanCapture);
            OpenScreenshotCommand = new RelayCommand<ScreenshotItem>(OpenScreenshot);
            Title = game == null ? "截图画廊" : "截图 - " + game.Name;
            if (screenshotService != null)
            {
                screenshotService.ScreenshotCaptured += ScreenshotService_ScreenshotCaptured;
            }

            Refresh();
        }

        // 重新读取截图列表。
        public void Refresh()
        {
            var items = game == null ? store.LoadAllScreenshots() : store.LoadGameScreenshots(game.Id);
            Screenshots.Clear();
            foreach (var item in items)
            {
                Screenshots.Add(item);
            }

            RefreshGroups(items);
        }

        // 按游戏整理画廊截图分组。
        private void RefreshGroups(IEnumerable<ScreenshotItem> items)
        {
            ScreenshotGroups.Clear();
            if (game != null)
            {
                return;
            }

            var groups = items
                .GroupBy(a => a.GameId)
                .Select(a => new
                {
                    GameId = a.Key,
                    GameName = a.First().GameName,
                    Screenshots = a.OrderByDescending(b => b.CapturedAt).ToList(),
                    LastCapturedAt = a.Max(b => b.CapturedAt)
                })
                .OrderByDescending(a => a.LastCapturedAt);

            foreach (var groupData in groups)
            {
                var group = new ScreenshotGameGroup
                {
                    GameId = groupData.GameId,
                    GameName = groupData.GameName
                };

                foreach (var item in groupData.Screenshots)
                {
                    group.Screenshots.Add(item);
                }

                ScreenshotGroups.Add(group);
            }
        }

        // 为当前游戏保存新截图。
        public void Capture()
        {
            if (!CanCapture())
            {
                return;
            }

            try
            {
                var item = screenshotService.CaptureGame(game, DateTime.Now);
                Refresh();
                if (messages != null)
                {
                    messages.ShowInfo("截图已保存：" + item.FileName);
                }
            }
            catch (Exception e)
            {
                if (messages != null)
                {
                    messages.ShowError(e.Message);
                }

                throw;
            }
        }

        // 判断当前页面是否可直接截图。
        private bool CanCapture()
        {
            return game != null && screenshotService != null;
        }

        // 打开截图文件。
        private void OpenScreenshot(ScreenshotItem item)
        {
            if (item == null)
            {
                return;
            }

            ScreenshotFileOpener.Open(item.FilePath);
        }

        // 截图保存后自动刷新当前页面。
        private void ScreenshotService_ScreenshotCaptured(object sender, ScreenshotCapturedEventArgs e)
        {
            if (disposed)
            {
                return;
            }

            if (e == null || e.Item == null)
            {
                return;
            }

            if (game != null && e.Item.GameId != game.Id)
            {
                return;
            }

            Refresh();
        }

        // 释放页面事件订阅，避免旧页面继续刷新。
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (screenshotService != null)
            {
                screenshotService.ScreenshotCaptured -= ScreenshotService_ScreenshotCaptured;
            }

            disposed = true;
        }
    }
}
