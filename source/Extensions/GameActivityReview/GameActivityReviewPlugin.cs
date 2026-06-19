// 文件用途：Playnite 游戏回顾插件入口，负责记录游玩会话并提供桌面侧边栏和全屏首页控件。
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameActivityReview
{
    // Playnite 插件入口，负责监听游戏事件并提供回顾页面。
    public class GameActivityReviewPlugin : GenericPlugin
    {
        private readonly Dictionary<Guid, RunningGameSession> runningSessions = new Dictionary<Guid, RunningGameSession>();
        private readonly GameActivityStore store;
        private readonly GameActivityReviewFullscreenState fullscreenState = new GameActivityReviewFullscreenState();

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
            fullscreenState.Dispose();
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
                Title = "游戏时光回顾",
                Icon = CreateSidebarIcon(),
                Opened = () => CreateReviewView()
            };
        }

        // 全屏模式不注册弹窗菜单，避免手柄场景下关闭路径不直观。
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield break;
        }

        // 创建回顾页面。
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
                return new GameActivityReviewHomeView(new GameActivityReviewViewModel(PlayniteApi, store), fullscreenState);
            }

            if (args.Name == "FullscreenReviewPanel")
            {
                return new GameActivityReviewFullscreenPanelView(new GameActivityReviewViewModel(PlayniteApi, store), fullscreenState);
            }

            return null;
        }

        // 创建侧边栏图标。
        private static TextBlock CreateSidebarIcon()
        {
            return new TextBlock
            {
                Text = char.ConvertFromUtf32(0xe983),
                FontSize = 20,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            };
        }

        private class RunningGameSession
        {
            public Guid GameId { get; set; }
            public string GameName { get; set; }
            public DateTime StartedAt { get; set; }
        }
    }
}

