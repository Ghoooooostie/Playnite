// 文件用途：保存模拟器启动遮罩的全局设置，并提供 Playnite 设置页数据模型。
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace EmulatorQuickLaunchHide
{
    // 保存遮罩保持秒数。
    public class EmulatorQuickLaunchHideSettings : ObservableObject
    {
        private int overlayHoldSeconds = 4;
        private bool exitHotkeyEnabled = true;
        private string exitHotkeyKey = "F5";
        private string exitHotkeyModifiers = "";

        public int OverlayHoldSeconds
        {
            get => overlayHoldSeconds;
            set => SetValue(ref overlayHoldSeconds, value);
        }

        public bool ExitHotkeyEnabled
        {
            get => exitHotkeyEnabled;
            set => SetValue(ref exitHotkeyEnabled, value);
        }

        public string ExitHotkeyKey
        {
            get => exitHotkeyKey;
            set => SetValue(ref exitHotkeyKey, value);
        }

        public string ExitHotkeyModifiers
        {
            get => exitHotkeyModifiers;
            set => SetValue(ref exitHotkeyModifiers, value);
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

        public EmulatorExitHotkey ExitHotkey
        {
            get => CreateExitHotkey();
            set
            {
                if (value == null || value.Key == Key.None)
                {
                    return;
                }

                Settings.ExitHotkeyKey = value.Key.ToString();
                Settings.ExitHotkeyModifiers = FormatModifiers(value.Modifiers);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExitHotkeyText));
            }
        }

        public string ExitHotkeyText
        {
            get => FormatHotkey(CreateExitHotkey());
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
            plugin?.ReloadExitHotkey();
        }

        public bool VerifySettings(out List<string> errors)
        {
            NormalizeSettings();
            errors = new List<string>();
            return true;
        }

        // 创建当前设置对应的结束快捷键。
        internal EmulatorExitHotkey CreateExitHotkey()
        {
            NormalizeSettings();
            return new EmulatorExitHotkey(ParseKey(Settings.ExitHotkeyKey), ParseModifiers(Settings.ExitHotkeyModifiers));
        }

        // 限制为全局等待秒数和可注册快捷键。
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

            if (ParseKey(Settings.ExitHotkeyKey) == Key.None)
            {
                Settings.ExitHotkeyKey = "F5";
            }

            if (!IsValidModifiersText(Settings.ExitHotkeyModifiers))
            {
                Settings.ExitHotkeyModifiers = "";
            }
        }

        // 解析按键名。
        private static Key ParseKey(string keyText)
        {
            if (string.IsNullOrWhiteSpace(keyText))
            {
                return Key.None;
            }

            try
            {
                return (Key)Enum.Parse(typeof(Key), keyText.Trim(), true);
            }
            catch
            {
                return Key.None;
            }
        }

        // 解析修饰键文本。
        private static ModifierKeys ParseModifiers(string modifiersText)
        {
            if (string.IsNullOrWhiteSpace(modifiersText))
            {
                return ModifierKeys.None;
            }

            var result = ModifierKeys.None;
            var parts = modifiersText.Split(new[] { ',', '+', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var modifierText = part.Trim();
                if (string.Equals(modifierText, "Ctrl", StringComparison.OrdinalIgnoreCase))
                {
                    modifierText = "Control";
                }

                try
                {
                    result |= (ModifierKeys)Enum.Parse(typeof(ModifierKeys), modifierText, true);
                }
                catch
                {
                    return ModifierKeys.None;
                }
            }

            return result;
        }

        // 判断修饰键文本是否能被解析，空文本表示单键快捷键。
        private static bool IsValidModifiersText(string modifiersText)
        {
            if (string.IsNullOrWhiteSpace(modifiersText))
            {
                return true;
            }

            return ParseModifiers(modifiersText) != ModifierKeys.None;
        }

        // 格式化快捷键，用于设置页显示。
        private static string FormatHotkey(EmulatorExitHotkey hotkey)
        {
            if (hotkey == null || hotkey.Key == Key.None)
            {
                return string.Empty;
            }

            var modifiers = FormatModifiers(hotkey.Modifiers);
            if (string.IsNullOrWhiteSpace(modifiers))
            {
                return hotkey.Key.ToString();
            }

            return modifiers.Replace(", ", " + ") + " + " + hotkey.Key;
        }

        // 格式化修饰键，用于保存设置。
        private static string FormatModifiers(ModifierKeys modifiers)
        {
            var parts = new List<string>();
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                parts.Add("Control");
            }

            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                parts.Add("Alt");
            }

            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                parts.Add("Shift");
            }

            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                parts.Add("Windows");
            }

            return string.Join(", ", parts);
        }
    }
}
