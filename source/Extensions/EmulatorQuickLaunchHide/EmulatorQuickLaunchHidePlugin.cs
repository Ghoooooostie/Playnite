// 文件用途：Playnite 插件入口，负责在模拟器启动时显示遮罩并延迟切换到模拟器窗口。
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
    // Playnite 插件入口，保持原生模拟器启动流程，只遮住启动过渡画面。
    public class EmulatorQuickLaunchHidePlugin : GenericPlugin
    {
        private readonly EmulatorQuickLaunchHideSettingsViewModel settingsViewModel;
        private readonly IStartupOverlay startupOverlay;
        private readonly IWindowHandoff windowHandoff;
        private readonly ILogger logger = LogManager.GetLogger();
        private CancellationTokenSource handoffToken;
        private bool overlayActive;

        public override Guid Id { get; } = Guid.Parse("941A6E02-3F88-483D-829A-8B1F7797C681");

        public EmulatorQuickLaunchHidePlugin(IPlayniteAPI api) : base(api)
        {
            settingsViewModel = new EmulatorQuickLaunchHideSettingsViewModel(this);
            startupOverlay = new StartupOverlay(api);
            windowHandoff = new ProcessWindowHandoff();
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        internal EmulatorQuickLaunchHidePlugin(
            IPlayniteAPI api,
            EmulatorQuickLaunchHideSettings settings,
            IStartupOverlay startupOverlay,
            IWindowHandoff windowHandoff) : base(api)
        {
            settingsViewModel = new EmulatorQuickLaunchHideSettingsViewModel(this, settings);
            this.startupOverlay = startupOverlay;
            this.windowHandoff = windowHandoff;
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

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            if (args?.SourceAction?.Type == GameActionType.Emulator)
            {
                BeginOverlay();
            }
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (!overlayActive)
            {
                return;
            }

            StartWindowHandoff(args?.StartedProcessId ?? 0);
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            CloseOverlay();
        }

        public override void OnGameStartupCancelled(OnGameStartupCancelledEventArgs args)
        {
            CloseOverlay();
        }

        public override void Dispose()
        {
            CloseOverlay();
            base.Dispose();
        }

        // 显示遮罩，并取消上一轮未完成的窗口交接。
        private void BeginOverlay()
        {
            CloseOverlay();
            handoffToken = new CancellationTokenSource();
            overlayActive = true;
            startupOverlay.Show();
        }

        // 启动延迟交接，完成后关闭遮罩。
        private async void StartWindowHandoff(int processId)
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
                logger.Error(e, "Failed to hand off focus to emulator window.");
            }
            finally
            {
                if (ReferenceEquals(currentToken, handoffToken))
                {
                    CloseOverlay();
                }
            }
        }

        // 关闭遮罩并释放当前交接任务。
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
    }
}
