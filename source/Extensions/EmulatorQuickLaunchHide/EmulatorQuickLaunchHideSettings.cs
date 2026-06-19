// 文件用途：保存模拟器启动遮罩的全局设置，并提供 Playnite 设置页数据模型。
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace EmulatorQuickLaunchHide
{
    // 保存遮罩保持秒数。
    public class EmulatorQuickLaunchHideSettings : ObservableObject
    {
        private int overlayHoldSeconds = 4;

        public int OverlayHoldSeconds
        {
            get => overlayHoldSeconds;
            set => SetValue(ref overlayHoldSeconds, value);
        }
    }

    // Playnite 设置页模型，负责读写插件配置。
    public class EmulatorQuickLaunchHideSettingsViewModel : ObservableObject, ISettings
    {
        private readonly EmulatorQuickLaunchHidePlugin plugin;
        private EmulatorQuickLaunchHideSettings editingClone;
        private EmulatorQuickLaunchHideSettings settings;

        public EmulatorQuickLaunchHideSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public EmulatorQuickLaunchHideSettingsViewModel(EmulatorQuickLaunchHidePlugin plugin)
            : this(plugin, plugin?.LoadPluginSettings<EmulatorQuickLaunchHideSettings>() ?? new EmulatorQuickLaunchHideSettings())
        {
        }

        internal EmulatorQuickLaunchHideSettingsViewModel(EmulatorQuickLaunchHidePlugin plugin, EmulatorQuickLaunchHideSettings settings)
        {
            this.plugin = plugin;
            Settings = settings ?? new EmulatorQuickLaunchHideSettings();
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
        }

        public bool VerifySettings(out List<string> errors)
        {
            NormalizeSettings();
            errors = new List<string>();
            return true;
        }

        // 限制为全局等待秒数，避免无意义的超长遮罩。
        private void NormalizeSettings()
        {
            if (Settings == null)
            {
                Settings = new EmulatorQuickLaunchHideSettings();
            }

            if (Settings.OverlayHoldSeconds < 0)
            {
                Settings.OverlayHoldSeconds = 0;
            }

            if (Settings.OverlayHoldSeconds > 60)
            {
                Settings.OverlayHoldSeconds = 60;
            }
        }
    }
}
