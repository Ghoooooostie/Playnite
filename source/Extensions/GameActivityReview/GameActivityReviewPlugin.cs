using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameActivityReview
{
    // Playnite 插件入口，负责监听游戏事件并提供侧边栏页面。
    public class GameActivityReviewPlugin : GenericPlugin
    {
        private readonly Dictionary<Guid, RunningGameSession> runningSessions = new Dictionary<Guid, RunningGameSession>();
        private readonly GameActivityStore store;

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

        // 注册侧边栏入口。
        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return new SidebarItem
            {
                Type = SiderbarItemType.View,
                Title = "游戏时光回顾",
                Icon = CreateSidebarIcon(),
                Opened = () => new GameActivityReviewView(new GameActivityReviewViewModel(PlayniteApi, store))
            };
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

