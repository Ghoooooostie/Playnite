// 文件用途：验证模拟器启动遮罩插件只覆盖启动过渡，并在安全时机清理上一次模拟器进程。
using NUnit.Framework;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EmulatorQuickLaunchHide.Tests
{
    // 验证插件接入 Playnite 原生启动流程。
    [TestFixture]
    public class EmulatorQuickLaunchHidePluginTests
    {
        [Test]
        public void Plugin_does_not_add_extra_play_actions()
        {
            var plugin = CreatePlugin(new FakeStartupOverlay(), new FakeWindowHandoff(), new FakeEmulatorProcessCloser(), new FakePlayniteWindowActivator());

            var actions = plugin.GetPlayActions(new GetPlayActionsArgs { Game = new Game("Test Game") }).ToList();

            Assert.AreEqual(0, actions.Count);
        }

        [Test]
        public void Plugin_shows_overlay_for_emulator_start()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var closer = new FakeEmulatorProcessCloser();
            var activator = new FakePlayniteWindowActivator();
            var plugin = CreatePlugin(overlay, handoff, closer, activator);
            var args = new OnGameStartingEventArgs();
            SetEventProperty(args, nameof(OnGameStartingEventArgs.SourceAction), new GameAction
            {
                Type = GameActionType.Emulator,
                IsPlayAction = true
            });

            plugin.OnGameStarting(args);

            Assert.AreEqual(1, overlay.ShowCount);
            Assert.AreEqual(0, handoff.HandoffCount);
            Assert.AreEqual(0, closer.CloseCount);
            Assert.AreEqual(0, activator.ActivateCount);
        }

        [Test]
        public void Plugin_ignores_non_emulator_start()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var closer = new FakeEmulatorProcessCloser();
            var activator = new FakePlayniteWindowActivator();
            var plugin = CreatePlugin(overlay, handoff, closer, activator);
            var args = new OnGameStartingEventArgs();
            SetEventProperty(args, nameof(OnGameStartingEventArgs.SourceAction), new GameAction
            {
                Type = GameActionType.File,
                IsPlayAction = true
            });

            plugin.OnGameStarting(args);

            Assert.AreEqual(0, overlay.ShowCount);
            Assert.AreEqual(0, handoff.HandoffCount);
            Assert.AreEqual(0, closer.CloseCount);
            Assert.AreEqual(0, activator.ActivateCount);
        }

        [Test]
        public void Plugin_keeps_overlay_for_configured_seconds_then_closes_it()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var closer = new FakeEmulatorProcessCloser();
            var activator = new FakePlayniteWindowActivator();
            var plugin = CreatePlugin(
                overlay,
                handoff,
                closer,
                activator,
                new EmulatorQuickLaunchHideSettings { OverlayHoldSeconds = 4 });
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
            Assert.AreEqual(0, closer.CloseCount);
            Assert.AreEqual(0, activator.ActivateCount);
        }

        [Test]
        public void Plugin_closes_overlay_when_startup_is_cancelled()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var closer = new FakeEmulatorProcessCloser();
            var activator = new FakePlayniteWindowActivator();
            var plugin = CreatePlugin(overlay, handoff, closer, activator);
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
            Assert.AreEqual(0, closer.CloseCount);
            Assert.AreEqual(0, activator.ActivateCount);
        }

        [Test]
        public void Plugin_does_not_close_emulator_when_playnite_reports_stop_during_startup_overlay()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var closer = new FakeEmulatorProcessCloser();
            var activator = new FakePlayniteWindowActivator();
            var plugin = CreatePlugin(overlay, handoff, closer, activator);
            var startingArgs = new OnGameStartingEventArgs();
            SetEventProperty(startingArgs, nameof(OnGameStartingEventArgs.SourceAction), new GameAction
            {
                Type = GameActionType.Emulator,
                IsPlayAction = true
            });
            var startedArgs = new OnGameStartedEventArgs();
            SetEventProperty(startedArgs, nameof(OnGameStartedEventArgs.StartedProcessId), 2468);

            plugin.OnGameStarting(startingArgs);
            plugin.OnGameStarted(startedArgs);
            plugin.OnGameStopped(new OnGameStoppedEventArgs());

            Assert.AreEqual(0, closer.CloseCount);
            Assert.AreEqual(0, activator.ActivateCount);
        }

        [Test]
        public void Plugin_shows_return_overlay_closes_emulator_and_activates_playnite_after_stop()
        {
            var calls = new List<string>();
            var overlay = new FakeStartupOverlay(calls);
            var handoff = new FakeWindowHandoff();
            var closer = new FakeEmulatorProcessCloser(calls);
            var activator = new FakePlayniteWindowActivator(calls);
            var plugin = CreatePlugin(
                overlay,
                handoff,
                closer,
                activator,
                new EmulatorQuickLaunchHideSettings { OverlayHoldSeconds = 0 });
            var startingArgs = CreateEmulatorStartingArgs();
            var startedArgs = new OnGameStartedEventArgs();
            SetEventProperty(startedArgs, nameof(OnGameStartedEventArgs.StartedProcessId), 2468);

            plugin.OnGameStarting(startingArgs);
            plugin.OnGameStarted(startedArgs);
            handoff.Complete();
            calls.Clear();
            plugin.OnGameStopped(new OnGameStoppedEventArgs());

            CollectionAssert.AreEqual(
                new[] { "overlay.show", "emulator.close", "playnite.activate", "overlay.close" },
                calls);
            Assert.AreEqual(2468, closer.ProcessId);
        }

        [Test]
        public void Plugin_closes_previous_emulator_before_next_emulator_start()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var closer = new FakeEmulatorProcessCloser();
            var activator = new FakePlayniteWindowActivator();
            var plugin = CreatePlugin(overlay, handoff, closer, activator);
            var firstStartingArgs = CreateEmulatorStartingArgs();
            var firstStartedArgs = new OnGameStartedEventArgs();
            SetEventProperty(firstStartedArgs, nameof(OnGameStartedEventArgs.StartedProcessId), 2468);

            plugin.OnGameStarting(firstStartingArgs);
            plugin.OnGameStarted(firstStartedArgs);
            handoff.Complete();
            plugin.OnGameStarting(CreateEmulatorStartingArgs());

            Assert.AreEqual(1, closer.CloseCount);
            Assert.AreEqual(2468, closer.ProcessId);
            Assert.AreEqual(0, activator.ActivateCount);
        }

        [Test]
        public void Plugin_registers_exit_hotkey_from_settings_when_application_started()
        {
            var hotkey = new FakeEmulatorExitHotkeyService();
            var plugin = CreatePlugin(
                new FakeStartupOverlay(),
                new FakeWindowHandoff(),
                new FakeEmulatorProcessCloser(),
                new FakePlayniteWindowActivator(),
                new EmulatorQuickLaunchHideSettings { ExitHotkeyKey = "F5", ExitHotkeyModifiers = "" },
                hotkey);

            plugin.OnApplicationStarted(new OnApplicationStartedEventArgs());

            Assert.AreEqual(1, hotkey.RegisterCount);
            Assert.AreEqual(Key.F5, hotkey.RegisteredHotkey.Key);
            Assert.AreEqual(ModifierKeys.None, hotkey.RegisteredHotkey.Modifiers);
        }

        [Test]
        public void Hotkey_service_uses_message_only_window_so_sink_is_not_visible()
        {
            var parameters = EmulatorExitHotkeyService.CreateSinkParameters();

            Assert.AreEqual(new IntPtr(-3), parameters.ParentWindow);
            Assert.AreEqual(0, parameters.WindowStyle & 0x10000000);
            Assert.AreEqual(0, parameters.ExtendedWindowStyle);
            Assert.AreEqual(0, parameters.Width);
            Assert.AreEqual(0, parameters.Height);
        }

        [Test]
        public void Plugin_exit_hotkey_immediately_shows_return_overlay_closes_emulator_and_activates_playnite()
        {
            var calls = new List<string>();
            var overlay = new FakeStartupOverlay(calls);
            var handoff = new FakeWindowHandoff();
            var closer = new FakeEmulatorProcessCloser(calls);
            var activator = new FakePlayniteWindowActivator(calls);
            var hotkey = new FakeEmulatorExitHotkeyService();
            var plugin = CreatePlugin(
                overlay,
                handoff,
                closer,
                activator,
                new EmulatorQuickLaunchHideSettings { OverlayHoldSeconds = 0 },
                hotkey);
            var startedArgs = new OnGameStartedEventArgs();
            SetEventProperty(startedArgs, nameof(OnGameStartedEventArgs.StartedProcessId), 2468);

            plugin.OnApplicationStarted(new OnApplicationStartedEventArgs());
            plugin.OnGameStarting(CreateEmulatorStartingArgs());
            plugin.OnGameStarted(startedArgs);
            handoff.Complete();
            calls.Clear();
            hotkey.Trigger();

            CollectionAssert.AreEqual(
                new[] { "overlay.show", "emulator.close", "playnite.activate", "overlay.close" },
                calls);
            Assert.AreEqual(2468, closer.ProcessId);
        }

        [Test]
        public void Plugin_exit_hotkey_ignores_startup_phase_before_overlay_finishes()
        {
            var overlay = new FakeStartupOverlay();
            var handoff = new FakeWindowHandoff();
            var closer = new FakeEmulatorProcessCloser();
            var activator = new FakePlayniteWindowActivator();
            var hotkey = new FakeEmulatorExitHotkeyService();
            var plugin = CreatePlugin(overlay, handoff, closer, activator, null, hotkey);
            var startedArgs = new OnGameStartedEventArgs();
            SetEventProperty(startedArgs, nameof(OnGameStartedEventArgs.StartedProcessId), 2468);

            plugin.OnApplicationStarted(new OnApplicationStartedEventArgs());
            plugin.OnGameStarting(CreateEmulatorStartingArgs());
            plugin.OnGameStarted(startedArgs);
            hotkey.Trigger();

            Assert.AreEqual(0, closer.CloseCount);
            Assert.AreEqual(0, activator.ActivateCount);
        }

        [Test]
        public async Task Window_handoff_only_waits_and_does_not_require_process_window()
        {
            var handoff = new ProcessWindowHandoff();

            await handoff.HandoffAsync(0, 0, CancellationToken.None);

            Assert.Pass();
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

        [Test]
        public void Settings_normalize_exit_hotkey_to_supported_values()
        {
            var settings = new EmulatorQuickLaunchHideSettings { ExitHotkeyKey = "", ExitHotkeyModifiers = "" };
            var viewModel = new EmulatorQuickLaunchHideSettingsViewModel(null, settings);

            viewModel.VerifySettings(out _);

            Assert.AreEqual("F5", settings.ExitHotkeyKey);
            Assert.AreEqual("", settings.ExitHotkeyModifiers);
            Assert.AreEqual(Key.F5, viewModel.CreateExitHotkey().Key);
            Assert.AreEqual(ModifierKeys.None, viewModel.CreateExitHotkey().Modifiers);
        }

        [Test]
        public void Settings_updates_exit_hotkey_from_user_recorded_hotkey()
        {
            var settings = new EmulatorQuickLaunchHideSettings();
            var viewModel = new EmulatorQuickLaunchHideSettingsViewModel(null, settings);

            viewModel.ExitHotkey = new EmulatorExitHotkey(Key.F9, ModifierKeys.Control | ModifierKeys.Alt);

            Assert.AreEqual("F9", settings.ExitHotkeyKey);
            Assert.AreEqual("Control, Alt", settings.ExitHotkeyModifiers);
            Assert.AreEqual(Key.F9, viewModel.CreateExitHotkey().Key);
            Assert.AreEqual(ModifierKeys.Control | ModifierKeys.Alt, viewModel.CreateExitHotkey().Modifiers);
        }

        [Test]
        public void Settings_accepts_single_key_f5_without_modifiers()
        {
            var settings = new EmulatorQuickLaunchHideSettings { ExitHotkeyKey = "F5", ExitHotkeyModifiers = "" };
            var viewModel = new EmulatorQuickLaunchHideSettingsViewModel(null, settings);

            viewModel.VerifySettings(out _);

            Assert.AreEqual("F5", settings.ExitHotkeyKey);
            Assert.AreEqual("", settings.ExitHotkeyModifiers);
            Assert.AreEqual(Key.F5, viewModel.CreateExitHotkey().Key);
            Assert.AreEqual(ModifierKeys.None, viewModel.CreateExitHotkey().Modifiers);
        }

        // 创建模拟器启动事件。
        private static OnGameStartingEventArgs CreateEmulatorStartingArgs()
        {
            var args = new OnGameStartingEventArgs();
            SetEventProperty(args, nameof(OnGameStartingEventArgs.SourceAction), new GameAction
            {
                Type = GameActionType.Emulator,
                IsPlayAction = true
            });

            return args;
        }

        // 创建测试插件实例。
        private static EmulatorQuickLaunchHidePlugin CreatePlugin(
            IStartupOverlay overlay,
            IWindowHandoff handoff,
            IEmulatorProcessCloser closer,
            IPlayniteWindowActivator activator,
            EmulatorQuickLaunchHideSettings settings = null,
            IEmulatorExitHotkeyService hotkey = null)
        {
            return new EmulatorQuickLaunchHidePlugin(
                null,
                settings ?? new EmulatorQuickLaunchHideSettings(),
                overlay,
                handoff,
                closer,
                activator,
                hotkey ?? new FakeEmulatorExitHotkeyService());
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
            private readonly IList<string> calls;

            public FakeStartupOverlay(IList<string> calls = null)
            {
                this.calls = calls;
            }

            public int ShowCount { get; private set; }
            public int CloseCount { get; private set; }

            public void Show()
            {
                ShowCount++;
                calls?.Add("overlay.show");
            }

            public void Close()
            {
                CloseCount++;
                calls?.Add("overlay.close");
            }
        }

        // 测试用延迟服务，手动完成异步流程。
        private class FakeWindowHandoff : IWindowHandoff
        {
            private readonly TaskCompletionSource<object> completion = new TaskCompletionSource<object>();

            public int HandoffCount { get; private set; }
            public int ProcessId { get; private set; }
            public int HoldSeconds { get; private set; }

            public Task HandoffAsync(int processId, int holdSeconds, CancellationToken cancellationToken)
            {
                HandoffCount++;
                ProcessId = processId;
                HoldSeconds = holdSeconds;
                return completion.Task;
            }

            public void Complete()
            {
                completion.SetResult(null);
            }
        }

        // 测试用模拟器进程关闭服务，只记录关闭目标。
        private class FakeEmulatorProcessCloser : IEmulatorProcessCloser
        {
            private readonly IList<string> calls;

            public FakeEmulatorProcessCloser(IList<string> calls = null)
            {
                this.calls = calls;
            }

            public int CloseCount { get; private set; }
            public int ProcessId { get; private set; }

            public void CloseProcess(int processId)
            {
                CloseCount++;
                ProcessId = processId;
                calls?.Add("emulator.close");
            }
        }

        // 测试用结束快捷键服务，手动触发热键回调。
        private class FakeEmulatorExitHotkeyService : IEmulatorExitHotkeyService
        {
            private Action action;

            public int RegisterCount { get; private set; }
            public int UnregisterCount { get; private set; }
            public EmulatorExitHotkey RegisteredHotkey { get; private set; }

            public void Register(EmulatorExitHotkey hotkey, Action action)
            {
                RegisterCount++;
                RegisteredHotkey = hotkey;
                this.action = action;
            }

            public void Unregister()
            {
                UnregisterCount++;
                action = null;
            }

            public void Trigger()
            {
                action?.Invoke();
            }
        }

        // 测试用 Playnite 激活服务，只记录调用次数。
        private class FakePlayniteWindowActivator : IPlayniteWindowActivator
        {
            private readonly IList<string> calls;

            public FakePlayniteWindowActivator(IList<string> calls = null)
            {
                this.calls = calls;
            }

            public int ActivateCount { get; private set; }

            public void Activate()
            {
                ActivateCount++;
                calls?.Add("playnite.activate");
            }
        }
    }
}
