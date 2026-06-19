// 文件用途：Playnite 插件入口，负责在模拟器启动时显示遮罩，并在结束后遮住回到 Playnite 的过程。
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EmulatorQuickLaunchHide
{
    // Playnite 插件入口，只覆盖启动/结束过渡画面，并在安全时机清理模拟器进程。
    public class EmulatorQuickLaunchHidePlugin : GenericPlugin
    {
        private readonly EmulatorQuickLaunchHideSettingsViewModel settingsViewModel;
        private readonly IStartupOverlay startupOverlay;
        private readonly IWindowHandoff windowHandoff;
        private readonly IEmulatorProcessCloser emulatorProcessCloser;
        private readonly IPlayniteWindowActivator playniteWindowActivator;
        private readonly IEmulatorExitHotkeyService exitHotkeyService;
        private readonly ILogger logger = LogManager.GetLogger();
        private CancellationTokenSource handoffToken;
        private CancellationTokenSource returnOverlayToken;
        private bool overlayActive;
        private bool returnOverlayActive;
        private bool startupOverlayFinished;
        private int startedEmulatorProcessId;

        public override Guid Id { get; } = Guid.Parse("941A6E02-3F88-483D-829A-8B1F7797C681");

        public EmulatorQuickLaunchHidePlugin(IPlayniteAPI api) : base(api)
        {
            settingsViewModel = new EmulatorQuickLaunchHideSettingsViewModel(this);
            startupOverlay = new StartupOverlay(api);
            windowHandoff = new ProcessWindowHandoff();
            emulatorProcessCloser = new EmulatorProcessCloser();
            playniteWindowActivator = new PlayniteWindowActivator(api);
            exitHotkeyService = new EmulatorExitHotkeyService();
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        internal EmulatorQuickLaunchHidePlugin(
            IPlayniteAPI api,
            EmulatorQuickLaunchHideSettings settings,
            IStartupOverlay startupOverlay,
            IWindowHandoff windowHandoff,
            IEmulatorProcessCloser emulatorProcessCloser,
            IPlayniteWindowActivator playniteWindowActivator,
            IEmulatorExitHotkeyService exitHotkeyService) : base(api)
        {
            settingsViewModel = new EmulatorQuickLaunchHideSettingsViewModel(this, settings);
            this.startupOverlay = startupOverlay;
            this.windowHandoff = windowHandoff;
            this.emulatorProcessCloser = emulatorProcessCloser;
            this.playniteWindowActivator = playniteWindowActivator;
            this.exitHotkeyService = exitHotkeyService;
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new EmulatorQuickLaunchHideSettingsView();
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            ReloadExitHotkey();
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            if (args?.SourceAction?.Type == GameActionType.Emulator)
            {
                ClosePreviousEmulatorProcess();
                BeginOverlay();
            }
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (!overlayActive)
            {
                return;
            }

            startedEmulatorProcessId = args?.StartedProcessId ?? 0;
            startupOverlayFinished = false;
            StartOverlayDelay(startedEmulatorProcessId);
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (!startupOverlayFinished)
            {
                return;
            }

            ReturnToPlayniteWithOverlay();
        }

        public override void OnGameStartupCancelled(OnGameStartupCancelledEventArgs args)
        {
            startedEmulatorProcessId = 0;
            startupOverlayFinished = false;
            CloseOverlay();
            CloseReturnOverlay();
        }

        public override void Dispose()
        {
            exitHotkeyService?.Unregister();
            CloseOverlay();
            CloseReturnOverlay();
            base.Dispose();
        }

        // 重新注册结束快捷键，用于应用启动和保存设置后生效。
        internal void ReloadExitHotkey()
        {
            exitHotkeyService?.Unregister();
            if (!settingsViewModel.Settings.ExitHotkeyEnabled)
            {
                return;
            }

            try
            {
                exitHotkeyService?.Register(settingsViewModel.CreateExitHotkey(), ReturnToPlayniteWithOverlay);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to register emulator exit hotkey.");
                PlayniteApi?.Dialogs?.ShowErrorMessage("模拟器结束快捷键注册失败：" + e.Message, "Emulator Quick Launch Hide");
            }
        }

        // 显示启动遮罩，并取消上一轮未完成的延迟关闭。
        private void BeginOverlay()
        {
            CloseOverlay();
            CloseReturnOverlay();
            handoffToken = new CancellationTokenSource();
            overlayActive = true;
            startupOverlayFinished = false;
            startupOverlay.Show();
        }

        // 显示结束遮罩，遮住关闭模拟器和回 Playnite 的过程。
        private void BeginReturnOverlay()
        {
            CloseOverlay();
            CloseReturnOverlay();
            returnOverlayToken = new CancellationTokenSource();
            returnOverlayActive = true;
            startupOverlay.Show();
        }

        // 关闭上一次记录的模拟器进程。
        private void ClosePreviousEmulatorProcess()
        {
            if (startedEmulatorProcessId <= 0)
            {
                return;
            }

            emulatorProcessCloser.CloseProcess(startedEmulatorProcessId);
            startedEmulatorProcessId = 0;
            startupOverlayFinished = false;
        }

        // 立刻遮住退出过程，清理模拟器并回到 Playnite。
        private void ReturnToPlayniteWithOverlay()
        {
            if (!startupOverlayFinished || startedEmulatorProcessId <= 0)
            {
                return;
            }

            BeginReturnOverlay();
            ClosePreviousEmulatorProcess();
            playniteWindowActivator.Activate();
            CloseReturnOverlayAfterDelay();
        }

        // 等待全局秒数后关闭启动遮罩，不切换、不隐藏模拟器窗口。
        private async void StartOverlayDelay(int processId)
        {
            var currentToken = handoffToken;
            if (currentToken == null)
            {
                return;
            }

            try
            {
                await windowHandoff.HandoffAsync(
                    processId,
                    settingsViewModel.Settings.OverlayHoldSeconds,
                    currentToken.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed while waiting to close emulator startup overlay.");
            }
            finally
            {
                if (ReferenceEquals(currentToken, handoffToken))
                {
                    startupOverlayFinished = true;
                    CloseOverlay();
                }
            }
        }

        // 等待全局秒数后关闭结束遮罩。
        private async void CloseReturnOverlayAfterDelay()
        {
            var currentToken = returnOverlayToken;
            if (currentToken == null)
            {
                return;
            }

            try
            {
                var holdSeconds = settingsViewModel.Settings.OverlayHoldSeconds;
                if (holdSeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(holdSeconds), currentToken.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(currentToken, returnOverlayToken))
                {
                    CloseReturnOverlay();
                }
            }
        }

        // 关闭启动遮罩并释放当前延迟任务。
        private void CloseOverlay()
        {
            var token = handoffToken;
            var shouldCloseOverlay = overlayActive;
            handoffToken = null;
            overlayActive = false;

            if (token != null)
            {
                token.Cancel();
                token.Dispose();
            }

            if (shouldCloseOverlay)
            {
                startupOverlay.Close();
            }
        }

        // 关闭结束遮罩并释放当前延迟任务。
        private void CloseReturnOverlay()
        {
            var token = returnOverlayToken;
            var shouldCloseOverlay = returnOverlayActive;
            returnOverlayToken = null;
            returnOverlayActive = false;

            if (token != null)
            {
                token.Cancel();
                token.Dispose();
            }

            if (shouldCloseOverlay)
            {
                startupOverlay.Close();
            }
        }
    }
}
