// 文件用途：为全屏首页截图区域提供当前游戏的紧凑截图列表。
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GameScreenshots
{
    // 只读取当前选中游戏最近几张截图。
    public class GameScreenshotsHomeViewModel : ObservableObject, IDisposable
    {
        private const int MaxHomeScreenshots = 5;
        private readonly IScreenshotStore store;
        private readonly IGameScreenshotService screenshotService;
        private Game game;
        private string title;
        private bool disposed;

        public ObservableCollection<ScreenshotItem> Screenshots { get; private set; }

        public string Title
        {
            get { return title; }
            private set { SetValue(ref title, value); }
        }

        public bool HasScreenshots
        {
            get { return Screenshots.Count > 0; }
        }

        public bool HasNoScreenshots
        {
            get { return Screenshots.Count == 0; }
        }

        public GameScreenshotsHomeViewModel(IScreenshotStore store, IGameScreenshotService screenshotService)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            this.store = store;
            this.screenshotService = screenshotService;
            Screenshots = new ObservableCollection<ScreenshotItem>();
            Title = "截图";
            if (screenshotService != null)
            {
                screenshotService.ScreenshotCaptured += ScreenshotService_ScreenshotCaptured;
            }
        }

        // 切换全屏首页当前选中的游戏。
        public void SetGame(Game selectedGame)
        {
            game = selectedGame;
            Title = game == null ? "截图" : "截图 - " + game.Name;
            Refresh();
        }

        // 重新读取当前游戏最近截图。
        public void Refresh()
        {
            Screenshots.Clear();
            if (game != null)
            {
                foreach (var item in store.LoadGameScreenshots(game.Id).Take(MaxHomeScreenshots))
                {
                    Screenshots.Add(item);
                }
            }

            OnPropertyChanged(nameof(HasScreenshots));
            OnPropertyChanged(nameof(HasNoScreenshots));
        }

        // 新截图保存后刷新对应游戏的紧凑列表。
        private void ScreenshotService_ScreenshotCaptured(object sender, ScreenshotCapturedEventArgs e)
        {
            if (disposed || game == null || e == null || e.Item == null || e.Item.GameId != game.Id)
            {
                return;
            }

            Refresh();
        }

        // 释放事件订阅，避免旧首页控件继续刷新。
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
