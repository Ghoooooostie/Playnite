// 文件用途：保存 Switch 智能导入插件设置，并提供 Playnite 设置页模型。
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace SwitchSmartImport
{
    // 插件设置模型，后续逐步补充扫描和导入配置。
    public class SwitchSmartImportSettings : ObservableObject
    {
        private ObservableCollection<SwitchScanPathConfig> scanPaths = new ObservableCollection<SwitchScanPathConfig>();
        private bool enableScheduledScan;
        private int scheduledScanMinutes = 60;
        private bool scanOnStartup = true;
        private bool includeSubdirectories = true;
        private bool showDlcInPendingList;
        private bool recordHighestPatchVersion = true;
        private bool requireManualConfirmation = true;
        private bool preferMergedPackage = true;
        private bool importWithRelativePaths = true;
        private Guid defaultEmulatorId;
        private string defaultEmulatorProfileId;
        private Guid defaultPlatformId;
        private SwitchMetadataSource metadataSource;

        public ObservableCollection<SwitchScanPathConfig> ScanPaths
        {
            get => scanPaths;
            set => SetValue(ref scanPaths, value);
        }

        public bool EnableScheduledScan
        {
            get => enableScheduledScan;
            set => SetValue(ref enableScheduledScan, value);
        }

        public int ScheduledScanMinutes
        {
            get => scheduledScanMinutes;
            set => SetValue(ref scheduledScanMinutes, value);
        }

        public bool ScanOnStartup
        {
            get => scanOnStartup;
            set => SetValue(ref scanOnStartup, value);
        }

        public bool IncludeSubdirectories
        {
            get => includeSubdirectories;
            set => SetValue(ref includeSubdirectories, value);
        }

        public bool ShowDlcInPendingList
        {
            get => showDlcInPendingList;
            set => SetValue(ref showDlcInPendingList, value);
        }

        public bool RecordHighestPatchVersion
        {
            get => recordHighestPatchVersion;
            set => SetValue(ref recordHighestPatchVersion, value);
        }

        public bool RequireManualConfirmation
        {
            get => requireManualConfirmation;
            set => SetValue(ref requireManualConfirmation, value);
        }

        public bool PreferMergedPackage
        {
            get => preferMergedPackage;
            set => SetValue(ref preferMergedPackage, value);
        }

        public bool ImportWithRelativePaths
        {
            get => importWithRelativePaths;
            set => SetValue(ref importWithRelativePaths, value);
        }

        public Guid DefaultEmulatorId
        {
            get => defaultEmulatorId;
            set => SetValue(ref defaultEmulatorId, value);
        }

        public string DefaultEmulatorProfileId
        {
            get => defaultEmulatorProfileId;
            set => SetValue(ref defaultEmulatorProfileId, value);
        }

        public Guid DefaultPlatformId
        {
            get => defaultPlatformId;
            set => SetValue(ref defaultPlatformId, value);
        }

        public SwitchMetadataSource MetadataSource
        {
            get => metadataSource;
            set => SetValue(ref metadataSource, value);
        }
    }

    // Playnite 设置页模型，负责读写插件配置。
    public class SwitchSmartImportSettingsViewModel : ObservableObject, ISettings
    {
        private readonly SwitchSmartImportPlugin plugin;
        private readonly Action runScanAction;
        private readonly Action openPendingImportsAction;
        private readonly List<SwitchPlatformChoice> platformChoices;
        private readonly List<SwitchEmulatorChoice> emulatorChoices;
        private SwitchSmartImportSettings editingClone;
        private SwitchSmartImportSettings settings;
        private int newPathIndex = 1;

        public SwitchSmartImportSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EmulatorProfileChoices));
            }
        }

        public Array MetadataSourceOptions
        {
            get => Enum.GetValues(typeof(SwitchMetadataSource));
        }

        public Array ScanPathTypeOptions
        {
            get => Enum.GetValues(typeof(SwitchScanPathType));
        }

        public List<SwitchPlatformChoice> PlatformChoices
        {
            get => platformChoices;
        }

        public List<SwitchEmulatorChoice> EmulatorChoices
        {
            get => emulatorChoices;
        }

        public List<SwitchEmulatorProfileChoice> EmulatorProfileChoices
        {
            get
            {
                var emulator = emulatorChoices.FirstOrDefault(a => a.Id == Settings.DefaultEmulatorId);
                return emulator?.Profiles ?? new List<SwitchEmulatorProfileChoice>();
            }
        }

        public ICommand RunManualScanCommand { get; }

        public ICommand OpenPendingImportsCommand { get; }

        public ICommand AddScanPathCommand { get; }

        public SwitchSmartImportSettingsViewModel(SwitchSmartImportPlugin plugin)
        {
            this.plugin = plugin;
            runScanAction = plugin == null ? null : new Action(plugin.RunScan);
            openPendingImportsAction = plugin == null ? null : new Action(plugin.OpenPendingImportWindow);
            platformChoices = new List<SwitchPlatformChoice>();
            emulatorChoices = new List<SwitchEmulatorChoice>();
            Settings = plugin.LoadPluginSettings<SwitchSmartImportSettings>() ?? new SwitchSmartImportSettings();
            AddScanPathCommand = new RelayCommand(AddScanPath);
            RunManualScanCommand = new RelayCommand(RunManualScan);
            OpenPendingImportsCommand = new RelayCommand(OpenPendingImports);
            NormalizeSettings();
        }

        internal SwitchSmartImportSettingsViewModel(SwitchSmartImportPlugin plugin, SwitchSmartImportSettings settings)
            : this(
                plugin,
                settings,
                plugin == null ? null : new Action(plugin.RunScan),
                plugin == null ? null : new Action(plugin.OpenPendingImportWindow))
        {
        }

        internal SwitchSmartImportSettingsViewModel(
            SwitchSmartImportPlugin plugin,
            SwitchSmartImportSettings settings,
            Action runScanAction,
            Action openPendingImportsAction)
        {
            this.plugin = plugin;
            this.runScanAction = runScanAction;
            this.openPendingImportsAction = openPendingImportsAction;
            platformChoices = new List<SwitchPlatformChoice>();
            emulatorChoices = new List<SwitchEmulatorChoice>();
            Settings = settings ?? new SwitchSmartImportSettings();
            AddScanPathCommand = new RelayCommand(AddScanPath);
            RunManualScanCommand = new RelayCommand(RunManualScan);
            OpenPendingImportsCommand = new RelayCommand(OpenPendingImports);
            NormalizeSettings();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
            NormalizeSettings();
        }

        public void EndEdit()
        {
            NormalizeSettings();
            plugin?.SavePluginSettings(Settings);
            plugin?.ApplyRuntimeSettings();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            NormalizeSettings();

            if (Settings.EnableScheduledScan && Settings.ScheduledScanMinutes < 1)
            {
                errors.Add("扫描间隔必须大于 0 分钟。");
            }

            foreach (var duplicate in Settings.ScanPaths
                .Where(a => a != null && a.Enabled && !string.IsNullOrWhiteSpace(a.Path))
                .GroupBy(a => a.Path.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(a => a.Count() > 1))
            {
                errors.Add("扫描目录重复：" + duplicate.Key);
            }

            return errors.Count == 0;
        }

        // 新增一条扫描目录配置。
        public void AddScanPath()
        {
            NormalizeSettings();
            Settings.ScanPaths.Add(new SwitchScanPathConfig
            {
                Name = "扫描目录 " + GetNextScanPathNumber(),
                Enabled = true,
                Priority = Settings.ScanPaths.Count,
                TypeHint = SwitchScanPathType.Auto
            });
            UpdatePathPriorities();
            OnPropertyChanged(nameof(Settings));
        }

        // 删除指定扫描目录配置。
        public void RemoveScanPath(SwitchScanPathConfig config)
        {
            if (config == null || Settings.ScanPaths == null)
            {
                return;
            }

            Settings.ScanPaths.Remove(config);
            UpdatePathPriorities();
            OnPropertyChanged(nameof(Settings));
        }

        // 上移指定扫描目录配置。
        public void MoveScanPathUp(SwitchScanPathConfig config)
        {
            MoveScanPath(config, -1);
        }

        // 下移指定扫描目录配置。
        public void MoveScanPathDown(SwitchScanPathConfig config)
        {
            MoveScanPath(config, 1);
        }

        // 选择扫描目录路径。
        public void SelectScanPath(SwitchScanPathConfig config)
        {
            if (plugin == null || config == null)
            {
                return;
            }

            var initial = string.IsNullOrWhiteSpace(config.Path) ? null : config.Path;
            var selected = plugin.SelectFolder(initial);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                config.Path = selected;
            }
        }

        // 手动立即执行一次扫描。
        public void RunManualScan()
        {
            runScanAction?.Invoke();
        }

        // 打开待确认导入列表。
        public void OpenPendingImports()
        {
            openPendingImportsAction?.Invoke();
        }

        // 重新从 Playnite 刷新平台和模拟器选项。
        public void RefreshChoices()
        {
            platformChoices.Clear();
            platformChoices.AddRange(BuildPlatformChoices(plugin));

            emulatorChoices.Clear();
            emulatorChoices.AddRange(BuildEmulatorChoices(plugin));

            if (!platformChoices.Any(a => a.Id == Settings.DefaultPlatformId))
            {
                Settings.DefaultPlatformId = platformChoices.FirstOrDefault()?.Id ?? Guid.Empty;
            }

            if (!emulatorChoices.Any(a => a.Id == Settings.DefaultEmulatorId))
            {
                Settings.DefaultEmulatorId = emulatorChoices.FirstOrDefault()?.Id ?? Guid.Empty;
            }

            OnDefaultEmulatorChanged();
            OnPropertyChanged(nameof(PlatformChoices));
            OnPropertyChanged(nameof(EmulatorChoices));
            OnPropertyChanged(nameof(EmulatorProfileChoices));
        }

        // 切换默认模拟器后刷新配置列表，并自动修正无效配置。
        public void OnDefaultEmulatorChanged()
        {
            if (!EmulatorProfileChoices.Any(a => string.Equals(a.Id, Settings.DefaultEmulatorProfileId, StringComparison.Ordinal)))
            {
                Settings.DefaultEmulatorProfileId = EmulatorProfileChoices.FirstOrDefault()?.Id;
            }

            OnPropertyChanged(nameof(EmulatorProfileChoices));
        }

        // 规范化设置默认值。
        private void NormalizeSettings()
        {
            if (Settings == null)
            {
                Settings = new SwitchSmartImportSettings();
            }

            if (Settings.ScanPaths == null)
            {
                Settings.ScanPaths = new ObservableCollection<SwitchScanPathConfig>();
            }

            UpdatePathPriorities();
            newPathIndex = Math.Max(newPathIndex, GetNextScanPathNumber());
        }

        // 计算新增目录时应使用的最小可用编号。
        private int GetNextScanPathNumber()
        {
            var usedNumbers = new HashSet<int>();
            foreach (var path in Settings.ScanPaths ?? new ObservableCollection<SwitchScanPathConfig>())
            {
                var match = Regex.Match(path?.Name ?? string.Empty, @"^扫描目录\s+(\d+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var number) && number > 0)
                {
                    usedNumbers.Add(number);
                }
            }

            var candidate = 1;
            while (usedNumbers.Contains(candidate))
            {
                candidate++;
            }

            newPathIndex = candidate + 1;
            return candidate;
        }

        // 调整扫描目录顺序。
        private void MoveScanPath(SwitchScanPathConfig config, int offset)
        {
            if (config == null || Settings.ScanPaths == null)
            {
                return;
            }

            var index = Settings.ScanPaths.IndexOf(config);
            if (index < 0)
            {
                return;
            }

            var targetIndex = index + offset;
            if (targetIndex < 0 || targetIndex >= Settings.ScanPaths.Count)
            {
                return;
            }

            Settings.ScanPaths.RemoveAt(index);
            Settings.ScanPaths.Insert(targetIndex, config);
            UpdatePathPriorities();
            OnPropertyChanged(nameof(Settings));
        }

        // 按当前顺序回写优先级。
        private void UpdatePathPriorities()
        {
            if (Settings?.ScanPaths == null)
            {
                return;
            }

            for (var i = 0; i < Settings.ScanPaths.Count; i++)
            {
                if (Settings.ScanPaths[i] != null)
                {
                    Settings.ScanPaths[i].Priority = i;
                }
            }
        }

        private static List<SwitchPlatformChoice> BuildPlatformChoices(SwitchSmartImportPlugin plugin)
        {
            if (plugin?.PlayniteApi?.Database?.Platforms == null)
            {
                return new List<SwitchPlatformChoice>();
            }

            return plugin.PlayniteApi.Database.Platforms
                .OrderBy(a => a.Name)
                .Select(a => new SwitchPlatformChoice
                {
                    Id = a.Id,
                    Name = a.Name
                })
                .ToList();
        }

        private static List<SwitchEmulatorChoice> BuildEmulatorChoices(SwitchSmartImportPlugin plugin)
        {
            if (plugin?.PlayniteApi?.Database?.Emulators == null)
            {
                return new List<SwitchEmulatorChoice>();
            }

            return plugin.PlayniteApi.Database.Emulators
                .OrderBy(a => a.Name)
                .Select(a => new SwitchEmulatorChoice
                {
                    Id = a.Id,
                    Name = a.Name,
                    Profiles = a.AllProfiles
                        .OrderBy(b => b.Name)
                        .Select(b => new SwitchEmulatorProfileChoice
                        {
                            Id = b.Id,
                            Name = b.Name
                        })
                        .ToList()
                })
                .ToList();
        }
    }

    // 设置页平台选项。
    public class SwitchPlatformChoice
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
    }

    // 设置页模拟器选项。
    public class SwitchEmulatorChoice
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public List<SwitchEmulatorProfileChoice> Profiles { get; set; } = new List<SwitchEmulatorProfileChoice>();
    }

    // 设置页模拟器配置选项。
    public class SwitchEmulatorProfileChoice
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }
}
