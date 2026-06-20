// 文件用途：Switch 智能导入插件入口，负责主菜单入口和设置页。
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using Playnite.SDK.Events;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SwitchSmartImport
{
    // 插件主入口，第一版只提供主菜单和设置页骨架。
    public class SwitchSmartImportPlugin : GenericPlugin
    {
        private readonly ISwitchPendingImportWindowService pendingImportWindowService;
        private readonly SwitchSmartImportSettingsViewModel settingsViewModel;
        private readonly ISwitchPendingImportStore pendingStore;
        private readonly ISwitchImportScanner scanner;
        private readonly SwitchScheduledScanService scheduledScanService;
        private readonly ISwitchImportExecutor importExecutor;
        private readonly ISwitchMetadataRefreshService metadataRefreshService;
        private readonly ISwitchMessageService messageService;
        private readonly ISwitchProgressService progressService;

        public override Guid Id => Guid.Parse("5E230E44-29A3-4A76-AF78-C71A6B6C5D54");

        public SwitchSmartImportPlugin(IPlayniteAPI api) : base(api)
        {
            settingsViewModel = new SwitchSmartImportSettingsViewModel(this);
            pendingStore = new SwitchPendingImportStore(GetPluginUserDataPath());
            scanner = new SwitchImportScanner(settingsViewModel.Settings);
            scheduledScanService = new SwitchScheduledScanService(scanner, pendingStore, settingsViewModel.Settings.ScheduledScanMinutes);
            importExecutor = PlayniteApi?.Database == null ? null : new SwitchImportExecutor(PlayniteApi.Database);
            metadataRefreshService = PlayniteApi == null ? null : new SwitchMetadataRefreshService(PlayniteApi);
            messageService = PlayniteApi == null
                ? (ISwitchMessageService)new NullSwitchMessageService()
                : new PlayniteSwitchMessageService(PlayniteApi);
            progressService = PlayniteApi == null
                ? (ISwitchProgressService)new InlineSwitchProgressService()
                : new PlayniteSwitchProgressService(PlayniteApi);
            pendingImportWindowService = new PlayniteSwitchPendingImportWindowService(api);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        internal SwitchSmartImportPlugin(
            IPlayniteAPI api,
            SwitchSmartImportSettings settings,
            ISwitchImportScanner scanner,
            ISwitchPendingImportStore pendingStore,
            SwitchScheduledScanService scheduledScanService = null,
            ISwitchImportExecutor importExecutor = null,
            ISwitchMetadataRefreshService metadataRefreshService = null,
            ISwitchMessageService messageService = null,
            ISwitchProgressService progressService = null,
            ISwitchPendingImportWindowService pendingImportWindowService = null) : base(api)
        {
            settingsViewModel = new SwitchSmartImportSettingsViewModel(this, settings);
            this.pendingStore = pendingStore;
            this.scanner = scanner;
            this.scheduledScanService = scheduledScanService ?? new SwitchScheduledScanService(scanner, pendingStore, settings.ScheduledScanMinutes);
            this.importExecutor = importExecutor;
            this.metadataRefreshService = metadataRefreshService;
            this.messageService = messageService ?? new NullSwitchMessageService();
            this.progressService = progressService ?? new InlineSwitchProgressService();
            this.pendingImportWindowService = pendingImportWindowService;
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
            settingsViewModel.RefreshChoices();
            return new SwitchSmartImportSettingsView(settingsViewModel);
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            if (settingsViewModel.Settings.ScanOnStartup)
            {
                RunScan();
            }

            if (settingsViewModel.Settings.EnableScheduledScan)
            {
                scheduledScanService.Start();
            }
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                Description = "Switch 智能导入",
                MenuSection = "@",
                Action = delegate { OpenPendingImportWindow(); }
            };

            yield return new MainMenuItem
            {
                Description = "立即扫描 Switch 智能导入",
                MenuSection = "@",
                Action = delegate { RunScan(); }
            };

            yield return new MainMenuItem
            {
                Description = "Switch 智能导入设置",
                MenuSection = "@",
                Action = delegate { OpenSettingsView(); }
            };
        }

        internal void RunScan()
        {
            var result = scanner.Scan();
            pendingStore.Save(result.Candidates, DateTime.Now, result.SkippedItems);

            if (PlayniteApi?.Notifications != null)
            {
                PlayniteApi.Notifications.Add("switch-smart-import-scan", "Switch 智能导入扫描完成。", NotificationType.Info);
            }
        }

        internal void OpenPendingImportWindow()
        {
            pendingImportWindowService?.Show(CreatePendingImportViewModel());
        }

        // 打开目录选择对话框。
        internal string SelectFolder(string initialDirectory)
        {
            if (PlayniteApi?.Dialogs == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(initialDirectory))
            {
                return PlayniteApi.Dialogs.SelectFolder();
            }

            return PlayniteApi.Dialogs.SelectFolder(initialDirectory);
        }

        private SwitchPendingImportViewModel CreatePendingImportViewModel()
        {
            if (importExecutor == null || metadataRefreshService == null)
            {
                return new SwitchPendingImportViewModel(
                    settingsViewModel.Settings,
                    pendingStore,
                    new NullSwitchImportExecutor(),
                    new NullSwitchMetadataRefreshService(),
                    messageService,
                    progressService);
            }

            return new SwitchPendingImportViewModel(
                settingsViewModel.Settings,
                pendingStore,
                importExecutor,
                metadataRefreshService,
                messageService,
                progressService);
        }

        private class NullSwitchImportExecutor : ISwitchImportExecutor
        {
            public List<Playnite.SDK.Models.Game> Import(IEnumerable<SwitchImportCandidate> candidates, SwitchSmartImportSettings settings)
            {
                return new List<Playnite.SDK.Models.Game>();
            }
        }

        private class NullSwitchMetadataRefreshService : ISwitchMetadataRefreshService
        {
            public void Refresh(IEnumerable<Playnite.SDK.Models.Game> games, SwitchMetadataSource source)
            {
            }
        }
    }

    // 待确认窗口服务接口。
    public interface ISwitchPendingImportWindowService
    {
        void Show(SwitchPendingImportViewModel viewModel);
    }

    // 基于 Playnite 对话框的待确认窗口服务。
    public class PlayniteSwitchPendingImportWindowService : ISwitchPendingImportWindowService
    {
        private readonly IPlayniteAPI api;

        public PlayniteSwitchPendingImportWindowService(IPlayniteAPI api)
        {
            this.api = api;
        }

        public void Show(SwitchPendingImportViewModel viewModel)
        {
            if (api?.Dialogs == null)
            {
                return;
            }

            var window = api.Dialogs.CreateWindow(new WindowCreationOptions());
            window.Title = "Switch 智能导入";
            window.Content = new SwitchPendingImportView(viewModel);
            window.Owner = api.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Width = 640;
            window.Height = 420;
            window.ShowDialog();
        }
    }

    // 基于 Playnite 对话框的错误提示服务。
    public class PlayniteSwitchMessageService : ISwitchMessageService
    {
        private readonly IPlayniteAPI api;

        public PlayniteSwitchMessageService(IPlayniteAPI api)
        {
            this.api = api;
        }

        public void ShowError(string message, string caption)
        {
            if (api?.Notifications != null)
            {
                api.Notifications.Remove("switch-smart-import-last-message");
                api.Notifications.Add("switch-smart-import-last-message", message, NotificationType.Error);
                return;
            }

            api?.Dialogs?.ShowErrorMessage(message, caption);
        }

        public void ShowInfo(string message)
        {
            if (api?.Notifications != null)
            {
                api.Notifications.Remove("switch-smart-import-last-message");
                api.Notifications.Add("switch-smart-import-last-message", message, NotificationType.Info);
            }
        }
    }

    // 基于后台线程的导入执行服务。
    public class PlayniteSwitchProgressService : ISwitchProgressService
    {
        private readonly IPlayniteAPI api;

        public PlayniteSwitchProgressService(IPlayniteAPI api)
        {
            this.api = api;
        }

        public void Run(string title, Action action, Action onCompleted, Action<Exception> onFailed)
        {
            if (action == null)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    action();
                    InvokeOnUiThread(onCompleted);
                }
                catch (Exception e)
                {
                    InvokeOnUiThread(() => onFailed?.Invoke(e));
                }
            });
        }

        private void InvokeOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
        }
    }
}
