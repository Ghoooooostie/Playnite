// 文件用途：验证模拟器启动遮罩插件只覆盖启动过渡，不自行拼接 ROM 参数。
using NUnit.Framework;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace EmulatorQuickLaunchHide.Tests
{
    // 验证插件接入 Playnite 原生启动流程。
    [TestFixture]
    public class EmulatorQuickLaunchHidePluginTests
    {
        [Test]
        public void Plugin_does_not_add_extra_play_actions()
        {
            var plugin = new EmulatorQuickLaunchHidePlugin(
                null,
                new EmulatorQuickLaunchHideSettings(),
                new FakeStartupOverlay(),
                new FakeWindowHandoff());

            var actions = plugin.GetPlayActions(new GetPlayActionsArgs { Game = new Game("Test Game") }).ToList();

            Assert.AreEqual(0, actions.Count);
        }

        [Test]
        public void Plugin_shows_overlay_for_emulator_start()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var plugin = new EmulatorQuickLaunchHidePlugin(null, new EmulatorQuickLaunchHideSettings(), overlay, handoff);
            var args = new OnGameStartingEventArgs();
            SetEventProperty(args, nameof(OnGameStartingEventArgs.SourceAction), new GameAction
            {
                Type = GameActionType.Emulator,
                IsPlayAction = true
            });

            plugin.OnGameStarting(args);

            Assert.AreEqual(1, overlay.ShowCount);
            Assert.AreEqual(0, handoff.HandoffCount);
        }

        [Test]
        public void Plugin_ignores_non_emulator_start()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var plugin = new EmulatorQuickLaunchHidePlugin(null, new EmulatorQuickLaunchHideSettings(), overlay, handoff);
            var args = new OnGameStartingEventArgs();
            SetEventProperty(args, nameof(OnGameStartingEventArgs.SourceAction), new GameAction
            {
                Type = GameActionType.File,
                IsPlayAction = true
            });

            plugin.OnGameStarting(args);

            Assert.AreEqual(0, overlay.ShowCount);
            Assert.AreEqual(0, handoff.HandoffCount);
        }

        [Test]
        public void Plugin_keeps_overlay_for_configured_seconds_then_switches_to_started_process()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var plugin = new EmulatorQuickLaunchHidePlugin(
                null,
                new EmulatorQuickLaunchHideSettings { OverlayHoldSeconds = 4 },
                overlay,
                handoff);

            var startingArgs = new OnGameStartingEventArgs();
            SetEventProperty(startingArgs, nameof(OnGameStartingEventArgs.SourceAction), new GameAction
            {
                Type = GameActionType.Emulator,
                IsPlayAction = true
            });

            var startedArgs = new OnGameStartedEventArgs();
            SetEventProperty(startedArgs, nameof(OnGameStartedEventArgs.StartedProcessId), 1357);

            plugin.OnGameStarting(startingArgs);
            plugin.OnGameStarted(startedArgs);
            handoff.Complete();

            Assert.AreEqual(1, handoff.HandoffCount);
            Assert.AreEqual(1357, handoff.ProcessId);
            Assert.AreEqual(4, handoff.HoldSeconds);
            Assert.AreEqual(1, overlay.CloseCount);
        }

        [Test]
        public void Plugin_closes_overlay_when_startup_is_cancelled()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var plugin = new EmulatorQuickLaunchHidePlugin(null, new EmulatorQuickLaunchHideSettings(), overlay, handoff);
            var startingArgs = new OnGameStartingEventArgs();
            SetEventProperty(startingArgs, nameof(OnGameStartingEventArgs.SourceAction), new GameAction
            {
                Type = GameActionType.Emulator,
                IsPlayAction = true
            });

            plugin.OnGameStarting(startingArgs);
            plugin.OnGameStartupCancelled(new OnGameStartupCancelledEventArgs());

            Assert.AreEqual(1, overlay.CloseCount);
            Assert.AreEqual(0, handoff.HandoffCount);
        }

        [Test]
        public void Settings_normalize_overlay_seconds_to_supported_range()
        {
            var settings = new EmulatorQuickLaunchHideSettings { OverlayHoldSeconds = -2 };
            var viewModel = new EmulatorQuickLaunchHideSettingsViewModel(null, settings);

            viewModel.VerifySettings(out _);
            Assert.AreEqual(0, settings.OverlayHoldSeconds);

            settings.OverlayHoldSeconds = 90;
            viewModel.VerifySettings(out _);
            Assert.AreEqual(60, settings.OverlayHoldSeconds);
        }

        // 设置内部 setter 的事件属性，用来模拟 Playnite 传入的启动事件。
        private static void SetEventProperty<TArgs, TValue>(TArgs args, string propertyName, TValue value)
        {
            var property = typeof(TArgs).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);

            property.SetValue(args, value);
        }

        // 测试用遮罩服务，只记录显示和关闭次数。
        private class FakeStartupOverlay : IStartupOverlay
        {
            public int ShowCount { get; private set; }
            public int CloseCount { get; private set; }

            public void Show()
            {
                ShowCount++;
            }

            public void Close()
            {
                CloseCount++;
            }
        }

        // 测试用窗口交接服务，手动完成异步流程。
        private class FakeWindowHandoff : IWindowHandoff
        {
            private readonly TaskCompletionSource<object> completion = new TaskCompletionSource<object>();
            private CancellationToken token;

            public int HandoffCount { get; private set; }
            public int ProcessId { get; private set; }
            public int HoldSeconds { get; private set; }

            public Task HandoffAsync(int processId, int holdSeconds, CancellationToken cancellationToken)
            {
                HandoffCount++;
                ProcessId = processId;
                HoldSeconds = holdSeconds;
                token = cancellationToken;
                return completion.Task;
            }

            public void Complete()
            {
                completion.SetResult(null);
            }
        }
    }
}
