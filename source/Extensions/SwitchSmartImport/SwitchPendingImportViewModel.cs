// 文件用途：管理待确认导入列表，并执行确认导入。
using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK.Models;

namespace SwitchSmartImport
{
    // 待确认导入列表视图模型。
    public class SwitchPendingImportViewModel
    {
        private readonly SwitchSmartImportSettings settings;
        private readonly ISwitchPendingImportStore pendingStore;
        private readonly ISwitchImportExecutor importExecutor;
        private readonly ISwitchMetadataRefreshService metadataRefreshService;
        private readonly ISwitchMessageService messageService;
        private readonly ISwitchProgressService progressService;
        private readonly List<SwitchSkippedItem> skippedItems;
        private readonly List<SwitchPlatformOption> platformOptions;

        public List<SwitchImportCandidate> Candidates { get; }

        public List<SwitchSkippedItem> SkippedItems { get; }

        public DateTime SavedAt { get; }

        public string LastErrorMessage { get; private set; }

        public bool IsImporting { get; private set; }

        public List<SwitchPlatformOption> PlatformOptions { get { return platformOptions; } }

        public event Action StateChanged;

        public SwitchPendingImportViewModel(
            SwitchSmartImportSettings settings,
            ISwitchPendingImportStore pendingStore,
            ISwitchImportExecutor importExecutor,
            ISwitchMetadataRefreshService metadataRefreshService,
            ISwitchMessageService messageService = null,
            ISwitchProgressService progressService = null)
        {
            this.settings = settings ?? throw new ArgumentNullException("settings");
            this.pendingStore = pendingStore ?? throw new ArgumentNullException("pendingStore");
            this.importExecutor = importExecutor ?? throw new ArgumentNullException("importExecutor");
            this.metadataRefreshService = metadataRefreshService ?? throw new ArgumentNullException("metadataRefreshService");
            this.messageService = messageService ?? new NullSwitchMessageService();
            this.progressService = progressService ?? new InlineSwitchProgressService();

            var snapshot = pendingStore.Load() ?? new SwitchPendingImportSnapshot();
            Candidates = snapshot.Candidates ?? new List<SwitchImportCandidate>();
            skippedItems = snapshot.SkippedItems ?? new List<SwitchSkippedItem>();
            SkippedItems = skippedItems;
            SavedAt = snapshot.SavedAt;
            platformOptions = BuildPlatformOptions(settings);
            ApplyDefaultPlatform();
        }

        // 导入当前勾选的候选项，并更新待确认缓存。
        public void ImportSelected()
        {
            if (IsImporting)
            {
                return;
            }

            var selected = Candidates.Where(a => a != null && a.Import).ToList();
            if (selected.Count == 0)
            {
                return;
            }

            IsImporting = true;
            LastErrorMessage = null;
            RaiseStateChanged();
            messageService.ShowInfo("已开始后台导入 " + selected.Count + " 个 Switch 游戏。");

            List<Game> importedGames = null;
            progressService.Run("正在导入 Switch 游戏...", () =>
            {
                importedGames = importExecutor.Import(selected, settings);
                metadataRefreshService.Refresh(importedGames, settings.MetadataSource);
            },
            () =>
            {
                Candidates.RemoveAll(a => a != null && selected.Any(b => string.Equals(b.BasePath, a.BasePath, StringComparison.OrdinalIgnoreCase)));
                pendingStore.Save(Candidates, DateTime.Now, skippedItems);
                IsImporting = false;
                messageService.ShowInfo("Switch 智能导入完成，已导入 " + selected.Count + " 个游戏。");
                RaiseStateChanged();
            },
            e =>
            {
                LastErrorMessage = e.Message;
                IsImporting = false;
                messageService.ShowError(e.Message, "Switch 智能导入");
                RaiseStateChanged();
            });
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }

        private List<SwitchPlatformOption> BuildPlatformOptions(SwitchSmartImportSettings settings)
        {
            var options = new List<SwitchPlatformOption>
            {
                new SwitchPlatformOption
                {
                    Id = Guid.Empty,
                    Name = "使用默认平台"
                }
            };

            if (settings.DefaultPlatformId != Guid.Empty)
            {
                options.Add(new SwitchPlatformOption
                {
                    Id = settings.DefaultPlatformId,
                    Name = "Nintendo Switch"
                });
            }

            return options;
        }

        private void ApplyDefaultPlatform()
        {
            foreach (var candidate in Candidates.Where(a => a != null && a.SelectedPlatformId == Guid.Empty))
            {
                candidate.SelectedPlatformId = settings.DefaultPlatformId;
            }
        }
    }

    // 待确认列表里的平台选项。
    public class SwitchPlatformOption
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
    }

    // 插件消息提示接口。
    public interface ISwitchMessageService
    {
        void ShowInfo(string message);
        void ShowError(string message, string caption);
    }

    // 默认空消息服务，方便测试和非 UI 场景。
    public class NullSwitchMessageService : ISwitchMessageService
    {
        public void ShowInfo(string message)
        {
        }

        public void ShowError(string message, string caption)
        {
        }
    }

    // 导入进度服务接口。
    public interface ISwitchProgressService
    {
        void Run(string title, Action action, Action onCompleted, Action<Exception> onFailed);
    }

    // 默认同步执行，便于测试和无 UI 场景。
    public class InlineSwitchProgressService : ISwitchProgressService
    {
        public void Run(string title, Action action, Action onCompleted, Action<Exception> onFailed)
        {
            try
            {
                action();
                onCompleted();
            }
            catch (Exception e)
            {
                onFailed(e);
            }
        }
    }
}
