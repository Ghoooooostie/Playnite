// 文件用途：Playnite 游戏时长插件入口，负责记录游玩会话并提供桌面侧边栏和全屏首页控件。
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameActivityReview
{
    // Playnite 插件入口，负责监听游戏事件并提供时长页面。
    public class GameActivityReviewPlugin : GenericPlugin
    {
        private readonly Dictionary<Guid, RunningGameSession> runningSessions = new Dictionary<Guid, RunningGameSession>();
        private readonly GameActivityStore store;
        private GameActivityReviewFullscreenState fullscreenState;

        public override Guid Id
        {
            get { return Guid.Parse("7E2B780F-51D2-4BC5-9D80-91DDAA64DF88"); }
        }

        public GameActivityReviewPlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties
            {
                HasSettings = false
            };
            store = new GameActivityStore(GetPluginUserDataPath());
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                SourceName = "GameActivityReview",
                ElementList = new List<string> { "FullscreenHomeReview", "FullscreenReviewPanel" }
            });
        }

        // 游戏开始后记录开始时间。
        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            runningSessions[args.Game.Id] = new RunningGameSession
            {
                GameId = args.Game.Id,
                GameName = args.Game.Name,
                StartedAt = DateTime.Now
            };
        }

        // 游戏退出后保存本次游玩记录。
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            var endedAt = DateTime.Now;
            var duration = args.ElapsedSeconds;
            RunningGameSession running;
            if (duration == 0 && runningSessions.TryGetValue(args.Game.Id, out running))
            {
                duration = (ulong)Math.Max(0, Math.Floor((endedAt - running.StartedAt).TotalSeconds));
            }

            if (duration == 0)
            {
                runningSessions.Remove(args.Game.Id);
                return;
            }

            RunningGameSession session;
            var startedAt = runningSessions.TryGetValue(args.Game.Id, out session)
                ? session.StartedAt
                : endedAt.AddSeconds(-(double)duration);

            store.AddSession(GameSessionRecordFactory.Create(args.Game.Id, args.Game.Name, startedAt, endedAt, duration));
            runningSessions.Remove(args.Game.Id);
        }

        // Playnite 关闭时，把仍在运行的游戏按当前时间收口。
        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            if (fullscreenState != null)
            {
                fullscreenState.Dispose();
            }

            foreach (var session in runningSessions.Values)
            {
                var endedAt = DateTime.Now;
                var duration = (ulong)Math.Max(0, Math.Floor((endedAt - session.StartedAt).TotalSeconds));
                if (duration == 0)
                {
                    continue;
                }

                store.AddSession(GameSessionRecordFactory.Create(session.GameId, session.GameName, session.StartedAt, endedAt, duration));
            }

            runningSessions.Clear();
        }

        // 注册桌面侧边栏入口。
        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return new SidebarItem
            {
                Type = SiderbarItemType.View,
                Title = "时长",
                Icon = CreateSidebarIcon(),
                Opened = () => CreateReviewView()
            };
        }

        // 桌面模式保留入口，全屏模式改用顶部栏目避免弹窗。
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            if (PlayniteApi.ApplicationInfo != null && PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                yield break;
            }

            yield return new MainMenuItem
            {
                Description = "时长",
                MenuSection = "@",
                Action = delegate { OpenDesktopReviewWindow(); }
            };
        }

        // 打开桌面时长窗口。
        private void OpenDesktopReviewWindow()
        {
            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions());
            window.Title = "时长";
            window.Content = CreateReviewView();
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Width = 1180;
            window.Height = 760;
            window.ShowDialog();
        }

        // 创建时长页面。
        private GameActivityReviewView CreateReviewView()
        {
            return new GameActivityReviewView(new GameActivityReviewViewModel(PlayniteApi, store));
        }

        // 为全屏顶部栏目和主内容区提供控件。
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args == null || args.Mode != ApplicationMode.Fullscreen)
            {
                return null;
            }

            if (args.Name == "FullscreenHomeReview")
            {
                return new GameActivityReviewHomeView(new GameActivityReviewViewModel(PlayniteApi, store), GetFullscreenState());
            }

            if (args.Name == "FullscreenReviewPanel")
            {
                return new GameActivityReviewFullscreenPanelView(new GameActivityReviewViewModel(PlayniteApi, store), GetFullscreenState());
            }

            return null;
        }

        // 获取全屏共享状态。
        private GameActivityReviewFullscreenState GetFullscreenState()
        {
            if (fullscreenState == null)
            {
                fullscreenState = new GameActivityReviewFullscreenState();
            }

            return fullscreenState;
        }

        // 创建侧边栏图标。
        private static TextBlock CreateSidebarIcon()
        {
            var icon = new TextBlock
            {
                Text = char.ConvertFromUtf32(0xe983),
                FontSize = 20
            };
            var font = ResourceProvider.GetResource("FontIcoFont") as FontFamily;
            if (font != null)
            {
                icon.FontFamily = font;
            }

            return icon;
        }

        private class RunningGameSession
        {
            public Guid GameId { get; set; }
            public string GameName { get; set; }
            public DateTime StartedAt { get; set; }
        }
    }
}